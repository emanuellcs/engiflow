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
import RefreshIcon from "@mui/icons-material/Refresh";
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
import DialogContentText from "@mui/material/DialogContentText";
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
import { DataGrid, type GridColDef, type GridToolbarProps } from "@mui/x-data-grid";
import DataGridCustomToolbar from "@/components/ui/DataGridCustomToolbar";
import DataGridEmptyState from "@/components/ui/DataGridEmptyState";
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
 * Renders the authenticated ECO detail route with a GitHub-style PR experience.
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
  const isCreator = user?.id === eco.createdByUserId;

  return (
    <Stack spacing={2.5}>
      {/* 1. Header & Navigation */}
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
          <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", alignItems: "center" }}>
            <Tooltip title="Refresh data">
              <span>
                <IconButton
                  onClick={() => void loadEcoDetails()}
                  disabled={isLoading}
                  color="primary"
                  size="small"
                  sx={{
                    border: 1,
                    borderColor: "divider",
                    bgcolor: "background.paper",
                    width: 34,
                    height: 34,
                  }}
                >
                  {isLoading ? (
                    <CircularProgress size={18} color="inherit" thickness={5} />
                  ) : (
                    <RefreshIcon fontSize="small" />
                  )}
                </IconButton>
              </span>
            </Tooltip>
            <PriorityChip priority={eco.priority} />
            <Chip size="small" label={`ECO ${formatShortId(eco.id)}`} variant="outlined" sx={{ height: 34, border: 1, borderColor: "divider" }} />
          </Stack>
        </Stack>
      </Stack>

      {actionErrorMessage ? <Alert severity="error">{actionErrorMessage}</Alert> : null}
      {hub.status === "reconnecting" || hub.status === "disconnected" ? (
        <Alert severity="warning" variant="outlined">
          Real-time ECO updates are {hub.status}. {hub.errorMessage ?? ""}
        </Alert>
      ) : null}

      {/* 2. Tabs Shell */}
      <Paper
        elevation={0}
        variant="outlined"
        sx={{
          display: "flex",
          flexDirection: "column",
          flexGrow: 1,
          minHeight: 0,
          border: 1,
          borderColor: "divider",
        }}
      >
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
        <Box sx={{ p: { xs: 1.5, md: 2.5 }, flexGrow: 1, minHeight: 0 }}>
          {activeTab === "conversation" ? (
            <ConversationTab
              eco={eco}
              isBlocked={Boolean(pendingAction) || isConflictRefreshing}
              isCreator={isCreator}
              canReview={canReview}
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
              approvers={approvers}
              minApprovalsRequired={minApprovalsRequired}
            />
          ) : null}
          {activeTab === "items" ? (
           <AffectedItemsTab
             canEdit={eco.status === "Draft" && isCreator}
             eco={eco}
             isBlocked={Boolean(pendingAction) || isConflictRefreshing}
             isLoading={isLoading}
             loadEcoDetails={loadEcoDetails}
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

type ConversationTabProps = {
  eco: EcoDetailsDto;
  isBlocked: boolean;
  isCreator: boolean;
  canReview: boolean;
  onApprove: () => void;
  onCancel: () => void;
  onRequestChanges: (comment: string) => void;
  onSubmit: () => void;
  onCommentSubmit: (body: string) => Promise<void> | void;
  onUpload: (file: File) => Promise<void> | void;
  pendingAction: PendingAction | null;
  usersById: Map<string, EcoUserDto>;
  approvers: EcoUserDto[];
  minApprovalsRequired: number;
};

/**
 * Renders the GitHub-style conversation grid.
 */
function ConversationTab({
  eco,
  isBlocked,
  isCreator,
  canReview,
  onApprove,
  onCancel,
  onRequestChanges,
  onSubmit,
  onCommentSubmit,
  onUpload,
  pendingAction,
  usersById,
  approvers,
  minApprovalsRequired,
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
    <Grid container spacing={2.5}>
      {/* Left Column: Context, Timeline, and Action */}
      <Grid size={{ xs: 12, md: 8 }}>
        <Stack spacing={3}>
          {/* A. Change Summary */}
          <Card elevation={0} variant="outlined" sx={{ border: 1, borderColor: "divider" }}>
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6" component="h2" sx={{ fontSize: "1.1rem", fontWeight: 600 }}>
                  Change Summary
                </Typography>
                <Divider />
                <RichTextRenderer value={eco.description} />
              </Stack>
            </CardContent>
          </Card>

          {/* B. Vertical Timeline */}
          <Stack spacing={0} sx={{ position: "relative" }}>
            {timeline.map((entry) => (
              <TimelineItemContainer
                key={entry.type === "comment" ? `comment-${entry.comment.id}` : `event-${entry.event.id}`}
                icon={
                  entry.type === "comment" ? (
                    <Avatar sx={{ width: 20, height: 20, fontSize: 10 }}>
                      {getInitials(usersById.get(entry.comment.authorUserId)?.name ?? "U")}
                    </Avatar>
                  ) : (
                    <TimelineIcon sx={{ fontSize: 14, color: "text.secondary" }} />
                  )
                }
              >
                {entry.type === "comment" ? (
                  <CommentCard
                    comment={entry.comment}
                    highlighted={highlightedCommentId === entry.comment.id}
                    user={usersById.get(entry.comment.authorUserId)}
                  />
                ) : (
                  <SystemEventRow
                    event={entry.event}
                    user={usersById.get(entry.event.actorUserId)}
                  />
                )}
              </TimelineItemContainer>
            ))}
          </Stack>

          {/* C. Action Area (Composer + Workflow Actions) */}
          <ActionArea
            eco={eco}
            isBlocked={isBlocked}
            isCreator={isCreator}
            canReview={canReview}
            onApprove={onApprove}
            onCancel={onCancel}
            onRequestChanges={onRequestChanges}
            onSubmit={onSubmit}
            onCommentSubmit={onCommentSubmit}
            onUpload={onUpload}
            pendingAction={pendingAction}
          />
        </Stack>
      </Grid>

      {/* Right Column: Sticky Metadata Sidebar */}
      <Grid size={{ xs: 12, md: 4 }}>
        <Stack spacing={2.5} sx={{ position: "sticky", top: 24 }}>
          <ReviewersWidget
            approvals={eco.approvals}
            approvers={approvers}
            minApprovalsRequired={minApprovalsRequired}
            reviewRound={eco.reviewRound}
            usersById={usersById}
          />
          <Card elevation={0} variant="outlined" sx={{ border: 1, borderColor: "divider" }}>
            <CardContent>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 600 }}>
                Metadata
              </Typography>
              <Stack spacing={1.5} sx={{ mt: 1 }}>
                <DetailRow label="Created">
                  <Typography variant="body2">{formatDateTime(eco.createdAt)}</Typography>
                </DetailRow>
                <DetailRow label="Priority">
                  <PriorityChip priority={eco.priority} />
                </DetailRow>
                <DetailRow label="Status">
                  <StatusChip status={eco.status} />
                </DetailRow>
              </Stack>
            </CardContent>
          </Card>
        </Stack>
      </Grid>
    </Grid>
  );
}

/**
 * A container that handles the visual timeline separator (dot + connector).
 */
function TimelineItemContainer({ children, icon }: { children: ReactNode; icon?: ReactNode }) {
  return (
    <Stack direction="row" spacing={2}>
      <Stack sx={{ alignItems: "center", width: 24, flexShrink: 0 }}>
        <Box sx={{ width: 2, height: 12, bgcolor: "divider" }} />
        <Box
          sx={{
            width: 24,
            height: 24,
            borderRadius: "50%",
            bgcolor: "background.paper",
            border: 1,
            borderColor: "divider",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
            zIndex: 1,
            boxShadow: 1,
          }}
        >
          {icon ?? <Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: "divider" }} />}
        </Box>
        <Box sx={{ width: 2, flexGrow: 1, bgcolor: "divider" }} />
      </Stack>
      <Box sx={{ flexGrow: 1, pb: 3, pt: 1.5 }}>{children}</Box>
    </Stack>
  );
}

