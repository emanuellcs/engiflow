"use client";

import AccountCircleOutlinedIcon from "@mui/icons-material/AccountCircleOutlined";
import AddIcon from "@mui/icons-material/Add";
import AssignmentIcon from "@mui/icons-material/Assignment";
import BusinessIcon from "@mui/icons-material/Business";
import BusinessOutlinedIcon from "@mui/icons-material/BusinessOutlined";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import DashboardIcon from "@mui/icons-material/Dashboard";
import ExpandLess from "@mui/icons-material/ExpandLess";
import ExpandMore from "@mui/icons-material/ExpandMore";
import GitHubIcon from "@mui/icons-material/GitHub";
import GroupIcon from "@mui/icons-material/Group";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import LogoutIcon from "@mui/icons-material/Logout";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import MenuIcon from "@mui/icons-material/Menu";
import NotificationsOutlinedIcon from "@mui/icons-material/NotificationsOutlined";
import PeopleOutlinedIcon from "@mui/icons-material/PeopleOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SettingsIcon from "@mui/icons-material/Settings";
import UnfoldMoreOutlinedIcon from "@mui/icons-material/UnfoldMoreOutlined";
import AppBar from "@mui/material/AppBar";
import Avatar from "@mui/material/Avatar";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Breadcrumbs from "@mui/material/Breadcrumbs";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Collapse from "@mui/material/Collapse";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";

