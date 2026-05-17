import Chip from "@mui/material/Chip";
import type { EcoStatus } from "@/lib/types/eco";

export type { EcoStatus } from "@/lib/types/eco";

export type StatusChipProps = {
  /** ECO workflow status returned by the API. */
  status: EcoStatus;
};

const chipSxByStatus: Record<EcoStatus, object> = {
  Draft: {
    bgcolor: "grey.100",
    color: "text.secondary",
    borderColor: "grey.300",
  },
  UnderReview: {
    bgcolor: "info.light",
    color: "info.contrastText",
  },
  Approved: {
    bgcolor: "success.main",
    color: "success.contrastText",
  },
  Canceled: {
    bgcolor: "grey.700",
    color: "common.white",
  },
  Rejected: {
    bgcolor: "error.main",
    color: "error.contrastText",
  },
  Implemented: {
    bgcolor: "secondary.main",
    color: "secondary.contrastText",
  },
};

/**
 * Renders a compact Material UI chip for an ECO workflow status using the
 * application's shared status color semantics.
 *
 * @param props - Status chip rendering options.
 * @param props.status - ECO status value to display.
 * @returns A dense status chip suitable for tables and summary views.
 */
export default function StatusChip({ status }: StatusChipProps) {
  return (
    <Chip
      label={formatStatusLabel(status)}
      size="small"
      variant={status === "Draft" ? "outlined" : "filled"}
      sx={{
        minWidth: 104,
        fontWeight: 500,
        ...chipSxByStatus[status],
      }}
    />
  );
}

/**
 * Converts a PascalCase or camelCase enum token into readable UI copy.
 *
 * @param value - Raw API enum string.
 * @returns A display label with word boundaries inserted.
 */
function formatStatusLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}
