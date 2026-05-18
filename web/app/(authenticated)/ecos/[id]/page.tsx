"use client";

import AddIcon from "@mui/icons-material/Add";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import CancelIcon from "@mui/icons-material/Cancel";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import CloseIcon from "@mui/icons-material/Close";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteIcon from "@mui/icons-material/Delete";
import DownloadIcon from "@mui/icons-material/Download";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutlined";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import InsertDriveFileIcon from "@mui/icons-material/InsertDriveFile";
import SendIcon from "@mui/icons-material/Send";
import TimelineIcon from "@mui/icons-material/Timeline";
import Avatar from "@mui/material/Avatar";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import FormControl from "@mui/material/FormControl";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import InputLabel from "@mui/material/InputLabel";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Skeleton from "@mui/material/Skeleton";
import Snackbar from "@mui/material/Snackbar";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { useParams } from "next/navigation";
import {
  type ChangeEvent,
  type ReactNode,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import RichTextRenderer from "@/components/ecos/RichTextRenderer";
import { useEcoHub } from "@/components/ecos/useEcoHub";
import NextLink from "@/components/ui/NextLink";
import PriorityChip from "@/components/ui/PriorityChip";
import StatusChip from "@/components/ui/StatusChip";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import type {
  AddAffectedItemRequest,
  AddCommentRequest,
  EcoAffectedItemAction,
  EcoAffectedItemDto,
  EcoApprovalDecision,
  EcoAttachmentDownloadDto,
  EcoAttachmentDto,
  EcoCommentDto,
  EcoDetailsDto,
  EcoEventDto,
  EcoEventType,
  EcoRealtimeUpdate,
  EcoReviewContextDto,
  EcoUserDto,
  SubmitReviewDecisionRequest,
} from "@/lib/types/eco";

type TabKey = "conversation" | "items" | "files";
type PendingAction =
  | "submit"
  | "approve"
  | "requestChanges"
  | "cancel"
  | "comment"
  | "upload"
  | "addItem"
  | "removeItem";
type TimelineEntry =
  | { type: "event"; occurredAt: string; event: EcoEventDto }
  | { type: "comment"; occurredAt: string; comment: EcoCommentDto };
type AffectedItemFormState = AddAffectedItemRequest;

const authorRoles = ["Owner", "Administrator", "Requester"] as const;
const reviewerRoles = ["Owner", "Administrator", "Approver"] as const;
const approverRoles = new Set<string>(reviewerRoles);
const minimumRequestChangesLength = 10;
const maximumCommentLength = 4000;
const initialAffectedItemForm: AffectedItemFormState = {
  partNumber: "",
  description: "",
  currentRevision: "",
  newRevision: "",
  action: "Modify",
};
const eventLabelByType: Record<EcoEventType, string> = {
  AffectedItemAdded: "Affected item added",
  AffectedItemRemoved: "Affected item removed",
  Approved: "Approved",
  AttachmentAdded: "Attachment added",
  Canceled: "Canceled",
  ChangesRequested: "Changes requested",
  CommentAdded: "Comment added",
  Created: "Created",
  DetailsUpdated: "Details updated",
  Implemented: "Implemented",
  Rejected: "Rejected",
  ReviewDecisionSubmitted: "Review decision submitted",
  SubmittedForReview: "Submitted for review",
};

/**
 * Renders the authenticated ECO detail route.
 *
 * @returns The PR-like ECO detail shell.
 */
export default function EcoDetailsPage() {
  const params = useParams();
  const ecoId = readRouteEcoId(params?.id);
  const { token, user } = useAuth();
  const [eco, setEco] = useState<EcoDetailsDto | null>(null);
  const [reviewContext, setReviewContext] = useState<EcoReviewContextDto | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("conversation");
  const [isLoading, setIsLoading] = useState(true);
  const [isConflictRefreshing, setIsConflictRefreshing] = useState(false);
  const [loadErrorMessage, setLoadErrorMessage] = useState<string | null>(null);
  const [actionErrorMessage, setActionErrorMessage] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null);
  const [isConflictAlertOpen, setIsConflictAlertOpen] = useState(false);
  const usersById = useMemo(
    () => createUserLookup(reviewContext?.users ?? []),
    [reviewContext],
  );
  const approvers = useMemo(
    () => (reviewContext?.users ?? []).filter((item) => approverRoles.has(item.role)),
    [reviewContext],
  );
  const minApprovalsRequired = reviewContext?.minApprovalsRequired ?? 1;
  const canAuthor = roleAllows(user?.role, authorRoles);
  const canReview = roleAllows(user?.role, reviewerRoles);

  const loadEcoDetails = useCallback(
    async (options: { showLoading?: boolean } = {}) => {
      if (!ecoId) {
        return;
      }

      if (options.showLoading !== false) {
        setIsLoading(true);
      }
      setLoadErrorMessage(null);

      try {
        const [details, context] = await Promise.all([
          apiFetch<EcoDetailsDto>(`/api/ecos/${encodeURIComponent(ecoId)}`),
          apiFetch<EcoReviewContextDto>("/api/ecos/review-context"),
        ]);

        setEco(normalizeEcoDetails(details));
        setReviewContext(context);
      } catch (error) {
        setEco(null);
        setLoadErrorMessage(getLoadEcoErrorMessage(error));
      } finally {
        if (options.showLoading !== false) {
          setIsLoading(false);
        }
      }
    },
    [ecoId],
  );

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadEcoDetails();
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [loadEcoDetails]);

  const handleEcoChanged = useCallback(
    (update: EcoRealtimeUpdate) => {
      if (!ecoId || update.ecoId.toLowerCase() !== ecoId.toLowerCase()) {
        return;
      }

      const shouldRefresh =
        update.events.some((event) =>
          ["CommentAdded", "AttachmentAdded", "AffectedItemAdded", "AffectedItemRemoved"].includes(
            event.eventType,
          ),
        );
      setEco((current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          status: update.status,
          reviewRound: update.reviewRound,
          events: update.events,
        };
      });

      if (shouldRefresh) {
        void loadEcoDetails({ showLoading: false });
      }
    },
    [ecoId, loadEcoDetails],
  );
  const hub = useEcoHub({ token, onEcoChanged: handleEcoChanged });

  /**
   * Runs an ECO workflow request and handles concurrency conflicts consistently.
   *
   * @param action - Pending action identifier.
   * @param request - Request that returns the updated ECO.
   * @returns A promise that resolves after the action attempt.
   */
  async function runEcoAction(
    action: PendingAction,
    request: () => Promise<EcoDetailsDto>,
  ): Promise<void> {
    if (pendingAction || isConflictRefreshing) {
      return;
    }

    setPendingAction(action);
    setActionErrorMessage(null);

    try {
      const updated = await request();
      setEco(normalizeEcoDetails(updated));
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        await handleConflict();
      } else {
        setActionErrorMessage(getActionErrorMessage(error));
      }
    } finally {
      setPendingAction(null);
    }
  }

  async function handleConflict(): Promise<void> {
    setIsConflictAlertOpen(true);
    setIsConflictRefreshing(true);
    try {
      await loadEcoDetails({ showLoading: false });
    } finally {
      setIsConflictRefreshing(false);
    }
  }

  if (!ecoId) {
    return (
      <Stack spacing={2.5}>
        <Alert severity="error">The ECO identifier in the route is invalid.</Alert>
        <Button
          component={NextLink}
          href="/ecos"
          variant="outlined"
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start", textTransform: "none" }}
        >
          Back to ECOs
        </Button>
      </Stack>
    );
  }

  if (isLoading) {
    return <EcoDetailsSkeleton />;
  }

  if (loadErrorMessage || !eco) {
    return (
      <Stack spacing={2.5}>
        <Alert severity="error">
          {loadErrorMessage ?? "Unable to load the Engineering Change Order."}
        </Alert>
        <Button
          component={NextLink}
          href="/ecos"
          variant="outlined"
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start", textTransform: "none" }}
        >
          Back to ECOs
        </Button>
      </Stack>
    );
  }

  const requester = usersById.get(eco.createdByUserId);

  return (
    <Stack spacing={2.5}>
      <Stack spacing={1.5}>
        <Button
          component={NextLink}
          href="/ecos"
          variant="text"
          startIcon={<ArrowBackIcon />}
          sx={{ alignSelf: "flex-start", textTransform: "none" }}
        >
          Back to ECOs
        </Button>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          sx={{
            alignItems: { xs: "flex-start", md: "center" },
            justifyContent: "space-between",
          }}
        >
          <Stack spacing={1} sx={{ minWidth: 0 }}>
            <Stack
              direction={{ xs: "column", sm: "row" }}
              spacing={1}
              sx={{ alignItems: { xs: "flex-start", sm: "center" } }}
            >
              <Typography variant="h4" component="h1" sx={{ overflowWrap: "anywhere" }}>
                {eco.title}
              </Typography>
              <StatusChip status={eco.status} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {requester?.name ?? formatShortId(eco.createdByUserId)} submitted this change
              {" • "}
              Review Round {eco.reviewRound || 0}
              {" • "}
              {formatDateTime(eco.createdAt)}
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
            <PriorityChip priority={eco.priority} />
            <Chip size="small" label={`ECO ${formatShortId(eco.id)}`} variant="outlined" />
          </Stack>
        </Stack>
      </Stack>

      <StickyActionBar
        canAuthor={canAuthor}
        canReview={canReview}
        eco={eco}
        isBlocked={isConflictRefreshing}
        pendingAction={pendingAction}
        onApprove={() =>
          runEcoAction("approve", () =>
            submitReviewDecision(eco.id, { decision: "Approve" }),
          )
        }
        onCancel={() =>
          runEcoAction("cancel", () =>
            apiFetch<EcoDetailsDto>(`/api/ecos/${encodeURIComponent(eco.id)}/cancel`, {
              method: "PUT",
            }),
          )
        }
        onRequestChanges={(comment) =>
          runEcoAction("requestChanges", () =>
            submitReviewDecision(eco.id, {
              decision: "RequestChanges",
              comment,
            }),
          )
        }
        onSubmit={() =>
          runEcoAction("submit", () =>
            apiFetch<EcoDetailsDto>(`/api/ecos/${encodeURIComponent(eco.id)}/submit`, {
              method: "PUT",
            }),
          )
        }
      />

      {actionErrorMessage ? <Alert severity="error">{actionErrorMessage}</Alert> : null}
      {hub.status === "reconnecting" || hub.status === "disconnected" ? (
        <Alert severity="warning" variant="outlined">
          Real-time ECO updates are {hub.status}. {hub.errorMessage ?? ""}
        </Alert>
      ) : null}

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Card elevation={1}>
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6" component="h2">
                  Change Summary
                </Typography>
                <Divider />
                <RichTextRenderer value={eco.description} />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <ReviewersWidget
            approvals={eco.approvals}
            approvers={approvers}
            minApprovalsRequired={minApprovalsRequired}
            reviewRound={eco.reviewRound}
            usersById={usersById}
          />
        </Grid>
      </Grid>

      <Paper elevation={1}>
        <Tabs
          value={activeTab}
          onChange={(_, value: TabKey) => setActiveTab(value)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{ borderBottom: 1, borderColor: "divider" }}
        >
          <Tab value="conversation" label="Conversation" />
          <Tab value="items" label="Affected Items" />
          <Tab value="files" label="Files & Attachments" />
        </Tabs>
        <Box sx={{ p: { xs: 1.5, md: 2.5 } }}>
          {activeTab === "conversation" ? (
            <ConversationTab
              eco={eco}
              isBlocked={Boolean(pendingAction) || isConflictRefreshing}
              onCommentSubmit={(body) =>
                runEcoAction("comment", () =>
                  apiFetch<EcoDetailsDto>(
                    `/api/ecos/${encodeURIComponent(eco.id)}/comments`,
                    {
                      method: "POST",
                      body: { body } satisfies AddCommentRequest,
                    },
                  ),
                )
              }
              onUpload={(file) =>
                runEcoAction("upload", () => uploadAttachment(eco.id, file))
              }
              pendingAction={pendingAction}
              usersById={usersById}
            />
          ) : null}
          {activeTab === "items" ? (
            <AffectedItemsTab
              canEdit={eco.status === "Draft" && canAuthor}
              eco={eco}
              isBlocked={Boolean(pendingAction) || isConflictRefreshing}
              onAddItem={(item) =>
                runEcoAction("addItem", () =>
                  apiFetch<EcoDetailsDto>(
                    `/api/ecos/${encodeURIComponent(eco.id)}/affected-items`,
                    {
                      method: "POST",
                      body: item,
                    },
                  ),
                )
              }
              onRemoveItem={(itemId) =>
                runEcoAction("removeItem", () =>
                  apiFetch<EcoDetailsDto>(
                    `/api/ecos/${encodeURIComponent(eco.id)}/affected-items/${encodeURIComponent(
                      itemId,
                    )}`,
                    { method: "DELETE" },
                  ),
                )
              }
              pendingAction={pendingAction}
            />
          ) : null}
          {activeTab === "files" ? (
            <FilesTab attachments={eco.attachments} ecoId={eco.id} usersById={usersById} />
          ) : null}
        </Box>
      </Paper>

      <Snackbar
        open={isConflictAlertOpen}
        autoHideDuration={8000}
        onClose={() => setIsConflictAlertOpen(false)}
        anchorOrigin={{ vertical: "top", horizontal: "center" }}
      >
        <Alert
          severity="warning"
          variant="filled"
          onClose={() => setIsConflictAlertOpen(false)}
          sx={{ width: "100%" }}
        >
          ECO state changed by another user. The latest data has been loaded.
        </Alert>
      </Snackbar>
    </Stack>
  );
}

