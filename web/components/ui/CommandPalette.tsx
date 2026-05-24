"use client";

import AssignmentIcon from "@mui/icons-material/Assignment";
import DashboardIcon from "@mui/icons-material/Dashboard";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import PersonIcon from "@mui/icons-material/Person";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogContent from "@mui/material/DialogContent";
import Divider from "@mui/material/Divider";
import InputBase from "@mui/material/InputBase";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import { alpha, useTheme } from "@mui/material/styles";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { apiFetch } from "@/lib/api/client";
import StatusChip, { type EcoStatus } from "@/components/ui/StatusChip";

/**
 * Represents a static navigational shortcut.
 */
type ShortcutItem = {
  label: string;
  href: string;
  icon: React.ReactNode;
};

/**
 * Represents an ECO search result from the database.
 */
type EcoResult = {
  id: string;
  title: string;
  description: string;
  status: EcoStatus;
};

/**
 * Represents a user search result from the database.
 */
type UserResult = {
  id: string;
  displayName: string;
  email: string;
  role: string;
};

/**
 * Aggregated search results from the backend.
 */
type SearchResults = {
  ecos: EcoResult[];
  users: UserResult[];
};

/**
 * Discriminatory union for the flattened list used in keyboard navigation.
 */
type PaletteItem =
  | { type: "shortcut"; data: ShortcutItem }
  | { type: "eco"; data: EcoResult }
  | { type: "user"; data: UserResult };

/**
 * Properties for the CommandPalette component.
 */
type CommandPaletteProps = {
  open: boolean;
  onClose: () => void;
};

const staticShortcuts: ShortcutItem[] = [
  { label: "Go to Dashboard", href: "/", icon: <DashboardIcon fontSize="small" /> },
  { label: "Go to Engineering Change Orders (ECOs)", href: "/ecos", icon: <AssignmentIcon fontSize="small" /> },
  { label: "Go to Team Settings", href: "/settings/users", icon: <ManageAccountsIcon fontSize="small" /> },
];

/**
 * A highly optimized Command Palette with hybrid static navigation and database search.
 *
 * @param props - The component properties.
 * @returns The rendered Command Palette dialog.
 */
