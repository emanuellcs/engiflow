"use client";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CancelIcon from "@mui/icons-material/Cancel";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import HistoryIcon from "@mui/icons-material/History";
import SendIcon from "@mui/icons-material/Send";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import Grid from "@mui/material/Grid";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Paper from "@mui/material/Paper";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import Step from "@mui/material/Step";
import StepContent from "@mui/material/StepContent";
import StepLabel from "@mui/material/StepLabel";
import Stepper from "@mui/material/Stepper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useParams } from "next/navigation";
import { type ReactNode, useEffect, useMemo, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import PageHeader from "@/components/ui/PageHeader";
import PriorityChip from "@/components/ui/PriorityChip";
import StatusChip from "@/components/ui/StatusChip";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import type {
  EcoDetailsDto,
  EcoEventDto,
  EcoEventType,
  EcoStatus,
  RejectEcoRequest,
} from "@/lib/types/eco";

type WorkflowAction = "submit" | "approve" | "reject";

type LifecycleStep = {
  label: string;
  status: EcoStatus;
  event: EcoEventDto | null;
  isError?: boolean;
};

type EcoPageActionsProps = {
  eco: EcoDetailsDto | null;
  role: string | undefined;
  pendingAction: WorkflowAction | null;
  onSubmit: () => void;
  onApprove: () => void;
  onReject: () => void;
};

type DetailItemProps = {
  label: string;
  children: ReactNode;
};

const requesterRoles = ["Requester", "Administrator"] as const;
const approverRoles = ["Approver", "Administrator"] as const;
const minimumRejectReasonLength = 10;
const maximumRejectReasonLength = 500;
const defaultLoadError =
  "Unable to load the Engineering Change Order. Refresh the page or return to the list.";
const transitionErrorByAction: Record<WorkflowAction, string> = {
  submit: "Unable to submit this ECO for review. Refresh the page and try again.",
  approve: "Unable to approve this ECO. Refresh the page and try again.",
  reject: "Unable to reject this ECO. Refresh the page and try again.",
};
const eventLabelByType: Record<EcoEventType, string> = {
  Approved: "Approved",
  Created: "Created",
  DetailsUpdated: "Details Updated",
  Implemented: "Implemented",
  Rejected: "Rejected",
  SubmittedForReview: "Submitted for Review",
};

/**
 * Renders the authenticated ECO detail and workflow route.
 *
 * @returns The ECO detail page with role-aware workflow actions.
 */
export default function EcoDetailsPage() {
  const params = useParams();
  const ecoId = readRouteEcoId(params?.id);
  const { user } = useAuth();
  const theme = useTheme();
  const isSmallScreen = useMediaQuery(theme.breakpoints.down("sm"), {
    noSsr: true,
  });
  const [eco, setEco] = useState<EcoDetailsDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadErrorMessage, setLoadErrorMessage] = useState<string | null>(null);
  const [actionErrorMessage, setActionErrorMessage] = useState<string | null>(
    null,
  );
  const [pendingAction, setPendingAction] = useState<WorkflowAction | null>(null);
  const [isRejectDialogOpen, setIsRejectDialogOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectDialogError, setRejectDialogError] = useState<string | null>(null);
  const lifecycleSteps = useMemo(
    () => (eco ? getLifecycleSteps(eco) : []),
    [eco],
  );
  const routeLoadErrorMessage = ecoId
    ? loadErrorMessage
    : "The ECO identifier in the route is invalid.";
  const isPageLoading = Boolean(ecoId) && isLoading;

  useEffect(() => {
    if (!ecoId) {
      return;
    }

    const requestEcoId = ecoId;
    const controller = new AbortController();

    /**
     * Loads the tenant-scoped ECO details for this route.
     *
     * @returns A promise that resolves after the ECO load attempt completes.
     */
    async function loadEcoDetails(): Promise<void> {
      setIsLoading(true);
      setLoadErrorMessage(null);
      setActionErrorMessage(null);

      try {
        const details = await apiFetch<EcoDetailsDto>(
          `/api/ecos/${encodeURIComponent(requestEcoId)}`,
          { signal: controller.signal },
        );

        setEco(details);
      } catch (error) {
        if (isAbortError(error)) {
          return;
        }

        setEco(null);
        setLoadErrorMessage(getLoadEcoErrorMessage(error));
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadEcoDetails();

    return () => {
      controller.abort();
    };
  }, [ecoId]);

  /**
   * Submits a draft ECO for review.
   *
   * @returns Nothing.
   */
  function handleSubmitForReview(): void {
    void runWorkflowTransition("submit");
  }

  /**
   * Approves an ECO currently under review.
   *
   * @returns Nothing.
   */
  function handleApprove(): void {
    void runWorkflowTransition("approve");
  }

  /**
   * Opens the rejection dialog and resets prior rejection input state.
   *
   * @returns Nothing.
   */
  function handleRejectOpen(): void {
    setRejectReason("");
    setRejectDialogError(null);
    setIsRejectDialogOpen(true);
  }

  /**
   * Closes the rejection dialog unless a reject call is in progress.
   *
   * @returns Nothing.
   */
  function handleRejectClose(): void {
    if (pendingAction === "reject") {
      return;
    }

    setIsRejectDialogOpen(false);
    setRejectDialogError(null);
  }

  /**
   * Confirms rejection with the required reason and updates local ECO details.
   *
   * @returns A promise that resolves after the rejection attempt completes.
   */
  async function handleRejectConfirm(): Promise<void> {
    const normalizedReason = rejectReason.trim();

    if (!eco || normalizedReason.length < minimumRejectReasonLength) {
      return;
    }

    setPendingAction("reject");
    setRejectDialogError(null);
    setActionErrorMessage(null);

    try {
      const request: RejectEcoRequest = { reason: normalizedReason };
      const updated = await apiFetch<EcoDetailsDto>(
        `/api/ecos/${encodeURIComponent(eco.id)}/reject`,
        {
          method: "PUT",
          body: request,
        },
      );

      setEco(updated);
      setIsRejectDialogOpen(false);
      setRejectReason("");
    } catch (error) {
      setRejectDialogError(getWorkflowErrorMessage(error, "reject"));
    } finally {
      setPendingAction(null);
    }
  }

  /**
   * Calls one non-rejection ECO workflow endpoint and applies the updated DTO.
   *
   * @param action - Workflow transition to execute.
   * @returns A promise that resolves after the transition attempt completes.
   */
  async function runWorkflowTransition(action: Exclude<WorkflowAction, "reject">) {
    if (!eco || pendingAction) {
      return;
    }

    setPendingAction(action);
    setActionErrorMessage(null);

    try {
      const updated = await apiFetch<EcoDetailsDto>(
        `/api/ecos/${encodeURIComponent(eco.id)}/${action}`,
        { method: "PUT" },
      );

      setEco(updated);
    } catch (error) {
      setActionErrorMessage(getWorkflowErrorMessage(error, action));
    } finally {
      setPendingAction(null);
    }
  }

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title={eco?.title ?? "Engineering Change Order"}
        description={
          eco ? `ECO ${formatShortId(eco.id)}` : "Review ECO detail and workflow state."
        }
        actionButton={
          <EcoPageActions
            eco={eco}
            role={user?.role}
            pendingAction={pendingAction}
            onSubmit={handleSubmitForReview}
            onApprove={handleApprove}
            onReject={handleRejectOpen}
          />
        }
      />

      {actionErrorMessage ? (
        <Alert severity="error" variant="outlined">
          {actionErrorMessage}
        </Alert>
      ) : null}

      {isPageLoading ? <EcoDetailsSkeleton /> : null}

      {!isPageLoading && routeLoadErrorMessage ? (
        <Paper
          component="section"
          elevation={1}
          sx={{
            p: { xs: 2, sm: 3 },
          }}
        >
          <Stack spacing={2}>
            <Alert severity="error" variant="outlined">
              {routeLoadErrorMessage}
            </Alert>
            <Box>
              <Button
                component={NextLink}
                href="/ecos"
                variant="outlined"
                startIcon={<ArrowBackIcon />}
                sx={{ textTransform: "none" }}
              >
                Back to ECOs
              </Button>
            </Box>
          </Stack>
        </Paper>
      ) : null}

      {!isPageLoading && !routeLoadErrorMessage && eco ? (
        <>
          <Grid container spacing={2.5}>
            <Grid size={{ xs: 12, lg: 8 }}>
              <Card elevation={1}>
                <CardContent>
                  <Stack spacing={2}>
                    <Stack
                      direction={{ xs: "column", sm: "row" }}
                      spacing={1}
                      sx={{
                        alignItems: { xs: "flex-start", sm: "center" },
                        justifyContent: "space-between",
                      }}
                    >
                      <Typography variant="h6" component="h2">
                        Change Request
                      </Typography>
                      <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
                        <StatusChip status={eco.status} />
                        <PriorityChip priority={eco.priority} />
                      </Stack>
                    </Stack>
                    <Divider />
                    <Typography
                      variant="body2"
                      color="text.primary"
                      sx={{ whiteSpace: "pre-wrap" }}
                    >
                      {eco.description}
                    </Typography>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
            <Grid size={{ xs: 12, lg: 4 }}>
              <Card elevation={1}>
                <CardContent>
                  <Stack spacing={2}>
                    <Typography variant="h6" component="h2">
                      Record Details
                    </Typography>
                    <Divider />
                    <Grid container spacing={1.5}>
                      <Grid size={12}>
                        <DetailItem label="Full ID">
                          <Typography
                            variant="body2"
                            sx={{
                              fontFamily: "monospace",
                              overflowWrap: "anywhere",
                            }}
                          >
                            {eco.id}
                          </Typography>
                        </DetailItem>
                      </Grid>
                      <Grid size={{ xs: 12, sm: 6, lg: 12 }}>
                        <DetailItem label="Creator">
                          <Typography
                            variant="body2"
                            sx={{
                              fontFamily: "monospace",
                              overflowWrap: "anywhere",
                            }}
                          >
                            {eco.createdByUserId}
                          </Typography>
                        </DetailItem>
                      </Grid>
                      <Grid size={{ xs: 12, sm: 6, lg: 12 }}>
                        <DetailItem label="Created">
                          {formatDateTime(eco.createdAt)}
                        </DetailItem>
                      </Grid>
                      <Grid size={{ xs: 12, sm: 6, lg: 12 }}>
                        <DetailItem label="Last Updated">
                          {formatDateTime(eco.updatedAt)}
                        </DetailItem>
                      </Grid>
                    </Grid>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          </Grid>

          <Card elevation={1}>
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6" component="h2">
                  Lifecycle
                </Typography>
                <Divider />
                <Stepper
                  activeStep={getLifecycleActiveStep(eco)}
                  alternativeLabel={!isSmallScreen}
                  orientation={isSmallScreen ? "vertical" : "horizontal"}
                >
                  {lifecycleSteps.map((step, index) => (
                    <Step
                      key={step.label}
                      completed={index < getLifecycleActiveStep(eco)}
                    >
                      <StepLabel
                        error={Boolean(step.isError)}
                        optional={
                          !isSmallScreen && step.event ? (
                            <Typography variant="caption" color="text.secondary">
                              {formatDateTime(step.event.occurredAt)}
                            </Typography>
                          ) : null
                        }
                      >
                        {step.label}
                      </StepLabel>
                      {isSmallScreen ? (
                        <StepContent>
                          <Typography variant="caption" color="text.secondary">
                            {step.event
                              ? formatDateTime(step.event.occurredAt)
                              : "Pending"}
                          </Typography>
                        </StepContent>
                      ) : null}
                    </Step>
                  ))}
                </Stepper>
                <Divider />
                <AuditEventList events={eco.events} />
              </Stack>
            </CardContent>
          </Card>
        </>
      ) : null}

      <RejectEcoDialog
        open={isRejectDialogOpen}
        reason={rejectReason}
        errorMessage={rejectDialogError}
        isPending={pendingAction === "reject"}
        onReasonChange={setRejectReason}
        onClose={handleRejectClose}
        onConfirm={handleRejectConfirm}
      />
    </Stack>
  );
}

/**
 * Renders the page header action cluster for ECO navigation and allowed workflow actions.
 *
 * @param props - ECO page action rendering options.
 * @returns A responsive group of navigation and workflow buttons.
 */
function EcoPageActions({
  eco,
  role,
  pendingAction,
  onSubmit,
  onApprove,
  onReject,
}: EcoPageActionsProps) {
  const canSubmit =
    eco?.status === "Draft" && roleAllows(role, requesterRoles) && !pendingAction;
  const canApprove =
    eco?.status === "UnderReview" && roleAllows(role, approverRoles) && !pendingAction;

  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      sx={{ width: { xs: "100%", sm: "auto" } }}
    >
      <Button
        component={NextLink}
        href="/ecos"
        type="button"
        variant="outlined"
        startIcon={<ArrowBackIcon />}
        sx={{ textTransform: "none" }}
      >
        Back
      </Button>
      {canSubmit || pendingAction === "submit" ? (
        <Button
          type="button"
          variant="contained"
          disabled={Boolean(pendingAction)}
          startIcon={pendingAction === "submit" ? undefined : <SendIcon />}
          onClick={onSubmit}
          sx={{ minWidth: 164, textTransform: "none" }}
        >
          {pendingAction === "submit" ? (
            <CircularProgress color="inherit" size={20} thickness={5} />
          ) : (
            "Submit for Review"
          )}
        </Button>
      ) : null}
      {canApprove || pendingAction === "approve" ? (
        <Button
          type="button"
          variant="contained"
          color="success"
          disabled={Boolean(pendingAction)}
          startIcon={pendingAction === "approve" ? undefined : <CheckCircleIcon />}
          onClick={onApprove}
          sx={{ minWidth: 112, textTransform: "none" }}
        >
          {pendingAction === "approve" ? (
            <CircularProgress color="inherit" size={20} thickness={5} />
          ) : (
            "Approve"
          )}
        </Button>
      ) : null}
      {canApprove || pendingAction === "reject" ? (
        <Button
          type="button"
          variant="outlined"
          color="error"
          disabled={Boolean(pendingAction)}
          startIcon={pendingAction === "reject" ? undefined : <CancelIcon />}
          onClick={onReject}
          sx={{ minWidth: 104, textTransform: "none" }}
        >
          {pendingAction === "reject" ? (
            <CircularProgress color="inherit" size={20} thickness={5} />
          ) : (
            "Reject"
          )}
        </Button>
      ) : null}
    </Stack>
  );
}

