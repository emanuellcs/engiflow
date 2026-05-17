import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import PageHeader from "@/components/ui/PageHeader";

/**
 * Renders the administrator-only user management placeholder route.
 *
 * @returns The user management placeholder view.
 */
export default function UserManagementPage() {
  return (
    <Stack spacing={2.5}>
      <PageHeader
        title="User Management"
        description="Manage workspace users, roles, and access policies."
      />
      <Paper
        elevation={1}
        sx={{
          p: { xs: 2, sm: 3 },
          minHeight: 180,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          textAlign: "center",
        }}
      >
        <Typography variant="h6" component="p" color="text.secondary">
          User Management
        </Typography>
      </Paper>
    </Stack>
  );
}
