"use client";

import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import RefreshIcon from "@mui/icons-material/Refresh";
import SearchIcon from "@mui/icons-material/Search";
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
import FormControl from "@mui/material/FormControl";
import FormHelperText from "@mui/material/FormHelperText";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import InputLabel from "@mui/material/InputLabel";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { DataGrid, type GridColDef, type GridRenderCellParams } from "@mui/x-data-grid";
import { type FormEvent, type MouseEvent, useCallback, useEffect, useMemo, useState } from "react";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";

type UserRole = "Owner" | "Administrator" | "Approver" | "Requester" | "Viewer";
type MutableUserRole = Exclude<UserRole, "Owner">;
type RoleFilter = "All" | UserRole;

type UserSummary = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  lastLoginAt: string | null;
};

type InviteFormState = {
  name: string;
  email: string;
  password: string;
  role: MutableUserRole;
};

type InviteFieldErrors = Partial<Record<keyof InviteFormState, string>>;

const allRoles: UserRole[] = ["Owner", "Administrator", "Approver", "Requester", "Viewer"];
const mutableRoles: MutableUserRole[] = ["Administrator", "Approver", "Requester", "Viewer"];
const initialInviteForm: InviteFormState = {
  name: "",
  email: "",
  password: "",
  role: "Requester",
};
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const symbolPattern = /[^a-zA-Z0-9]/;