/**
 * Renders a compact labeled value used in the ECO detail metadata card.
 *
 * @param props - Detail item rendering options.
 * @returns A label/value pair.
 */
function DetailItem({ label, children }: DetailItemProps) {
  return (
    <Stack spacing={0.25}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Box sx={{ minWidth: 0 }}>{children}</Box>
    </Stack>
  );
}

/**
 * Renders loading placeholders for the ECO detail cards.
 *
 * @returns Skeleton content matching the final ECO detail layout.
 */
function EcoDetailsSkeleton() {
  return (
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, lg: 8 }}>
        <Paper elevation={1} sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2}>
            <Skeleton variant="text" width="38%" height={32} />
            <Skeleton variant="rounded" height={24} width={220} />
            <Skeleton variant="text" width="96%" />
            <Skeleton variant="text" width="88%" />
            <Skeleton variant="text" width="72%" />
          </Stack>
        </Paper>
      </Grid>
      <Grid size={{ xs: 12, lg: 4 }}>
        <Paper elevation={1} sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={1.5}>
            <Skeleton variant="text" width="50%" height={32} />
            <Skeleton variant="text" width="80%" />
            <Skeleton variant="text" width="72%" />
            <Skeleton variant="text" width="64%" />
          </Stack>
        </Paper>
      </Grid>
      <Grid size={12}>
        <Paper elevation={1} sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2}>
            <Skeleton variant="text" width="24%" height={32} />
            <Skeleton variant="rounded" height={64} />
            <Skeleton variant="rounded" height={120} />
          </Stack>
        </Paper>
      </Grid>
    </Grid>
  );
}

