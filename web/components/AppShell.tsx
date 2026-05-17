"use client";

import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import AssignmentIcon from "@mui/icons-material/Assignment";
import DashboardIcon from "@mui/icons-material/Dashboard";
import LogoutIcon from "@mui/icons-material/Logout";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import MenuIcon from "@mui/icons-material/Menu";
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
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
import Typography from "@mui/material/Typography";
import { usePathname } from "next/navigation";
import type { PropsWithChildren, ReactNode } from "react";
import { useMemo, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { useAuth } from "@/lib/auth/AuthContext";

const drawerWidth = 240;
const administratorRole = "Administrator";

type NavigationItem = {
  label: string;
  href: string;
  icon: ReactNode;
  administratorOnly?: boolean;
};

type NavigationDrawerContentProps = {
  pathname: string;
  isAdministrator: boolean;
  onNavigate: () => void;
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
    label: "Settings",
    href: "/settings/users",
    icon: <ManageAccountsIcon fontSize="small" />,
    administratorOnly: true,
  },
];

/**
 * Renders the authenticated EngiFlow application shell with a fixed AppBar,
 * responsive navigation drawer, route title, and user logout controls.
 *
 * @param props - Shell properties.
 * @param props.children - Authenticated route content rendered in the shell body.
 * @returns The global authenticated workspace layout.
 */
export default function AppShell({ children }: PropsWithChildren) {
  const pathname = usePathname() ?? "/";
  const { logout, user } = useAuth();
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false);
  const isAdministrator = user?.role === administratorRole;
  const pageTitle = useMemo(() => getPageTitle(pathname), [pathname]);

  /**
   * Opens the temporary mobile drawer from the AppBar hamburger button.
   *
   * @returns Nothing.
   */
  function handleDrawerOpen(): void {
    setIsMobileDrawerOpen(true);
  }

  /**
   * Closes the temporary mobile drawer after navigation or backdrop dismissal.
   *
   * @returns Nothing.
   */
  function handleDrawerClose(): void {
    setIsMobileDrawerOpen(false);
  }

  return (
    <Box sx={{ display: "flex", bgcolor: "background.default", minHeight: "100vh" }}>
      <AppBar
        position="fixed"
        color="primary"
        elevation={3}
        sx={{
          width: { md: `calc(100% - ${drawerWidth}px)` },
          ml: { md: `${drawerWidth}px` },
        }}
      >
        <Toolbar variant="dense" sx={{ minHeight: { xs: 56, md: 48 } }}>
          <IconButton
            color="inherit"
            edge="start"
            aria-label="Open navigation"
            onClick={handleDrawerOpen}
            sx={{ mr: 1, display: { md: "none" } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography
            variant="h6"
            component="p"
            noWrap
            sx={{ flexGrow: 1, minWidth: 0, letterSpacing: 0 }}
          >
            {pageTitle}
          </Typography>
          <Stack
            direction="row"
            spacing={{ xs: 0.75, sm: 1.5 }}
            sx={{ alignItems: "center", minWidth: 0, ml: 1 }}
          >
            <AccountCircleIcon
              fontSize="small"
              sx={{ display: { xs: "none", sm: "block" } }}
            />
            <Typography
              variant="body2"
              color="inherit"
              noWrap
              sx={{ maxWidth: { xs: 92, sm: 180 } }}
            >
              {user?.role ?? "User"}
            </Typography>
            <Button
              color="inherit"
              size="small"
              aria-label="Logout"
              startIcon={<LogoutIcon fontSize="small" />}
              onClick={logout}
              sx={{
                minWidth: { xs: 36, sm: 72 },
                px: { xs: 1, sm: 1.5 },
                textTransform: "none",
                "& .MuiButton-startIcon": {
                  mr: { xs: 0, sm: 0.5 },
                },
              }}
            >
              <Box component="span" sx={{ display: { xs: "none", sm: "inline" } }}>
                Logout
              </Box>
            </Button>
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
            isAdministrator={isAdministrator}
            onNavigate={handleDrawerClose}
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
            isAdministrator={isAdministrator}
            onNavigate={handleDrawerClose}
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
        <Toolbar variant="dense" sx={{ minHeight: { xs: 56, md: 48 } }} />
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

/**
 * Renders drawer branding and the role-filtered navigation list.
 *
 * @param props - Drawer content rendering options.
 * @param props.pathname - Current App Router pathname.
 * @param props.isAdministrator - Whether the current user can see admin links.
 * @param props.onNavigate - Callback invoked after a navigation item is selected.
 * @returns The shared mobile and desktop drawer content.
 */
function NavigationDrawerContent({
  pathname,
  isAdministrator,
  onNavigate,
}: NavigationDrawerContentProps) {
  return (
    <Box sx={{ height: "100%", bgcolor: "background.paper" }}>
      <Box
        sx={{
          minHeight: { xs: 56, md: 48 },
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
          Workspace
        </Typography>
      </Box>
      <Divider />
      <List dense sx={{ py: 1 }}>
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
                    borderRight: 3,
                    borderRightColor: isSelected ? "primary.main" : "transparent",
                    "&.Mui-selected": {
                      bgcolor: "action.selected",
                    },
                    "&.Mui-selected:hover": {
                      bgcolor: "action.hover",
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
  );
}

/**
 * Resolves the current App Router pathname into the page title shown in the
 * top AppBar.
 *
 * @param pathname - Current path from usePathname.
 * @returns The human-readable title for the active workspace route.
 */
function getPageTitle(pathname: string): string {
  if (pathname === "/") {
    return "Dashboard";
  }

  if (pathname.startsWith("/ecos")) {
    return "Engineering Change Orders";
  }

  if (pathname.startsWith("/settings/users")) {
    return "User Management";
  }

  return "EngiFlow";
}

/**
 * Determines whether a navigation item should be marked as active for the
 * current route.
 *
 * @param pathname - Current path from usePathname.
 * @param href - Navigation item destination.
 * @returns True when the navigation item represents the active route branch.
 */
function isNavigationItemSelected(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}