export default function UserManagementPage() {
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
      setErrorMessage(getApiErrorMessage(error, "Unable to load users."));
    } finally {
      setIsLoading(false);
    }
  }, [requestUsers]);

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
          setErrorMessage(getApiErrorMessage(error, "Unable to load users."));
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
  }, [isAdministrator, requestUsers]);

  const filteredUsers = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    return users.filter((workspaceUser) => {
      const matchesRole = roleFilter === "All" || workspaceUser.role === roleFilter;
      const matchesQuery =
        !normalizedQuery ||
        workspaceUser.name.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.email.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.role.toLowerCase().includes(normalizedQuery);

      return matchesRole && matchesQuery;
    });
  }, [roleFilter, searchQuery, users]);

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
      setSuccessMessage(`${updatedUser.name}'s role was updated.`);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Unable to update the user's role."));
    } finally {
      setRoleUpdatingUserId(null);
    }
  }, []);

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
      setUsers((current) => current.filter((item) => item.id !== confirmDeactivateUser.id));
      setSuccessMessage(`${confirmDeactivateUser.name} was deactivated.`);
      setConfirmDeactivateUser(null);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Unable to deactivate the user."));
    } finally {
      setIsDeactivationPending(false);
    }
  }, [confirmDeactivateUser]);

  const columns = useMemo<GridColDef<UserSummary>[]>(() => [
    {
      field: "name",
      headerName: "Name",
      minWidth: 240,
      flex: 1,
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
      headerName: "Email",
      minWidth: 250,
      flex: 1,
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
      headerName: "Role",
      minWidth: 220,
      renderCell: (params: GridRenderCellParams<UserSummary, UserRole>) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
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
      headerName: "Last Active",
      minWidth: 190,
      renderCell: (params) => (
        <Box sx={{ display: "flex", alignItems: "center", height: "100%" }}>
          <Typography variant="body2" color="text.secondary">
            {formatLastLogin(params.value)}
          </Typography>
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
      renderCell: (params: GridRenderCellParams<UserSummary>) => (
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "center", height: "100%" }}>
          <UserRowActions
            workspaceUser={params.row}
            currentUserId={user?.id}
            onDeactivate={setConfirmDeactivateUser}
          />
        </Box>
      ),
    },
  ], [handleRoleChange, roleUpdatingUserId, user?.id]);

  if (!isAdministrator) {
    return (
      <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", gap: 2.5 }}>
        <PageHeader
          title="Team Management"
          description="Manage workspace users, roles, and access policies."
        />
        <Alert severity="warning">
          Administrator access is required to manage workspace users.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", gap: 2.5, minHeight: 0 }}>
      <PageHeader
        title="Team Management"
        description="Manage workspace users, roles, and access policies."
        actionButton={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setSuccessMessage(null);
              setIsInviteOpen(true);
            }}
            sx={{ textTransform: "none", fontWeight: 600 }}
          >
            Invite User
          </Button>
        }
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
          placeholder="Search by name, email, or role"
          aria-label="Search team members"
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
            <InputLabel id="role-filter-label">Role</InputLabel>
            <Select<RoleFilter>
              labelId="role-filter-label"
              id="role-filter"
              value={roleFilter}
              label="Role"
              onChange={(event) => setRoleFilter(event.target.value as RoleFilter)}
            >
              <MenuItem value="All">All roles</MenuItem>
              {allRoles.map((role) => (
                <MenuItem key={role} value={role}>{role}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Chip
            label={`${filteredUsers.length} active`}
            size="small"
            variant="outlined"
            sx={{ fontWeight: 500 }}
          />
          <Tooltip title="Refresh">
            <span>
              <IconButton
                aria-label="Refresh team members"
                size="small"
                onClick={loadUsers}
                disabled={isLoading}
              >
                <RefreshIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      </Stack>

      <Box sx={{ flexGrow: 1, width: "100%", minHeight: 400 }}>
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
          localeText={{
            noRowsLabel: "No matching team members.",
          }}
          sx={{
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
            },
          }}
        />
      </Box>

      <InviteUserDialog
        open={isInviteOpen}
        onClose={() => setIsInviteOpen(false)}
        onCreated={async () => {
          setIsInviteOpen(false);
          setSuccessMessage("User invited successfully.");
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
    </Box>
  );
}

type RoleSelectCellProps = {
  workspaceUser: UserSummary;
  currentUserId: string | undefined;
  isPending: boolean;
  onRoleChange: (workspaceUser: UserSummary, role: MutableUserRole) => Promise<void>;
};

function RoleSelectCell({
  workspaceUser,
  currentUserId,
  isPending,
  onRoleChange,
}: RoleSelectCellProps) {
  const disabledReason = getMutationDisabledReason(workspaceUser, currentUserId);
  const isDisabled = Boolean(disabledReason) || isPending;

  return (
    <Tooltip title={disabledReason ?? ""} disableHoverListener={!disabledReason}>
      <span>
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
                {role}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </span>
    </Tooltip>
  );
}


type UserRowActionsProps = {
  workspaceUser: UserSummary;
  currentUserId: string | undefined;
  onDeactivate: (workspaceUser: UserSummary) => void;
};

function UserRowActions({ workspaceUser, currentUserId, onDeactivate }: UserRowActionsProps) {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const disabledReason = getMutationDisabledReason(workspaceUser, currentUserId);
  const isMenuOpen = Boolean(anchorEl);

  function handleOpen(event: MouseEvent<HTMLButtonElement>) {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  }

  function handleClose() {
    setAnchorEl(null);
  }

  return (
    <>
      <Tooltip title={disabledReason ?? "Row actions"}>
        <span>
          <IconButton
            aria-label={`Open actions for ${workspaceUser.name}`}
            size="small"
            disabled={Boolean(disabledReason)}
            onClick={handleOpen}
          >
            <MoreVertIcon fontSize="small" />
          </IconButton>
        </span>
      </Tooltip>
      <Menu
        anchorEl={anchorEl}
        open={isMenuOpen}
        onClose={handleClose}
        onClick={(event) => event.stopPropagation()}
      >
        <MenuItem
          onClick={() => {
            handleClose();
            onDeactivate(workspaceUser);
          }}
          sx={{ color: "error.main" }}
        >
          Deactivate
        </MenuItem>
      </Menu>
    </>
  );
}

type DeactivateUserDialogProps = {
  workspaceUser: UserSummary | null;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => Promise<void>;
};

function DeactivateUserDialog({
  workspaceUser,
  isPending,
  onCancel,
  onConfirm,
}: DeactivateUserDialogProps) {
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
        {isAdministratorTarget ? "Deactivate Administrator" : "Deactivate User"}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 0.5 }}>
          {isAdministratorTarget ? (
            <Alert severity="error">
              This user has administrative access. Deactivation will immediately revoke their
              tenant-management permissions and active sessions.
            </Alert>
          ) : (
            <Alert severity="warning">
              Deactivation immediately prevents this user from authenticating or taking
              workflow actions.
            </Alert>
          )}
          <Typography variant="body2">
            {workspaceUser
              ? `Deactivate ${workspaceUser.name} (${workspaceUser.email})?`
              : ""}
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onCancel} disabled={isPending} sx={{ textTransform: "none" }}>
          Cancel
        </Button>
        <Button
          onClick={() => void onConfirm()}
          variant="contained"
          color="error"
          disabled={isPending}
          sx={{ minWidth: 112, textTransform: "none" }}
        >
          {isPending ? <CircularProgress color="inherit" size={18} thickness={5} /> : "Deactivate"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

type InviteUserDialogProps = {
  open: boolean;
  onClose: () => void;
  onCreated: () => Promise<void>;
};

function InviteUserDialog({ open, onClose, onCreated }: InviteUserDialogProps) {
  const [form, setForm] = useState<InviteFormState>(initialInviteForm);
  const [fieldErrors, setFieldErrors] = useState<InviteFieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextErrors = validateInviteForm(form);

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
          password: form.password,
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
        getApiErrorMessage(error, "Unable to invite user. Review the details and try again."),
      );
    } finally {
      setIsPending(false);
    }
  }

  function handleClose() {
    if (isPending) {
      return;
    }

    setForm(initialInviteForm);
    setFieldErrors({});
    setSubmitError(null);
    onClose();
  }

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
          sx: { width: "100%", maxWidth: 520 },
        },
      }}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle>Invite User</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {submitError ? (
              <Alert severity="error">
                {submitError}
              </Alert>
            ) : null}
            <TextField
              id="invite-name"
              name="name"
              label="Name"
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
              label="Email"
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
            <TextField
              id="invite-password"
              name="password"
              label="Password"
              type="password"
              value={form.password}
              onChange={(event) => handleFieldChange("password", event.target.value)}
              autoComplete="new-password"
              disabled={isPending}
              error={Boolean(fieldErrors.password)}
              helperText={
                fieldErrors.password ??
                "At least 12 characters with uppercase, lowercase, number, and symbol."
              }
              required
              fullWidth
              size="small"
            />
            <FormControl error={Boolean(fieldErrors.role)} fullWidth size="small">
              <InputLabel id="invite-role-label">Role</InputLabel>
              <Select<MutableUserRole>
                labelId="invite-role-label"
                id="invite-role"
                name="role"
                value={form.role}
                label="Role"
                onChange={(event: SelectChangeEvent<MutableUserRole>) =>
                  handleFieldChange("role", event.target.value)
                }
                disabled={isPending}
              >
                {mutableRoles.map((role) => (
                  <MenuItem key={role} value={role}>{role}</MenuItem>
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
            Cancel
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
              "Invite User"
            )}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

function validateInviteForm(form: InviteFormState): InviteFieldErrors {
  const errors: InviteFieldErrors = {};

  if (!form.name.trim()) {
    errors.name = "Name is required.";
  }

  if (!form.email.trim()) {
    errors.email = "Email is required.";
  } else if (!emailPattern.test(form.email.trim())) {
    errors.email = "Enter a valid email address.";
  }

  if (!form.password) {
    errors.password = "Password is required.";
  } else if (form.password.length < 12) {
    errors.password = "Password must be at least 12 characters.";
  } else if (!/[A-Z]/.test(form.password)) {
    errors.password = "Password must include at least one uppercase letter.";
  } else if (!/[a-z]/.test(form.password)) {
    errors.password = "Password must include at least one lowercase letter.";
  } else if (!/[0-9]/.test(form.password)) {
    errors.password = "Password must include at least one number.";
  } else if (!symbolPattern.test(form.password)) {
    errors.password = "Password must include at least one symbol.";
  }

  if (!mutableRoles.includes(form.role)) {
    errors.role = "Role must be Administrator, Approver, Requester, or Viewer.";
  }

  return errors;
}

function getMutationDisabledReason(workspaceUser: UserSummary, currentUserId: string | undefined): string | null {
  if (currentUserId && workspaceUser.id.toLowerCase() === currentUserId.toLowerCase()) {
    return "You cannot modify your own account.";
  }

  if (workspaceUser.role === "Owner") {
    return "Owner accounts cannot be modified.";
  }

  return null;
}

function formatLastLogin(value: string | null): string {
  if (!value) {
    return "Never";
  }

  const timestamp = new Date(value);

  if (Number.isNaN(timestamp.getTime())) {
    return "Never";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(timestamp);
}

function getInitials(name: string): string {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");

  return initials || "U";
}

function hasErrors(errors: InviteFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? fallback;
  }

  return fallback;
}

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
    Password: "password",
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
