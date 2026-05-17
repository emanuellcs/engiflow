"use client";

import AddIcon from "@mui/icons-material/Add";
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
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import Select, { type SelectChangeEvent } from "@mui/material/Select";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";

type UserSummary = {
  id: string;
  name: string;
  email: string;
  role: string;
};

type InviteFormState = {
  name: string;
  email: string;
  password: string;
  role: InviteRole;
};

type InviteRole = "Requester" | "Approver";
type RoleFilter = "All" | "Administrator" | InviteRole;
type InviteFieldErrors = Partial<Record<keyof InviteFormState, string>>;

const administratorRole = "Administrator";
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
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [searchQuery, setSearchQuery] = useState("");
  const [roleFilter, setRoleFilter] = useState<RoleFilter>("All");
  const isAdministrator = user?.role === administratorRole;
  const filteredUsers = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    return users.filter((workspaceUser) => {
      const matchesRole =
        roleFilter === "All" || workspaceUser.role === roleFilter;
      const matchesQuery =
        !normalizedQuery ||
        workspaceUser.name.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.email.toLowerCase().includes(normalizedQuery) ||
        workspaceUser.role.toLowerCase().includes(normalizedQuery);

      return matchesRole && matchesQuery;
    });
  }, [roleFilter, searchQuery, users]);
  const maxPage = Math.max(0, Math.ceil(filteredUsers.length / rowsPerPage) - 1);
  const currentPage = Math.min(page, maxPage);

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

  const visibleUsers = useMemo(() => {
    const start = currentPage * rowsPerPage;
    return filteredUsers.slice(start, start + rowsPerPage);
  }, [currentPage, filteredUsers, rowsPerPage]);

  if (!isAdministrator) {
    return (
      <Stack spacing={2.5}>
        <PageHeader
          title="Team Management"
          description="Manage workspace users, roles, and access policies."
        />
        <Alert severity="warning">
          Administrator access is required to manage workspace users.
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={2.5}>
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
            sx={{ textTransform: "none" }}
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

      <Paper elevation={1}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={1.5}
          sx={{
            alignItems: { xs: "stretch", md: "center" },
            justifyContent: "space-between",
            px: 2,
            py: 1.5,
            borderBottom: 1,
            borderColor: "divider",
          }}
        >
          <TextField
            value={searchQuery}
            onChange={(event) => {
              setSearchQuery(event.target.value);
              setPage(0);
            }}
            placeholder="Search by name, email, or role"
            aria-label="Search team members"
            size="small"
            sx={{ width: { xs: "100%", md: 360 } }}
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
            <FormControl size="small" sx={{ minWidth: 144 }}>
              <InputLabel id="role-filter-label">Role</InputLabel>
              <Select<RoleFilter>
                labelId="role-filter-label"
                id="role-filter"
                value={roleFilter}
                label="Role"
                onChange={(event) => {
                  setRoleFilter(event.target.value as RoleFilter);
                  setPage(0);
                }}
              >
                <MenuItem value="All">All roles</MenuItem>
                <MenuItem value="Administrator">Administrator</MenuItem>
                <MenuItem value="Approver">Approver</MenuItem>
                <MenuItem value="Requester">Requester</MenuItem>
              </Select>
            </FormControl>
            <Chip
              label={`${filteredUsers.length} total`}
              size="small"
              variant="outlined"
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
        <TableContainer sx={{ overflowX: "auto" }}>
          <Table size="small" aria-label="Workspace team members">
            <TableHead>
              <TableRow>
                <TableCell
                  sx={{
                    fontWeight: 500,
                    minWidth: 260,
                    bgcolor: "action.hover",
                  }}
                >
                  Name
                </TableCell>
                <TableCell
                  sx={{
                    fontWeight: 500,
                    minWidth: 240,
                    bgcolor: "action.hover",
                  }}
                >
                  Email
                </TableCell>
                <TableCell
                  sx={{
                    fontWeight: 500,
                    minWidth: 160,
                    bgcolor: "action.hover",
                  }}
                >
                  Role
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={3} sx={{ height: 160, textAlign: "center" }}>
                    <CircularProgress size={28} thickness={4} />
                  </TableCell>
                </TableRow>
              ) : visibleUsers.length > 0 ? (
                visibleUsers.map((workspaceUser) => (
                  <TableRow key={workspaceUser.id} hover>
                    <TableCell>
                      <Stack direction="row" spacing={1.25} sx={{ alignItems: "center" }}>
                        <Avatar sx={{ width: 32, height: 32, bgcolor: "secondary.main" }}>
                          {getInitials(workspaceUser.name)}
                        </Avatar>
                        <Typography variant="body2" sx={{ fontWeight: 500 }}>
                          {workspaceUser.name}
                        </Typography>
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" color="text.secondary">
                        {workspaceUser.email}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={workspaceUser.role}
                        size="small"
                        variant="outlined"
                        sx={{
                          minWidth: 104,
                          fontWeight: 500,
                          ...getRoleChipSx(workspaceUser.role),
                        }}
                      />
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={3} sx={{ height: 120, textAlign: "center" }}>
                    <Typography variant="body2" color="text.secondary">
                      No matching team members.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        <TablePagination
          component="div"
          count={filteredUsers.length}
          page={currentPage}
          rowsPerPage={rowsPerPage}
          rowsPerPageOptions={[5, 10, 25]}
          onPageChange={(_, nextPage) => setPage(nextPage)}
          onRowsPerPageChange={(event) => {
            setRowsPerPage(Number(event.target.value));
            setPage(0);
          }}
        />
      </Paper>

      <InviteUserDialog
        open={isInviteOpen}
        onClose={() => setIsInviteOpen(false)}
        onCreated={async () => {
          setIsInviteOpen(false);
          setSuccessMessage("User invited successfully.");
          await loadUsers();
        }}
      />
    </Stack>
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

  function handleRoleChange(event: SelectChangeEvent<InviteRole>) {
    handleFieldChange("role", event.target.value as InviteRole);
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
              <Select<InviteRole>
                labelId="invite-role-label"
                id="invite-role"
                name="role"
                value={form.role}
                label="Role"
                onChange={handleRoleChange}
                disabled={isPending}
              >
                <MenuItem value="Requester">Requester</MenuItem>
                <MenuItem value="Approver">Approver</MenuItem>
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

  if (form.role !== "Requester" && form.role !== "Approver") {
    errors.role = "Role must be Requester or Approver.";
  }

  return errors;
}

function getRoleChipSx(role: string): object {
  if (role === "Administrator") {
    return {
      borderColor: "primary.main",
      color: "primary.dark",
      bgcolor: "rgba(21, 101, 192, 0.08)",
    };
  }

  if (role === "Approver") {
    return {
      borderColor: "success.main",
      color: "success.dark",
      bgcolor: "rgba(46, 125, 50, 0.08)",
    };
  }

  if (role === "Requester") {
    return {
      borderColor: "info.main",
      color: "info.dark",
      bgcolor: "rgba(2, 136, 209, 0.08)",
    };
  }

  return {
    borderColor: "grey.400",
    color: "text.secondary",
    bgcolor: "grey.50",
  };
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