import { alpha, useTheme } from "@mui/material/styles";
import Toolbar from "@mui/material/Toolbar";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { usePathname } from "next/navigation";
import type { PropsWithChildren, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import TenantList from "@/components/auth/TenantList";
import { useEcoHub } from "@/components/ecos/useEcoHub";
import { useNotificationHub } from "@/components/security/useNotificationHub";
import { useSecurityHub } from "@/components/security/useSecurityHub";
import NextLink from "@/components/ui/NextLink";
import NotificationPopover from "@/components/ui/NotificationPopover";
import CommandPalette from "@/components/ui/CommandPalette";
import { apiFetch } from "@/lib/api/client";
import { type AuthSessionResult, useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import { getRememberMe } from "@/lib/auth/token-storage";
import { type WorkspaceTenantOption } from "./auth/LoginForm";

const drawerWidth = 240;
const collapsedDrawerWidth = 64;
const requesterRole = "Requester";

/**
 * Represents a single navigation item in the sidebar.
 */
type NavigationItem = {
  label: string;
  href: string;
  icon: ReactNode;
  administratorOnly?: boolean;
  subItems?: NavigationItem[];
  badgeCount?: number;
};

/**
 * Properties for the NavigationDrawerContent component.
 */
type NavigationDrawerContentProps = {
  pathname: string;
  userName: string;
  role: string;
  isAdministrator: boolean;
  isExpanded: boolean;
  navigationItems: NavigationItem[];
  onNavigate: () => void;
  onLogout: () => void;
  onAboutOpen: () => void;
  onDrawerToggle?: () => void;
};

export default function AppShell({ children }: PropsWithChildren) {
  const theme = useTheme();
  const pathname = usePathname() ?? "/";
  const { login, logout, token, user } = useAuth();
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const [ecoCount, setEcoCount] = useState<number>(0);

  // Real-time Notification Hub
  const {
    notifications,
    unreadCount,
    markAsRead,
    markAllAsRead,
  } = useNotificationHub({
    token,
    tenantId: user?.tenantId ?? null,
  });

  // Workspace Switcher State
  const [workspaceAnchorEl, setWorkspaceAnchorEl] = useState<null | HTMLElement>(null);
  const [tenants, setTenants] = useState<WorkspaceTenantOption[]>([]);
  const [isTenantsLoading, setIsTenantsLoading] = useState(false);
  const [switchingTenantId, setSwitchingTenantId] = useState<string | null>(null);

  // Search Dialog State
  const [isSearchOpen, setIsSearchOpen] = useState(false);

  // Quick Actions State
  const [actionsAnchorEl, setActionsAnchorEl] = useState<null | HTMLElement>(null);
  const [notificationsAnchorEl, setNotificationsAnchorEl] = useState<null | HTMLElement>(null);

  const isAdministrator = isAdminOrOwner(user?.role);
  const companyName = user?.companyName ?? "Workspace";
  const userName = user?.userName ?? "User";
  const role = user?.role ?? "User";
  const canCreateEco = isAdministrator || role === requesterRole;

  const currentDrawerWidth = isExpanded ? drawerWidth : collapsedDrawerWidth;

  // Keyboard shortcut for Command Palette
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key === "k") {
        event.preventDefault();
        setIsSearchOpen(true);
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  /**
   * Opens the workspace switcher menu and fetches available tenants if not already loaded.
   * @param event - The click event used to anchor the menu.
   */
  const handleWorkspaceOpen = async (event: React.MouseEvent<HTMLElement>) => {
    setWorkspaceAnchorEl(event.currentTarget);
    if (tenants.length === 0) {
      setIsTenantsLoading(true);
      try {
        const result = await apiFetch<WorkspaceTenantOption[]>("/api/auth/tenants");
        setTenants(result);
      } catch (error) {
        console.error("Failed to fetch tenants:", error);
      } finally {
        setIsTenantsLoading(false);
      }
    }
  };

  /**
   * Closes the workspace switcher menu.
   */
  const handleWorkspaceClose = () => {
    setWorkspaceAnchorEl(null);
  };

  /**
   * Switches the active authenticated session to a different tenant and refreshes the application.
   * @param tenantId - The target tenant identifier.
   */
  const handleTenantSelect = async (tenantId: string) => {
    if (tenantId === user?.tenantId) {
      handleWorkspaceClose();
      return;
    }

    setSwitchingTenantId(tenantId);
    const rememberMe = getRememberMe();

    try {
      const result = await apiFetch<AuthSessionResult>("/api/auth/switch-tenant", {
        method: "POST",
        body: { tenantId },
      });

      login(result, rememberMe);

      // Force a full hydration in the new workspace context.
      // This ensures all hooks (SWR, SignalR) are reset with the new tenant claims.
      window.location.assign("/");
    } catch (error) {
      console.error("Failed to switch tenant:", error);
      setSwitchingTenantId(null);
    }
  };

  /**
   * Opens the global search command palette.
   */
  const handleSearchOpen = () => setIsSearchOpen(true);

  /**
   * Closes the global search command palette.
   */
  const handleSearchClose = () => setIsSearchOpen(false);

  /**
   * Opens the global quick actions menu.
   * @param event - The click event used to anchor the menu.
   */
  const handleActionsOpen = (event: React.MouseEvent<HTMLElement>) => {
    setActionsAnchorEl(event.currentTarget);
  };

  /**
   * Closes the global quick actions menu.
   */
  const handleActionsClose = () => {
    setActionsAnchorEl(null);
  };

  /**
   * Opens the Topbar notifications menu.
   * @param event - The click event used to anchor the menu.
   */
  const handleNotificationsOpen = (event: React.MouseEvent<HTMLElement>) => {
    setNotificationsAnchorEl(event.currentTarget);
  };

  /**
   * Closes the Topbar notifications menu.
   */
  const handleNotificationsClose = () => {
    setNotificationsAnchorEl(null);
  };

  const fetchEcoCount = useCallback(async (isMounted: boolean) => {
    try {
      // Filter for 'UnderReview' status to only count actionable items
      const result = await apiFetch<{ totalCount: number }>("/api/ecos?status=UnderReview&pageSize=1");
      if (isMounted) {
        setEcoCount(result.totalCount);
      }
    } catch (error) {
      console.error("Failed to fetch ECO count:", error);
    }
  }, []);

  useEffect(() => {
    let isMounted = true;
    if (token) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      void fetchEcoCount(isMounted);
    }

    const handleUpdateCount = (event: Event) => {
      const customEvent = event as CustomEvent<{ count: number }>;
      if (isMounted) {
        setEcoCount(customEvent.detail.count);
      }
    };

    window.addEventListener("engiflow:update-eco-count", handleUpdateCount);

    return () => {
      isMounted = false;
      window.removeEventListener("engiflow:update-eco-count", handleUpdateCount);
    };
  }, [token, fetchEcoCount]);

  useSecurityHub({ token, currentUserId: user?.id });
  const handleEcoChanged = useCallback(() => {
    void fetchEcoCount(true);
  }, [fetchEcoCount]);

  useEcoHub({
    token,
    onEcoChanged: handleEcoChanged,
  });

  const handleDrawerToggle = () => {
    setIsExpanded((prev) => !prev);
  };

  const handleMobileDrawerOpen = () => {
    setIsMobileDrawerOpen(true);
  };

  const handleMobileDrawerClose = () => {
    setIsMobileDrawerOpen(false);
  };

  const handleAboutOpen = () => {
    setIsAboutOpen(true);
  };

  const handleAboutClose = () => {
    setIsAboutOpen(false);
  };

  const navigationItems = useMemo<NavigationItem[]>(
    () => [
      {
        label: "Dashboard",
        href: "/",
        icon: <DashboardIcon fontSize="small" />,
      },
      {
        label: "ECOs",
        href: "/ecos",
        icon: <AssignmentIcon fontSize="small" />,
        badgeCount: ecoCount,
      },
      {
        label: "Settings",
        href: "/settings",
        icon: <SettingsIcon fontSize="small" />,
        administratorOnly: true,
        subItems: [
          {
            label: "Team Management",
            href: "/settings/users",
            icon: <ManageAccountsIcon fontSize="small" />,
          },
          {
            label: "Workflow Policies",
            href: "/settings/workflow-policies",
            icon: <GroupIcon fontSize="small" />,
          },
        ],
      },
    ],
    [ecoCount],
  );

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          width: { md: `calc(100% - ${currentDrawerWidth}px)` },
          ml: { md: `${currentDrawerWidth}px` },
          borderBottom: 1,
          borderColor: "divider",
          zIndex: (theme) => ({
            xs: theme.zIndex.drawer - 1,
            md: theme.zIndex.drawer + 1,
          }),
          transition: theme.transitions.create(["width", "margin"], {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.leavingScreen,
          }),
        }}
      >
        <Toolbar
          variant="dense"
          sx={{
            minHeight: { xs: 56, md: 52 },
            gap: 1,
            px: { xs: 1, md: 2 },
          }}
        >
          <IconButton
            edge="start"
            aria-label="Open navigation"
            onClick={handleMobileDrawerOpen}
            sx={{ display: { md: "none" } }}
          >
            <MenuIcon />
          </IconButton>

          {/* Workspace Switcher */}
          <Button
            size="small"
            color="inherit"
            onClick={handleWorkspaceOpen}
            startIcon={<BusinessIcon fontSize="small" />}
            endIcon={<UnfoldMoreOutlinedIcon fontSize="small" />}
            sx={{
              textTransform: "none",
              fontWeight: 700,
              fontSize: "0.875rem",
              px: 1,
              minWidth: 0,
              display: { xs: "none", sm: "inline-flex" },
              maxWidth: { sm: 180, md: 240 },
              "& .MuiButton-startIcon": { mr: 0.75 },
              "& .MuiButton-endIcon": { ml: 0.5, color: "text.disabled" },
            }}
          >
            <Typography variant="inherit" noWrap>
              {companyName}
            </Typography>
          </Button>

          {/* Mobile Workspace Toggle */}
          <IconButton
            size="small"
            onClick={handleWorkspaceOpen}
            sx={{ display: { xs: "inline-flex", sm: "none" } }}
          >
            <BusinessIcon fontSize="small" />
          </IconButton>

          <Menu
            anchorEl={workspaceAnchorEl}
            open={Boolean(workspaceAnchorEl)}
            onClose={handleWorkspaceClose}
            slotProps={{
              paper: {
                sx: { width: 320, mt: 1, borderRadius: 2, boxShadow: theme.shadows[4] },
              },
            }}
          >
            <Box sx={{ p: 2, pb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 800, color: "text.secondary", fontSize: "0.75rem", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                Switch Workspace
              </Typography>
            </Box>
            <Box sx={{ px: 2, pb: 2 }}>
              {isTenantsLoading ? (
                <Stack direction="row" spacing={1} sx={{ py: 2, justifyContent: "center", alignItems: "center" }}>
                  <CircularProgress size={16} />
                  <Typography variant="body2" color="text.secondary">Loading workspaces...</Typography>
                </Stack>
              ) : (
                <TenantList
                  tenants={tenants}
                  selectedTenantId={switchingTenantId}
                  currentTenantId={user?.tenantId}
                  onSelect={handleTenantSelect}
                  maxHeight={400}
                />
              )}
            </Box>
          </Menu>

          {/* Dynamic Breadcrumbs */}
          <Breadcrumbs
            aria-label="breadcrumb"
            separator={<Typography color="text.disabled" variant="caption">/</Typography>}
            sx={{
              ml: 1,
              display: "flex",
              flex: 1,
              minWidth: 0,
              "& .MuiBreadcrumbs-ol": { flexWrap: "nowrap" },
            }}
          >
            {getPathSegments(pathname).map((segment, index, array) => {
              const isLast = index === array.length - 1;
              const isDesktop = { xs: "none", md: "inline" };
              const isMobileActive = { xs: "inline", md: "inline" };

              // On mobile, only show the last segment
              const display = isLast ? isMobileActive : isDesktop;

              return (
                <Typography
                  key={segment.href}
                  variant="body2"
                  noWrap
                  sx={{
                    display,
                    fontWeight: isLast ? 700 : 500,
                    color: isLast ? "text.primary" : "text.secondary",
                    maxWidth: { xs: 120, sm: 160, md: 200 },
                    textDecoration: "none",
                    "&:hover": { textDecoration: isLast ? "none" : "underline" },
                  }}
                  {...(!isLast ? { component: NextLink, href: segment.href } : {})}
                >
                  {segment.label}
                </Typography>
              );
            })}
          </Breadcrumbs>

          {/* Command Palette Anchor */}
          <Box
            onClick={handleSearchOpen}
            sx={{
              display: { xs: "none", lg: "flex" },
              justifyContent: "center",
              flex: 1,
              px: 2,
            }}
          >
            <Box
              sx={{
                width: "100%",
                maxWidth: 400,
                height: 36,
                bgcolor: (theme) => alpha(theme.palette.text.primary, 0.04),
                borderRadius: 2,
                display: "flex",
                alignItems: "center",
                px: 2,
                cursor: "pointer",
                border: 1,
                borderColor: "divider",
                transition: "all 0.2s",
                "&:hover": {
                  bgcolor: (theme) => alpha(theme.palette.text.primary, 0.08),
                  borderColor: "primary.light",
                },
              }}
            >
              <SearchOutlinedIcon fontSize="small" sx={{ color: "text.disabled", mr: 1.5 }} />
              <Typography variant="body2" color="text.disabled" sx={{ flex: 1 }}>
                Search or type a command...
              </Typography>
              <Typography
                variant="caption"
                sx={{
                  bgcolor: (theme) => alpha(theme.palette.text.primary, 0.1),
                  px: 0.8,
                  py: 0.2,
                  borderRadius: 0.8,
                  fontWeight: 700,
                  fontSize: "0.65rem",
                  color: "text.secondary",
                }}
              >
                ⌘K
              </Typography>
            </Box>
          </Box>

          {/* Global Quick Actions & Utilities */}
          <Stack direction="row" spacing={0.5} sx={{ alignItems: "center", ml: "auto" }}>
            <IconButton
              size="small"
              onClick={handleSearchOpen}
              sx={{ display: { xs: "inline-flex", lg: "none" } }}
            >
              <SearchOutlinedIcon fontSize="small" />
            </IconButton>

            <IconButton
              size="small"
              onClick={handleNotificationsOpen}
            >
              <Badge badgeContent={unreadCount} color="error" variant="dot">
                <NotificationsOutlinedIcon fontSize="small" />
              </Badge>
            </IconButton>

            <NotificationPopover
              anchorEl={notificationsAnchorEl}
              open={Boolean(notificationsAnchorEl)}
              onClose={handleNotificationsClose}
              notifications={notifications}
              onMarkAsRead={markAsRead}
              onMarkAllAsRead={markAllAsRead}
            />

            <Tooltip title="Global Actions">
              <IconButton
                size="small"
                onClick={handleActionsOpen}
                sx={{
                  bgcolor: "primary.main",
                  color: "white",
                  width: 32,
                  height: 32,
                  "&:hover": { bgcolor: "primary.dark" },
                }}
              >
                <AddIcon fontSize="small" />
              </IconButton>
            </Tooltip>

            <Menu
              anchorEl={actionsAnchorEl}
              open={Boolean(actionsAnchorEl)}
              onClose={handleActionsClose}
              transformOrigin={{ horizontal: "right", vertical: "top" }}
              anchorOrigin={{ horizontal: "right", vertical: "bottom" }}
              slotProps={{
                paper: {
                  sx: { width: 180, mt: 1, borderRadius: 2, boxShadow: theme.shadows[3] },
                },
              }}
            >
              {canCreateEco ? (
                <MenuItem component={NextLink} href="/ecos/new" onClick={handleActionsClose}>
                  <ListItemIcon><AddIcon fontSize="small" /></ListItemIcon>
                  <ListItemText primary="New ECO" slotProps={{ primary: { variant: "body2", sx: { fontWeight: 600 } } }} />
                </MenuItem>
              ) : null}
              {isAdministrator ? (
                <MenuItem component={NextLink} href="/settings/users" onClick={handleActionsClose}>
                  <ListItemIcon><PeopleOutlinedIcon fontSize="small" /></ListItemIcon>
                  <ListItemText primary="Invite User" slotProps={{ primary: { variant: "body2", sx: { fontWeight: 600 } } }} />
                </MenuItem>
              ) : null}
              {!canCreateEco && !isAdministrator ? (
                <MenuItem disabled>
                  <ListItemText primary="No actions available" slotProps={{ primary: { variant: "body2", sx: { color: "text.disabled" } } }} />
                </MenuItem>
              ) : null}
            </Menu>
          </Stack>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        aria-label="Workspace navigation"
        sx={{
          width: { md: currentDrawerWidth },
          flexShrink: { md: 0 },
          transition: theme.transitions.create("width", {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.leavingScreen,
          }),
        }}
      >
        {/* Mobile Drawer */}
        <Drawer
          variant="temporary"
          open={isMobileDrawerOpen}
          onClose={handleMobileDrawerClose}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: "block", md: "none" },
            "& .MuiDrawer-paper": {
              width: drawerWidth,
              boxSizing: "border-box",
            },
          }}
        >
          <NavigationDrawerContent
            pathname={pathname}
            userName={userName}
            role={role}
            isAdministrator={isAdministrator}
            isExpanded={true}
            navigationItems={navigationItems}
            onNavigate={handleMobileDrawerClose}
            onLogout={logout}
            onAboutOpen={handleAboutOpen}
          />
        </Drawer>

        {/* Desktop Drawer */}
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: "none", md: "block" },
            "& .MuiDrawer-paper": {
              width: currentDrawerWidth,
              boxSizing: "border-box",
              borderRightColor: "divider",
              overflowX: "hidden",
              transition: theme.transitions.create("width", {
                easing: theme.transitions.easing.sharp,
                duration: theme.transitions.duration.leavingScreen,
              }),
            },
          }}
        >
          <NavigationDrawerContent
            pathname={pathname}
            userName={userName}
            role={role}
            isAdministrator={isAdministrator}
            isExpanded={isExpanded}
            navigationItems={navigationItems}
            onNavigate={() => {}}
            onLogout={logout}
            onAboutOpen={handleAboutOpen}
            onDrawerToggle={handleDrawerToggle}
          />
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          display: "flex",
          flexDirection: "column",
          minHeight: "100vh",
          width: { xs: "100%", md: `calc(100% - ${currentDrawerWidth}px)` },
          transition: theme.transitions.create(["width", "margin"], {
            easing: theme.transitions.easing.sharp,
            duration: theme.transitions.duration.leavingScreen,
          }),
        }}
      >
        <Toolbar variant="dense" sx={{ minHeight: { xs: 56, md: 52 } }} />
        <Box
          sx={{
            flexGrow: 1,
            width: "100%",
            maxWidth: 1600,
            mx: "auto",
            px: { xs: 2, sm: 3, lg: 4 },
            py: { xs: 2, sm: 3 },
            display: "flex",
            flexDirection: "column",
            minHeight: 0,
          }}
        >
          {children}
        </Box>
      </Box>

      <AboutDialog open={isAboutOpen} onClose={handleAboutClose} />

      <CommandPalette open={isSearchOpen} onClose={handleSearchClose} />
    </Box>
  );
}