type StickyActionBarProps = {
  canAuthor: boolean;
  canReview: boolean;
  eco: EcoDetailsDto;
  isBlocked: boolean;
  pendingAction: PendingAction | null;
  onApprove: () => void;
  onCancel: () => void;
  onRequestChanges: (comment: string) => void;
  onSubmit: () => void;
};

/**
 * Renders sticky workflow controls for the ECO state machine.
 *
 * @param props - Action bar options.
 * @returns Sticky action bar.
 */
function StickyActionBar({
  canAuthor,
  canReview,
  eco,
  isBlocked,
  pendingAction,
  onApprove,
  onCancel,
  onRequestChanges,
  onSubmit,
}: StickyActionBarProps) {
  const [isRequestChangesOpen, setIsRequestChangesOpen] = useState(false);
  const canSubmit = eco.status === "Draft" && canAuthor;
  const canCancel = (eco.status === "Draft" || eco.status === "UnderReview") && canAuthor;
  const canApprove = eco.status === "UnderReview" && canReview;
  const disableActions = isBlocked || Boolean(pendingAction);

  return (
    <Paper
      elevation={2}
      sx={{
        position: "sticky",
        top: 72,
        zIndex: 10,
        p: 1.5,
      }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        spacing={1}
        sx={{
          alignItems: { xs: "stretch", md: "center" },
          justifyContent: "space-between",
        }}
      >
        <Typography variant="body2" color="text.secondary">
          State actions are locked to the current ECO lifecycle and role.
        </Typography>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          {canSubmit || pendingAction === "submit" ? (
            <ActionButton
              color="primary"
              disabled={disableActions}
              icon={<SendIcon />}
              isPending={pendingAction === "submit"}
              label="Submit"
              onClick={onSubmit}
              variant="contained"
            />
          ) : null}
          {canApprove || pendingAction === "approve" ? (
            <ActionButton
              color="success"
              disabled={disableActions}
              icon={<CheckCircleIcon />}
              isPending={pendingAction === "approve"}
              label="Approve"
              onClick={onApprove}
              variant="contained"
            />
          ) : null}
          {canApprove || pendingAction === "requestChanges" ? (
            <ActionButton
              color="warning"
              disabled={disableActions}
              icon={<ErrorOutlineIcon />}
              isPending={pendingAction === "requestChanges"}
              label="Request Changes"
              onClick={() => setIsRequestChangesOpen(true)}
              variant="outlined"
            />
          ) : null}
          {canCancel || pendingAction === "cancel" ? (
            <ActionButton
              color="error"
              disabled={disableActions}
              icon={<CancelIcon />}
              isPending={pendingAction === "cancel"}
              label="Cancel"
              onClick={onCancel}
              variant="outlined"
            />
          ) : null}
        </Stack>
      </Stack>
      <RequestChangesDialog
        key={isRequestChangesOpen ? "request-changes-open" : "request-changes-closed"}
        open={isRequestChangesOpen}
        isPending={pendingAction === "requestChanges"}
        onClose={() => setIsRequestChangesOpen(false)}
        onConfirm={(comment) => {
          setIsRequestChangesOpen(false);
          onRequestChanges(comment);
        }}
      />
    </Paper>
  );
}

type ActionButtonProps = {
  color: "primary" | "success" | "warning" | "error";
  disabled: boolean;
  icon: ReactNode;
  isPending: boolean;
  label: string;
  onClick: () => void;
  variant: "contained" | "outlined";
};

function ActionButton({
  color,
  disabled,
  icon,
  isPending,
  label,
  onClick,
  variant,
}: ActionButtonProps) {
  return (
    <Button
      type="button"
      color={color}
      disabled={disabled}
      onClick={onClick}
      startIcon={isPending ? undefined : icon}
      variant={variant}
      sx={{ minWidth: 120, textTransform: "none" }}
    >
      {isPending ? <CircularProgress color="inherit" size={20} thickness={5} /> : label}
    </Button>
  );
}

type RequestChangesDialogProps = {
  open: boolean;
  isPending: boolean;
  onClose: () => void;
  onConfirm: (comment: string) => void;
};

/**
 * Renders the request-changes dialog used by reviewers.
 *
 * @param props - Dialog options.
 * @returns Request changes dialog.
 */
function RequestChangesDialog({
  open,
  isPending,
  onClose,
  onConfirm,
}: RequestChangesDialogProps) {
  const [comment, setComment] = useState("");
  const normalizedComment = comment.trim();
  const isTooShort =
    normalizedComment.length > 0 &&
    normalizedComment.length < minimumRequestChangesLength;

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Request Changes</DialogTitle>
      <DialogContent>
        <TextField
          label="Reason"
          value={comment}
          onChange={(event) => setComment(event.target.value)}
          error={isTooShort}
          helperText={
            isTooShort
              ? `Enter at least ${minimumRequestChangesLength} characters.`
              : `${comment.length}/${maximumCommentLength} characters`
          }
          multiline
          minRows={4}
          fullWidth
          required
          size="small"
          sx={{ mt: 1 }}
          slotProps={{ htmlInput: { maxLength: maximumCommentLength } }}
        />
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button disabled={isPending} onClick={onClose} sx={{ textTransform: "none" }}>
          Cancel
        </Button>
        <Button
          color="warning"
          disabled={isPending || normalizedComment.length < minimumRequestChangesLength}
          onClick={() => onConfirm(normalizedComment)}
          variant="contained"
          sx={{ textTransform: "none" }}
        >
          Request Changes
        </Button>
      </DialogActions>
    </Dialog>
  );
}