export default function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const theme = useTheme();
  const router = useRouter();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<SearchResults | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  // Filter static shortcuts based on the current query
  const filteredShortcuts = useMemo(() => {
    if (!query) return staticShortcuts;
    const lowerQuery = query.toLowerCase();
    return staticShortcuts.filter((s) => s.label.toLowerCase().includes(lowerQuery));
  }, [query]);

  // Flatten all visible items into a single list for sequential arrow-key navigation
  const flattenedItems = useMemo<PaletteItem[]>(() => {
    const items: PaletteItem[] = [];

    filteredShortcuts.forEach((s) => items.push({ type: "shortcut", data: s }));

    if (results) {
      results.ecos.forEach((e) => items.push({ type: "eco", data: e }));
      results.users.forEach((u) => items.push({ type: "user", data: u }));
    }

    return items;
  }, [filteredShortcuts, results]);

  // Focus the input when the dialog opens
  useEffect(() => {
    if (open) {
      // Small delay to ensure input is ready for focus
      const timer = setTimeout(() => inputRef.current?.focus(), 50);
      return () => clearTimeout(timer);
    }
    return undefined;
  }, [open]);

  /**
   * Executes a database-driven search using the current query term.
   */
  const handleSearch = async () => {
    if (!query.trim() || isLoading) return;

    setIsLoading(true);
    try {
      const data = await apiFetch<SearchResults>(`/api/search?q=${encodeURIComponent(query)}`);
      setResults(data);
      setActiveIndex(0); // Reset cursor when results arrive
    } catch (error) {
      console.error("Search failed:", error);
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Handles changes to the search input, resetting the active index.
   * @param e - The change event.
   */
  const handleQueryChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setQuery(e.target.value);
    setActiveIndex(0);
  };

  /**
   * Navigates to a specific destination and closes the palette.
   * @param href - The target URL.
   */
  const handleNavigate = (href: string) => {
    router.push(href);
    onClose();
  };

  /**
   * Handles keyboard events for navigation and submission.
   * @param event - The keyboard event.
   */
  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((prev) => (prev + 1) % flattenedItems.length);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((prev) => (prev - 1 + flattenedItems.length) % flattenedItems.length);
    } else if (event.key === "Enter") {
      event.preventDefault();

      // If a shortcut is highlighted, navigate immediately
      if (activeIndex < filteredShortcuts.length) {
        handleNavigate(filteredShortcuts[activeIndex].href);
      } 
      // If a DB result is highlighted, navigate to it
      else if (activeIndex < flattenedItems.length) {
        const item = flattenedItems[activeIndex];
        if (item.type === "eco") {
          handleNavigate(`/ecos/${item.data.id}`);
        } else if (item.type === "user") {
          handleNavigate("/settings/users"); // Users don't have detail pages in the current requirements
        }
      } 
      // Otherwise, trigger a new database search
      else {
        void handleSearch();
      }
    } else if (event.key === "Escape") {
      onClose();
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      transitionDuration={{ enter: 300, exit: 200 }}
      slotProps={{
        paper: {
          sx: { 
            borderRadius: 3, 
            mt: "10vh", 
            verticalAlign: "top",
            backgroundImage: "none",
            boxShadow: theme.shadows[10],
          },
        },
      }}
    >
      <Box 
        sx={{ 
          p: 2, 
          display: "flex", 
          alignItems: "center", 
          borderBottom: 1, 
          borderColor: "divider",
          bgcolor: (theme) => alpha(theme.palette.background.paper, 0.8),
          backdropFilter: "blur(8px)",
        }}
      >
        <SearchOutlinedIcon sx={{ color: "text.disabled", mr: 2 }} />
        <InputBase
          inputRef={inputRef}
          fullWidth
          placeholder="Search or type a command..."
          value={query}
          onChange={handleQueryChange}
          onKeyDown={handleKeyDown}
          sx={{ fontSize: "1rem", fontWeight: 500 }}
        />
        {isLoading ? (
          <CircularProgress size={16} sx={{ ml: 1 }} />
        ) : (
          <Chip 
            label="ESC" 
            size="small" 
            variant="outlined" 
            sx={{ fontWeight: 700, fontSize: "0.65rem", ml: 1, height: 20, color: "text.disabled" }} 
          />
        )}
      </Box>

      <DialogContent sx={{ p: 0, minHeight: 300, maxHeight: 450, display: "flex", flexDirection: "column" }}>
        {flattenedItems.length > 0 ? (
          <List dense disablePadding sx={{ py: 1 }}>
            {/* Shortcuts Section */}
            {filteredShortcuts.length > 0 && (
              <>
                <Box sx={{ px: 2, py: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 800, color: "text.secondary", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                    Navigation
                  </Typography>
                </Box>
                {filteredShortcuts.map((s, i) => (
                  <ListItemButton
                    key={s.href}
                    selected={activeIndex === i}
                    onClick={() => handleNavigate(s.href)}
                    sx={{ px: 2, py: 1 }}
                  >
                    <ListItemIcon sx={{ minWidth: 40 }}>{s.icon}</ListItemIcon>
                    <ListItemText 
                      primary={s.label} 
                      slotProps={{ primary: { variant: "body2", sx: { fontWeight: activeIndex === i ? 600 : 400 } } }} 
                    />
                  </ListItemButton>
                ))}
              </>
            )}

            {/* ECOs Section */}
            {results?.ecos && results.ecos.length > 0 && (
              <>
                <Divider sx={{ my: 1 }} />
                <Box sx={{ px: 2, py: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 800, color: "text.secondary", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                    Engineering Change Orders
                  </Typography>
                </Box>
                {results.ecos.map((eco, i) => {
                  const idx = filteredShortcuts.length + i;
                  return (
                    <ListItemButton
                      key={eco.id}
                      selected={activeIndex === idx}
                      onClick={() => handleNavigate(`/ecos/${eco.id}`)}
                      sx={{ px: 2, py: 1 }}
                    >
                      <ListItemIcon sx={{ minWidth: 40 }}><AssignmentIcon fontSize="small" /></ListItemIcon>
                      <ListItemText 
                        primary={eco.title} 
                        secondary={eco.description}
                        slotProps={{ 
                          primary: { variant: "body2", noWrap: true, sx: { fontWeight: activeIndex === idx ? 600 : 400 } },
                          secondary: { variant: "caption", noWrap: true }
                        }} 
                      />
                      <StatusChip status={eco.status} />
                    </ListItemButton>
                  );
                })}
              </>
            )}

            {/* Users Section */}
            {results?.users && results.users.length > 0 && (
              <>
                <Divider sx={{ my: 1 }} />
                <Box sx={{ px: 2, py: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 800, color: "text.secondary", textTransform: "uppercase", letterSpacing: "0.05em" }}>
                    Team Members
                  </Typography>
                </Box>
                {results.users.map((user, i) => {
                  const idx = filteredShortcuts.length + (results.ecos?.length || 0) + i;
                  return (
                    <ListItemButton
                      key={user.id}
                      selected={activeIndex === idx}
                      onClick={() => handleNavigate("/settings/users")}
                      sx={{ px: 2, py: 1 }}
                    >
                      <ListItemIcon sx={{ minWidth: 40 }}><PersonIcon fontSize="small" /></ListItemIcon>
                      <ListItemText 
                        primary={user.displayName} 
                        secondary={user.email}
                        slotProps={{ 
                          primary: { variant: "body2", sx: { fontWeight: activeIndex === idx ? 600 : 400 } },
                          secondary: { variant: "caption" }
                        }} 
                      />
                      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                        {user.role}
                      </Typography>
                    </ListItemButton>
                  );
                })}
              </>
            )}
          </List>
        ) : (
          <Box sx={{ flex: 1, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", py: 8 }}>
            <SearchOutlinedIcon sx={{ fontSize: 48, color: "text.disabled", mb: 2, opacity: 0.5 }} />
            <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 500 }}>
              {query ? "No results found" : "No recent commands found"}
            </Typography>
            <Typography variant="caption" color="text.disabled">
              {query ? "Try searching for a different term." : "Type to start searching across ECOs, users, and settings."}
            </Typography>
          </Box>
        )}
      </DialogContent>

      <Divider />
      <Box sx={{ p: 1.5, px: 2, bgcolor: (theme) => alpha(theme.palette.action.disabledBackground, 0.04) }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: "center" }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
            <Chip label="↑↓" size="small" sx={{ height: 16, fontSize: "0.6rem", fontWeight: 800, borderRadius: 0.5 }} />
            <Typography variant="caption" color="text.secondary">to navigate</Typography>
          </Box>
          <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
            <Chip label="ENTER" size="small" sx={{ height: 16, fontSize: "0.6rem", fontWeight: 800, borderRadius: 0.5 }} />
            <Typography variant="caption" color="text.secondary">to search</Typography>
          </Box>
          <Box sx={{ display: "flex", alignItems: "center", gap: 0.5, ml: "auto" }}>
            <Chip label="ESC" size="small" sx={{ height: 16, fontSize: "0.6rem", fontWeight: 800, borderRadius: 0.5 }} />
            <Typography variant="caption" color="text.secondary">to close</Typography>
          </Box>
        </Stack>
      </Box>
    </Dialog>
  );
}
