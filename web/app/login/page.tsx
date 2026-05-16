import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";

export default function LoginPage() {
  return (
    <Stack spacing={3} sx={{ maxWidth: 480 }}>
      <Typography variant="h4" component="h1">
        Sign in
      </Typography>
      <Paper elevation={1} sx={{ p: 3 }}>
        <Typography variant="body1" color="text.secondary">
          Authentication UI will be added in the next frontend workflow step.
        </Typography>
      </Paper>
    </Stack>
  );
}