type ReviewersWidgetProps = {
  approvals: EcoDetailsDto["approvals"];
  approvers: EcoUserDto[];
  minApprovalsRequired: number;
  reviewRound: number;
  usersById: Map<string, EcoUserDto>;
};

/**
 * Renders quorum progress and approver status rows.
 *
 * @param props - Reviewer widget props.
 * @returns Reviewers widget.
 */
function ReviewersWidget({
  approvals,
  approvers,
  minApprovalsRequired,
  reviewRound,
  usersById,
}: ReviewersWidgetProps) {
  const currentRoundApprovals = approvals.filter(
    (approval) => approval.reviewRound === reviewRound,
  );
  const approvedCount = currentRoundApprovals.filter(
    (approval) => approval.decision === "Approve",
  ).length;

  return (
    <Card elevation={1}>
      <CardContent>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              Reviewers
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {approvedCount}/{minApprovalsRequired} approvals required
            </Typography>
          </Stack>
          <Divider />
          <List dense disablePadding>
            {approvers.map((approver) => {
              const decision = currentRoundApprovals.find(
                (approval) => approval.approverUserId === approver.id,
              );

              return (
                <ListItem key={approver.id} disableGutters>
                  <ListItemIcon sx={{ minWidth: 36 }}>
                    {renderDecisionIcon(decision?.decision)}
                  </ListItemIcon>
                  <ListItemText
                    primary={approver.name}
                    secondary={decision ? formatDateTime(decision.updatedAt) : "Pending"}
                  />
                </ListItem>
              );
            })}
            {approvers.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No active approvers are configured for this tenant.
              </Typography>
            ) : null}
          </List>
          {currentRoundApprovals
            .filter((approval) => !usersById.has(approval.approverUserId))
            .map((approval) => (
              <Chip
                key={approval.id}
                size="small"
                label={`${formatShortId(approval.approverUserId)}: ${formatDecisionLabel(
                  approval.decision,
                )}`}
              />
            ))}
        </Stack>
      </CardContent>
    </Card>
  );
}

