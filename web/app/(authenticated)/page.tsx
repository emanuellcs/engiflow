import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import PageHeader from "@/components/ui/PageHeader";

/**
 * Renders the authenticated workspace landing page reserved for future
 * operational metrics and MUI X chart visualizations.
 *
 * @returns The metrics dashboard placeholder view.
 */
export default function MetricsDashboardPage() {
  return (
    <Stack spacing={2.5}>
      <PageHeader
        title="Dashboard"
        description="Monitor engineering change activity across the workspace."
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
          Metrics Dashboard (Charts coming soon)
        </Typography>
      </Paper>
    </Stack>
  );
}
