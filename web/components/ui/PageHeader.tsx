import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { ReactNode } from "react";

export type PageHeaderProps = {
  /** Primary page title rendered as the route-level heading. */
  title: string;
  /** Optional secondary text that clarifies the page purpose. */
  description?: string;
  /** Optional action element, commonly a right-aligned command button. */
  actionButton?: ReactNode;
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
 * @returns A standardized page header for authenticated EngiFlow views.
 */
export default function PageHeader({
  title,
  description,
  actionButton,
}: PageHeaderProps) {
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
      {actionButton ? (
        <Box sx={{ alignSelf: { xs: "stretch", sm: "center" } }}>
          {actionButton}
        </Box>
      ) : null}
    </Stack>
  );
}
