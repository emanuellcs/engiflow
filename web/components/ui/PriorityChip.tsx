import Chip from "@mui/material/Chip";
import type { EcoPriority } from "@/lib/types/eco";

export type { EcoPriority } from "@/lib/types/eco";

export type PriorityChipProps = {
  /** ECO priority returned by the API. */
  priority: EcoPriority;
};

const chipSxByPriority: Record<EcoPriority, object> = {
  Critical: {
    borderColor: "error.main",
    color: "error.dark",
    bgcolor: "rgba(211, 47, 47, 0.08)",
  },
  High: {
    borderColor: "warning.dark",
    color: "warning.dark",
    bgcolor: "rgba(245, 124, 0, 0.08)",
  },
  Medium: {
    borderColor: "info.main",
    color: "info.dark",
    bgcolor: "rgba(2, 136, 209, 0.08)",
  },
  Low: {
    borderColor: "success.main",
    color: "success.dark",
    bgcolor: "rgba(46, 125, 50, 0.08)",
  },
};

/**
 * Renders a compact Material UI chip for an ECO priority using the application's
 * shared priority color semantics.
 *
 * @param props - Priority chip rendering options.
 * @param props.priority - ECO priority value to display.
 * @returns A dense priority chip suitable for tables and summary views.
 */
export default function PriorityChip({ priority }: PriorityChipProps) {
  return (
    <Chip
      label={priority}
      size="small"
      variant="outlined"
      sx={{
        minWidth: 76,
        fontWeight: 500,
        ...chipSxByPriority[priority],
      }}
    />
  );
}
