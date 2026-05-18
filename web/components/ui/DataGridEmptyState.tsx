import FolderOffOutlinedIcon from "@mui/icons-material/FolderOffOutlined";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";

type DataGridEmptyStateProps = {
  icon?: ReactNode;
  message?: string;
  description?: string;
};

export default function DataGridEmptyState({
  icon,
  message = "No records found",
  description = "There is currently no data to display.",
}: DataGridEmptyStateProps) {
  return (
    <Stack
      spacing={1.5}
      sx={{
        alignItems: "center",
        justifyContent: "center",
        minHeight: "100%",
        p: 3,
        textAlign: "center",
      }}
    >
      {icon ?? <FolderOffOutlinedIcon sx={{ fontSize: 48, color: "grey.400" }} />}
      <Stack spacing={0.5}>
        <Typography variant="h6" color="text.primary" sx={{ fontWeight: 600 }}>
          {message}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>
      </Stack>
    </Stack>
  );
}