/**
 * Parses the current pathname into a list of breadcrumb segments.
 *
 * @param pathname - The current URL pathname.
 * @returns A list of breadcrumb segments with labels and hrefs.
 */
function getPathSegments(pathname: string): { label: string; href: string }[] {
  const segments: { label: string; href: string }[] = [{ label: "Workspace", href: "/" }];

  if (pathname === "/") {
    return segments;
  }

  const parts = pathname.split("/").filter(Boolean);
  const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  let currentPath = "";

  for (const part of parts) {
    currentPath += `/${part}`;
    
    // Custom labels for known routes
    let label = part.charAt(0).toUpperCase() + part.slice(1).replace(/-/g, " ");
    
    // Handle specific route naming
    if (part.toLowerCase() === "ecos") label = "ECOs";
    if (part.startsWith("eco-")) label = part.toUpperCase(); // e.g. ECO-2026-001

    // Replace UUIDs with short human-readable identifiers
    if (uuidRegex.test(part)) {
      label = part.slice(0, 8).toUpperCase();
    }

    segments.push({ label, href: currentPath });
  }

  return segments;
}

/**
 * About modal component displaying product information and credits.
 */
function AboutDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xs"
      fullWidth
      slotProps={{
        paper: {
          sx: { borderRadius: 3, p: 1 },
        },
      }}
    >
      <IconButton
        aria-label="close"
        onClick={onClose}
        sx={{
          position: "absolute",
          right: 8,
          top: 8,
          color: (theme) => theme.palette.grey[500],
        }}
      >
        <CloseIcon />
      </IconButton>
      <DialogTitle sx={{ textAlign: "center", pb: 0 }}>
        <Stack component="div" direction="row" spacing={1} sx={{ justifyContent: "center", alignItems: "center" }}>
          <Typography variant="h5" sx={{ fontWeight: 800, letterSpacing: "-0.02em" }}>
            EngiFlow
          </Typography>
          <Chip label="v0.1.0" size="small" color="primary" variant="outlined" sx={{ fontWeight: 600 }} />
        </Stack>
      </DialogTitle>
      <DialogContent sx={{ textAlign: "center", py: 3 }}>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Engineering Change Management
        </Typography>
        <Box sx={{ my: 3 }}>
          <Typography variant="caption" component="span" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
            DESIGNED & DEVELOPED BY
          </Typography>
          <Typography variant="body1" sx={{ fontWeight: 600 }}>
            Emanuel Lázaro
          </Typography>
        </Box>
        <Button
          variant="outlined"
          color="inherit"
          startIcon={<GitHubIcon />}
          component="a"
          href="https://github.com/emanuellcs/engiflow"
          target="_blank"
          rel="noopener noreferrer"
          fullWidth
          sx={{ borderRadius: 2, textTransform: "none" }}
        >
          View Source on GitHub
        </Button>
      </DialogContent>
      <Divider sx={{ mx: 2 }} />
      <DialogActions sx={{ justifyContent: "center", p: 2 }}>
        <Typography variant="caption" color="text.disabled">
          MIT License - Copyright © 2026
        </Typography>
      </DialogActions>
    </Dialog>
  );
}

