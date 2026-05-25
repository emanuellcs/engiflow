"use client";

import AddIcon from "@mui/icons-material/Add";
import PersonOffIcon from "@mui/icons-material/PersonOff";
import RestoreIcon from "@mui/icons-material/Restore";
import SearchIcon from "@mui/icons-material/Search";
import GroupIcon from "@mui/icons-material/Group";
import Alert from "@mui/material/Alert";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Divider from "@mui/material/Divider";
import FormControl from "@mui/material/FormControl";
import FormHelperText from "@mui/material/FormHelperText";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import InputLabel from "@mui/material/InputLabel";
import MenuItem from "@mui/material/MenuItem";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { DataGrid, type GridColDef, type GridRenderCellParams, type GridToolbarProps } from "@mui/x-data-grid";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import DataGridCustomToolbar from "@/components/ui/DataGridCustomToolbar";
import DataGridEmptyState from "@/components/ui/DataGridEmptyState";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";
import { useTranslation } from "@/context/I18nContext";

/** Defines all roles returned by the user-management API. */
type UserRole = "Owner" | "Administrator" | "Approver" | "Requester" | "Viewer";

/** Defines roles administrators can assign after tenant bootstrap. */
type MutableUserRole = Exclude<UserRole, "Owner">;

/** Defines user lifecycle states rendered in the admin user table. */
type UserStatus = "PendingActivation" | "Active" | "Deactivated";

/** Defines the role filter options available above the user grid. */
type RoleFilter = "All" | UserRole;

/** Describes the user summary returned by the admin user-management API. */
type UserSummary = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  lastLoginAt: string | null;
};

/** Stores the invite-user modal form values. */
type InviteFormState = {
  name: string;
  email: string;
  role: MutableUserRole;
};

/** Stores field-level invite-user validation errors. */
type InviteFieldErrors = Partial<Record<keyof InviteFormState, string>>;

const allRoles: UserRole[] = ["Owner", "Administrator", "Approver", "Requester", "Viewer"];
const mutableRoles: MutableUserRole[] = ["Administrator", "Approver", "Requester", "Viewer"];
const initialInviteForm: InviteFormState = {
  name: "",
  email: "",
  role: "Requester",
};
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Renders tenant user management, including invitation, status, deactivation, and reactivation workflows.
 */