type RejectEcoDialogProps = {
  open: boolean;
  reason: string;
  errorMessage: string | null;
  isPending: boolean;
  onReasonChange: (reason: string) => void;
  onClose: () => void;
  onConfirm: () => void;
};

/**
 * Renders the confirmation dialog used to reject an ECO under review.
 *
 * @param props - Dialog rendering and event options.
 * @returns The ECO rejection dialog.
 */
function RejectEcoDialog({
  open,
  reason,
  errorMessage,
  isPending,
  onReasonChange,
  onClose,
  onConfirm,
}: RejectEcoDialogProps) {
  const normalizedReasonLength = reason.trim().length;
  const isReasonTooShort =
    normalizedReasonLength > 0 &&
    normalizedReasonLength < minimumRejectReasonLength;
  const isConfirmDisabled =
    isPending || normalizedReasonLength < minimumRejectReasonLength;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Reject Engineering Change Order</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          <DialogContentText>
            Provide a rejection reason for the audit trail. Rejection is terminal
            in the current ECO workflow.
          </DialogContentText>
          {errorMessage ? (
            <Alert severity="error" variant="outlined">
              {errorMessage}
            </Alert>
          ) : null}
          <TextField
            id="rejectReason"
            name="rejectReason"
            label="Reason"
            value={reason}
            onChange={(event) => onReasonChange(event.target.value)}
            disabled={isPending}
            error={isReasonTooShort}
            helperText={
              isReasonTooShort
                ? `Enter at least ${minimumRejectReasonLength} characters.`
                : `${reason.length}/${maximumRejectReasonLength} characters`
            }
            multiline
            minRows={4}
            slotProps={{
              htmlInput: {
                maxLength: maximumRejectReasonLength,
              },
            }}
            required
            fullWidth
            size="small"
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button
          type="button"
          variant="outlined"
          disabled={isPending}
          onClick={onClose}
          sx={{ textTransform: "none" }}
        >
          Cancel
        </Button>
        <Button
          type="button"
          variant="contained"
          color="error"
          disabled={isConfirmDisabled}
          onClick={onConfirm}
          sx={{ minWidth: 112, textTransform: "none" }}
        >
          {isPending ? (
            <CircularProgress color="inherit" size={20} thickness={5} />
          ) : (
            "Reject ECO"
          )}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * Renders a dense chronological list of ECO audit events.
 *
 * @param props - Audit event rendering options.
 * @returns The audit event list or an empty-state message.
 */
function AuditEventList({ events }: { events: EcoEventDto[] }) {
  if (events.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        No audit events have been recorded for this ECO.
      </Typography>
    );
  }

  return (
    <List dense disablePadding aria-label="ECO audit trail">
      {events.map((event, index) => (
        <Box key={event.id}>
          <ListItem alignItems="flex-start" disableGutters>
            <ListItemIcon sx={{ minWidth: 36, color: "text.secondary", pt: 0.5 }}>
              <HistoryIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText
              primary={
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  spacing={{ xs: 0.25, sm: 1 }}
                  sx={{
                    justifyContent: "space-between",
                    minWidth: 0,
                  }}
                >
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>
                    {eventLabelByType[event.eventType]}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatDateTime(event.occurredAt)}
                  </Typography>
                </Stack>
              }
              secondary={
                <Stack spacing={0.25} sx={{ mt: 0.5 }}>
                  <Typography
                    component="span"
                    variant="body2"
                    color="text.secondary"
                  >
                    {event.description}
                  </Typography>
                  <Typography
                    component="span"
                    variant="caption"
                    color="text.secondary"
                    sx={{ fontFamily: "monospace", overflowWrap: "anywhere" }}
                  >
                    Actor {event.actorUserId}
                    {formatStatusTransition(event)}
                  </Typography>
                </Stack>
              }
            />
          </ListItem>
          {index < events.length - 1 ? <Divider component="li" /> : null}
        </Box>
      ))}
    </List>
  );
}

