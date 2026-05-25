import Chip from "@mui/material/Chip";
import { useTranslation } from "@/context/I18nContext";
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
  const { t } = useTranslation();

  return (
    <Chip
      label={getStatusLabel(status, t)}
      size="small"
      variant={status === "Draft" ? "outlined" : "filled"}
      sx={{
        minWidth: 104,
        height: 34,
        fontWeight: 500,
        ...chipSxByStatus[status],
      }}
    />
  );
}

/**
 * Converts a PascalCase or camelCase enum token into localized UI copy.
 *
 * @param value - Raw API enum string.
 * @param t - Translation function.
 * @returns A localized display label.
 */
function getStatusLabel(value: EcoStatus, t: (key: string) => string): string {
  const map: Record<EcoStatus, string> = {
    Draft: t("status.draft"),
    UnderReview: t("status.underReview"),
    Approved: t("status.approved"),
    Canceled: t("status.canceled"),
    Rejected: t("status.rejected"),
    Implemented: t("status.implemented"),
  };
  return map[value] || value;
}
