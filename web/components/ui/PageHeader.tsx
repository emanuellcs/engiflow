import RefreshIcon from "@mui/icons-material/Refresh";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import CircularProgress from "@mui/material/CircularProgress";
import { useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { useTranslation } from "@/context/I18nContext";

export type PageHeaderProps = {
  /** Primary page title rendered as the route-level heading. */
  title: string;
  /** Optional secondary text that clarifies the page purpose. */
  description?: string;
  /** Optional action element, commonly a right-aligned command button. */
  actionButton?: ReactNode;
  /** Whether to show the global refresh button. Defaults to true. */
  showRefresh?: boolean;
  /** Optional custom refresh handler. If provided, replaces the default router.refresh() behavior. */
  onRefresh?: () => void;
  /** Whether the current page is loading data. If true, the refresh button shows a spinner. */
  isLoading?: boolean;
};

/**
 * Renders a dense, responsive page heading with optional supporting copy and
 * one right-aligned action. The layout stacks on mobile and aligns horizontally
 * on larger screens.
 *
 * @param props - Page header rendering options.
 * @param props.title - Primary page title rendered in an h1.
 * @param props.description - Optional supporting copy below the title.
 * @param props.actionButton - Optional action element displayed at the end.
 * @param props.showRefresh - Whether to show the refresh button.
 * @param props.onRefresh - Custom refresh logic.
 * @param props.isLoading - Whether the page is in a loading state.
 * @returns A standardized page header for authenticated EngiFlow views.
 */
export default function PageHeader({
  title,
  description,
  actionButton,
  showRefresh = true,
  onRefresh,
  isLoading = false,
}: PageHeaderProps) {
  const { t } = useTranslation();
  const router = useRouter();

  const handleRefresh = () => {
    if (onRefresh) {
      onRefresh();
    } else {
      router.refresh();
    }
  };

  return (
    <Stack
      component="header"
      direction={{ xs: "column", sm: "row" }}
      spacing={2}
      sx={{
        alignItems: { xs: "stretch", sm: "center" },
        justifyContent: "space-between",
        minWidth: 0,
      }}
    >
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="h4" component="h1">
          {title}
        </Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}
      </Box>
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignSelf: { xs: "stretch", sm: "center" }, alignItems: "center" }}
      >
        {showRefresh && (
          <Tooltip title={onRefresh ? t("header.refreshData") : t("header.refreshPage")}>
            <span>
              <IconButton 
                onClick={handleRefresh} 
                disabled={isLoading}
                color="primary"
                size="small" 
                aria-label="refresh"
                sx={{
                  border: 1,
                  borderColor: "divider",
                  bgcolor: "background.paper",
                  width: 36,
                  height: 36,
                  "&:hover": {
                    bgcolor: "action.hover",
                  }
                }}
              >
                {isLoading ? (
                  <CircularProgress size={20} color="inherit" thickness={5} />
                ) : (
                  <RefreshIcon fontSize="small" />
                )}
              </IconButton>
            </span>
          </Tooltip>
        )}
        {actionButton}
      </Stack>
    </Stack>
  );
}