/**
 * The internal content of the navigation drawer, shared between mobile and desktop variants.
 * Handles the rendering of navigation links, active states, and user profile information.
 *
 * @param props - The component properties.
 * @returns The rendered navigation content.
 */
function NavigationDrawerContent({
  pathname,
  userName,
  role,
  isAdministrator,
  isExpanded,
  navigationItems,
  onNavigate,
  onLogout,
  onAboutOpen,
  onDrawerToggle,
}: NavigationDrawerContentProps) {
  const theme = useTheme();
  const [isSettingsOpen, setIsSettingsOpen] = useState(() => pathname.startsWith("/settings"));

  const toggleSettings = () => {
    if (!isExpanded && onDrawerToggle) {
      onDrawerToggle();
      setIsSettingsOpen(true);
    } else {
      setIsSettingsOpen((prev) => !prev);
    }
  };

  return (
    <Box
      sx={{
        height: "100%",
        bgcolor: "background.paper",
        display: "flex",
        flexDirection: "column",
        minHeight: 0,
      }}
    >
      <Box
        sx={{
          minHeight: { xs: 56, md: 52 },
          px: isExpanded ? 2.5 : 0,
          display: "flex",
          alignItems: "center",
          justifyContent: isExpanded ? "space-between" : "center",
        }}
      >
        <Typography
          variant="h6"
          component="p"
          noWrap
          sx={{
            fontWeight: 800,
            letterSpacing: "-0.02em",
            display: isExpanded ? "block" : "none",
          }}
        >
          EngiFlow
        </Typography>
        {!isExpanded && (
          <Avatar
            variant="rounded"
            onClick={onDrawerToggle}
            sx={{
              width: 32,
              height: 32,
              bgcolor: "primary.main",
              fontSize: "1rem",
              fontWeight: 800,
              cursor: "pointer",
              transition: theme.transitions.create(["background-color", "transform"]),
              "& svg": { display: "none" },
              "&:hover": {
                bgcolor: "primary.dark",
                "& span": { display: "none" },
                "& svg": { display: "block" },
              },
            }}
          >
            <span>E</span>
            <ChevronRightIcon />
          </Avatar>
        )}
        {isExpanded && onDrawerToggle && (
          <IconButton onClick={onDrawerToggle} size="small" sx={{ ml: 1 }}>
            <ChevronLeftIcon />
          </IconButton>
        )}
      </Box>
      <Divider />

      <Box sx={{ flex: 1, minHeight: 0, overflowY: "auto", py: 2 }}>
        <List dense disablePadding sx={{ px: isExpanded ? 1 : 0.5 }}>
          {navigationItems
            .filter((item) => !item.administratorOnly || isAdministrator)
            .map((item) => {
              const isSelected = isNavigationItemSelected(pathname, item.href);
              const hasSubItems = Boolean(item.subItems?.length);

              const listItemContent = (
                <ListItem key={item.href} disablePadding sx={{ mb: 0.5 }}>
                  <ListItemButton
                    component={NextLink}
                    href={item.href}
                    selected={isSelected}
                    onClick={onNavigate}
                    sx={{
                      minHeight: 44,
                      justifyContent: isExpanded ? "initial" : "center",
                      px: isExpanded ? 2 : 2.5,
                      borderRadius: 1.5,
                      position: "relative",
                      "&.Mui-selected": {
                        bgcolor: (theme) => alpha(theme.palette.primary.main, 0.08),
                        "&::before": {
                          content: '""',
                          position: "absolute",
                          left: 0,
                          top: "20%",
                          bottom: "20%",
                          width: "3px",
                          bgcolor: "primary.main",
                          borderRadius: "0 4px 4px 0",
                        },
                        "&:hover": { bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12) },
                      },
                    }}
                  >
                    <ListItemIcon
                      sx={{
                        minWidth: 0,
                        mr: isExpanded ? 2 : "auto",
                        justifyContent: "center",
                        color: isSelected ? "primary.main" : "text.secondary",
                      }}
                    >
                      <Badge
                        badgeContent={item.badgeCount}
                        color="primary"
                        invisible={!item.badgeCount || (item.badgeCount > 0 && isExpanded)}
                        sx={{ "& .MuiBadge-badge": { fontSize: "0.65rem", height: 16, minWidth: 16 } }}
                      >
                        {item.icon}
                      </Badge>
                    </ListItemIcon>
                    {isExpanded && (
                      <>
                        <ListItemText
                          primary={item.label}
                          slotProps={{
                            primary: {
                              variant: "body2",
                              sx: { fontWeight: isSelected ? 600 : 500 },
                            },
                          }}
                        />
                        {item.badgeCount ? (
                          <Chip
                            label={item.badgeCount}
                            size="small"
                            color="primary"
                            sx={{ height: 20, fontSize: "0.7rem", fontWeight: 700 }}
                          />
                        ) : null}
                      </>
                    )}
                  </ListItemButton>
                </ListItem>
              );

              if (hasSubItems) {
                return (
                  <Box key={item.label} sx={{ mb: 0.5 }}>
                    <Tooltip title={!isExpanded ? item.label : ""} placement="right">
                      <ListItem disablePadding>
                        <ListItemButton
                          onClick={toggleSettings}
                          sx={{
                            minHeight: 44,
                            justifyContent: isExpanded ? "initial" : "center",
                            px: isExpanded ? 2 : 2.5,
                            borderRadius: 1.5,
                            color: isSelected ? "primary.main" : "text.primary",
                          }}
                        >
                          <ListItemIcon
                            sx={{
                              minWidth: 0,
                              mr: isExpanded ? 2 : "auto",
                              justifyContent: "center",
                              color: isSelected ? "primary.main" : "text.secondary",
                            }}
                          >
                            {item.icon}
                          </ListItemIcon>
                          {isExpanded && (
                            <>
                              <ListItemText
                                primary={item.label}
                                slotProps={{
                                  primary: {
                                    variant: "body2",
                                    sx: { fontWeight: isSelected ? 600 : 500 },
                                  },
                                }}
                              />
                              {isSettingsOpen ? <ExpandLess fontSize="small" /> : <ExpandMore fontSize="small" />}
                            </>
                          )}
                        </ListItemButton>
                      </ListItem>
                    </Tooltip>
                    <Collapse in={isSettingsOpen && isExpanded} timeout="auto" unmountOnExit>
                      <List component="div" disablePadding sx={{ mt: 0.5 }}>
                        {item.subItems?.map((subItem) => {
                          const isSubSelected = pathname.startsWith(subItem.href);
                          const subIcon = subItem.href.includes("users") ? (
                            <PeopleOutlinedIcon fontSize="small" />
                          ) : subItem.href.includes("policies") ? (
                            <BusinessOutlinedIcon fontSize="small" />
                          ) : (
                            <AccountCircleOutlinedIcon fontSize="small" />
                          );

                          return (
                            <ListItem key={subItem.href} disablePadding sx={{ mb: 0.5 }}>
                              <ListItemButton
                                component={NextLink}
                                href={subItem.href}
                                selected={isSubSelected}
                                onClick={onNavigate}
                                sx={{
                                  minHeight: 36,
                                  pl: 2.5,
                                  pr: 2,
                                  borderRadius: 1.5,
                                  "&.Mui-selected": {
                                    bgcolor: (theme) => alpha(theme.palette.primary.main, 0.08),
                                    "&::before": {
                                      content: '""',
                                      position: "absolute",
                                      left: 0,
                                      top: "20%",
                                      bottom: "20%",
                                      width: "3px",
                                      bgcolor: "primary.main",
                                      borderRadius: "0 4px 4px 0",
                                    },
                                    "&:hover": { bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12) },
                                  },
                                }}
                              >
                                <ListItemIcon
                                  sx={{
                                    minWidth: 0,
                                    mr: 2,
                                    color: isSubSelected ? "primary.main" : "text.secondary",
                                  }}
                                >
                                  {subIcon}
                                </ListItemIcon>
                                <ListItemText
                                  primary={subItem.label}
                                  slotProps={{
                                    primary: {
                                      variant: "body2",
                                      sx: {
                                        fontWeight: isSubSelected ? 600 : 400,
                                        fontSize: "0.8125rem",
                                      },
                                    },
                                  }}
                                />
                              </ListItemButton>
                            </ListItem>
                          );
                        })}
                      </List>
                    </Collapse>
                  </Box>
                );
              }

              return (
                <Tooltip key={item.href} title={!isExpanded ? item.label : ""} placement="right">
                  {listItemContent}
                </Tooltip>
              );
            })}
        </List>
      </Box>

      <Divider />
      <Box sx={{ py: 1, px: isExpanded ? 1 : 0.5 }}>
        <Tooltip title={!isExpanded ? "About EngiFlow" : ""} placement="right">
          <ListItemButton
            onClick={onAboutOpen}
            sx={{
              minHeight: 44,
              justifyContent: isExpanded ? "initial" : "center",
              px: isExpanded ? 2 : 2.5,
              borderRadius: 1.5,
            }}
          >
            <ListItemIcon
              sx={{
                minWidth: 0,
                mr: isExpanded ? 2 : "auto",
                justifyContent: "center",
                color: "text.secondary",
              }}
            >
              <InfoOutlinedIcon fontSize="small" />
            </ListItemIcon>
            {isExpanded && (
              <ListItemText
                primary="About EngiFlow"
                slotProps={{ primary: { variant: "body2", sx: { fontWeight: 500 } } }}
              />
            )}
          </ListItemButton>
        </Tooltip>
      </Box>

      <Box
        sx={{
          p: 1.5,
          bgcolor: (theme) => (isExpanded ? alpha(theme.palette.action.disabledBackground, 0.04) : "transparent"),
          borderTop: 1,
          borderColor: "divider",
        }}
      >
        <Stack
          direction={isExpanded ? "row" : "column"}
          spacing={isExpanded ? 1.25 : 1}
          sx={{
            alignItems: "center",
            minWidth: 0,
          }}
        >
          <Avatar
            sx={{
              width: 36,
              height: 36,
              bgcolor: "secondary.main",
              fontSize: "0.875rem",
              fontWeight: 600,
            }}
          >
            {getInitials(userName)}
          </Avatar>
          {isExpanded && (
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="body2" noWrap sx={{ fontWeight: 700, fontSize: "0.875rem" }}>
                {userName}
              </Typography>
              <Typography variant="caption" color="text.secondary" noWrap sx={{ display: "block", mt: -0.5 }}>
                {role}
              </Typography>
            </Box>
          )}
          <Tooltip title="Logout" placement={isExpanded ? "top" : "right"}>
            <IconButton size="small" aria-label="Logout" onClick={onLogout}>
              <LogoutIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Stack>
        {isExpanded && (
          <Typography
            variant="caption"
            sx={{
              display: "block",
              textAlign: "center",
              mt: 1.5,
              color: "text.disabled",
              fontSize: "0.65rem",
              fontWeight: 600,
              letterSpacing: "0.05em",
            }}
          >
            v0.1.0
          </Typography>
        )}
      </Box>
    </Box>
  );
}

/**
 * Determines if a navigation item is currently selected based on the active path.
 *
 * @param pathname - The current URL pathname.
 * @param href - The navigation item's destination URL.
 * @returns True if the item should be marked as selected.
 */
function isNavigationItemSelected(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }

  // Handle nested routes like /ecos/[id]
  if (href === "/ecos") {
    return pathname.startsWith("/ecos");
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}

/**
 * Extracts initials from a user's name for avatar display.
 *
 * @param name - The full name of the user.
 * @returns A string containing up to two uppercase initials.
 */
function getInitials(name: string): string {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");

  return initials || "U";
}