/**
 * Reads the route ECO identifier from Next.js client route params.
 *
 * @param value - Route parameter value supplied by useParams.
 * @returns The ECO route identifier when available, otherwise null.
 */
function readRouteEcoId(value: string | string[] | undefined): string | null {
  if (Array.isArray(value)) {
    return value[0]?.trim() || null;
  }

  return value?.trim() || null;
}

/**
 * Determines whether a user role is in an allowed role set.
 *
 * @param role - Current authenticated user role.
 * @param allowedRoles - Roles allowed to perform an action.
 * @returns True when the supplied role is allowed.
 */
function roleAllows(
  role: string | undefined,
  allowedRoles: readonly string[],
): boolean {
  const normalizedRole = role?.trim().toLowerCase();

  return Boolean(
    normalizedRole &&
      allowedRoles.some((allowedRole) => allowedRole.toLowerCase() === normalizedRole),
  );
}

/**
 * Builds lifecycle steps from current ECO status and audit events.
 *
 * @param eco - Detailed ECO response.
 * @returns Ordered lifecycle steps for the read-only stepper.
 */
function getLifecycleSteps(eco: EcoDetailsDto): LifecycleStep[] {
  const createdEvent = findFirstEvent(eco.events, "Created");
  const submittedEvent =
    findFirstEvent(eco.events, "SubmittedForReview") ??
    findFirstStatusEvent(eco.events, "UnderReview");
  const rejectedEvent =
    findFirstEvent(eco.events, "Rejected") ??
    findFirstStatusEvent(eco.events, "Rejected");
  const approvedEvent =
    findFirstEvent(eco.events, "Approved") ??
    findFirstStatusEvent(eco.events, "Approved");
  const implementedEvent =
    findFirstEvent(eco.events, "Implemented") ??
    findFirstStatusEvent(eco.events, "Implemented");
  const decisionStatus: EcoStatus =
    eco.status === "Rejected" ? "Rejected" : "Approved";
  const decisionEvent = decisionStatus === "Rejected" ? rejectedEvent : approvedEvent;
  const steps: LifecycleStep[] = [
    {
      label: "Draft",
      status: "Draft",
      event: createdEvent,
    },
    {
      label: "Under Review",
      status: "UnderReview",
      event: submittedEvent,
    },
    {
      label: decisionStatus,
      status: decisionStatus,
      event: decisionEvent,
      isError: decisionStatus === "Rejected",
    },
  ];

  if (eco.status === "Implemented" || implementedEvent) {
    steps.push({
      label: "Implemented",
      status: "Implemented",
      event: implementedEvent,
    });
  }

  return steps;
}

