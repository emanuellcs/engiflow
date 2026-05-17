"use client";

import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import AddIcon from "@mui/icons-material/Add";
import AssignmentIcon from "@mui/icons-material/Assignment";
import BusinessIcon from "@mui/icons-material/Business";
import DashboardIcon from "@mui/icons-material/Dashboard";
import GroupIcon from "@mui/icons-material/Group";
import LogoutIcon from "@mui/icons-material/Logout";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import MenuIcon from "@mui/icons-material/Menu";
import AppBar from "@mui/material/AppBar";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { usePathname } from "next/navigation";
import type { PropsWithChildren, ReactNode } from "react";
import { useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";

const drawerWidth = 248;
const requesterRole = "Requester";

type NavigationItem = {
  label: string;
  href: string;
  icon: ReactNode;
  administratorOnly?: boolean;
};

type NavigationDrawerContentProps = {
  pathname: string;
  companyName: string;
  userName: string;
  role: string;
  isAdministrator: boolean;
  onNavigate: () => void;
  onLogout: () => void;
};

const navigationItems: NavigationItem[] = [
  {
    label: "Dashboard",
    href: "/",
    icon: <DashboardIcon fontSize="small" />,
  },
  {
    label: "ECOs",
    href: "/ecos",
    icon: <AssignmentIcon fontSize="small" />,
  },
  {
    label: "Team Management",
    href: "/settings/users",
    icon: <ManageAccountsIcon fontSize="small" />,
    administratorOnly: true,
  },
];

export default function AppShell({ children }: PropsWithChildren) {
  const pathname = usePathname() ?? "/";
  const { logout, user } = useAuth();
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false);
  const isAdministrator = isAdminOrOwner(user?.role);
  const companyName = user?.companyName ?? "Workspace";
  const userName = user?.userName ?? "User";
  const role = user?.role ?? "User";
  const canCreateEco = isAdministrator || role === requesterRole;
  const showDashboardAction = pathname !== "/";
  const showEcosAction = !pathname.startsWith("/ecos");
  const showNewEcoAction = canCreateEco && !pathname.startsWith("/ecos/new");
  const showTeamAction = isAdministrator && !pathname.startsWith("/settings/users");

  function handleDrawerOpen(): void {
    setIsMobileDrawerOpen(true);
  }

  function handleDrawerClose(): void {
    setIsMobileDrawerOpen(false);
  }

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          width: { md: `calc(100% - ${drawerWidth}px)` },
          ml: { md: `${drawerWidth}px` },
          borderBottom: 1,
          borderColor: "divider",
          zIndex: (theme) => theme.zIndex.drawer + 1,
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
            onClick={handleDrawerOpen}
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
        sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}
      >
        <Drawer
          variant="temporary"
          open={isMobileDrawerOpen}
          onClose={handleDrawerClose}
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
            companyName={companyName}
            userName={userName}
            role={role}
            isAdministrator={isAdministrator}
            onNavigate={handleDrawerClose}
            onLogout={logout}
          />
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: "none", md: "block" },
            "& .MuiDrawer-paper": {
              width: drawerWidth,
              boxSizing: "border-box",
              borderRightColor: "divider",
            },
          }}
        >
          <NavigationDrawerContent
            pathname={pathname}
            companyName={companyName}
            userName={userName}
            role={role}
            isAdministrator={isAdministrator}
            onNavigate={handleDrawerClose}
            onLogout={logout}
          />
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          width: { xs: "100%", md: `calc(100% - ${drawerWidth}px)` },
        }}
      >
        <Toolbar variant="dense" sx={{ minHeight: { xs: 56, md: 52 } }} />
        <Box
          sx={{
            width: "100%",
            maxWidth: 1600,
            mx: "auto",
            px: { xs: 2, sm: 3, lg: 4 },
            py: { xs: 2, sm: 3 },
          }}
        >
          {children}
        </Box>
      </Box>
    </Box>
  );
}

function NavigationDrawerContent({
  pathname,
  companyName,
  userName,
  role,
  isAdministrator,
  onNavigate,
  onLogout,
}: NavigationDrawerContentProps) {
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
          minHeight: { xs: 64, md: 68 },
          px: 2,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
        }}
      >
        <Typography variant="h6" component="p" noWrap>
          EngiFlow
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          {companyName}
        </Typography>
      </Box>
      <Divider />

      <Box sx={{ flex: 1, minHeight: 0, overflowY: "auto", py: 1 }}>
        <List dense disablePadding>
          {navigationItems
            .filter((item) => !item.administratorOnly || isAdministrator)
            .map((item) => {
              const isSelected = isNavigationItemSelected(pathname, item.href);

              return (
                <ListItem key={item.href} disablePadding>
                  <ListItemButton
                    component={NextLink}
                    href={item.href}
                    selected={isSelected}
                    onClick={onNavigate}
                    sx={{
                      minHeight: 40,
                      px: 2,
                      mx: 1,
                      borderRadius: 1,
                      "&.Mui-selected": {
                        bgcolor: "action.selected",
                      },
                    }}
                  >
                    <ListItemIcon
                      sx={{
                        minWidth: 36,
                        color: isSelected ? "primary.main" : "text.secondary",
                      }}
                    >
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText
                      primary={item.label}
                      slotProps={{
                        primary: {
                          variant: "body2",
                          sx: { fontWeight: isSelected ? 500 : 400 },
                        },
                      }}
                    />
                  </ListItemButton>
                </ListItem>
              );
            })}
        </List>
      </Box>

      <Divider />
      <Stack
        direction="row"
        spacing={1.25}
        sx={{
          alignItems: "center",
          p: 1.5,
          minWidth: 0,
        }}
      >
        <Avatar sx={{ width: 36, height: 36, bgcolor: "secondary.main" }}>
          {getInitials(userName)}
        </Avatar>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}>
            {userName}
          </Typography>
          <Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
            <AccountCircleIcon sx={{ fontSize: 14, color: "text.secondary" }} />
            <Typography variant="caption" color="text.secondary" noWrap>
              {role}
            </Typography>
          </Stack>
        </Box>
        <Tooltip title="Logout">
          <IconButton size="small" aria-label="Logout" onClick={onLogout}>
            <LogoutIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>
    </Box>
  );
}

function isNavigationItemSelected(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}

function getInitials(name: string): string {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");

  return initials || "U";
}
