"use client";

import RefreshIcon from "@mui/icons-material/Refresh";
import SaveIcon from "@mui/icons-material/Save";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";

type CompanySettings = {
  minApprovalsRequired: number;
  updatedAt: string;
};

type UserSummary = {
  id: string;
  name: string;
  email: string;
  role: string;
  lastLoginAt: string | null;
};

export default function WorkflowPoliciesPage() {
  const { user } = useAuth();
  const [settings, setSettings] = useState<CompanySettings | null>(null);
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [formValue, setFormValue] = useState("1");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const isAdministrator = isAdminOrOwner(user?.role);

  const activeApproverCount = useMemo(
    () => users.filter((workspaceUser) => workspaceUser.role === "Approver").length,
    [users],
  );
  const parsedQuorum = Number(formValue);
  const showQuorumWarning =
    Number.isInteger(parsedQuorum) &&
    parsedQuorum > activeApproverCount;

  const loadPolicyData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const [settingsResponse, usersResponse] = await Promise.all([
        apiFetch<CompanySettings>("/api/settings"),
        apiFetch<UserSummary[]>("/api/users"),
      ]);

      setSettings(settingsResponse);
      setUsers(usersResponse);
      setFormValue(String(settingsResponse.minApprovalsRequired));
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Unable to load workflow policies."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!isAdministrator) {
      return;
    }

    let isMounted = true;

    Promise.all([
      apiFetch<CompanySettings>("/api/settings"),
      apiFetch<UserSummary[]>("/api/users"),
    ])
      .then(([settingsResponse, usersResponse]) => {
        if (isMounted) {
          setSettings(settingsResponse);
          setUsers(usersResponse);
          setFormValue(String(settingsResponse.minApprovalsRequired));
        }
      })
      .catch((error) => {
        if (isMounted) {
          setErrorMessage(getApiErrorMessage(error, "Unable to load workflow policies."));
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
  }, [isAdministrator]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!Number.isInteger(parsedQuorum) || parsedQuorum < 1) {
      setErrorMessage("Minimum approvals required must be at least one.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const response = await apiFetch<CompanySettings>("/api/settings", {
        method: "PUT",
        body: { minApprovalsRequired: parsedQuorum },
      });

      setSettings(response);
      setFormValue(String(response.minApprovalsRequired));
      setSuccessMessage("Workflow policies were updated.");
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Unable to update workflow policies."));
    } finally {
      setIsSaving(false);
    }
  }

  if (!isAdministrator) {
    return (
      <Stack spacing={2.5}>
        <PageHeader
          title="Workflow Policies"
          description="Control approval quorum rules for engineering change orders."
        />
        <Alert severity="warning">
          Administrator access is required to manage workflow policies.
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title="Workflow Policies"
        description="Control approval quorum rules for engineering change orders."
        actionButton={
          <Stack direction="row" spacing={1} sx={{ width: { xs: "100%", sm: "auto" }, alignItems: "center" }}>
            <Tooltip title="Refresh data">
              <span>
                <IconButton
                  onClick={() => void loadPolicyData()}
                  disabled={isLoading || isSaving}
                  color="primary"
                  size="small"
                  sx={{
                    border: 1,
                    borderColor: "divider",
                    bgcolor: "background.paper",
                    width: 36,
                    height: 36,
                  }}
                >
                  {isLoading ? (
                    <CircularProgress size={20} color="inherit" thickness={5} />
                  ) : (
                    <RefreshIcon fontSize="small" />
                  )}
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
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
      {showQuorumWarning ? (
        <Alert severity="warning">
          Warning: You require {parsedQuorum} approvals, but only have {activeApproverCount} Approvers active. ECOs may become stuck.
        </Alert>
      ) : null}

      <Box
        component="form"
        onSubmit={handleSubmit}
        sx={{
          maxWidth: 560,
          p: { xs: 2, sm: 3 },
          bgcolor: "background.paper",
          border: 1,
          borderColor: "divider",
          borderRadius: 1,
        }}
      >
        <Stack spacing={2.5}>
          <Stack spacing={0.5}>
            <Typography variant="h6" component="h2">
              ECO Approval Quorum
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Minimum approvals required before an ECO can move from review to approved.
            </Typography>
          </Stack>

          {isLoading ? (
            <Box sx={{ py: 4, display: "flex", justifyContent: "center" }}>
              <CircularProgress size={28} thickness={4} />
            </Box>
          ) : (
            <>
              <TextField
                id="min-approvals-required"
                label="Minimum approvals required"
                type="number"
                value={formValue}
                onChange={(event) => setFormValue(event.target.value)}
                slotProps={{
                  htmlInput: { min: 1, step: 1 },
                  input: {
                    endAdornment: (
                      <InputAdornment position="end">
                        approvals
                      </InputAdornment>
                    ),
                  },
                }}
                helperText={`${activeApproverCount} active Approver${activeApproverCount === 1 ? "" : "s"} available`}
                required
                fullWidth
                size="small"
              />

              <Stack direction="row" spacing={1.25} sx={{ justifyContent: "flex-end" }}>
                <Button
                  type="submit"
                  variant="contained"
                  startIcon={isSaving ? undefined : <SaveIcon fontSize="small" />}
                  disabled={isSaving || !settings}
                  sx={{ minWidth: 128, textTransform: "none" }}
                >
                  {isSaving ? (
                    <CircularProgress color="inherit" size={18} thickness={5} />
                  ) : (
                    "Save Policy"
                  )}
                </Button>
              </Stack>
            </>
          )}
        </Stack>
      </Box>
    </Stack>
  );
}

function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? fallback;
  }

  return fallback;
}

function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
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
