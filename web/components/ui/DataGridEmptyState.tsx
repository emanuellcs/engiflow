import FolderOffOutlinedIcon from "@mui/icons-material/FolderOffOutlined";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";
import { useTranslation } from "@/context/I18nContext";

type DataGridEmptyStateProps = {
  icon?: ReactNode;
  message?: string;
  description?: string;
};

export default function DataGridEmptyState({
  icon,
  message,
  description,
}: DataGridEmptyStateProps) {
  const { t } = useTranslation();

  return (
    <Stack
      spacing={1.5}
      sx={{
        alignItems: "center",
        justifyContent: "center",
        height: "100%",
        width: "100%",
        p: 3,
        textAlign: "center",
        flex: 1,
      }}
    >
      {icon ?? <FolderOffOutlinedIcon sx={{ fontSize: 48, color: "grey.400" }} />}
      <Stack spacing={0.5}>
        <Typography variant="h6" color="text.primary" sx={{ fontWeight: 600 }}>
          {message ?? t("common.noRecords")}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {description ?? t("common.noData")}
        </Typography>
      </Stack>
    </Stack>
  );
}