/**
 * Maps the ECO status to the active lifecycle step index.
 *
 * @param eco - Detailed ECO response.
 * @returns Zero-based active step index.
 */
function getLifecycleActiveStep(eco: EcoDetailsDto): number {
  if (eco.status === "Draft") {
    return 0;
  }

  if (eco.status === "UnderReview") {
    return 1;
  }

  if (eco.status === "Implemented") {
    return 3;
  }

  return 2;
}

/**
 * Finds the first audit event matching an event type.
 *
 * @param events - ECO audit events.
 * @param eventType - Event type to locate.
 * @returns The first matching event, otherwise null.
 */
function findFirstEvent(
  events: EcoEventDto[],
  eventType: EcoEventType,
): EcoEventDto | null {
  return events.find((event) => event.eventType === eventType) ?? null;
}

/**
 * Finds the first audit event that transitioned the ECO into a status.
 *
 * @param events - ECO audit events.
 * @param status - Status to locate in the newStatus field.
 * @returns The first matching transition event, otherwise null.
 */
function findFirstStatusEvent(
  events: EcoEventDto[],
  status: EcoStatus,
): EcoEventDto | null {
  return events.find((event) => event.newStatus === status) ?? null;
}

/**
 * Formats an ECO identifier into the compact token used in headings and links.
 *
 * @param id - Full ECO identifier.
 * @returns The first eight uppercase characters when the identifier is long.
 */