type ActionAreaProps = {
  eco: EcoDetailsDto;
  isBlocked: boolean;
  isCreator: boolean;
  canReview: boolean;
  onApprove: () => void;
  onCancel: () => void;
  onRequestChanges: (comment: string) => void;
  onSubmit: () => void;
  onCommentSubmit: (body: string) => Promise<void> | void;
  onUpload: (file: File) => Promise<void> | void;
  pendingAction: PendingAction | null;
};

/**
 * Combines workflow actions and the comment composer.
 */
function ActionArea({
  eco,
  isBlocked,
  isCreator,
  canReview,
  onApprove,
  onCancel,
  onRequestChanges,
  onSubmit,
  onCommentSubmit,
  onUpload,
  pendingAction,
}: ActionAreaProps) {
  const { user } = useAuth();
  const [isRequestChangesOpen, setIsRequestChangesOpen] = useState(false);
  const [wantsToChangeVote, setWantsToChangeVote] = useState(false);
  const [confirmation, setConfirmation] = useState<{
    open: boolean;
    title: string;
    message: string;
    onConfirm: () => void;
    color?: "primary" | "error" | "success";
  }>({ open: false, title: "", message: "", onConfirm: () => {} });

  const currentVote = useMemo(() => {
    return eco.approvals.find(
      (a) => a.reviewRound === eco.reviewRound && a.approverUserId === user?.id,
    );
  }, [eco.approvals, eco.reviewRound, user?.id]);

  const canSubmit = eco.status === "Draft" && isCreator;
  const canCancel = (eco.status === "Draft" || eco.status === "UnderReview") && isCreator;
  const canVote = eco.status === "UnderReview" && canReview && !isCreator;
  const showVoteButtons = canVote && (!currentVote || wantsToChangeVote);
  const showVoteIndicator = canVote && currentVote && !wantsToChangeVote;

  const disableActions = isBlocked || Boolean(pendingAction);

  const confirmAction = (
    title: string,
    message: string,
    onConfirm: () => void,
    color: "primary" | "error" | "success" = "primary",
  ) => {
    setConfirmation({ open: true, title, message, onConfirm, color });
  };

  return (
    <Card elevation={0} variant="outlined" sx={{ borderTop: 4, borderTopColor: "primary.main", border: 1, borderColor: "divider" }}>
      <CardContent>
        <Stack spacing={2.5}>
          {/* Workflow Buttons Area */}
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={1.5}
            sx={{
              alignItems: { xs: "stretch", sm: "center" },
              justifyContent: "space-between",
              bgcolor: "action.hover",
              p: 1.5,
              borderRadius: 1,
              border: 1,
              borderColor: "divider",
            }}
          >
            <Box>
              {isCreator && eco.status === "UnderReview" && (
                <Typography variant="body2" color="text.secondary">
                  As the author, you cannot participate in the approval quorum.
                </Typography>
              )}
              {!isCreator && !canReview && eco.status === "UnderReview" && (
                <Typography variant="body2" color="text.secondary">
                  You do not have the permissions required to review this ECO.
                </Typography>
              )}
              {eco.status === "Draft" && !isCreator && (
                <Typography variant="body2" color="text.secondary">
                  Only the creator can submit this ECO for review.
                </Typography>
              )}
            </Box>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
              {canSubmit ? (
                <ActionButton
                  color="primary"
                  disabled={disableActions}
                  icon={<SendIcon />}
                  isPending={pendingAction === "submit"}
                  label="Submit for Review"
                  onClick={() =>
                    confirmAction(
                      "Submit ECO",
                      "Are you sure you want to submit this Engineering Change Order for review? Once submitted, the affected items will be locked for editing.",
                      onSubmit,
                    )
                  }
                  variant="contained"
                />
              ) : null}

              {showVoteIndicator && currentVote && (
                <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                  <Chip
                    size="small"
                    icon={currentVote.decision === "Approve" ? <CheckCircleIcon /> : <ErrorOutlineIcon />}
                    label={currentVote.decision === "Approve" ? "Approved" : "Changes Requested"}
                    color={currentVote.decision === "Approve" ? "success" : "warning"}
                    variant="outlined"
                    sx={{ border: 1, borderColor: "divider" }}
                  />
                  <Button
                    size="small"
                    onClick={() => setWantsToChangeVote(true)}
                    sx={{ textTransform: "none" }}
                  >
                    Change Vote
                  </Button>
                </Stack>
              )}

              {showVoteButtons && (
                <>
                  <ActionButton
                    color="success"
                    disabled={disableActions || currentVote?.decision === "Approve"}
                    icon={<CheckCircleIcon />}
                    isPending={pendingAction === "approve"}
                    label="Approve"
                    onClick={() => {
                      confirmAction(
                        "Approve ECO",
                        "Are you sure you want to approve this Engineering Change Order?",
                        () => {
                          onApprove();
                          setWantsToChangeVote(false);
                        },
                        "success",
                      );
                    }}
                    variant="contained"
                  />
                  <ActionButton
                    color="warning"
                    disabled={disableActions || currentVote?.decision === "RequestChanges"}
                    icon={<ErrorOutlineIcon />}
                    isPending={pendingAction === "requestChanges"}
                    label="Request Changes"
                    onClick={() => setIsRequestChangesOpen(true)}
                    variant="outlined"
                  />
                  {wantsToChangeVote && (
                    <Button
                      size="small"
                      color="inherit"
                      onClick={() => setWantsToChangeVote(false)}
                      sx={{ textTransform: "none" }}
                    >
                      Cancel
                    </Button>
                  )}
                </>
              )}

              {canCancel ? (
                <ActionButton
                  color="error"
                  disabled={disableActions}
                  icon={<CancelIcon />}
                  isPending={pendingAction === "cancel"}
                  label="Cancel ECO"
                  onClick={() =>
                    confirmAction(
                      "Cancel ECO",
                      "Are you sure you want to cancel this ECO? This action is permanent and will move the ECO to a terminal Canceled state.",
                      onCancel,
                      "error",
                    )
                  }
                  variant="outlined"
                />
              ) : null}
            </Stack>
          </Stack>

          <Divider />

          {/* Comment Composer */}
          <CommentComposer
            ecoId={eco.id}
            isBlocked={isBlocked}
            isSubmitting={pendingAction === "comment"}
            isUploading={pendingAction === "upload"}
            onSubmit={onCommentSubmit}
            onUpload={onUpload}
          />
        </Stack>
      </CardContent>

      <RequestChangesDialog
        open={isRequestChangesOpen}
        isPending={pendingAction === "requestChanges"}
        onClose={() => setIsRequestChangesOpen(false)}
        onConfirm={(comment) => {
          setIsRequestChangesOpen(false);
          onRequestChanges(comment);
          setWantsToChangeVote(false);
        }}
      />

      <ConfirmationDialog
        open={confirmation.open}
        title={confirmation.title}
        message={confirmation.message}
        onClose={() => setConfirmation({ ...confirmation, open: false })}
        onConfirm={() => {
          setConfirmation({ ...confirmation, open: false });
          confirmation.onConfirm();
        }}
        color={confirmation.color}
      />
    </Card>
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
      sx={{ minWidth: 120, textTransform: "none", fontWeight: 600 }}
    >
      {isPending ? <CircularProgress color="inherit" size={20} thickness={5} /> : label}
    </Button>
  );
}

