"use client";

import AddIcon from "@mui/icons-material/Add";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import FilterAltOffIcon from "@mui/icons-material/FilterAltOff";
import SearchIcon from "@mui/icons-material/Search";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import FormControl from "@mui/material/FormControl";
import FormControlLabel from "@mui/material/FormControlLabel";
import InputAdornment from "@mui/material/InputAdornment";
import InputLabel from "@mui/material/InputLabel";
import Link from "@mui/material/Link";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { DataGrid, type GridColDef, type GridPaginationModel } from "@mui/x-data-grid";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import type { Dayjs } from "dayjs";
import { useCallback, useEffect, useMemo, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import PageHeader from "@/components/ui/PageHeader";
import PriorityChip from "@/components/ui/PriorityChip";
import StatusChip from "@/components/ui/StatusChip";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import type {
  EcoPriority,
  EcoReviewContextDto,
  EcoStatus,
  EcoSummaryDto,
  EcoUserDto,
  PagedResult,
} from "@/lib/types/eco";

type EcoListFilters = {
  search: string;
  status: "All" | EcoStatus;
  priority: "All" | EcoPriority;
  createdFrom: Dayjs | null;
  createdTo: Dayjs | null;
  createdByMe: boolean;
  awaitingMyReview: boolean;
};

const defaultPageSize = 20;
const requesterRole = "Requester";
const approverRoles = new Set(["Owner", "Administrator", "Approver"]);
const statusOptions: EcoStatus[] = [
  "Draft",
  "UnderReview",
  "Approved",
  "Canceled",
  "Rejected",
  "Implemented",
];
const priorityOptions: EcoPriority[] = ["Low", "Medium", "High", "Critical"];
const initialFilters: EcoListFilters = {
  search: "",
  status: "All",
  priority: "All",
  createdFrom: null,
  createdTo: null,
  createdByMe: false,
  awaitingMyReview: false,
};

/**
 * Renders the authenticated Engineering Change Orders page.
 *
 * @returns The ECO DataGrid dashboard.
 */
export default function EcosPage() {
  return <EcoDashboard />;
}

/**
 * Loads and renders the current tenant's Engineering Change Orders in a
 * server-paginated MUI X DataGrid with PR-like review context.
 *
 * @returns The ECO dashboard content.
 */
function EcoDashboard() {
  const { user } = useAuth();
  const [rows, setRows] = useState<EcoSummaryDto[]>([]);
  const [rowCount, setRowCount] = useState(0);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: defaultPageSize,
  });
  const [filters, setFilters] = useState<EcoListFilters>(initialFilters);
  const [reviewContext, setReviewContext] = useState<EcoReviewContextDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const usersById = useMemo(
    () => createUserLookup(reviewContext?.users ?? []),
    [reviewContext],
  );
  const approvers = useMemo(
    () => (reviewContext?.users ?? []).filter((item) => approverRoles.has(item.role)),
    [reviewContext],
  );
  const canCreateEco = isAdminOrOwner(user?.role) || user?.role === requesterRole;
  const minApprovalsRequired = reviewContext?.minApprovalsRequired ?? 1;

  useEffect(() => {
    const controller = new AbortController();

    async function loadReviewContext(): Promise<void> {
      try {
        const context = await apiFetch<EcoReviewContextDto>(
          "/api/ecos/review-context",
          { signal: controller.signal },
        );

        setReviewContext(context);
      } catch (error) {
        if (!isAbortError(error)) {
          setReviewContext({ minApprovalsRequired: 1, users: [] });
        }
      }
    }

    void loadReviewContext();

    return () => {
      controller.abort();
    };
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    const searchDebounce = window.setTimeout(() => {
      void loadEcos();
    }, 250);

    async function loadEcos(): Promise<void> {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const result = await apiFetch<PagedResult<EcoSummaryDto>>(
          buildEcoListPath(paginationModel, filters),
          { signal: controller.signal },
        );

        setRows(Array.isArray(result.items) ? result.items : []);
        setRowCount(Number.isFinite(result.totalCount) ? result.totalCount : 0);
      } catch (error) {
        if (isAbortError(error)) {
          return;
        }

        setRows([]);
        setRowCount(0);
        setErrorMessage(getEcoListErrorMessage(error));
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    }

    return () => {
      window.clearTimeout(searchDebounce);
      controller.abort();
    };
  }, [filters, paginationModel]);

  const resetToFirstPage = useCallback(() => {
    setPaginationModel((current) => ({ ...current, page: 0 }));
  }, []);

  const columns = useMemo<GridColDef<EcoSummaryDto>[]>(
    () => [
      {
        field: "id",
        headerName: "ECO",
        minWidth: 170,
        flex: 0.8,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
            <Stack spacing={0} sx={{ minWidth: 0 }}>
              <Link
                component={NextLink}
                href={`/ecos/${params.row.id}`}
                underline="hover"
                color="primary"
                sx={{ fontFamily: "monospace", fontWeight: 700, fontSize: "0.875rem" }}
              >
                {formatShortId(params.row.id)}
              </Link>
              <Typography variant="caption" color="text.secondary" noWrap sx={{ fontSize: "0.75rem" }}>
                Round {params.row.reviewRound || 0}
              </Typography>
            </Stack>
          </Box>
        ),
      },
      {
        field: "title",
        headerName: "Title",
        minWidth: 280,
        flex: 1.5,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
            <Link
              component={NextLink}
              href={`/ecos/${params.row.id}`}
              underline="hover"
              color="inherit"
              sx={{
                display: "block",
                minWidth: 0,
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
                width: "100%",
                fontWeight: 500,
                fontSize: "0.875rem",
              }}
            >
              {params.row.title}
            </Link>
          </Box>
        ),
      },
      {
        field: "createdByUserId",
        headerName: "Requester",
        minWidth: 210,
        flex: 1,
        sortable: false,
        renderCell: (params) => {
          const requester = usersById.get(params.row.createdByUserId);

          return (
            <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
              <UserCell
                fallbackId={params.row.createdByUserId}
                user={requester}
              />
            </Box>
          );
        },
      },
      {
        field: "review",
        headerName: "Review",
        minWidth: 210,
        flex: 1,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
            <ReviewerProgressCell
              approvalCount={params.row.currentRoundApprovalCount}
              approvers={approvers}
              minApprovalsRequired={minApprovalsRequired}
              requestChangesCount={params.row.currentRoundRequestChangesCount}
              status={params.row.status}
            />
          </Box>
        ),
      },
      {
        field: "priority",
        headerName: "Priority",
        minWidth: 118,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <PriorityChip priority={params.row.priority} />
          </Box>
        ),
      },
      {
        field: "status",
        headerName: "Status",
        minWidth: 138,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <StatusChip status={params.row.status} />
          </Box>
        ),
      },
      {
        field: "createdAt",
        headerName: "Created",
        minWidth: 150,
        sortable: false,
        renderCell: (params) => (
          <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
            <Typography variant="body2" color="text.secondary">
              {formatDate(params.value as string)}
            </Typography>
          </Box>
        ),
      },
    ],
    [approvers, minApprovalsRequired, usersById],
  );

  return (
    <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", minHeight: 0, gap: 2.5 }}>
      <PageHeader
        title="Engineering Change Orders"
        description="Search, review, and triage engineering changes across the workspace."
        actionButton={canCreateEco ? (
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
              fontWeight: 600,
            }}
          >
            Create ECO
          </Button>
        ) : undefined}
      />

      <Paper
        elevation={0}
        sx={{
          flexGrow: 1,
          display: "flex",
          flexDirection: "column",
          minHeight: 0,
          border: 1,
          borderColor: "divider",
          borderRadius: 2,
          overflow: "hidden",
        }}
      >
        <Stack spacing={0} sx={{ flexGrow: 1, minHeight: 0 }}>
          <Box sx={{ p: { xs: 1.5, md: 2 }, borderBottom: 1, borderColor: "divider", bgcolor: "background.paper" }}>
            <EcoFilterBar
              filters={filters}
              onFiltersChange={(nextFilters) => {
                setFilters(nextFilters);
                resetToFirstPage();
              }}
            />
          </Box>

          <Box sx={{ flexGrow: 1, width: "100%", minHeight: 0 }}>
            <DataGrid
              rows={rows}
              columns={columns}
              getRowId={(row) => row.id}
              rowCount={rowCount}
              loading={isLoading}
              paginationMode="server"
              paginationModel={paginationModel}
              onPaginationModelChange={setPaginationModel}
              pageSizeOptions={[10, 20, 50, 100]}
              disableRowSelectionOnClick
              slots={{
                noRowsOverlay: EmptyGridOverlay,
              }}
              slotProps={{
                loadingOverlay: {
                  variant: "linear-progress",
                  noRowsVariant: "skeleton",
                },
              }}
              sx={{
                border: 0,
                borderRadius: 0,
                "& .MuiDataGrid-columnHeaders": {
                  bgcolor: "action.hover",
                  borderBottom: 1,
                  borderColor: "divider",
                },
                "& .MuiDataGrid-cell": {
                  borderColor: "divider",
                },
                "& .MuiDataGrid-footerContainer": {
                  borderTop: 1,
                  borderColor: "divider",
                },
              }}
            />
          </Box>

          {errorMessage ? (
            <Box sx={{ p: 2, borderTop: 1, borderColor: "divider" }}>
              <Typography variant="body2" color="error">
                {errorMessage}
              </Typography>
            </Box>
          ) : null}
        </Stack>
      </Paper>
    </Box>
  );
}