function formatShortId(id: string): string {
  return id.length > 8 ? id.slice(0, 8).toUpperCase() : id.toUpperCase();
}

/**
 * Formats an API timestamp as a compact local date/time string.
 *
 * @param value - ISO timestamp returned by the API.
 * @returns A formatted date/time or a dash when parsing fails.
 */
function formatDateTime(value: string): string {
  const timestamp = Date.parse(value);

  if (Number.isNaN(timestamp)) {
    return "-";
  }

  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(timestamp);
}

/**
 * Formats old/new status data for an audit event.
 *
 * @param event - ECO audit event.
 * @returns A compact transition suffix when status data exists.
 */
function formatStatusTransition(event: EcoEventDto): string {
  if (event.oldStatus && event.newStatus) {
    return ` | ${formatStatusLabel(event.oldStatus)} to ${formatStatusLabel(
      event.newStatus,
    )}`;
  }

  if (event.newStatus) {
    return ` | ${formatStatusLabel(event.newStatus)}`;
  }

  return "";
}

/**
 * Converts an enum-style status token into readable text.
 *
 * @param value - ECO status token.
 * @returns Status label with word boundaries inserted.
 */
function formatStatusLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

/**
 * Produces the user-facing detail load error message.
 *
 * @param error - Unknown error thrown while loading ECO details.
 * @returns A stable error message suitable for a Material UI Alert.
 */
function getLoadEcoErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 404) {
    return "The requested Engineering Change Order was not found.";
  }

  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? defaultLoadError;
  }

  return defaultLoadError;
}

/**
 * Produces the user-facing workflow transition error message.
 *
 * @param error - Unknown error thrown by a workflow endpoint.
 * @param action - Workflow action that failed.
 * @returns A stable error message suitable for a Material UI Alert.
 */
function getWorkflowErrorMessage(
  error: unknown,
  action: WorkflowAction,
): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? transitionErrorByAction[action];
  }

  return transitionErrorByAction[action];
}

/**
 * Reads the most useful RFC 7807 or validation message from an API error body.
 *
 * @param details - Unknown API error details payload.
 * @returns A user-facing message when one is available, otherwise null.
 */
function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
  }

  const validationMessage = readValidationMessage(details);

  if (validationMessage) {
    return validationMessage;
  }

  if (
    "detail" in details &&
    typeof details.detail === "string" &&
    details.detail.trim().length > 0
  ) {
    return details.detail;
  }

  if (
    "title" in details &&
    typeof details.title === "string" &&
    details.title.trim().length > 0
  ) {
    return details.title;
  }

  return null;
}

/**
 * Reads the first validation message from a ProblemDetails errors dictionary.
 *
 * @param details - ProblemDetails-like API error object.
 * @returns First validation message when present, otherwise null.
 */
function readValidationMessage(details: object): string | null {
  if (
    !("errors" in details) ||
    !details.errors ||
    typeof details.errors !== "object"
  ) {
    return null;
  }

  for (const messages of Object.values(details.errors)) {
    if (!Array.isArray(messages)) {
      continue;
    }

    const message = messages.find(
      (item): item is string => typeof item === "string" && item.length > 0,
    );

    if (message) {
      return message;
    }
  }

  return null;
}

/**
 * Detects abort errors produced by an AbortController-backed fetch request.
 *
 * @param error - Unknown error thrown by the ECO load operation.
 * @returns True when the error is a browser abort event.
 */
function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}
