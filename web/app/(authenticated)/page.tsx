"use client";

import { useEffect, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Avatar from "@mui/material/Avatar";
import Divider from "@mui/material/Divider";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import FormControl from "@mui/material/FormControl";
import CheckCircleOutlinedIcon from "@mui/icons-material/CheckCircleOutlined";
import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import AddIcon from "@mui/icons-material/Add";
import SettingsIcon from "@mui/icons-material/Settings";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import CircularProgress from "@mui/material/CircularProgress";
import MuiLink from "@mui/material/Link";
import Alert from "@mui/material/Alert";
import Chip from "@mui/material/Chip";
import Tooltip from "@mui/material/Tooltip";
import IconButton from "@mui/material/IconButton";
import RefreshIcon from "@mui/icons-material/Refresh";
import { useTheme, alpha } from "@mui/material/styles";

import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import { getDashboardData, type DashboardData } from "@/lib/api/dashboard";
import NextLink from "@/components/ui/NextLink";
import DataGridEmptyState from "@/components/ui/DataGridEmptyState";

const RECENTLY_VIEWED_STORAGE_KEY = "engiflow.recently_viewed.ecos";

type RecentlyViewedItem = {
  id: string;
  title: string;
  timestamp: string;
};

/**
 * Renders the role-based multi-tenant dashboard.
 *
 * This page mimics the GitLab dashboard aesthetic, focusing on text-dense
 * utility, quick access, and role-tailored metrics.
 *
 * @returns The authenticated dashboard view.
 */
export default function DashboardPage() {
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
    if (hour < 12) return "Good morning";
    if (hour < 18) return "Good afternoon";
    return "Good evening";
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
        sx={{ 
          alignItems: "center", 
          justifyContent: "space-between",
          flexWrap: "wrap",
          gap: 2
        }}
      >
        <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
          <Avatar
            sx={{
              width: 48,
              height: 48,
              bgcolor: theme.palette.primary.main,
              fontSize: "1.25rem",
            }}
          >
            {user?.userName.charAt(0)}
          </Avatar>
          <Box sx={{ flexGrow: 1 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
              <Typography variant="h5" sx={{ fontWeight: 600 }}>
                {getGreeting()}, {user?.userName}
              </Typography>
              {isViewer && (
                <Chip 
                  label="Read-only" 
                  size="small" 
                  variant="outlined" 
                  color="info" 
                  icon={<InfoOutlinedIcon sx={{ fontSize: "0.875rem !important" }} />}
                />
              )}
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {isViewer ? "Explore the workspace activity feed below." : "Welcome to EngiFlow"}
            </Typography>
          </Box>
        </Stack>

        <Box>
          <Tooltip title="Refresh dashboard data">
            <span>
              <IconButton 
                onClick={() => void fetchDashboard()} 
                disabled={loading}
                color="primary"
                size="small" 
                aria-label="refresh"
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
          You have <strong>Viewer</strong> access to this workspace. You can browse ECOs and history but cannot perform mutations or approvals.
        </Alert>
      )}

      {/* Metric Row */}
      {data?.metrics && data.metrics.length > 0 && (
        <Grid container spacing={2}>
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
                    {metric.label}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {metric.footer}
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
                {isViewer ? "Recent Workspace Activity" : "Items that need your attention"}
              </Typography>
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <Select value="Everything" disabled variant="outlined">
                  <MenuItem value="Everything">Everything</MenuItem>
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
                      {isViewer ? "No activity found in this workspace." : "Good job! All your to-do items are done."}
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
                      {isViewer ? "Browse ECOs" : "All to-do items"}
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
                        {item.title}
                      </Typography>
                      <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                          {item.subtitle}
                        </Typography>
                        {item.occurredAt && (
                           <Typography variant="caption" color="text.secondary" sx={{ opacity: 0.8 }}>
                             • {new Date(item.occurredAt).toLocaleString(undefined, {
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
                <Tab label="Recently viewed" sx={{ textTransform: "none" }} />
                {!isViewer && <Tab label="Quick Actions" sx={{ textTransform: "none" }} />}
              </Tabs>
            </Box>
            <Box sx={{ p: 2, flexGrow: 1 }}>
              {tabValue === 0 && (
                <Stack component="div" spacing={recentlyViewed.length === 0 ? 2 : 1} sx={{ height: "100%", justifyContent: recentlyViewed.length === 0 ? "center" : "flex-start" }}>
                  {recentlyViewed.length === 0 ? (
                    <>
                      <DataGridEmptyState
                        icon={<HistoryOutlinedIcon sx={{ fontSize: 48, color: "grey.400" }} />}
                        message="No recent history"
                        description="You haven't viewed any ECOs recently."
                      />
                      <Box sx={{ textAlign: "center", pb: 2 }}>
                        <Button 
                          component={NextLink} 
                          href="/ecos" 
                          variant="outlined" 
                          size="small" 
                          sx={{ textTransform: "none" }}
                        >
                          Browse all ECOs
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
                            Viewed {new Date(item.timestamp).toLocaleString(undefined, { 
                              dateStyle: 'medium', 
                              timeStyle: 'short' 
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
                      New Engineering Change Order
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
                      Workspace Settings
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
