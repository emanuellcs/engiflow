"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Paper from "@mui/material/Paper";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import SvgIcon from "@mui/material/SvgIcon";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type { ChipProps } from "@mui/material/Chip";
import type { ComponentProps } from "react";
import { useEffect, useState } from "react";
import ProtectedRoute from "@/components/ProtectedRoute";
import { ApiError, apiFetch } from "@/lib/api/client";

type EcoPriority = "Low" | "Medium" | "High";
type EcoStatus = "Draft" | "UnderReview" | "Approved" | "Rejected" | "Implemented";

interface EcoSummaryDto {
  id: string;
  companyId: string;
  title: string;
  priority: EcoPriority;
  status: EcoStatus;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

interface PagedResult<TItem> {
  items: TItem[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

const pageSize = 20;
const skeletonRowCount = 6;

export default function Home() {
  return (
    <ProtectedRoute>
      <EcoDashboard />
    </ProtectedRoute>
  );
}

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
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={2}
        sx={{
          alignItems: { xs: "stretch", sm: "center" },
          justifyContent: "space-between",
        }}
      >
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="h4" component="h1">
            Engineering Change Orders
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Review current ECO activity across the workspace.
          </Typography>
        </Box>
        <Button
          type="button"
          variant="contained"
          onClick={() => {
            console.log("Create ECO clicked");
          }}
          sx={{
            alignSelf: { xs: "stretch", sm: "center" },
            minWidth: 128,
            textTransform: "none",
          }}
        >
          Create ECO
        </Button>
      </Stack>

      {errorMessage ? (
        <Alert severity="error" variant="outlined">
          {errorMessage}
        </Alert>
      ) : null}

      <TableContainer component={Paper} elevation={1}>
        <Table size="small" aria-label="Engineering Change Orders">
          <TableHead>
            <TableRow
              sx={{
                bgcolor: "grey.100",
                "& th": {
                  fontWeight: 600,
                  color: "text.primary",
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
                      <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
                        {formatShortId(eco.id)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" noWrap sx={{ maxWidth: 420 }}>
                        {eco.title}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <PriorityChip priority={eco.priority} />
                    </TableCell>
                    <TableCell>
                      <StatusChip status={eco.status} />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
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
          <EcoEmptyIcon sx={{ fontSize: 48, color: "grey.500" }} />
          <Typography variant="body1" color="text.secondary">
            No Engineering Change Orders found.
          </Typography>
        </Stack>
      </TableCell>
    </TableRow>
  );
}

function StatusChip({ status }: { status: EcoStatus }) {
  const colorByStatus: Record<EcoStatus, ChipProps["color"]> = {
    Approved: "success",
    Rejected: "error",
    UnderReview: "warning",
    Draft: "default",
    Implemented: "secondary",
  };

  return (
    <Chip
      label={formatEnumLabel(status)}
      color={colorByStatus[status]}
      size="small"
      variant={status === "Draft" ? "outlined" : "filled"}
      sx={{ minWidth: 98, fontWeight: 500 }}
    />
  );
}

function PriorityChip({ priority }: { priority: EcoPriority }) {
  const colorByPriority: Record<EcoPriority, ChipProps["color"]> = {
    High: "error",
    Medium: "warning",
    Low: "default",
  };

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

function EcoEmptyIcon(props: ComponentProps<typeof SvgIcon>) {
  return (
    <SvgIcon viewBox="0 0 48 48" {...props}>
      <path d="M12 8h18l6 6v26H12V8Zm16 3v6h6l-6-6Zm-12 1v24h16V20h-8V12h-8Zm4 12h8v3h-8v-3Zm0 7h12v3H20v-3Z" />
    </SvgIcon>
  );
}

function formatShortId(id: string): string {
  return id.length > 8 ? id.slice(0, 8).toUpperCase() : id.toUpperCase();
}

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

function formatEnumLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function getEcoListErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return "Unable to load Engineering Change Orders. Refresh the page or try again later.";
  }

  return "Unable to load Engineering Change Orders. Check your connection and try again.";
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}
