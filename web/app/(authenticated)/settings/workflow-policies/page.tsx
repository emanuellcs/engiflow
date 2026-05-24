"use client";

import SaveIcon from "@mui/icons-material/Save";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import InputAdornment from "@mui/material/InputAdornment";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import NextLink from "next/link";
import { type FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import PageHeader from "@/components/ui/PageHeader";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";
import { isAdminOrOwner } from "@/lib/auth/jwt";

/**
 * Describes the tenant-scoped workflow governance settings returned by the API.
 */
type CompanySettings = {
  /** Minimum approvals required for an ECO approval quorum. */
  minApprovalsRequired: number;
  /** Maximum review time before SLA breach (Days). */
  maxReviewDaysBeforeSlabreach: number;
  /** Whether ECO authors can approve their own submissions. */
  allowSelfApproval: boolean;
  /** The ISO timestamp of the last policy update. */
  updatedAt: string;
};

/**
 * Represents a summarized user profile for role checking.
 */
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
  const [quorumValue, setQuorumValue] = useState("1");
  const [slaValue, setSlaValue] = useState("5");
  const [allowSelfApproval, setAllowSelfApproval] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const isAdministrator = isAdminOrOwner(user?.role);

  const activeApproverCount = useMemo(
    () => users.filter((workspaceUser) => workspaceUser.role === "Approver").length,
    [users],
  );
  const parsedQuorum = Number(quorumValue);
  const parsedSla = Number(slaValue);
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
      setQuorumValue(String(settingsResponse.minApprovalsRequired));
      setSlaValue(String(settingsResponse.maxReviewDaysBeforeSlabreach));
      setAllowSelfApproval(settingsResponse.allowSelfApproval);
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
          setQuorumValue(String(settingsResponse.minApprovalsRequired));
          setSlaValue(String(settingsResponse.maxReviewDaysBeforeSlabreach));
          setAllowSelfApproval(settingsResponse.allowSelfApproval);
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

    if (!Number.isInteger(parsedSla) || parsedSla < 1) {
      setErrorMessage("Maximum review time must be at least one day.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const response = await apiFetch<CompanySettings>("/api/settings", {
        method: "PUT",
        body: {
          minApprovalsRequired: parsedQuorum,
          maxReviewDaysBeforeSlabreach: parsedSla,
          allowSelfApproval,
        },
      });

      setSettings(response);
      setQuorumValue(String(response.minApprovalsRequired));
      setSlaValue(String(response.maxReviewDaysBeforeSlabreach));
      setAllowSelfApproval(response.allowSelfApproval);
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
        onRefresh={() => void loadPolicyData()}
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

      {activeApproverCount === 0 ? (
        <Alert
          severity="warning"
          action={
            <Button
              color="inherit"
              size="small"
              component={NextLink}
              href="/settings/users"
              sx={{ fontWeight: 700, textTransform: "none" }}
            >
              Invite Approvers
            </Button>
          }
        >
          No active Approvers found. You must invite team members to the Approver role before ECOs can be processed.
        </Alert>
      ) : showQuorumWarning ? (
        <Alert severity="warning">
          Warning: You require {parsedQuorum} approval{parsedQuorum === 1 ? "" : "s"}, but only have {activeApproverCount} Approver{activeApproverCount === 1 ? "" : "s"} active. ECOs may become stuck.
        </Alert>
      ) : null}

      <Box component="form" onSubmit={handleSubmit}>
        <Grid container spacing={3}>
          {/* Column 1: Quorum & Compliance */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Paper
              variant="outlined"
              sx={{
                p: { xs: 2, sm: 3 },
                display: "flex",
                flexDirection: "column",
                height: "100%",
              }}
            >
              <Stack spacing={2.5} sx={{ height: "100%" }}>
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
                  <Stack spacing={3} sx={{ flexGrow: 1 }}>
                    <TextField
                      id="min-approvals-required"
                      label="Minimum approvals required"
                      type="number"
                      value={quorumValue}
                      onChange={(event) => setQuorumValue(event.target.value)}
                      error={showQuorumWarning}
                      slotProps={{
                        htmlInput: { min: 1, step: 1 },
                        input: {
                          endAdornment: (
                            <InputAdornment position="end">
                              approval{parsedQuorum === 1 ? "" : "s"}
                            </InputAdornment>
                          ),
                        },
                      }}
                      helperText={
                        showQuorumWarning
                          ? `Insufficient approvers: only ${activeApproverCount} available`
                          : `${activeApproverCount} active Approver${activeApproverCount === 1 ? "" : "s"} available`
                      }
                      required
                      fullWidth
                      size="small"
                    />

                    <FormControlLabel
                      control={
                        <Switch
                          checked={allowSelfApproval}
                          onChange={(event) => setAllowSelfApproval(event.target.checked)}
                          color="primary"
                        />
                      }
                      label={
                        <Typography variant="body2">
                          Allow ECO authors to approve their own submissions
                        </Typography>
                      }
                    />
                  </Stack>
                )}
              </Stack>
            </Paper>
          </Grid>

          {/* Column 2: SLA & Deadlines */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Paper
              variant="outlined"
              sx={{
                p: { xs: 2, sm: 3 },
                display: "flex",
                flexDirection: "column",
                height: "100%",
              }}
            >
              <Stack spacing={2.5} sx={{ height: "100%" }}>
                <Stack spacing={0.5}>
                  <Typography variant="h6" component="h2">
                    SLA & Deadlines
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Configure response thresholds before an engineering order triggers a critical bottleneck alert.
                  </Typography>
                </Stack>

                {isLoading ? (
                  <Box sx={{ py: 4, display: "flex", justifyContent: "center" }}>
                    <CircularProgress size={28} thickness={4} />
                  </Box>
                ) : (
                  <Stack spacing={3} sx={{ flexGrow: 1 }}>
                    <TextField
                      id="sla-threshold"
                      label="Maximum review time before SLA breach (Days)"
                      type="number"
                      value={slaValue}
                      onChange={(event) => setSlaValue(event.target.value)}
                      slotProps={{
                        htmlInput: { min: 1, step: 1 },
                        input: {
                          endAdornment: (
                            <InputAdornment position="end">
                              days
                            </InputAdornment>
                          ),
                        },
                      }}
                      required
                      fullWidth
                      size="small"
                    />
                  </Stack>
                )}
              </Stack>
            </Paper>
          </Grid>
        </Grid>

        <Stack direction="row" spacing={1.25} sx={{ mt: 3, justifyContent: "flex-end" }}>
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