type ConversationTabProps = {
  eco: EcoDetailsDto;
  isBlocked: boolean;
  onCommentSubmit: (body: string) => Promise<void> | void;
  onUpload: (file: File) => Promise<void> | void;
  pendingAction: PendingAction | null;
  usersById: Map<string, EcoUserDto>;
};

/**
 * Renders the conversation timeline and markdown comment composer.
 *
 * @param props - Conversation tab props.
 * @returns Conversation tab content.
 */
function ConversationTab({
  eco,
  isBlocked,
  onCommentSubmit,
  onUpload,
  pendingAction,
  usersById,
}: ConversationTabProps) {
  const [highlightedCommentId, setHighlightedCommentId] = useState<string | null>(null);
  const timeline = useMemo(() => createTimeline(eco.events, eco.comments), [
    eco.events,
    eco.comments,
  ]);

  useEffect(() => {
    if (!window.location.hash.startsWith("#comment-")) {
      return;
    }

    const commentId = window.location.hash.replace("#comment-", "");
    const element = document.getElementById(`comment-${commentId}`);
    if (!element) {
      return;
    }

    element.scrollIntoView({ behavior: "smooth", block: "center" });
    const highlightTimeoutId = window.setTimeout(() => {
      setHighlightedCommentId(commentId);
    }, 0);
    const timeoutId = window.setTimeout(() => setHighlightedCommentId(null), 2400);

    return () => {
      window.clearTimeout(highlightTimeoutId);
      window.clearTimeout(timeoutId);
    };
  }, [eco.comments]);

  return (
    <Stack spacing={2.5}>
      <Stack spacing={1.5}>
        {timeline.map((entry) =>
          entry.type === "comment" ? (
            <CommentCard
              key={`comment-${entry.comment.id}`}
              comment={entry.comment}
              highlighted={highlightedCommentId === entry.comment.id}
              user={usersById.get(entry.comment.authorUserId)}
            />
          ) : (
            <SystemEventRow
              key={`event-${entry.event.id}`}
              event={entry.event}
              user={usersById.get(entry.event.actorUserId)}
            />
          ),
        )}
      </Stack>
      <CommentComposer
        ecoId={eco.id}
        isBlocked={isBlocked}
        isSubmitting={pendingAction === "comment"}
        isUploading={pendingAction === "upload"}
        onSubmit={onCommentSubmit}
        onUpload={onUpload}
      />
    </Stack>
  );
}

