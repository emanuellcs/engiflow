import RefreshIcon from "@mui/icons-material/Refresh";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import {
  GridToolbarColumnsButton,
  GridToolbarContainer,
  GridToolbarDensitySelector,
  GridToolbarExport,
  GridToolbarFilterButton,
  type GridToolbarProps,
} from "@mui/x-data-grid";

interface DataGridCustomToolbarProps extends Partial<GridToolbarProps> {
  isLoading?: boolean;
  onRefresh?: () => void;
}

export default function DataGridCustomToolbar({
  isLoading = false,
  onRefresh,
  ...other
}: DataGridCustomToolbarProps) {
  return (
    <GridToolbarContainer
      {...other}
      sx={{
        p: 1,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        borderBottom: 1,
        borderColor: "divider",
        flexWrap: "nowrap", // Prevent wrapping to keep the refresh button visible
      }}
    >
      <Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
        <GridToolbarColumnsButton />
        <GridToolbarFilterButton />
        <GridToolbarDensitySelector />
        <GridToolbarExport />
      </Box>
      {onRefresh ? (
        <Box sx={{ ml: 1 }}>
          <Tooltip title="Refresh data">
            <span>
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation();
                  onRefresh();
                }}
                disabled={isLoading}
                color="primary"
                sx={{ width: 34, height: 34 }}
              >
                {isLoading ? (
                  <CircularProgress size={20} color="inherit" thickness={5} />
                ) : (
                  <RefreshIcon fontSize="small" />
                )}
              </IconButton>
            </span>
          </Tooltip>
        </Box>
      ) : null}
    </GridToolbarContainer>
  );
}
