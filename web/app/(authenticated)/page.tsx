"use client";

import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControl from "@mui/material/FormControl";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Select from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { alpha, useTheme } from "@mui/material/styles";
import RefreshIcon from "@mui/icons-material/Refresh";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import AddIcon from "@mui/icons-material/Add";
import SettingsIcon from "@mui/icons-material/Settings";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import MuiLink from "@mui/material/Link";

import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import { getDashboardData, type DashboardData } from "@/lib/api/dashboard";
import NextLink from "@/components/ui/NextLink";
import DataGridEmptyState from "@/components/ui/DataGridEmptyState";
import { useTranslation } from "@/context/I18nContext";

const RECENTLY_VIEWED_STORAGE_KEY = "engiflow.recently_viewed.ecos";

type RecentlyViewedItem = {
  id: string;
  title: string;
  timestamp: string;
};

/**
 * Renders the role-based dashboard for authenticated users.
 * Aggregates high-level metrics, actionable items (Action Hub), workflow
 * utility, quick access, and role-tailored metrics.
 *
 * @returns The authenticated dashboard view.
 */
export default function DashboardPage() {
  const { t, locale } = useTranslation();
  const { user } = useAuth();
  const theme = useTheme();
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [tabValue, setTabValue] = useState(0);
  const [recentlyViewed] = useState<RecentlyViewedItem[]>(() => {
    if (typeof window === "undefined") return [];
    const stored = localStorage.getItem(RECENTLY_VIEWED_STORAGE_KEY);
    if (stored) {
      try {
        return JSON.parse(stored);
      } catch {
        return [];
      }
    }
    return [];
  });

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return t("dashboard.greeting.morning");
    if (hour < 18) return t("dashboard.greeting.afternoon");
    return t("dashboard.greeting.evening");
  };

  const isViewer = user?.role === "Viewer";

  const fetchDashboard = async (signal?: AbortSignal) => {
    try {
      const dashboardData = await getDashboardData(signal);
      setData(dashboardData);
    } catch (error) {
      if (!(error instanceof Error && error.name === "AbortError")) {
        console.error("Failed to fetch dashboard data:", error);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    
    getDashboardData(controller.signal)
      .then((dashboardData) => {
        setData(dashboardData);
      })
      .catch((error) => {
        if (!(error instanceof Error && error.name === "AbortError")) {
          console.error("Failed to fetch dashboard data:", error);
        }
      })
      .finally(() => {
        setLoading(false);
      });

    return () => controller.abort();
  }, [user?.tenantId]);

  if (loading && !data) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Stack component="div" spacing={3} sx={{ py: 1 }}>
      {/* Greeting & Refresh Block */}
      <Stack 
        component="div" 
        direction="row" 
        spacing={2} 
        sx={{ alignItems: "center", justifyContent: "space-between" }}
      >
        <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
          <Avatar
            sx={{
              width: 56,
              height: 56,
              bgcolor: "primary.main",
              fontSize: "1.25rem",
              fontWeight: 700,
              boxShadow: 2,
            }}
          >
            {user?.userName?.[0]?.toUpperCase() || "U"}
          </Avatar>
          <Box sx={{ flexGrow: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Typography variant="h5" sx={{ fontWeight: 600 }}>
                {getGreeting()}, {user?.userName}
              </Typography>
              {isViewer && (
                <Chip 
                  label={t("common.readOnly")} 
                  size="small" 
                  variant="outlined" 
                  color="info" 
                  icon={<InfoOutlinedIcon sx={{ fontSize: "0.875rem !important" }} />}
                />
              )}
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {isViewer ? t("dashboard.actionHub.emptyStateActivity") : t("dashboard.greeting.welcome")}
            </Typography>
          </Box>
        </Stack>

        <Box>
          <Tooltip title={t("dashboard.metrics.refresh")}>
            <span>
              <IconButton 
                onClick={() => void fetchDashboard()} 
                disabled={loading}
                color="primary"
                size="small" 
                aria-label={t("common.refresh")}
                sx={{
                  border: 1,
                  borderColor: "divider",
                  bgcolor: "background.paper",
                  width: 36,
                  height: 36,
                  "&:hover": {
                    bgcolor: "action.hover",
                  }
                }}
              >
                {loading ? (
                  <CircularProgress size={20} color="inherit" thickness={5} />
                ) : (
                  <RefreshIcon fontSize="small" />
                )}
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      </Stack>

      {isViewer && (
        <Alert severity="info" variant="outlined" sx={{ borderStyle: "dashed" }}>
          {t("dashboard.greeting.viewerAlert")}
        </Alert>
      )}

      {/* Metric Row */}
      {data?.metrics && data.metrics.length > 0 && (
        <Grid container spacing={2.5}>
          {data.metrics.map((metric, index) => (
            <Grid key={index} size={{ xs: 12, sm: 6, md: 3 }}>
              <Card
                elevation={0}
                sx={{
                  bgcolor: "background.paper",
                  border: `1px solid ${theme.palette.divider}`,
                  height: "100%",
                }}
              >
                <CardContent>
                  <Typography
                    variant="h3"
                    sx={{ fontWeight: 700, mb: 0.5, lineHeight: 1 }}
                  >
                    {metric.value}
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{ fontWeight: 500, color: "text.primary", mb: 1 }}
                  >
                    {getMetricLabel(metric.label, t)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {getMetricFooter(metric.footer, t)}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* Main Content Grid */}
      <Grid container spacing={3}>
        {/* Action Hub */}
        <Grid size={{ xs: 12, md: 8 }}>
          <Card
            elevation={0}
            sx={{
              height: "100%",
              display: "flex",
              flexDirection: "column",
              border: `1px solid ${theme.palette.divider}`,
            }}
          >
            <Box
              sx={{
                p: 2,
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                borderBottom: `1px solid ${theme.palette.divider}`,
              }}
            >
              <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                {isViewer ? t("dashboard.actionHub.activityTitle") : t("dashboard.actionHub.title")}
              </Typography>
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <Select value="Everything" disabled variant="outlined">
                  <MenuItem value="Everything">{t("dashboard.actionHub.filterEverything")}</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <Box sx={{ flexGrow: 1 }}>
              {data?.attentionItems.length === 0 ? (
                <Stack
                  component="div"
                  spacing={2}
                  sx={{ 
                    py: 8, 
                    px: 2, 
                    textAlign: "center",
                    alignItems: "center",
                    justifyContent: "center"
                  }}
                >
                  <Box
                    sx={{
                      width: 64,
                      height: 64,
                      borderRadius: "50%",
                      bgcolor: alpha(theme.palette.success.main, 0.1),
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      mb: 1,
                    }}
                  >
                    <CheckCircleOutlinedIcon
                      color="success"
                      sx={{ fontSize: 40 }}
                    />
                  </Box>
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 600 }}>
                      {isViewer ? t("dashboard.actionHub.emptyStateActivity") : t("dashboard.actionHub.emptyState")}
                    </Typography>
                    <MuiLink
                      component={NextLink}
                      href="/ecos"
                      sx={{
                        mt: 2,
                        display: "inline-block",
                        color: "primary.main",
                        textDecoration: "none",
                        fontWeight: 500,
                        "&:hover": { textDecoration: "underline" },
                      }}
                    >
                      {isViewer ? t("dashboard.actionHub.browseEcos") : t("dashboard.actionHub.viewAll")}
                    </MuiLink>
                  </Box>
                </Stack>
              ) : (
                <Stack component="div" divider={<Divider />}>
                  {data?.attentionItems.map((item, index) => (
                    <Box
                      key={`${item.id}-${index}`}
                      component={NextLink}
                      href={item.deepLink}
                      sx={{
                        p: 2,
                        display: "block",
                        textDecoration: "none",
                        color: "inherit",
                        transition: "background-color 0.2s",
                        "&:hover": {
                          bgcolor: alpha(theme.palette.action.hover, 0.04),
                        },
                      }}
                    >
                      <Typography
                        variant="body1"
                        sx={{ fontWeight: 500, color: "primary.main" }}
                      >
                        {localizeEventDescription(item.title, t)}
                      </Typography>
                      <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                          {localizeAttentionSubtitle(item.subtitle, t)}
                        </Typography>
                        {item.occurredAt && (
                           <Typography variant="caption" color="text.secondary" sx={{ opacity: 0.8 }}>
                             • {new Date(item.occurredAt).toLocaleString(locale === "pt-BR" ? "pt-BR" : "en-US", {
                                dateStyle: "medium",
                                timeStyle: "short"
                             })}
                           </Typography>
                        )}
                      </Box>
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
          </Card>
        </Grid>

        {/* Quick Access Hub */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Card
            elevation={0}
            sx={{
              height: "100%",
              display: "flex",
              flexDirection: "column",
              border: `1px solid ${theme.palette.divider}`,
            }}
          >
            <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
              <Tabs
                value={tabValue}
                onChange={(_, v) => setTabValue(v)}
                aria-label="quick access tabs"
              >
                <Tab label={t("dashboard.quickAccess.recentlyViewed")} sx={{ textTransform: "none" }} />
                {!isViewer && <Tab label={t("dashboard.quickAccess.quickActions")} sx={{ textTransform: "none" }} />}
              </Tabs>
            </Box>
            <Box sx={{ p: 2, flexGrow: 1 }}>
              {tabValue === 0 && (
                <Stack component="div" spacing={recentlyViewed.length === 0 ? 2 : 1} sx={{ height: "100%", justifyContent: recentlyViewed.length === 0 ? "center" : "flex-start" }}>
                  {recentlyViewed.length === 0 ? (
                    <>
                      <DataGridEmptyState
                        icon={<HistoryOutlinedIcon sx={{ fontSize: 48, color: "grey.400" }} />}
                        message={t("dashboard.quickAccess.noHistory")}
                        description={t("dashboard.quickAccess.noHistoryDesc")}
                      />
                      <Box sx={{ textAlign: "center", pb: 2 }}>
                        <Button 
                          component={NextLink} 
                          href="/ecos" 
                          variant="outlined" 
                          size="small" 
                          sx={{ textTransform: "none" }}
                        >
                          {t("dashboard.actionHub.browseEcos")}
                        </Button>
                      </Box>
                    </>
                  ) : (
                    <Stack component="div" divider={<Divider />}>
                      {recentlyViewed.map((item) => (
                        <Box
                          key={item.id}
                          component={NextLink}
                          href={`/ecos/${item.id}`}
                          sx={{
                            py: 1.5,
                            px: 1,
                            display: "block",
                            textDecoration: "none",
                            color: "inherit",
                            borderRadius: 1,
                            "&:hover": {
                              bgcolor: alpha(theme.palette.action.hover, 0.04),
                            },
                          }}
                        >
                          <Typography variant="body2" sx={{ fontWeight: 600, color: "primary.main", mb: 0.5 }}>
                            {item.title}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {t("dashboard.quickAccess.viewedAt", {
                               timestamp: new Date(item.timestamp).toLocaleString(locale === "pt-BR" ? "pt-BR" : "en-US", { 
                                  dateStyle: 'medium', 
                                  timeStyle: 'short' 
                               })
                            })}
                          </Typography>
                        </Box>
                      ))}
                    </Stack>
                  )}
                </Stack>
              )}
              {tabValue === 1 && !isViewer && (
                <Stack component="div" spacing={1.5} sx={{ p: 1 }}>
                  {user?.role && (user.role === "Owner" || user.role === "Administrator" || user.role === "Requester") && (
                    <Button 
                      component={NextLink} 
                      href="/ecos/new" 
                      variant="contained" 
                      startIcon={<AddIcon />}
                      sx={{ textTransform: "none", justifyContent: "flex-start" }}
                    >
                      {t("topbar.actions.newEco")}
                    </Button>
                  )}
                  {isAdminOrOwner(user?.role) && (
                    <Button 
                      component={NextLink} 
                      href="/settings" 
                      variant="outlined" 
                      color="secondary"
                      startIcon={<SettingsIcon />}
                      sx={{ textTransform: "none", justifyContent: "flex-start" }}
                    >
                      {t("dashboard.quickAccess.workspaceSettings")}
                    </Button>
                  )}
                </Stack>
              )}
            </Box>
          </Card>
        </Grid>
      </Grid>
    </Stack>
  );
}

function getMetricLabel(label: string, t: (key: string) => string): string {
  const map: Record<string, string> = {
    "Waiting for your review": t("dashboard.metrics.waitingForReview"),
    "Authored by you": t("dashboard.metrics.authoredByYou"),
    "Assigned to you": t("dashboard.metrics.assignedToYou"),
    "SLA At Risk (>5 days)": t("dashboard.metrics.slaAtRisk"),
    "Open Orders Count": t("dashboard.metrics.openOrders"),
    "Pending Activations": t("dashboard.metrics.pendingActivations"),
  };
  return map[label] || label;
}

function getMetricFooter(footer: string, t: (key: string) => string): string {
  const map: Record<string, string> = {
    "Just now": t("dashboard.metrics.footers.justNow"),
    "Across the workspace": t("dashboard.metrics.footers.acrossWorkspace"),
  };
  return map[footer] || footer;
}

function localizeAttentionSubtitle(subtitle: string, t: (key: string, params?: Record<string, string | number>) => string): string {
  // Pattern 1: "{priority} priority • {status}"
  const priorityStatusRegex = /^(\w+) priority • ([\w\s]+)$/;
  const priorityStatusMatch = subtitle.match(priorityStatusRegex);
  if (priorityStatusMatch) {
    const priority = priorityStatusMatch[1];
    const status = priorityStatusMatch[2];
    return t("dashboard.actionHub.subtitles.priorityStatus", {
      priority: t(`priority.${priority.toLowerCase()}`) || priority,
      status: t(`status.${camelCase(status)}`) || status,
    });
  }

  // Pattern 2: "Status: {status} • Priority: {priority}"
  const statusPriorityRegex = /^Status: (\w+) • Priority: (\w+)$/;
  const statusPriorityMatch = subtitle.match(statusPriorityRegex);
  if (statusPriorityMatch) {
    const status = statusPriorityMatch[1];
    const priority = statusPriorityMatch[2];
    return t("dashboard.actionHub.subtitles.statusPriority", {
      status: t(`status.${camelCase(status)}`) || status,
      priority: t(`priority.${priority.toLowerCase()}`) || priority,
    });
  }

  // Pattern 3: "Activity by {name} • {title}"
  const viewerActivityRegex = /^Activity by (.+) • (.+)$/;
  const viewerActivityMatch = subtitle.match(viewerActivityRegex);
  if (viewerActivityMatch) {
    const name = viewerActivityMatch[1];
    const title = viewerActivityMatch[2];
    return t("dashboard.actionHub.subtitles.viewerActivity", { name, title });
  }

  return subtitle;
}

function localizeEventDescription(description: string, t: (key: string, params?: Record<string, string | number>) => string): string {
  const activityMap: Record<string, string> = {
    "ECO created.": "dashboard.actionHub.activity.created",
    "ECO details updated.": "dashboard.actionHub.activity.updated",
    "Comment added.": "dashboard.actionHub.activity.commentAdded",
    "ECO approved by quorum.": "dashboard.actionHub.activity.quorumApproved",
    "ECO canceled.": "dashboard.actionHub.activity.canceled",
    "ECO returned to draft because changes were requested.": "dashboard.actionHub.activity.returned"
  };

  if (activityMap[description]) {
    return t(activityMap[description]);
  }

  // Parameterized Patterns
  const itemAddedRegex = /^Affected item '(.+)' added\.$/;
  const itemRemovedRegex = /^Affected item '(.+)' removed\.$/;
  const attachmentAddedRegex = /^Attachment '(.+)' added\.$/;
  const submittedRegex = /^ECO submitted for review round (\d+)\.$/;
  const approvedRegex = /^Approval submitted for review round (\d+)\.$/;
  const requestedRegex = /^Changes requested for review round (\d+)\.$/;

  const itemAddedMatch = description.match(itemAddedRegex);
  if (itemAddedMatch) return t("dashboard.actionHub.activity.itemAdded", { part: itemAddedMatch[1] });

  const itemRemovedMatch = description.match(itemRemovedRegex);
  if (itemRemovedMatch) return t("dashboard.actionHub.activity.itemRemoved", { part: itemRemovedMatch[1] });

  const attachmentAddedMatch = description.match(attachmentAddedRegex);
  if (attachmentAddedMatch) return t("dashboard.actionHub.activity.attachmentAdded", { file: attachmentAddedMatch[1] });

  const submittedMatch = description.match(submittedRegex);
  if (submittedMatch) return t("dashboard.actionHub.activity.submitted", { round: submittedMatch[1] });

  const approvedMatch = description.match(approvedRegex);
  if (approvedMatch) return t("dashboard.actionHub.activity.approved", { round: approvedMatch[1] });

  const requestedMatch = description.match(requestedRegex);
  if (requestedMatch) return t("dashboard.actionHub.activity.requested", { round: requestedMatch[1] });

  return description;
}

function camelCase(str: string): string {
  return str
    .replace(/(?:^\w|[A-Z]|\b\w)/g, (word, index) =>
      index === 0 ? word.toLowerCase() : word.toUpperCase()
    )
    .replace(/\s+/g, "");
}