export default function UserManagementPage() {
  const { t, locale } = useTranslation();
  const { user } = useAuth();
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<RoleFilter>("All");
  const [roleUpdatingUserId, setRoleUpdatingUserId] = useState<string | null>(null);
  const [confirmDeactivateUser, setConfirmDeactivateUser] = useState<UserSummary | null>(null);
  const [isDeactivationPending, setIsDeactivationPending] = useState(false);
  const [confirmReactivateUser, setConfirmReactivateUser] = useState<UserSummary | null>(null);
  const [isReactivationPending, setIsReactivationPending] = useState(false);
  const isAdministrator = isAdminOrOwner(user?.role);

  const requestUsers = useCallback(async () => {
    return apiFetch<UserSummary[]>("/api/users");
  }, []);

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await requestUsers();
      setUsers(response);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, t("users.list.error"), t));
    } finally {
      setIsLoading(false);
    }
  }, [requestUsers, t]);

  const CustomToolbar = useMemo(() => {
    return function UserCustomToolbar(props: GridToolbarProps) {
      return (
        <DataGridCustomToolbar
          {...props}
          isLoading={isLoading}
          onRefresh={() => void loadUsers()}
        />
      );
    };
  }, [isLoading, loadUsers]);

  useEffect(() => {
    if (!isAdministrator) {
      return;
    }

    let isMounted = true;

    requestUsers()
      .then((response) => {
        if (isMounted) {
          setUsers(response);
        }
      })
      .catch((error) => {
        if (isMounted) {
          setErrorMessage(getApiErrorMessage(error, t("users.list.error"), t));
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [isAdministrator, requestUsers, t]);

  const filteredUsers = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    return users.filter((workspaceUser) => {
      const matchesRole = roleFilter === "All" || workspaceUser.role === roleFilter;
      const matchesQuery =
        !normalizedQuery ||
        workspaceUser.name.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.email.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.role.toLowerCase().includes(normalizedQuery) ||
        getStatusLabel(workspaceUser.status, t).toLowerCase().includes(normalizedQuery);

      return matchesRole && matchesQuery;
    });
  }, [roleFilter, searchQuery, users, t]);

  const handleRoleChange = useCallback(async (workspaceUser: UserSummary, role: MutableUserRole) => {
    if (workspaceUser.role === role) {
      return;
    }

    setRoleUpdatingUserId(workspaceUser.id);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const updatedUser = await apiFetch<UserSummary>(`/api/users/${workspaceUser.id}/role`, {
        method: "PUT",
        body: { role },
      });
      setUsers((current) =>
        current.map((item) => (item.id === updatedUser.id ? updatedUser : item)),
      );
      setSuccessMessage(t("users.roles.updateSuccess", { name: updatedUser.name }));
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, t("users.roles.updateError"), t));
    } finally {
      setRoleUpdatingUserId(null);
    }
  }, [t]);

  const handleDeactivateConfirmed = useCallback(async () => {
    if (!confirmDeactivateUser) {
      return;
    }

    setIsDeactivationPending(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await apiFetch<void>(`/api/users/${confirmDeactivateUser.id}/deactivate`, {
        method: "PUT",
      });
      setUsers((current) =>
        current.map((item) =>
          item.id === confirmDeactivateUser.id
            ? { ...item, status: "Deactivated" }
            : item,
        ),
      );
      setSuccessMessage(t("users.deactivate.success", { name: confirmDeactivateUser.name }));
      setConfirmDeactivateUser(null);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, t("users.deactivate.error"), t));
    } finally {
      setIsDeactivationPending(false);
    }
  }, [confirmDeactivateUser, t]);

  const handleReactivateConfirmed = useCallback(async (reason: string) => {
    if (!confirmReactivateUser) {
      return;
    }

    setIsReactivationPending(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const updatedUser = await apiFetch<UserSummary>(`/api/users/${confirmReactivateUser.id}/reactivate`, {
        method: "PUT",
        body: { reason },
      });
      setUsers((current) =>
        current.map((item) => (item.id === updatedUser.id ? updatedUser : item)),
      );
      setSuccessMessage(t("users.reactivate.success", { name: updatedUser.name }));
      setConfirmReactivateUser(null);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, t("users.reactivate.error"), t));
    } finally {
      setIsReactivationPending(false);
    }
  }, [confirmReactivateUser, t]);

  const columns = useMemo<GridColDef<UserSummary>[]>(() => [
    {
      field: "name",
      headerName: t("users.list.columns.name"),
      minWidth: 240,
      flex: 1,
      headerAlign: "left",
      align: "left",
      renderCell: (params: GridRenderCellParams<UserSummary, string>) => (
        <Stack direction="row" spacing={1.25} sx={{ alignItems: "center", height: "100%", minWidth: 0 }}>
          <Avatar sx={{ width: 32, height: 32, bgcolor: "secondary.main", fontSize: "0.8125rem", fontWeight: 600 }}>
            {getInitials(params.row.name)}
          </Avatar>
          <Typography variant="body2" sx={{ fontWeight: 500 }} noWrap>
            {params.row.name}
          </Typography>
        </Stack>
      ),
    },
    {
      field: "email",
      headerName: t("users.list.columns.email"),
      minWidth: 250,
      flex: 1,
      headerAlign: "left",
      align: "left",
      renderCell: (params) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
          <Typography variant="body2" color="text.secondary" noWrap>
            {params.value}
          </Typography>
        </Box>
      ),
    },
    {
      field: "role",
      headerName: t("users.list.columns.role"),
      minWidth: 220,
      headerAlign: "left",
      align: "left",
      renderCell: (params: GridRenderCellParams<UserSummary, UserRole>) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%", width: "100%" }}>
          <RoleSelectCell
            workspaceUser={params.row}
            currentUserId={user?.id}
            isPending={roleUpdatingUserId === params.row.id}
            onRoleChange={handleRoleChange}
          />
        </Box>
      ),
      sortComparator: (left, right) => allRoles.indexOf(left) - allRoles.indexOf(right),
    },
    {
      field: "lastLoginAt",
      headerName: t("users.list.columns.lastActive"),
      minWidth: 190,
      headerAlign: "left",
      align: "left",
      renderCell: (params) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
          <Typography variant="body2" color="text.secondary">
            {formatLastLogin(params.value, t, locale)}
          </Typography>
        </Box>
      ),
    },
    {
      field: "status",
      headerName: t("users.list.columns.status"),
      minWidth: 160,
      headerAlign: "left",
      align: "left",
      renderCell: (params: GridRenderCellParams<UserSummary, UserStatus>) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
          <Chip
            label={getStatusLabel(params.value ?? "PendingActivation", t)}
            color={getStatusChipColor(params.value ?? "PendingActivation")}
            size="small"
            variant={params.value === "Deactivated" ? "outlined" : "filled"}
            sx={{ fontWeight: 600 }}
          />
        </Box>
      ),
    },
    {
      field: "actions",
      headerName: "",
      width: 72,
      sortable: false,
      filterable: false,
      disableColumnMenu: true,
      headerAlign: "center",
      align: "center",
      renderCell: (params: GridRenderCellParams<UserSummary>) => (
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
          <UserRowActions
            workspaceUser={params.row}
            currentUserId={user?.id}
            onDeactivate={setConfirmDeactivateUser}
            onReactivate={setConfirmReactivateUser}
          />
        </Box>
      ),
    },
  ], [handleRoleChange, roleUpdatingUserId, user?.id, t, locale]);

  if (!isAdministrator) {
    return (
      <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", gap: 2.5 }}>
        <PageHeader
          title={t("users.list.title")}
          description={t("users.list.subtitle")}
        />
        <Alert severity="warning">
          {t("users.list.adminRequired")}
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", gap: 2.5, minHeight: 0 }}>
      <PageHeader
        title={t("users.list.title")}
        description={t("users.list.subtitle")}
        actionButton={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setSuccessMessage(null);
              setIsInviteOpen(true);
            }}
            sx={{
              flexGrow: { xs: 1, sm: 0 },
              minWidth: 128,
              textTransform: "none",
              fontWeight: 600,
              height: 36,
            }}
          >
            {t("users.list.inviteButton")}
          </Button>
        }
        onRefresh={() => void loadUsers()}
        isLoading={isLoading}
      />

      {successMessage ? (
        <Alert severity="success" onClose={() => setSuccessMessage(null)}>
          {successMessage}
        </Alert>
      ) : null}
      {errorMessage ? (
        <Alert severity="error">
          {errorMessage}
        </Alert>
      ) : null}

      <Stack
        direction={{ xs: "column", lg: "row" }}
        spacing={1.5}
        sx={{ alignItems: { xs: "stretch", lg: "center" }, justifyContent: "space-between" }}
      >
        <TextField
          value={searchQuery}
          onChange={(event) => setSearchQuery(event.target.value)}
          placeholder={t("users.list.searchPlaceholder")}
          aria-label={t("users.list.searchPlaceholder")}
          size="small"
          sx={{ width: { xs: "100%", lg: 360 } }}
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
        <Stack
          direction="row"
          spacing={1}
          sx={{ alignItems: "center", justifyContent: "flex-end" }}
        >
          <FormControl size="small" sx={{ minWidth: 164 }}>
            <InputLabel id="role-filter-label">{t("users.list.roleFilterLabel")}</InputLabel>
            <Select<RoleFilter>
              labelId="role-filter-label"
              id="role-filter"
              value={roleFilter}
              label={t("users.list.roleFilterLabel")}
              onChange={(event) => setRoleFilter(event.target.value as RoleFilter)}
            >
              <MenuItem value="All">{t("users.list.allRoles")}</MenuItem>
              {allRoles.map((role) => (
                <MenuItem key={role} value={role}>{getRoleLabel(role, t)}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Chip
            label={t("users.list.userCount", { count: filteredUsers.length })}
            size="small"
            variant="outlined"
            sx={{ fontWeight: 500 }}
          />
        </Stack>
      </Stack>

      <Box sx={{ flexGrow: 1, width: "100%", display: "flex", flexDirection: "column", minHeight: 440 }}>
        <DataGrid
          rows={filteredUsers}
          columns={columns}
          getRowId={(row) => row.id}
          loading={isLoading}
          rowHeight={64}
          disableRowSelectionOnClick
          pageSizeOptions={[10, 25, 50]}
          initialState={{
            pagination: {
              paginationModel: { page: 0, pageSize: 10 },
            },
          }}
          slots={{
            toolbar: CustomToolbar,
            noRowsOverlay: () => (
              <DataGridEmptyState
                icon={<GroupIcon sx={{ fontSize: 48, color: "grey.400" }} />}
                message={t("users.list.emptyMessage")}
                description={t("users.list.emptyDescription")}
              />
            ),
          }}
          sx={{
            flex: 1,
            borderColor: "divider",
            bgcolor: "background.paper",
            borderRadius: 2,
            "& .MuiDataGrid-columnHeaders": {
              bgcolor: "action.hover",
              borderBottom: 1,
              borderColor: "divider",
            },
            "& .MuiDataGrid-cell": {
              borderColor: "divider",
              display: "flex !important",
              alignItems: "center !important",
            },
            "& .MuiDataGrid-footerContainer": {
              borderTop: 1,
              borderColor: "divider",
              mt: "auto",
            },
          }}
        />
      </Box>

      <InviteUserDialog
        open={isInviteOpen}
        onClose={() => setIsInviteOpen(false)}
        onCreated={async () => {
          setIsInviteOpen(false);
          setSuccessMessage(t("users.invite.success"));
          await loadUsers();
        }}
      />

      <DeactivateUserDialog
        workspaceUser={confirmDeactivateUser}
        isPending={isDeactivationPending}
        onCancel={() => {
          if (!isDeactivationPending) {
            setConfirmDeactivateUser(null);
          }
        }}
        onConfirm={handleDeactivateConfirmed}
      />

      <ReactivateUserDialog
        key={confirmReactivateUser?.id ?? "reactivation-dialog-empty"}
        workspaceUser={confirmReactivateUser}
        isPending={isReactivationPending}
        onCancel={() => {
          if (!isReactivationPending) {
            setConfirmReactivateUser(null);
          }
        }}
        onConfirm={handleReactivateConfirmed}
      />
    </Box>
  );
}

/** Describes props for the role select grid cell. */
type RoleSelectCellProps = {
  workspaceUser: UserSummary;
  currentUserId: string | undefined;
  isPending: boolean;
  onRoleChange: (workspaceUser: UserSummary, role: MutableUserRole) => Promise<void>;
};

/**
 * Renders an inline role selector for mutable tenant users.
 */
function RoleSelectCell({
  workspaceUser,
  currentUserId,
  isPending,
  onRoleChange,
}: RoleSelectCellProps) {
  const { t } = useTranslation();
  const disabledReason = getMutationDisabledReason(workspaceUser, currentUserId, t);
  const statusDisabledReason =
    workspaceUser.status === "Deactivated"
      ? t("users.list.actions.reactivationConstraint")
      : null;
  const isDisabled = Boolean(disabledReason || statusDisabledReason) || isPending;

  return (
    <Tooltip
      title={disabledReason ?? statusDisabledReason ?? ""}
      disableHoverListener={!disabledReason && !statusDisabledReason}
    >
      <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
        <FormControl size="small" disabled={isDisabled} sx={{ minWidth: 150 }}>
          <Select<UserRole>
            value={workspaceUser.role}
            onClick={(event) => event.stopPropagation()}
            onChange={(event) => {
              const role = event.target.value as MutableUserRole;
              void onRoleChange(workspaceUser, role);
            }}
            sx={{
              fontSize: "0.875rem",
              "& .MuiSelect-select": {
                py: 0.75,
              },
            }}
          >
            {allRoles.map((role) => (
              <MenuItem key={role} value={role} disabled={role === "Owner"}>
                {getRoleLabel(role, t)}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>
    </Tooltip>
  );
}


/** Describes props for the lifecycle action grid cell. */
type UserRowActionsProps = {
  workspaceUser: UserSummary;
  currentUserId: string | undefined;
  onDeactivate: (workspaceUser: UserSummary) => void;
  onReactivate: (workspaceUser: UserSummary) => void;
};

/**
 * Renders the correct user lifecycle action for the current row status.
 */
function UserRowActions({
  workspaceUser,
  currentUserId,
  onDeactivate,
  onReactivate,
}: UserRowActionsProps) {
  const { t } = useTranslation();
  const disabledReason = getMutationDisabledReason(workspaceUser, currentUserId, t);
  const action =
    workspaceUser.status === "Deactivated"
      ? {
          label: t("users.list.actions.reactivate", { name: workspaceUser.name }),
          icon: <RestoreIcon fontSize="small" />,
          color: "success.main",
          onClick: () => onReactivate(workspaceUser),
        }
      : workspaceUser.status === "Active"
        ? {
            label: t("users.list.actions.deactivate", { name: workspaceUser.name }),
            icon: <PersonOffIcon fontSize="small" />,
            color: "error.main",
            onClick: () => onDeactivate(workspaceUser),
          }
        : null;

  return (
    <Tooltip title={disabledReason ?? action?.label ?? t("users.list.actions.noAction")}>
      <span>
        <IconButton
          aria-label={action?.label ?? t("users.list.actions.noAction")}
          size="small"
          disabled={Boolean(disabledReason) || !action}
          onClick={(event) => {
            event.stopPropagation();
            action?.onClick();
          }}
          sx={{ color: action?.color }}
        >
          {action?.icon ?? <RestoreIcon fontSize="small" />}
        </IconButton>
      </span>
    </Tooltip>
  );
}

/** Describes props for the user deactivation confirmation dialog. */
type DeactivateUserDialogProps = {
  workspaceUser: UserSummary | null;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => Promise<void>;
};

/**
 * Renders the confirmation dialog for user deactivation.
 */
function DeactivateUserDialog({
  workspaceUser,
  isPending,
  onCancel,
  onConfirm,
}: DeactivateUserDialogProps) {
  const { t } = useTranslation();
  const isAdministratorTarget = workspaceUser?.role === "Administrator";

  return (
    <Dialog
      open={Boolean(workspaceUser)}
      onClose={onCancel}
      slotProps={{
        paper: {
          sx: { width: "100%", maxWidth: 520 },
        },
      }}
    >
      <DialogTitle>
        {isAdministratorTarget ? t("users.deactivate.titleAdmin") : t("users.deactivate.titleUser")}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {isAdministratorTarget ? (
            <Alert severity="error">
              {t("users.deactivate.alertAdmin")}
            </Alert>
          ) : (
            <Alert severity="warning">
              {t("users.deactivate.alertUser")}
            </Alert>
          )}
          <Typography variant="body2">
            {workspaceUser
              ? t("users.deactivate.confirm", { name: workspaceUser.name, email: workspaceUser.email })
              : ""}
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onCancel} disabled={isPending} sx={{ textTransform: "none" }}>
          {t("common.cancel")}
        </Button>
        <Button
          onClick={() => void onConfirm()}
          variant="contained"
          color="error"
          disabled={isPending}
          sx={{ minWidth: 112, textTransform: "none" }}
        >
          {isPending ? <CircularProgress color="inherit" size={18} thickness={5} /> : t("common.confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Describes props for the compliance reactivation confirmation dialog. */
type ReactivateUserDialogProps = {
  workspaceUser: UserSummary | null;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: (reason: string) => Promise<void>;
};

/**
 * Renders the mandatory-reason confirmation dialog for ISO-compliant reactivation.
 */
function ReactivateUserDialog({
  workspaceUser,
  isPending,
  onCancel,
  onConfirm,
}: ReactivateUserDialogProps) {
  const { t } = useTranslation();
  const [reason, setReason] = useState("");
  const [fieldError, setFieldError] = useState<string | null>(null);

  /**
   * Validates and submits the reactivation reason.
   */
  function handleConfirm() {
    if (!reason.trim()) {
      setFieldError(t("users.reactivate.reasonRequired"));
      return;
    }

    void onConfirm(reason.trim());
  }

  /**
   * Clears local dialog state before closing the reactivation prompt.
   */
  function handleCancel() {
    setReason("");
    setFieldError(null);
    onCancel();
  }

  return (
    <Dialog
      open={Boolean(workspaceUser)}
      onClose={handleCancel}
      slotProps={{
        paper: {
          sx: { width: "100%", maxWidth: 560 },
        },
      }}
    >
      <DialogTitle>{t("users.reactivate.title")}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          <Alert severity="info">
            {t("users.reactivate.alert")}
          </Alert>
          <Typography variant="body2">
            {workspaceUser
              ? t("users.reactivate.confirm", { name: workspaceUser.name, email: workspaceUser.email })
              : ""}
          </Typography>
          <TextField
            id="reactivation-reason"
            label={t("users.reactivate.reasonLabel")}
            value={reason}
            onChange={(event) => {
              setReason(event.target.value);
              setFieldError(null);
            }}
            error={Boolean(fieldError)}
            helperText={fieldError ?? " "}
            required
            multiline
            minRows={3}
            disabled={isPending}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={handleCancel} disabled={isPending} sx={{ textTransform: "none" }}>
          {t("common.cancel")}
        </Button>
        <Button
          onClick={handleConfirm}
          variant="contained"
          color="success"
          disabled={isPending}
          sx={{ minWidth: 112, textTransform: "none" }}
        >
          {isPending ? <CircularProgress color="inherit" size={18} thickness={5} /> : t("common.confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/** Describes props for the invite-user modal. */
type InviteUserDialogProps = {
  open: boolean;
  onClose: () => void;
  onCreated: () => Promise<void>;
};

/**
 * Renders the admin invitation modal without administrator-created passwords.
 */
function InviteUserDialog({ open, onClose, onCreated }: InviteUserDialogProps) {
  const { t } = useTranslation();
  const [form, setForm] = useState<InviteFormState>(initialInviteForm);
  const [fieldErrors, setFieldErrors] = useState<InviteFieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);

  /**
   * Validates and submits the invitation form to create a pending activation user.
   */
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextErrors = validateInviteForm(form, t);

    if (hasErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      return;
    }

    setIsPending(true);
    setSubmitError(null);

    try {
      await apiFetch<UserSummary>("/api/users", {
        method: "POST",
        body: {
          name: form.name.trim(),
          email: form.email.trim(),
          role: form.role,
        },
      });
      setForm(initialInviteForm);
      setFieldErrors({});
      await onCreated();
    } catch (error) {
      const validationErrors = readValidationFieldErrors(error);

      if (hasErrors(validationErrors)) {
        setFieldErrors(validationErrors);
      }

      setSubmitError(
        getApiErrorMessage(error, t("users.invite.error"), t),
      );
    } finally {
      setIsPending(false);
    }
  }

  /**
   * Closes the invitation dialog and resets local form state when no request is pending.
   */
  function handleClose() {
    if (isPending) {
      return;
    }

    setForm(initialInviteForm);
    setFieldErrors({});
    setSubmitError(null);
    onClose();
  }

  /**
   * Updates a single invitation form field and clears its validation error.
   */
  function handleFieldChange(field: keyof InviteFormState, value: string) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
    setFieldErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      slotProps={{
        paper: {
          sx: { width: "100%", maxWidth: 560 },
        },
      }}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle>{t("users.invite.title")}</DialogTitle>
        <DialogContent sx={{ p: 3 }}>
          <Stack spacing={3} sx={{ pt: 1 }}>
            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {t("users.invite.activationFlowTitle")}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t("users.invite.activationFlowDesc")}
              </Typography>
            </Stack>
            <Divider />
            {submitError ? (
              <Alert severity="error">
                {submitError}
              </Alert>
            ) : null}
            <TextField
              id="invite-name"
              name="name"
              label={t("users.list.columns.name")}
              value={form.name}
              onChange={(event) => handleFieldChange("name", event.target.value)}
              autoComplete="name"
              disabled={isPending}
              error={Boolean(fieldErrors.name)}
              helperText={fieldErrors.name ?? " "}
              required
              fullWidth
              size="small"
            />
            <TextField
              id="invite-email"
              name="email"
              label={t("users.list.columns.email")}
              type="email"
              value={form.email}
              onChange={(event) => handleFieldChange("email", event.target.value)}
              autoComplete="email"
              disabled={isPending}
              error={Boolean(fieldErrors.email)}
              helperText={fieldErrors.email ?? " "}
              required
              fullWidth
              size="small"
            />
            <FormControl error={Boolean(fieldErrors.role)} fullWidth size="small">
              <InputLabel id="invite-role-label">{t("users.list.columns.role")}</InputLabel>
              <Select<MutableUserRole>
                labelId="invite-role-label"
                id="invite-role"
                name="role"
                value={form.role}
                label={t("users.list.columns.role")}
                onChange={(event: SelectChangeEvent<MutableUserRole>) =>
                  handleFieldChange("role", event.target.value)
                }
                disabled={isPending}
              >
                {mutableRoles.map((role) => (
                  <MenuItem key={role} value={role}>{getRoleLabel(role, t)}</MenuItem>
                ))}
              </Select>
              <FormHelperText>{fieldErrors.role ?? " "}</FormHelperText>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={handleClose}
            disabled={isPending}
            sx={{ textTransform: "none" }}
          >
            {t("common.cancel")}
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isPending}
            sx={{ minWidth: 112, textTransform: "none" }}
          >
            {isPending ? (
              <CircularProgress color="inherit" size={18} thickness={5} />
            ) : (
              t("users.invite.title")
            )}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

/**
 * Validates the invite-user form before submitting it to the API.
 */
function validateInviteForm(form: InviteFormState, t: (key: string) => string): InviteFieldErrors {
  const errors: InviteFieldErrors = {};

  if (!form.name.trim()) {
    errors.name = t("users.invite.validation.nameRequired");
  }

  if (!form.email.trim()) {
    errors.email = t("users.invite.validation.emailRequired");
  } else if (!emailPattern.test(form.email.trim())) {
    errors.email = t("users.invite.validation.emailInvalid");
  }

  if (!mutableRoles.includes(form.role)) {
    errors.role = t("users.invite.validation.roleInvalid");
  }

  return errors;
}

/**
 * Returns the reason a user lifecycle or role mutation is disabled.
 */
function getMutationDisabledReason(
  workspaceUser: UserSummary,
  currentUserId: string | undefined,
  t: (key: string) => string,
): string | null {
  if (currentUserId && workspaceUser.id.toLowerCase() === currentUserId.toLowerCase()) {
    return t("users.list.actions.selfMutationConstraint");
  }

  if (workspaceUser.role === "Owner") {
    return t("users.list.actions.ownerMutationConstraint");
  }

  return null;
}

/**
 * Formats a nullable login timestamp for the user grid.
 */
function formatLastLogin(value: string | null, t: (key: string) => string, locale: string): string {
  if (!value) {
    return t("users.list.status.never");
  }

  const timestamp = new Date(value);

  if (Number.isNaN(timestamp.getTime())) {
    return t("users.list.status.never");
  }

  return new Intl.DateTimeFormat(locale === "pt-BR" ? "pt-BR" : "en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(timestamp);
}

/**
 * Builds initials for the user avatar.
 */
function getInitials(name: string): string {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");

  return initials || "U";
}

/**
 * Converts an API lifecycle status into a compact chip label.
 */
function getStatusLabel(status: UserStatus, t: (key: string) => string): string {
  if (status === "PendingActivation") {
    return t("users.list.status.pending");
  }
  if (status === "Active") {
    return t("users.list.status.active");
  }
  if (status === "Deactivated") {
    return t("users.list.status.deactivated");
  }

  return status;
}

function getRoleLabel(role: string, t: (key: string) => string): string {
  const map: Record<string, string> = {
    Owner: t("users.roles.owner"),
    Administrator: t("users.roles.administrator"),
    Approver: t("users.roles.approver"),
    Requester: t("users.roles.requester"),
    Viewer: t("users.roles.viewer"),
  };
  return map[role] || role;
}

/**
 * Maps a lifecycle status to the MUI chip color palette.
 */
function getStatusChipColor(status: UserStatus): "success" | "warning" | "error" {
  if (status === "Active") {
    return "success";
  }

  if (status === "PendingActivation") {
    return "warning";
  }

  return "error";
}

/**
 * Indicates whether any invite form validation errors are present.
 */
function hasErrors(errors: InviteFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

/**
 * Converts an API error into user-facing copy.
 */
function getApiErrorMessage(error: unknown, fallback: string, t: (key: string) => string): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? fallback;
  }

  return t(fallback) || fallback;
}

/**
 * Reads server validation errors into invite form field errors.
 */
function readValidationFieldErrors(error: unknown): InviteFieldErrors {
  const details = error instanceof ApiError ? error.details : error;

  if (
    !details ||
    typeof details !== "object" ||
    !("errors" in details) ||
    !details.errors ||
    typeof details.errors !== "object"
  ) {
    return {};
  }

  const fieldMap: Record<string, keyof InviteFormState> = {
    Name: "name",
    Email: "email",
    Role: "role",
  };
  const errors: InviteFieldErrors = {};

  for (const [field, messages] of Object.entries(details.errors)) {
    const formField = fieldMap[field];

    if (!formField || !Array.isArray(messages)) {
      continue;
    }

    const message = messages.find(
      (item): item is string => typeof item === "string" && item.length > 0,
    );

    if (message) {
      errors[formField] = message;
    }
  }

  return errors;
}

/**
 * Reads the best display message from RFC 7807 problem details.
 */
function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
  }

  const validationMessage = readValidationMessage(details);

  if (validationMessage) {
    return validationMessage;
  }

  if (
    "detail" in details &&
    typeof details.detail === "string" &&
    details.detail.trim().length > 0
  ) {
    return details.detail;
  }

  if (
    "title" in details &&
    typeof details.title === "string" &&
    details.title.trim().length > 0
  ) {
    return details.title;
  }

  return null;
}

/**
 * Reads the first validation error message from problem details.
 */
function readValidationMessage(details: object): string | null {
  if (
    !("errors" in details) ||
    !details.errors ||
    typeof details.errors !== "object"
  ) {
    return null;
  }

  for (const messages of Object.values(details.errors)) {
    if (!Array.isArray(messages)) {
      continue;
    }

    const message = messages.find(
      (item): item is string => typeof item === "string" && item.length > 0,
    );

    if (message) {
      return message;
    }
  }

  return null;
}