type EcoFilterBarProps = {
  filters: EcoListFilters;
  onFiltersChange: (filters: EcoListFilters) => void;
};

/**
 * Renders external MUI controls for server-side ECO filtering.
 *
 * @param props - Filter bar props.
 * @returns Filter controls used by the ECO DataGrid.
 */
function EcoFilterBar({ filters, onFiltersChange }: EcoFilterBarProps) {
  function patchFilters(patch: Partial<EcoListFilters>): void {
    onFiltersChange({ ...filters, ...patch });
  }

  return (
    <Stack spacing={1.5}>
      <Stack
        direction={{ xs: "column", lg: "row" }}
        spacing={1.5}
        sx={{ alignItems: { xs: "stretch", lg: "center" } }}
      >
        <TextField
          label="Search ECOs"
          value={filters.search}
          onChange={(event) => patchFilters({ search: event.target.value })}
          size="small"
          sx={{ minWidth: { xs: "100%", lg: 300 }, flex: 1 }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel id="eco-status-filter-label">Status</InputLabel>
          <Select
            labelId="eco-status-filter-label"
            label="Status"
            value={filters.status}
            onChange={(event: SelectChangeEvent) =>
              patchFilters({ status: event.target.value as EcoListFilters["status"] })
            }
          >
            <MenuItem value="All">All</MenuItem>
            {statusOptions.map((status) => (
              <MenuItem key={status} value={status}>
                {formatStatusLabel(status)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel id="eco-priority-filter-label">Priority</InputLabel>
          <Select
            labelId="eco-priority-filter-label"
            label="Priority"
            value={filters.priority}
            onChange={(event: SelectChangeEvent) =>
              patchFilters({ priority: event.target.value as EcoListFilters["priority"] })
            }
          >
            <MenuItem value="All">All</MenuItem>
            {priorityOptions.map((priority) => (
              <MenuItem key={priority} value={priority}>
                {priority}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        <DatePicker
          label="Created from"
          value={filters.createdFrom}
          onChange={(value) => patchFilters({ createdFrom: value })}
          slotProps={{ textField: { size: "small", sx: { minWidth: 170 } } }}
        />
        <DatePicker
          label="Created to"
          value={filters.createdTo}
          onChange={(value) => patchFilters({ createdTo: value })}
          slotProps={{ textField: { size: "small", sx: { minWidth: 170 } } }}
        />
        <Button
          type="button"
          variant="outlined"
          startIcon={<FilterAltOffIcon />}
          onClick={() => onFiltersChange(initialFilters)}
          sx={{ minWidth: 128, textTransform: "none" }}
        >
          Clear
        </Button>
      </Stack>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
        <FormControlLabel
          control={
            <Checkbox
              checked={filters.createdByMe}
              onChange={(event) => patchFilters({ createdByMe: event.target.checked })}
              size="small"
            />
          }
          label="Created by me"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={filters.awaitingMyReview}
              onChange={(event) =>
                patchFilters({ awaitingMyReview: event.target.checked })
              }
              size="small"
            />
          }
          label="Awaiting my review"
        />
      </Stack>
    </Stack>
  );
}

type UserCellProps = {
  fallbackId: string;
  user: EcoUserDto | undefined;
};

/**
 * Renders a user avatar and display name for DataGrid cells.
 *
 * @param props - User cell props.
 * @returns User identity cell.
 */
function UserCell({ fallbackId, user }: UserCellProps) {
  const name = user?.name ?? formatShortId(fallbackId);

  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: "center", minWidth: 0 }}>
      <Avatar sx={{ width: 28, height: 28, fontSize: 12 }}>
        {getInitials(name)}
      </Avatar>
      <Stack sx={{ minWidth: 0 }}>
        <Typography variant="body2" noWrap>
          {name}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          {user?.role ?? "User"}
        </Typography>
      </Stack>
    </Stack>
  );
}

type ReviewerProgressCellProps = {
  approvalCount: number;
  approvers: EcoUserDto[];
  minApprovalsRequired: number;
  requestChangesCount: number;
  status: EcoStatus;
};

/**
 * Renders current review progress and approver avatars in an ECO list row.
 *
 * @param props - Reviewer progress options.
 * @returns Review progress cell.
 */
function ReviewerProgressCell({
  approvalCount,
  approvers,
  minApprovalsRequired,
  requestChangesCount,
  status,
}: ReviewerProgressCellProps) {
  return (
    <Stack spacing={0.5} sx={{ minWidth: 0 }}>
      <Stack direction="row" spacing={0.5} sx={{ alignItems: "center" }}>
        {approvers.slice(0, 3).map((approver) => (
          <Avatar
            key={approver.id}
            sx={{ width: 24, height: 24, fontSize: 11 }}
            title={approver.name}
          >
            {getInitials(approver.name)}
          </Avatar>
        ))}
        {approvers.length > 3 ? (
          <Typography variant="caption" color="text.secondary">
            +{approvers.length - 3}
          </Typography>
        ) : null}
      </Stack>
      <Typography variant="caption" color="text.secondary" noWrap>
        {status === "UnderReview"
          ? `${approvalCount}/${minApprovalsRequired} approved`
          : formatStatusLabel(status)}
        {requestChangesCount > 0 ? ` • ${requestChangesCount} changes` : ""}
      </Typography>
    </Stack>
  );
}

/**
 * Renders the empty DataGrid overlay.
 *
 * @returns Empty grid overlay.
 */
function EmptyGridOverlay() {
  return (
    <Stack
      spacing={1.5}
      sx={{
        alignItems: "center",
        justifyContent: "center",
        minHeight: "100%",
        p: 3,
        textAlign: "center",
      }}
    >
      <AssignmentOutlinedIcon sx={{ fontSize: 48, color: "grey.500" }} />
      <Typography variant="body2" color="text.secondary">
        No Engineering Change Orders match the current filters.
      </Typography>
    </Stack>
  );
}

function buildEcoListPath(
  paginationModel: GridPaginationModel,
  filters: EcoListFilters,
): string {
  const params = new URLSearchParams({
    pageNumber: String(paginationModel.page + 1),
    pageSize: String(paginationModel.pageSize),
  });

  appendIfPresent(params, "search", filters.search.trim());
  appendIfPresent(params, "status", filters.status === "All" ? "" : filters.status);
  appendIfPresent(params, "priority", filters.priority === "All" ? "" : filters.priority);
  appendIfPresent(
    params,
    "createdFrom",
    filters.createdFrom?.startOf("day").toISOString() ?? "",
  );
  appendIfPresent(
    params,
    "createdTo",
    filters.createdTo?.endOf("day").toISOString() ?? "",
  );

  if (filters.createdByMe) {
    params.set("createdByMe", "true");
  }

  if (filters.awaitingMyReview) {
    params.set("awaitingMyReview", "true");
  }

  return `/api/ecos?${params.toString()}`;
}

function appendIfPresent(params: URLSearchParams, key: string, value: string): void {
  if (value.trim().length > 0) {
    params.set(key, value);
  }
}

function createUserLookup(users: EcoUserDto[]): Map<string, EcoUserDto> {
  return new Map(users.map((user) => [user.id, user]));
}

function formatShortId(id: string): string {
  return id.length > 8 ? id.slice(0, 8).toUpperCase() : id.toUpperCase();
}

function formatDate(value: string): string {
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

function formatStatusLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function getInitials(value: string): string {
  const parts = value.trim().split(/\s+/).filter(Boolean);
  const initials = parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join("");

  return initials || "?";
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
