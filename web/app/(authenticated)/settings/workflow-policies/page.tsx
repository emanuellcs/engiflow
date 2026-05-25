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
import { useTranslation } from "@/context/I18nContext";

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
  const { t } = useTranslation();
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
      setErrorMessage(getApiErrorMessage(error, t("policies.loadError"), t));
    } finally {
      setIsLoading(false);
    }
  }, [t]);

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
          setErrorMessage(getApiErrorMessage(error, t("policies.loadError"), t));
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
  }, [isAdministrator, t]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!Number.isInteger(parsedQuorum) || parsedQuorum < 1) {
      setErrorMessage(t("policies.validation.quorumMin"));
      return;
    }

    if (!Number.isInteger(parsedSla) || parsedSla < 1) {
      setErrorMessage(t("policies.validation.slaMin"));
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
      setSuccessMessage(t("policies.saveSuccess"));
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, t("policies.updateError"), t));
    } finally {
      setIsSaving(false);
    }
  }

  if (!isAdministrator) {
    return (
      <Stack spacing={2.5}>
        <PageHeader
          title={t("policies.title")}
          description={t("policies.subtitle")}
        />
        <Alert severity="warning">
          {t("policies.adminRequired")}
        </Alert>
      </Stack>
    );
  }

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title={t("policies.title")}
        description={t("policies.subtitle")}
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
              {t("policies.inviteApprovers")}
            </Button>
          }
        >
          {t("policies.noApprovers")}
        </Alert>
      ) : showQuorumWarning ? (
        <Alert severity="warning">
          {t("policies.quorumWarningPlural", { count: parsedQuorum, available: activeApproverCount })}
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
                    {t("policies.sections.quorum.title")}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t("policies.sections.quorum.subtitle")}
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
                      label={t("policies.sections.quorum.fieldLabel")}
                      type="number"
                      value={quorumValue}
                      onChange={(event) => setQuorumValue(event.target.value)}
                      error={showQuorumWarning}
                      slotProps={{
                        htmlInput: { min: 1, step: 1 },
                        input: {
                          endAdornment: (
                            <InputAdornment position="end">
                              {parsedQuorum === 1 ? t("policies.sections.quorum.fieldSuffix") : t("policies.sections.quorum.fieldSuffixPlural")}
                            </InputAdornment>
                          ),
                        },
                      }}
                      helperText={
                        showQuorumWarning
                          ? t("policies.sections.quorum.helperInsufficient", { count: activeApproverCount })
                          : activeApproverCount === 1 
                            ? t("policies.sections.quorum.helperAvailable", { count: activeApproverCount })
                            : t("policies.sections.quorum.helperAvailablePlural", { count: activeApproverCount })
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
                          {t("policies.sections.quorum.selfApprovalLabel")}
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
                    {t("policies.sections.sla.title")}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t("policies.sections.sla.subtitle")}
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
                      label={t("policies.sections.sla.fieldLabel")}
                      type="number"
                      value={slaValue}
                      onChange={(event) => setSlaValue(event.target.value)}
                      slotProps={{
                        htmlInput: { min: 1, step: 1 },
                        input: {
                          endAdornment: (
                            <InputAdornment position="end">
                              {parsedSla === 1 ? t("policies.sections.sla.fieldSuffix") : t("policies.sections.sla.fieldSuffixPlural")}
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
              t("policies.saveButton")
            )}
          </Button>
        </Stack>
      </Box>
    </Stack>
  );
}

function getApiErrorMessage(error: unknown, fallback: string, t: (key: string) => string): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? fallback;
  }

  return t(fallback) || fallback;
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
