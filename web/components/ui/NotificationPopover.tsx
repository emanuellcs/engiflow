"use client";

import ChatBubbleOutlinedIcon from "@mui/icons-material/ChatBubbleOutlined";
import DoneAllIcon from "@mui/icons-material/DoneAll";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import NotificationsOutlinedIcon from "@mui/icons-material/NotificationsOutlined";
import PriorityHighIcon from "@mui/icons-material/PriorityHigh";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import List from "@mui/material/List";
import ListItemAvatar from "@mui/material/ListItemAvatar";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { alpha, useTheme } from "@mui/material/styles";
import { useRouter } from "next/navigation";
import type { NotificationDto, NotificationCategory } from "@/components/security/useNotificationHub";
import { useTranslation } from "@/context/I18nContext";

/**
 * Describes the props accepted by the notification popover component.
 */
interface NotificationPopoverProps {
  /** The anchor element for the menu positioning. */
  anchorEl: HTMLElement | null;
  /** True if the popover is currently visible. */
  open: boolean;
  /** Callback invoked when the popover should be dismissed. */
  onClose: () => void;
  /** The list of notifications to render. */
  notifications: NotificationDto[];
  /** Callback invoked when a specific notification is marked as read. */
  onMarkAsRead: (id: string) => Promise<void>;
  /** Callback invoked when all unread notifications are marked as read. */
  onMarkAllAsRead: () => Promise<void>;
}

/**
 * Renders the real-time notification stream in a Topbar-anchored menu.
 * Provides categorized alerts with deep-linking support and unread tracking.
 */
export default function NotificationPopover({
  anchorEl,
  open,
  onClose,
  notifications,
  onMarkAsRead,
  onMarkAllAsRead,
}: NotificationPopoverProps) {
  const { t } = useTranslation();
  const theme = useTheme();
  const router = useRouter();

  /**
   * Handles a notification item interaction.
   * Marks as read, navigates to the deep link if present, and closes the popover.
   */
  const handleItemClick = async (notification: NotificationDto) => {
    onClose();
    if (!notification.isRead) {
      void onMarkAsRead(notification.id);
    }
    if (notification.deepLink) {
      router.push(notification.deepLink);
    }
  };

  return (
    <Menu
      anchorEl={anchorEl}
      open={open}
      onClose={onClose}
      transformOrigin={{ horizontal: "right", vertical: "top" }}
      anchorOrigin={{ horizontal: "right", vertical: "bottom" }}
      slotProps={{
        paper: {
          sx: {
            bgcolor: "#ffffff",
            width: 360,
            mt: 1,
            borderRadius: 2,
            boxShadow: theme.shadows[3],
            maxHeight: 500,
            display: "flex",
            flexDirection: "column",
          },
        },
      }}
    >
      <Box
        sx={{
          px: 2,
          py: 1.5,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          borderBottom: 1,
          borderColor: "divider",
        }}
      >
        <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
          {t("notifications.title")}
        </Typography>
        {notifications.some((n) => !n.isRead) && (
          <Button
            size="small"
            variant="text"
            startIcon={<DoneAllIcon sx={{ fontSize: "1rem" }} />}
            onClick={onMarkAllAsRead}
            sx={{ fontSize: "0.7rem", fontWeight: 700, textTransform: "none" }}
          >
            {t("notifications.markAllRead")}
          </Button>
        )}
      </Box>

      <Box sx={{ flex: 1, overflowY: "auto", minHeight: 100 }}>
        {notifications.length === 0 ? (
          <Box
            sx={{
              py: 6,
              textAlign: "center",
              bgcolor: "transparent",
            }}
          >
            <NotificationsOutlinedIcon
              sx={{ fontSize: 40, color: "text.disabled", mb: 1, opacity: 0.5 }}
            />
            <Typography variant="body2" color="text.secondary">
              {t("notifications.emptyMessage")}
            </Typography>
            <Typography variant="caption" color="text.disabled">
              {t("notifications.emptyDescription")}
            </Typography>
          </Box>
        ) : (
          <List disablePadding>
            {notifications.map((notification, index) => {
              const { title, message } = localizeNotification(notification, t);
              return (
                <ListItemButton
                  key={notification.id}
                  onClick={() => void handleItemClick(notification)}
                  divider={index < notifications.length - 1}
                  sx={{
                    py: 1.5,
                    alignItems: "flex-start",
                    bgcolor: notification.isRead
                      ? "transparent"
                      : alpha(theme.palette.primary.main, 0.03),
                    "&:hover": {
                      bgcolor: notification.isRead
                        ? alpha(theme.palette.action.hover, 0.5)
                        : alpha(theme.palette.primary.main, 0.06),
                    },
                  }}
                >
                  <ListItemAvatar sx={{ minWidth: 40 }}>
                    <CategoryIcon category={notification.category} isRead={notification.isRead} />
                  </ListItemAvatar>
                  <ListItemText
                    primary={
                      <Typography
                        variant="body2"
                        sx={{
                          fontWeight: notification.isRead ? 500 : 700,
                          color: notification.isRead ? "text.secondary" : "text.primary",
                          lineHeight: 1.3,
                        }}
                      >
                        {title}
                      </Typography>
                    }
                    secondary={
                      <Stack spacing={0.5} sx={{ mt: 0.5 }}>
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{
                            display: "-webkit-box",
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: "vertical",
                            overflow: "hidden",
                            lineHeight: 1.4,
                          }}
                        >
                          {message}
                        </Typography>
                        <Typography
                          variant="caption"
                          color="text.disabled"
                          sx={{ fontSize: "0.65rem", fontWeight: 600 }}
                        >
                          {t("notifications.timeAgo", { time: formatDistanceToNow(new Date(notification.createdAt), t) })}
                        </Typography>
                      </Stack>
                    }
                  />
                  {!notification.isRead && (
                    <Box
                      sx={{
                        width: 8,
                        height: 8,
                        bgcolor: "primary.main",
                        borderRadius: "50%",
                        mt: 1,
                        ml: 1,
                        flexShrink: 0,
                      }}
                    />
                  )}
                </ListItemButton>
              );
            })}
          </List>
        )}
      </Box>
    </Menu>
  );
}

