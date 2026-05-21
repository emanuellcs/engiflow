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
import PeopleOutlinedIcon from "@mui/icons-material/PeopleOutlined";
import SettingsIcon from "@mui/icons-material/Settings";
import AppBar from "@mui/material/AppBar";
import Avatar from "@mui/material/Avatar";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
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
import Stack from "@mui/material/Stack";
import { alpha, useTheme } from "@mui/material/styles";
import Toolbar from "@mui/material/Toolbar";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { usePathname } from "next/navigation";
import type { PropsWithChildren, ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useEcoHub } from "@/components/ecos/useEcoHub";
import { useSecurityHub } from "@/components/security/useSecurityHub";
import NextLink from "@/components/ui/NextLink";
import { apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";

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
  const { logout, token, user } = useAuth();
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false);
  const [isExpanded, setIsExpanded] = useState(true);
  const [isAboutOpen, setIsAboutOpen] = useState(false);
  const [ecoCount, setEcoCount] = useState<number>(0);

  const isAdministrator = isAdminOrOwner(user?.role);
  const companyName = user?.companyName ?? "Workspace";
  const userName = user?.userName ?? "User";
  const role = user?.role ?? "User";
  const canCreateEco = isAdministrator || role === requesterRole;
  const showDashboardAction = pathname !== "/";
  const showEcosAction = !pathname.startsWith("/ecos");
  const showNewEcoAction = canCreateEco && !pathname.startsWith("/ecos/new");
  const showTeamAction = isAdministrator && !pathname.startsWith("/settings/users");

  const currentDrawerWidth = isExpanded ? drawerWidth : collapsedDrawerWidth;

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
          }}
        >
          <IconButton
            edge="start"
            aria-label="Open navigation"
            onClick={handleMobileDrawerOpen}
            sx={{ mr: 1, display: { md: "none" } }}
          >
            <MenuIcon />
          </IconButton>
          <Chip
            icon={<BusinessIcon fontSize="small" />}
            label={companyName}
            variant="outlined"
            size="small"
            sx={{
              maxWidth: { xs: 170, sm: 320 },
              "& .MuiChip-label": {
                overflow: "hidden",
                textOverflow: "ellipsis",
              },
            }}
          />
          <Box sx={{ flex: 1, minWidth: 0 }} />
          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
            {showDashboardAction ? (
              <Tooltip title="Dashboard">
                <IconButton
                  component={NextLink}
                  href="/"
                  aria-label="Open dashboard"
                  size="small"
                >
                  <DashboardIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
            {showEcosAction ? (
              <Tooltip title="ECOs">
                <IconButton
                  component={NextLink}
                  href="/ecos"
                  aria-label="Open ECOs"
                  size="small"
                >
                  <AssignmentIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
            {showTeamAction ? (
              <Tooltip title="Team Management">
                <IconButton
                  component={NextLink}
                  href="/settings/users"
                  aria-label="Open team management"
                  size="small"
                >
                  <GroupIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
            {showNewEcoAction ? (
              <Button
                component={NextLink}
                href="/ecos/new"
                variant="contained"
                size="small"
                startIcon={<AddIcon fontSize="small" />}
                sx={{
                  minHeight: 32,
                  textTransform: "none",
                  display: { xs: "none", sm: "inline-flex" },
                }}
              >
                New ECO
              </Button>
            ) : null}
            {showNewEcoAction ? (
              <Tooltip title="New ECO">
                <IconButton
                  component={NextLink}
                  href="/ecos/new"
                  aria-label="Create ECO"
                  size="small"
                  sx={{ display: { xs: "inline-flex", sm: "none" } }}
                >
                  <AddIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
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
    </Box>
  );
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
