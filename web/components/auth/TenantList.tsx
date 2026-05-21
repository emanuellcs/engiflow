"use client";

import Avatar from "@mui/material/Avatar";
import CircularProgress from "@mui/material/CircularProgress";
import List from "@mui/material/List";
import ListItemAvatar from "@mui/material/ListItemAvatar";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { type WorkspaceTenantOption } from "./LoginForm";

/**
 * Describes props accepted by the tenant selection list.
 */
interface TenantListProps {
  /**
   * The list of available workspace options.
   */
  tenants: WorkspaceTenantOption[];
  /**
   * The identifier of the currently selected tenant, if any.
   */
  selectedTenantId: string | null;
  /**
   * Callback invoked when a tenant is clicked.
   */
  onSelect: (tenantId: string) => void;
  /**
   * Optional maximum height for the scrollable list container.
   * @default 320
   */
  maxHeight?: number | string;
}

/**
 * Renders a consistent MUI List for selecting a workspace tenant.
 */
export default function TenantList({
  tenants,
  selectedTenantId,
  onSelect,
  maxHeight = 320,
}: TenantListProps) {
  return (
    <List
      aria-label="Available EngiFlow workspaces"
      sx={{
        width: "100%",
        maxHeight,
        overflowY: "auto",
        border: 1,
        borderColor: "divider",
        borderRadius: 1.5,
        bgcolor: "background.paper",
        py: 0,
      }}
    >
      {tenants.map((tenant) => (
        <ListItemButton
          key={tenant.tenantId}
          alignItems="flex-start"
          divider
          disabled={Boolean(selectedTenantId)}
          selected={selectedTenantId === tenant.tenantId}
          onClick={() => void onSelect(tenant.tenantId)}
          sx={{ py: 1.5 }}
        >
          <ListItemAvatar>
            <Avatar sx={{ bgcolor: "primary.main", fontWeight: 700 }}>
              {getCompanyInitial(tenant.companyName)}
            </Avatar>
          </ListItemAvatar>
          <ListItemText
            primary={
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {tenant.companyName}
              </Typography>
            }
            secondary={
              <Stack spacing={0.25} component="span">
                <Typography component="span" variant="body2" color="text.secondary">
                  {tenant.companyEmail}
                </Typography>
                <Typography component="span" variant="caption" color="text.secondary">
                  Owner: {tenant.ownerName} - {tenant.ownerEmail}
                </Typography>
              </Stack>
            }
          />
          {selectedTenantId === tenant.tenantId ? (
            <CircularProgress size={18} sx={{ mt: 1.5 }} />
          ) : null}
        </ListItemButton>
      ))}
    </List>
  );
}

/**
 * Reads the initial character for a company avatar.
 */
function getCompanyInitial(companyName: string): string {
  return companyName.trim().charAt(0).toUpperCase() || "W";
}
