"use client";

import AddIcon from "@mui/icons-material/Add";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { useEffect, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import PageHeader from "@/components/ui/PageHeader";
import PriorityChip from "@/components/ui/PriorityChip";
import StatusChip from "@/components/ui/StatusChip";
import { ApiError, apiFetch } from "@/lib/api/client";
import type { EcoSummaryDto, PagedResult } from "@/lib/types/eco";

const pageSize = 20;
const skeletonRowCount = 6;

/**
 * Renders the authenticated Engineering Change Orders page.
 *
 * @returns The ECO dashboard table view.
 */
export default function EcosPage() {
  return <EcoDashboard />;
}

/**
 * Loads and renders the current tenant's Engineering Change Orders in a dense,
 * horizontally scrollable Material UI table.
 *
 * @returns The ECO dashboard content including header, errors, table, and empty state.
 */
function EcoDashboard() {
  const [ecos, setEcos] = useState<EcoSummaryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadEcos() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const result = await apiFetch<PagedResult<EcoSummaryDto>>(
          `/api/ecos?pageNumber=1&pageSize=${pageSize}`,
          { signal: controller.signal },
        );

        setEcos(Array.isArray(result.items) ? result.items : []);
      } catch (error) {
        if (isAbortError(error)) {
          return;
        }

        setErrorMessage(getEcoListErrorMessage(error));
        setEcos([]);
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    void loadEcos();

    return () => {
      controller.abort();
    };
  }, []);

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title="Engineering Change Orders"
        description="Review current ECO activity across the workspace."
        actionButton={
          <Button
            component={NextLink}
            href="/ecos/new"
            type="button"
            variant="contained"
            startIcon={<AddIcon />}
            sx={{
              width: { xs: "100%", sm: "auto" },
              minWidth: 128,
              textTransform: "none",
            }}
          >
            Create ECO
          </Button>
        }
      />

      {errorMessage ? (
        <Alert severity="error" variant="outlined">
          {errorMessage}
        </Alert>
      ) : null}

      <TableContainer
        component={Paper}
        elevation={1}
        sx={{ width: "100%", overflowX: "auto" }}
      >
        <Table
          size="small"
          aria-label="Engineering Change Orders"
          sx={{ minWidth: 760 }}
        >
          <TableHead>
            <TableRow
              sx={{
                bgcolor: "grey.100",
                "& th": {
                  fontWeight: 600,
                  color: "text.primary",
                  whiteSpace: "nowrap",
                },
              }}
            >
              <TableCell width="18%">ID</TableCell>
              <TableCell>Title</TableCell>
              <TableCell width="16%">Priority</TableCell>
              <TableCell width="18%">Status</TableCell>
              <TableCell width="18%">Created Date</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading ? <EcoSkeletonRows /> : null}
            {!isLoading && ecos.length > 0
              ? ecos.map((eco) => (
                <TableRow hover key={eco.id}>
                  <TableCell>
                      <Link
                        component={NextLink}
                        href={`/ecos/${eco.id}`}
                        underline="hover"
                        color="inherit"
                        aria-label={`View ECO ${formatShortId(eco.id)}`}
                        sx={{ fontFamily: "monospace", fontSize: 14 }}
                      >
                        {formatShortId(eco.id)}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <Link
                        component={NextLink}
                        href={`/ecos/${eco.id}`}
                        underline="hover"
                        color="inherit"
                        sx={{
                          display: "block",
                          maxWidth: 420,
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                          fontSize: 14,
                        }}
                      >
                        {eco.title}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <PriorityChip priority={eco.priority} />
                    </TableCell>
                    <TableCell>
                      <StatusChip status={eco.status} />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" noWrap>
                        {formatCreatedDate(eco.createdAt)}
                      </Typography>
                    </TableCell>
                  </TableRow>
                ))
              : null}
            {!isLoading && !errorMessage && ecos.length === 0 ? (
              <EcoEmptyRow />
            ) : null}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}

/**
 * Renders skeleton rows that reserve table space while ECO data is loading.
 *
 * @returns A fixed set of loading placeholder rows.
 */
function EcoSkeletonRows() {
  return Array.from({ length: skeletonRowCount }, (_, index) => (
    <TableRow key={`eco-skeleton-${index}`}>
      <TableCell>
        <Skeleton variant="text" width={96} />
      </TableCell>
      <TableCell>
        <Skeleton variant="text" width="72%" />
      </TableCell>
      <TableCell>
        <Skeleton variant="rounded" width={78} height={24} />
      </TableCell>
      <TableCell>
        <Skeleton variant="rounded" width={106} height={24} />
      </TableCell>
      <TableCell>
        <Skeleton variant="text" width={112} />
      </TableCell>
    </TableRow>
  ));
}

/**
 * Renders the table body empty state shown when the API returns no ECO rows.
 *
 * @returns A full-width empty table row.
 */
function EcoEmptyRow() {
  return (
    <TableRow>
      <TableCell colSpan={5}>
        <Stack
          spacing={1.5}
          sx={{
            minHeight: 220,
            alignItems: "center",
            justifyContent: "center",
            color: "text.secondary",
            textAlign: "center",
          }}
        >
          <AssignmentOutlinedIcon sx={{ fontSize: 48, color: "grey.500" }} />
          <Typography variant="body1" color="text.secondary">
            No Engineering Change Orders found.
          </Typography>
        </Stack>
      </TableCell>
    </TableRow>
  );
}

/**
 * Formats a full ECO identifier into the short uppercase token used in tables.
 *
 * @param id - ECO identifier returned by the API.
 * @returns The first eight uppercase characters when the identifier is long.
 */
function formatShortId(id: string): string {
  return id.length > 8 ? id.slice(0, 8).toUpperCase() : id.toUpperCase();
}

/**
 * Formats an API timestamp as a compact U.S. display date.
 *
 * @param value - ISO timestamp returned by the API.
 * @returns A formatted date or a dash when the timestamp cannot be parsed.
 */
function formatCreatedDate(value: string): string {
  const timestamp = Date.parse(value);

  if (Number.isNaN(timestamp)) {
    return "-";
  }

  return new Intl.DateTimeFormat("en-US", {
    year: "numeric",
    month: "short",
    day: "2-digit",
  }).format(timestamp);
}

/**
 * Produces the user-facing ECO list error message for API and network failures.
 *
 * @param error - Unknown error thrown while loading the ECO list.
 * @returns A stable supportable error message for the page alert.
 */
function getEcoListErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return "Unable to load Engineering Change Orders. Refresh the page or try again later.";
  }

  return "Unable to load Engineering Change Orders. Check your connection and try again.";
}

/**
 * Detects abort errors produced by an AbortController-backed fetch request.
 *
 * @param error - Unknown error thrown by the ECO load operation.
 * @returns True when the error is a browser abort event.
 */
function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}