type CommentCardProps = {
  comment: EcoCommentDto;
  highlighted: boolean;
  user: EcoUserDto | undefined;
};

function CommentCard({ comment, highlighted, user }: CommentCardProps) {
  const authorName = user?.name ?? formatShortId(comment.authorUserId);

  return (
    <Card
      id={`comment-${comment.id}`}
      elevation={highlighted ? 3 : 1}
      sx={{
        border: highlighted ? 1 : undefined,
        borderColor: highlighted ? "primary.main" : undefined,
        scrollMarginTop: 128,
      }}
    >
      <CardContent>
        <Stack spacing={1.5}>
          <Stack
            direction="row"
            spacing={1}
            sx={{ alignItems: "center", justifyContent: "space-between" }}
          >
            <Stack direction="row" spacing={1} sx={{ alignItems: "center", minWidth: 0 }}>
              <Avatar sx={{ width: 32, height: 32, fontSize: 13 }}>
                {getInitials(authorName)}
              </Avatar>
              <Stack sx={{ minWidth: 0 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                  {authorName}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {formatDateTime(comment.createdAt)}
                </Typography>
              </Stack>
            </Stack>
            <Tooltip title="Copy link">
              <IconButton size="small" onClick={() => copyCommentLink(comment.id)}>
                <ContentCopyIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
          <RichTextRenderer value={comment.body} />
        </Stack>
      </CardContent>
    </Card>
  );
}

type SystemEventRowProps = {
  event: EcoEventDto;
  user: EcoUserDto | undefined;
};

function SystemEventRow({ event, user }: SystemEventRowProps) {
  return (
    <Stack
      direction="row"
      spacing={1}
      sx={{
        alignItems: "flex-start",
        color: "text.secondary",
        px: { xs: 0.5, md: 2 },
      }}
    >
      <TimelineIcon fontSize="small" sx={{ mt: 0.25 }} />
      <Typography variant="body2">
        <strong>{eventLabelByType[event.eventType]}</strong>
        {" by "}
        {user?.name ?? formatShortId(event.actorUserId)}
        {" • "}
        {event.description}
        {" • "}
        {formatDateTime(event.occurredAt)}
      </Typography>
    </Stack>
  );
}

type CommentComposerProps = {
  ecoId: string;
  isBlocked: boolean;
  isSubmitting: boolean;
  isUploading: boolean;
  onSubmit: (body: string) => Promise<void> | void;
  onUpload: (file: File) => Promise<void> | void;
};

/**
 * Renders the write/preview comment form with localStorage draft persistence.
 *
 * @param props - Composer props.
 * @returns Comment composer.
 */
function CommentComposer({
  ecoId,
  isBlocked,
  isSubmitting,
  isUploading,
  onSubmit,
  onUpload,
}: CommentComposerProps) {
  const [mode, setMode] = useState<"write" | "preview">("write");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const draftKey = `eco-${ecoId}-draft`;
  const [draft, setDraft] = useState(() =>
    typeof window === "undefined" ? "" : window.localStorage.getItem(draftKey) ?? "",
  );

  useEffect(() => {
    window.localStorage.setItem(draftKey, draft);
  }, [draft, draftKey]);

  async function handleSubmit(): Promise<void> {
    const body = draft.trim();
    if (!body || isBlocked) {
      return;
    }

    await onSubmit(body);
    window.localStorage.removeItem(draftKey);
    setDraft("");
    setMode("write");
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>): void {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (file) {
      void onUpload(file);
    }
  }

  return (
    <Card elevation={1}>
      <CardContent>
        <Stack spacing={1.5}>
          <Tabs value={mode} onChange={(_, value: "write" | "preview") => setMode(value)}>
            <Tab value="write" label="Write" />
            <Tab value="preview" label="Preview" />
          </Tabs>
          {mode === "write" ? (
            <TextField
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              placeholder="Leave a comment"
              multiline
              minRows={5}
              fullWidth
              disabled={isBlocked || isSubmitting}
              slotProps={{ htmlInput: { maxLength: maximumCommentLength } }}
            />
          ) : (
            <Paper variant="outlined" sx={{ minHeight: 148, p: 2 }}>
              {draft.trim() ? (
                <RichTextRenderer value={draft} />
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Nothing to preview.
                </Typography>
              )}
            </Paper>
          )}
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={1}
            sx={{ justifyContent: "space-between" }}
          >
            <Box>
              <input
                ref={fileInputRef}
                hidden
                type="file"
                onChange={handleFileChange}
                accept=".pdf,.png,.jpg,.jpeg,.csv,.xlsx,.step,.stp,.dwg"
              />
              <Button
                type="button"
                variant="outlined"
                startIcon={<AttachFileIcon />}
                disabled={isBlocked || isUploading}
                onClick={() => fileInputRef.current?.click()}
                sx={{ textTransform: "none" }}
              >
                {isUploading ? "Uploading" : "Attach file"}
              </Button>
            </Box>
            <Button
              type="button"
              variant="contained"
              startIcon={isSubmitting ? undefined : <SendIcon />}
              disabled={isBlocked || isSubmitting || draft.trim().length === 0}
              onClick={() => void handleSubmit()}
              sx={{ minWidth: 132, textTransform: "none" }}
            >
              {isSubmitting ? (
                <CircularProgress color="inherit" size={20} thickness={5} />
              ) : (
                "Comment"
              )}
            </Button>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}

type AffectedItemsTabProps = {
  canEdit: boolean;
  eco: EcoDetailsDto;
  isBlocked: boolean;
  onAddItem: (item: AddAffectedItemRequest) => Promise<void> | void;
  onRemoveItem: (itemId: string) => Promise<void> | void;
  pendingAction: PendingAction | null;
};

function AffectedItemsTab({
  canEdit,
  eco,
  isBlocked,
  onAddItem,
  onRemoveItem,
  pendingAction,
}: AffectedItemsTabProps) {
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const columns = useMemo<GridColDef<EcoAffectedItemDto>[]>(
    () => [
      {
        field: "partNumber",
        headerName: "Part Number",
        minWidth: 160,
        flex: 1,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <Typography variant="body2" sx={{ fontWeight: 600, fontFamily: "monospace" }}>
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "description",
        headerName: "Description",
        minWidth: 240,
        flex: 1.5,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
            <Typography variant="body2" noWrap sx={{ width: "100%" }}>
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "currentRevision",
        headerName: "Current Rev",
        minWidth: 130,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <Typography variant="body2" color="text.secondary">
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "arrow",
        headerName: "",
        width: 72,
        sortable: false,
        renderCell: () => (
          <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%", width: "100%" }}>
            <Typography variant="body2" color="text.secondary">
              {"->"}
            </Typography>
          </Box>
        ),
      },
      {
        field: "newRevision",
        headerName: "New Rev",
        minWidth: 120,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "action",
        headerName: "Action",
        minWidth: 130,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <AffectedItemActionChip action={params.row.action} />
          </Box>
        ),
      },
      {
        field: "remove",
        headerName: "",
        width: 76,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%", width: "100%" }}>
            {canEdit ? (
              <Tooltip title="Remove item">
                <span>
                  <IconButton
                    size="small"
                    disabled={isBlocked || pendingAction === "removeItem"}
                    onClick={() => onRemoveItem(params.row.id)}
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            ) : null}
          </Box>
        ),
      },
    ],
    [canEdit, isBlocked, onRemoveItem, pendingAction],
  );

  return (
    <Stack spacing={2}>
      {canEdit ? (
        <Button
          type="button"
          variant="contained"
          startIcon={<AddIcon />}
          disabled={isBlocked}
          onClick={() => setIsAddDialogOpen(true)}
          sx={{ alignSelf: "flex-start", textTransform: "none", fontWeight: 600 }}
        >
          Add Item
        </Button>
      ) : null}
      <Box sx={{ width: "100%", minHeight: 300 }}>
        <DataGrid
          rows={eco.affectedItems}
          columns={columns}
          getRowId={(row) => row.id}
          disableRowSelectionOnClick
          pageSizeOptions={[10, 25, 50]}
          initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
          getRowClassName={(params) =>
            params.row.action === "Remove" ? "affected-item-obsolete" : ""
          }
          sx={{
            borderColor: "divider",
            bgcolor: "background.paper",
            borderRadius: 2,
            "& .MuiDataGrid-columnHeaders": {
              bgcolor: "action.hover",
              borderBottom: 1,
              borderColor: "divider",
            },
            "& .MuiDataGrid-cell": {
              borderColor: "divider",
            },
            "& .affected-item-obsolete .MuiDataGrid-cell": {
              color: "text.secondary",
              textDecoration: "line-through",
            },
          }}
        />
      </Box>
      {isAddDialogOpen ? (
        <AddAffectedItemDialog
          open={isAddDialogOpen}
          isPending={pendingAction === "addItem"}
          onClose={() => setIsAddDialogOpen(false)}
          onConfirm={(item) => {
            setIsAddDialogOpen(false);
            onAddItem(item);
          }}
        />
      ) : null}
    </Stack>
  );
}

type AddAffectedItemDialogProps = {
  open: boolean;
  isPending: boolean;
  onClose: () => void;
  onConfirm: (item: AddAffectedItemRequest) => void;
};

function AddAffectedItemDialog({
  open,
  isPending,
  onClose,
  onConfirm,
}: AddAffectedItemDialogProps) {
  const [form, setForm] = useState<AffectedItemFormState>(initialAffectedItemForm);
  const isValid =
    form.partNumber.trim() &&
    form.description.trim() &&
    form.currentRevision.trim() &&
    form.newRevision.trim();

  function patchForm(patch: Partial<AffectedItemFormState>): void {
    setForm((current) => ({ ...current, ...patch }));
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>Add Affected Item</DialogTitle>
      <DialogContent>
        <Grid container spacing={2} sx={{ pt: 1 }}>
          <Grid size={{ xs: 12, md: 5 }}>
            <TextField
              label="Part Number"
              value={form.partNumber}
              onChange={(event) => patchForm({ partNumber: event.target.value })}
              fullWidth
              required
              size="small"
            />
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              label="Current Rev"
              value={form.currentRevision}
              onChange={(event) => patchForm({ currentRevision: event.target.value })}
              fullWidth
              required
              size="small"
            />
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              label="New Rev"
              value={form.newRevision}
              onChange={(event) => patchForm({ newRevision: event.target.value })}
              fullWidth
              required
              size="small"
            />
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <FormControl fullWidth size="small">
              <InputLabel id="affected-item-action-label">Action</InputLabel>
              <Select
                labelId="affected-item-action-label"
                label="Action"
                value={form.action}
                onChange={(event: SelectChangeEvent) =>
                  patchForm({ action: event.target.value as EcoAffectedItemAction })
                }
              >
                <MenuItem value="Add">Create</MenuItem>
                <MenuItem value="Modify">Modify</MenuItem>
                <MenuItem value="Remove">Obsolete</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid size={12}>
            <TextField
              label="Description"
              value={form.description}
              onChange={(event) => patchForm({ description: event.target.value })}
              fullWidth
              required
              multiline
              minRows={3}
              size="small"
            />
          </Grid>
        </Grid>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button disabled={isPending} onClick={onClose} sx={{ textTransform: "none" }}>
          Cancel
        </Button>
        <Button
          disabled={isPending || !isValid}
          onClick={() =>
            onConfirm({
              ...form,
              partNumber: form.partNumber.trim(),
              description: form.description.trim(),
              currentRevision: form.currentRevision.trim(),
              newRevision: form.newRevision.trim(),
            })
          }
          variant="contained"
          sx={{ textTransform: "none" }}
        >
          Add Item
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function AffectedItemActionChip({ action }: { action: EcoAffectedItemAction }) {
  const labelByAction: Record<EcoAffectedItemAction, string> = {
    Add: "Create",
    Modify: "Modify",
    Remove: "Obsolete",
  };
  const sxByAction: Record<EcoAffectedItemAction, object> = {
    Add: { bgcolor: "rgba(46, 125, 50, 0.12)", color: "success.dark" },
    Modify: { bgcolor: "rgba(2, 136, 209, 0.12)", color: "info.dark" },
    Remove: { bgcolor: "rgba(211, 47, 47, 0.12)", color: "error.dark" },
  };

  return <Chip label={labelByAction[action]} size="small" sx={sxByAction[action]} />;
}

type FilesTabProps = {
  attachments: EcoAttachmentDto[];
  ecoId: string;
  usersById: Map<string, EcoUserDto>;
};

function FilesTab({ attachments, ecoId, usersById }: FilesTabProps) {
  const [selectedAttachment, setSelectedAttachment] = useState<EcoAttachmentDto | null>(null);

  if (attachments.length === 0) {
    return (
      <Stack spacing={1.5} sx={{ alignItems: "center", py: 6, textAlign: "center" }}>
        <InsertDriveFileIcon sx={{ color: "grey.500", fontSize: 48 }} />
        <Typography variant="body2" color="text.secondary">
          No files have been attached to this ECO.
        </Typography>
      </Stack>
    );
  }

  return (
    <>
      <Grid container spacing={1.5}>
        {attachments.map((attachment) => {
          const uploader = usersById.get(attachment.uploadedByUserId);

          return (
            <Grid key={attachment.id} size={{ xs: 12, md: 6, xl: 4 }}>
              <Paper
                component="button"
                type="button"
                onClick={() => setSelectedAttachment(attachment)}
                variant="outlined"
                sx={{
                  p: 1.5,
                  textAlign: "left",
                  width: "100%",
                  cursor: "pointer",
                  bgcolor: "background.paper",
                  borderColor: "divider",
                }}
              >
                <Stack direction="row" spacing={1.25} sx={{ alignItems: "flex-start" }}>
                  <InsertDriveFileIcon color="primary" />
                  <Stack sx={{ minWidth: 0 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }} noWrap>
                      {attachment.fileName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatFileSize(attachment.fileSize)} • {attachment.mimeType}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" noWrap>
                      Uploaded by {uploader?.name ?? formatShortId(attachment.uploadedByUserId)}
                    </Typography>
                  </Stack>
                </Stack>
              </Paper>
            </Grid>
          );
        })}
      </Grid>
      <AttachmentDrawer
        key={selectedAttachment?.id ?? "attachment-drawer-empty"}
        attachment={selectedAttachment}
        ecoId={ecoId}
        uploader={selectedAttachment ? usersById.get(selectedAttachment.uploadedByUserId) : undefined}
        onClose={() => setSelectedAttachment(null)}
      />
    </>
  );
}

type AttachmentDrawerProps = {
  attachment: EcoAttachmentDto | null;
  ecoId: string;
  uploader: EcoUserDto | undefined;
  onClose: () => void;
};

function AttachmentDrawer({ attachment, ecoId, uploader, onClose }: AttachmentDrawerProps) {
  const [isDownloading, setIsDownloading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleDownload(): Promise<void> {
    if (!attachment || isDownloading) {
      return;
    }

    setIsDownloading(true);
    setErrorMessage(null);

    try {
      const download = await apiFetch<EcoAttachmentDownloadDto>(
        `/api/ecos/${encodeURIComponent(ecoId)}/attachments/${encodeURIComponent(
          attachment.id,
        )}/download`,
      );
      window.open(download.url, "_blank", "noopener,noreferrer");
    } catch (error) {
      setErrorMessage(getActionErrorMessage(error));
    } finally {
      setIsDownloading(false);
    }
  }

  return (
    <Drawer
      anchor="right"
      open={Boolean(attachment)}
      onClose={onClose}
      slotProps={{ paper: { sx: { width: { xs: "100%", sm: 460 } } } }}
    >
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
          <Typography variant="h6" component="h2">
            File Details
          </Typography>
          <IconButton onClick={onClose} size="small">
            <CloseIcon fontSize="small" />
          </IconButton>
        </Stack>
        {attachment ? (
          <>
            <Divider />
            <Stack spacing={1}>
              <Typography variant="body1" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>
                {attachment.fileName}
              </Typography>
              <DetailRow label="Size">{formatFileSize(attachment.fileSize)}</DetailRow>
              <DetailRow label="Type">{attachment.mimeType}</DetailRow>
              <DetailRow label="Uploaded by">
                {uploader?.name ?? formatShortId(attachment.uploadedByUserId)}
              </DetailRow>
              <DetailRow label="Uploaded">{formatDateTime(attachment.uploadedAt)}</DetailRow>
              <DetailRow label="Object key">
                <Typography
                  component="span"
                  variant="body2"
                  sx={{ fontFamily: "monospace", overflowWrap: "anywhere" }}
                >
                  {attachment.objectKey}
                </Typography>
              </DetailRow>
            </Stack>
            {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}
            <Button
              type="button"
              variant="contained"
              startIcon={isDownloading ? undefined : <DownloadIcon />}
              disabled={isDownloading}
              onClick={() => void handleDownload()}
              sx={{ textTransform: "none" }}
            >
              {isDownloading ? (
                <CircularProgress color="inherit" size={20} thickness={5} />
              ) : (
                "Download"
              )}
            </Button>
          </>
        ) : null}
      </Stack>
    </Drawer>
  );
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Stack spacing={0.25}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Box>{children}</Box>
    </Stack>
  );
}

function EcoDetailsSkeleton() {
  return (
    <Stack spacing={2.5}>
      <Skeleton variant="text" width="40%" height={48} />
      <Skeleton variant="rounded" height={72} />
      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Skeleton variant="rounded" height={220} />
        </Grid>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Skeleton variant="rounded" height={220} />
        </Grid>
      </Grid>
      <Skeleton variant="rounded" height={420} />
    </Stack>
  );
}

async function submitReviewDecision(
  ecoId: string,
  request: SubmitReviewDecisionRequest,
): Promise<EcoDetailsDto> {
  return apiFetch<EcoDetailsDto>(
    `/api/ecos/${encodeURIComponent(ecoId)}/review-decisions`,
    {
      method: "POST",
      body: request,
    },
  );
}

async function uploadAttachment(ecoId: string, file: File): Promise<EcoDetailsDto> {
  const formData = new FormData();
  formData.set("file", file);

  return apiFetch<EcoDetailsDto>(
    `/api/ecos/${encodeURIComponent(ecoId)}/attachments`,
    {
      method: "POST",
      body: formData,
    },
  );
}

function normalizeEcoDetails(details: EcoDetailsDto): EcoDetailsDto {
  return {
    ...details,
    affectedItems: details.affectedItems ?? [],
    approvals: details.approvals ?? [],
    attachments: details.attachments ?? [],
    comments: details.comments ?? [],
    currentRoundApprovalCount: details.currentRoundApprovalCount ?? 0,
    currentRoundRequestChangesCount: details.currentRoundRequestChangesCount ?? 0,
    events: details.events ?? [],
    reviewRound: details.reviewRound ?? 0,
    rowVersion: details.rowVersion ?? 0,
  };
}

function createTimeline(
  events: EcoEventDto[],
  comments: EcoCommentDto[],
): TimelineEntry[] {
  return [
    ...events.map((event) => ({
      type: "event" as const,
      occurredAt: event.occurredAt,
      event,
    })),
    ...comments.map((comment) => ({
      type: "comment" as const,
      occurredAt: comment.createdAt,
      comment,
    })),
  ].sort((left, right) => Date.parse(left.occurredAt) - Date.parse(right.occurredAt));
}

function renderDecisionIcon(decision: EcoApprovalDecision | undefined): ReactNode {
  if (decision === "Approve") {
    return <CheckCircleIcon color="success" fontSize="small" />;
  }

  if (decision === "RequestChanges") {
    return <ErrorOutlineIcon color="warning" fontSize="small" />;
  }

  return <HourglassEmptyIcon color="disabled" fontSize="small" />;
}

function formatDecisionLabel(decision: EcoApprovalDecision): string {
  return decision === "Approve" ? "Approved" : "Requested changes";
}

function readRouteEcoId(value: string | string[] | undefined): string | null {
  if (Array.isArray(value)) {
    return value[0]?.trim() || null;
  }

  return value?.trim() || null;
}

function roleAllows(role: string | undefined, allowedRoles: readonly string[]): boolean {
  const normalizedRole = role?.trim().toLowerCase();

  return Boolean(
    normalizedRole &&
      allowedRoles.some((allowedRole) => allowedRole.toLowerCase() === normalizedRole),
  );
}

function createUserLookup(users: EcoUserDto[]): Map<string, EcoUserDto> {
  return new Map(users.map((item) => [item.id, item]));
}

function formatShortId(id: string): string {
  return id.length > 8 ? id.slice(0, 8).toUpperCase() : id.toUpperCase();
}

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

function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "-";
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getInitials(value: string): string {
  const parts = value.trim().split(/\s+/).filter(Boolean);
  const initials = parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join("");

  return initials || "?";
}

function copyCommentLink(commentId: string): void {
  const nextUrl = `${window.location.origin}${window.location.pathname}#comment-${commentId}`;
  window.history.replaceState(null, "", `#comment-${commentId}`);
  void window.navigator.clipboard?.writeText(nextUrl);
}

function getLoadEcoErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 404) {
    return "The requested Engineering Change Order was not found.";
  }

  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? "Unable to load the Engineering Change Order.";
  }

  return "Unable to load the Engineering Change Order. Check your connection and try again.";
}

function getActionErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? "The ECO action failed.";
  }

  return "The ECO action failed. Check your connection and try again.";
}

function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
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