/**
 * Renders a specific icon and color based on the notification category.
 */
function CategoryIcon({
  category,
  isRead,
}: {
  category: NotificationCategory;
  isRead: boolean;
}) {
  const iconSx = { fontSize: 20, opacity: isRead ? 0.5 : 1 };

  switch (category) {
    case "ActionRequired":
      return <PriorityHighIcon sx={{ ...iconSx, color: "error.main" }} />;
    case "Update":
      return <InfoOutlinedIcon sx={{ ...iconSx, color: "info.main" }} />;
    case "Activity":
      return <ChatBubbleOutlinedIcon sx={{ ...iconSx, color: "success.main" }} />;
    default:
      return <InfoOutlinedIcon sx={{ ...iconSx }} />;
  }
}

/**
 * Simple formatter to show time distance from now.
 */
function formatDistanceToNow(date: Date, t: (key: string, params?: Record<string, string | number>) => string): string {
  const seconds = Math.floor((new Date().getTime() - date.getTime()) / 1000);
  if (seconds < 60) return t("notifications.time.seconds", { count: Math.max(0, seconds) });
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return t("notifications.time.minutes", { count: minutes });
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return t("notifications.time.hours", { count: hours });
  const days = Math.floor(hours / 24);
  return t("notifications.time.days", { count: days });
}

function localizeNotification(
  notification: NotificationDto, 
  t: (key: string, params?: Record<string, string | number>) => string
): { title: string; message: string } {
  let title = notification.title;
  let message = notification.message;

  // Title Patterns
  const newCommentRegex = /^New Comment on (.+)$/;
  const submittedRegex = /^(.+) Submitted for Review$/;
  const changesRequestedRegex = /^Changes Requested for (.+)$/;
  const approvedRegex = /^(.+) Approved$/;
  const reviewDecisionRegex = /^New Review Decision on (.+)$/;

  const newCommentMatch = title.match(newCommentRegex);
  if (newCommentMatch) title = t("notifications.dynamic.newComment.title", { title: newCommentMatch[1] });

  const submittedMatch = title.match(submittedRegex);
  if (submittedMatch) title = t("notifications.dynamic.submitted.title", { title: submittedMatch[1] });

  const changesRequestedMatch = title.match(changesRequestedRegex);
  if (changesRequestedMatch) title = t("notifications.dynamic.changesRequested.title", { title: changesRequestedMatch[1] });

  const approvedMatch = title.match(approvedRegex);
  if (approvedMatch) title = t("notifications.dynamic.approved.title", { title: approvedMatch[1] });

  const reviewDecisionMatch = title.match(reviewDecisionRegex);
  if (reviewDecisionMatch) title = t("notifications.dynamic.reviewDecision.title", { title: reviewDecisionMatch[1] });

  // Message Patterns (Static matching)
  const messageMap: Record<string, string> = {
    "A reviewer or teammate added a new comment to your ECO timeline.": t("notifications.dynamic.newComment.message"),
    "An ECO has been moved into the formal review phase and is awaiting approval.": t("notifications.dynamic.submitted.message"),
    "An approver has requested changes. Review their feedback and resubmit when ready.": t("notifications.dynamic.changesRequested.message"),
    "Your ECO has reached quorum and has been formally approved.": t("notifications.dynamic.approved.message"),
    "A team member has submitted their decision for the current review round.": t("notifications.dynamic.reviewDecision.message"),
  };

  if (messageMap[message]) {
    message = messageMap[message];
  }

  return { title, message };
}
