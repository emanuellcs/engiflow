import Chip from "@mui/material/Chip";
import type { ChipProps } from "@mui/material/Chip";
import type { EcoPriority } from "@/lib/types/eco";

export type { EcoPriority } from "@/lib/types/eco";

export type PriorityChipProps = {
  /** ECO priority returned by the API. */
  priority: EcoPriority;
};

const colorByPriority: Record<EcoPriority, ChipProps["color"]> = {
  Critical: "error",
  High: "warning",
  Medium: "info",
  Low: "default",
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
      color={colorByPriority[priority]}
      size="small"
      variant={priority === "Low" ? "outlined" : "filled"}
      sx={{ minWidth: 76, fontWeight: 500 }}
    />
  );
}