function ConfirmationDialog({
  open,
  title,
  message,
  onClose,
  onConfirm,
  color = "primary",
}: {
  open: boolean;
  title: string;
  message: string;
  onClose: () => void;
  onConfirm: () => void;
  color?: "primary" | "error" | "success";
}) {
  return (
    <Dialog open={open} onClose={onClose}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText>{message}</DialogContentText>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} sx={{ textTransform: "none" }}>Cancel</Button>
        <Button
          onClick={onConfirm}
          color={color}
          variant="contained"
          sx={{ textTransform: "none" }}
          autoFocus
        >
          Confirm
        </Button>
      </DialogActions>
    </Dialog>
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
    <Card elevation={0} variant="outlined" sx={{ border: 1, borderColor: "divider" }}>
      <CardContent>
        <Stack spacing={2}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2" sx={{ fontSize: "1rem", fontWeight: 600 }}>
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
                sx={{ border: 1, borderColor: "divider" }}
              />
            ))}
        </Stack>
      </CardContent>
    </Card>
  );
}

function CommentCard({
  comment,
  highlighted,
  user,
}: {
  comment: EcoCommentDto;
  highlighted: boolean;
  user: EcoUserDto | undefined;
}) {
  const authorName = user?.name ?? formatShortId(comment.authorUserId);

  return (
    <Card
      id={`comment-${comment.id}`}
      elevation={0}
      variant="outlined"
      sx={{
        border: 1,
        borderColor: highlighted ? "primary.main" : "divider",
        scrollMarginTop: 128,
        zIndex: 1,
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
              <Avatar sx={{ width: 32, height: 32, fontSize: 13, border: 1, borderColor: "divider" }}>
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

function SystemEventRow({
  event,
  user,
}: {
  event: EcoEventDto;
  user: EcoUserDto | undefined;
}) {
  return (
    <Stack
      direction="row"
      spacing={1.5}
      sx={{
        alignItems: "center",
        color: "text.secondary",
        py: 0.5,
        zIndex: 1,
        position: "relative",
      }}
    >
      <Typography variant="body2" sx={{ flexGrow: 1 }}>
        <strong>{eventLabelByType[event.eventType]}</strong>
        {" by "}
        {user?.name ?? formatShortId(event.actorUserId)}
        {" • "}
        {event.description}
      </Typography>
      <Typography variant="caption" sx={{ whiteSpace: "nowrap" }}>
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
 * Renders the write/preview comment form with account-namespaced draft persistence.
 */
function CommentComposer({
  ecoId,
  isBlocked,
  isSubmitting,
  isUploading,
  onSubmit,
  onUpload,
}: CommentComposerProps) {
  const { user } = useAuth();
  const [mode, setMode] = useState<"write" | "preview">("write");
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const draftKey = `eco-${ecoId}-${user?.id}-draft`;
  const [draft, setDraft] = useState(() =>
    typeof window === "undefined" ? "" : window.localStorage.getItem(draftKey) ?? "",
  );

  useEffect(() => {
    if (user?.id) {
      window.localStorage.setItem(draftKey, draft);
    }
  }, [draft, draftKey, user?.id]);

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
          sx={{ "& .MuiOutlinedInput-root": { border: 1, borderColor: "divider" } }}
        />
      ) : (
        <Paper variant="outlined" sx={{ minHeight: 148, p: 2, border: 1, borderColor: "divider" }}>
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
            sx={{ textTransform: "none", border: 1, borderColor: "divider" }}
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
  );
}

type AffectedItemsTabProps = {
  canEdit: boolean;
  eco: EcoDetailsDto;
  isBlocked: boolean;
  isLoading: boolean;
  loadEcoDetails: () => Promise<void>;
  onAddItem: (item: AddAffectedItemRequest) => Promise<void> | void;
  onRemoveItem: (itemId: string) => Promise<void> | void;
  pendingAction: PendingAction | null;
};

function AffectedItemsTab({
  canEdit,
  eco,
  isBlocked,
  isLoading,
  loadEcoDetails,
  onAddItem,
  onRemoveItem,
  pendingAction,
}: AffectedItemsTabProps) {
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [confirmation, setConfirmation] = useState<{ open: boolean; itemId: string | null }>({
    open: false,
    itemId: null,
  });

  const CustomToolbar = useMemo(() => {
    return function AffectedItemsCustomToolbar(props: GridToolbarProps) {
      return (
        <DataGridCustomToolbar
          {...props}
          isLoading={isLoading}
          onRefresh={() => void loadEcoDetails()}
        />
      );
    };
  }, [isLoading, loadEcoDetails]);

  const columns = useMemo<GridColDef<EcoAffectedItemDto>[]>(() => {
    const baseColumns: GridColDef<EcoAffectedItemDto>[] = [
      {
        field: "partNumber",
        headerName: "Part Number",
        width: 160,
        headerAlign: "left",
        align: "left",
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
        minWidth: 200,
        flex: 1,
        headerAlign: "left",
        align: "left",
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
        width: 110,
        headerAlign: "center",
        align: "center",
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <Typography variant="body2" color="text.secondary">
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "newRevision",
        headerName: "New Rev",
        width: 130,
        headerAlign: "center",
        align: "center",
        renderCell: (params) => (
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              height: "100%",
              width: "100%",
              gap: 1.5,
            }}
          >
            <Typography variant="body2" color="text.secondary" sx={{ opacity: 0.5 }}>
              {"->"}
            </Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {params.value}
            </Typography>
          </Box>
        ),
      },
      {
        field: "action",
        headerName: "Action",
        width: 140,
        headerAlign: "center",
        align: "center",
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <AffectedItemActionChip action={params.row.action} />
          </Box>
        ),
      },
    ];

    if (canEdit) {
      baseColumns.push({
        field: "remove",
        headerName: "Actions",
        width: 100,
        sortable: false,
        headerAlign: "center",
        align: "center",
        renderCell: (params) => (
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              height: "100%",
              width: "100%",
            }}
          >
            <Tooltip title="Remove item">
              <span>
                <IconButton
                  size="small"
                  disabled={isBlocked || pendingAction === "removeItem"}
                  onClick={() => setConfirmation({ open: true, itemId: params.row.id })}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Box>
        ),
      });
    }

    return baseColumns;
  }, [canEdit, isBlocked, pendingAction]);

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
      <Box sx={{ width: "100%", height: 480, border: 1, borderColor: "divider", borderRadius: 2, overflow: "hidden", bgcolor: "background.paper" }}>
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
          slots={{
            toolbar: CustomToolbar,
            noRowsOverlay: () => (
              <DataGridEmptyState
                message="No affected items"
                description="There are currently no items associated with this ECO."
              />
            ),
          }}
          sx={{
            height: "100%",
            border: 0,
            "& .MuiDataGrid-main": {
              borderColor: "divider",
            },
            "& .MuiDataGrid-columnHeaders": {
              bgcolor: "action.hover",
              borderBottom: 1,
              borderColor: "divider",
            },
            "& .MuiDataGrid-cell": {
              borderColor: "divider",
              display: "flex !important",
              alignItems: "center !important",
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
      <ConfirmationDialog
        open={confirmation.open}
        title="Remove Affected Item"
        message="Are you sure you want to remove this item from the ECO?"
        onClose={() => setConfirmation({ open: false, itemId: null })}
        onConfirm={() => {
          if (confirmation.itemId) {
            onRemoveItem(confirmation.itemId);
          }
          setConfirmation({ open: false, itemId: null });
        }}
        color="error"
      />
    </Stack>
  );
}

function AddAffectedItemDialog({
  open,
  isPending,
  onClose,
  onConfirm,
}: {
  open: boolean;
  isPending: boolean;
  onClose: () => void;
  onConfirm: (item: AddAffectedItemRequest) => void;
}) {
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

  return <Chip label={labelByAction[action]} size="small" sx={{ ...sxByAction[action], border: 1, borderColor: "divider" }} />;
}

function FilesTab({
  attachments,
  ecoId,
  usersById,
}: {
  attachments: EcoAttachmentDto[];
  ecoId: string;
  usersById: Map<string, EcoUserDto>;
}) {
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
                  border: 1,
                  borderColor: "divider",
                  "&:hover": { bgcolor: "action.hover" },
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

function AttachmentDrawer({
  attachment,
  ecoId,
  uploader,
  onClose,
}: {
  attachment: EcoAttachmentDto | null;
  ecoId: string;
  uploader: EcoUserDto | undefined;
  onClose: () => void;
}) {
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
      slotProps={{ paper: { sx: { width: { xs: "100%", sm: 500 }, borderLeft: 1, borderColor: "divider" } } }}
    >
      <Stack spacing={2.5} sx={{ p: 3 }}>
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
            <Stack spacing={1.5}>
              <Typography variant="subtitle1" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>
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
                  sx={{ fontFamily: "monospace", overflowWrap: "anywhere", fontSize: "0.8rem", color: "text.secondary" }}
                >
                  {attachment.objectKey}
                </Typography>
              </DetailRow>
            </Stack>
            {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}
            <Button
              type="button"
              variant="contained"
              size="large"
              fullWidth
              startIcon={isDownloading ? undefined : <DownloadIcon />}
              disabled={isDownloading}
              onClick={() => void handleDownload()}
              sx={{ textTransform: "none", mt: 2, fontWeight: 600 }}
            >
              {isDownloading ? (
                <CircularProgress color="inherit" size={24} thickness={5} />
              ) : (
                "Download File"
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
      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: "uppercase", letterSpacing: 0.5 }}>
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
