"use client";

import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useSearchParams } from "next/navigation";
import { Suspense, type FormEvent, useEffect, useMemo, useState } from "react";
import AuthLayout from "@/components/auth/AuthLayout";
import PasswordChecklist from "@/components/auth/PasswordChecklist";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useTranslation } from "@/context/I18nContext";

/**
 * Describes the setup-password token purpose returned by the API.
 */
type SetupPasswordPurpose = "Invitation" | "Reset";

/**
 * Describes setup-password context returned by the API.
 */
interface SetupPasswordContext {
  purpose: SetupPasswordPurpose;
  email: string;
}

/**
 * Describes client-side setup-password field errors.
 */
type SetupPasswordFieldErrors = {
  password?: string;
  confirmPassword?: string;
};

/**
 * Renders the setup-password route inside a suspense boundary for search params.
 */
export default function SetupPasswordPage() {
  return (
    <Suspense>
      <SetupPasswordContent />
    </Suspense>
  );
}

/**
 * Renders the context-aware invitation setup and password reset form.
 */
function SetupPasswordContent() {
  const { t } = useTranslation();
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const email = searchParams.get("email") ?? "";
  const [context, setContext] = useState<SetupPasswordContext | null>(null);
  const [isContextLoading, setIsContextLoading] = useState(true);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<SetupPasswordFieldErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const copy = useMemo(() => {
    if (context?.purpose === "Reset") {
      return {
        title: t("auth.setupPassword.reset.title"),
        description: t("auth.setupPassword.reset.description"),
      };
    }
    return {
      title: t("auth.setupPassword.invitation.title"),
      description: t("auth.setupPassword.invitation.description"),
    };
  }, [context?.purpose, t]);

  useEffect(() => {
    let isMounted = true;

    /**
     * Loads the token context before rendering the final page copy.
     */
    async function loadContext() {
      if (!token || !email) {
        setErrorMessage(t("auth.setupPassword.invalidContext"));
        setIsContextLoading(false);
        return;
      }

      try {
        const response = await apiFetch<SetupPasswordContext>(
          "/api/auth/setup-password/context",
          {
            method: "POST",
            skipAuth: true,
            body: { token, email },
          },
        );

        if (isMounted) {
          setContext(response);
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(readSetupError(error, t));
        }
      } finally {
        if (isMounted) {
          setIsContextLoading(false);
        }
      }
    }

    void loadContext();

    return () => {
      isMounted = false;
    };
  }, [email, token, t]);

  /**
   * Submits the new password and consumes the setup token.
   */
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting || !context) {
      return;
    }

    const nextErrors = validateSetupPassword(password, confirmPassword, t);

    if (hasSetupErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      await apiFetch("/api/auth/setup-password", {
        method: "POST",
        skipAuth: true,
        body: {
          token,
          email: context.email,
          password,
        },
      });
      setSuccessMessage(t("auth.setupPassword.success"));
      setPassword("");
      setConfirmPassword("");
    } catch (error) {
      setErrorMessage(readSetupError(error, t));
    } finally {
      setIsSubmitting(false);
    }
  }

  /**
   * Updates a password field and clears stale validation errors.
   */
  function handleFieldChange(field: keyof SetupPasswordFieldErrors, value: string) {
    if (field === "password") {
      setPassword(value);
    } else {
      setConfirmPassword(value);
    }

    setFieldErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  return (
    <AuthLayout>
      <Card
        variant="outlined"
        sx={{
          width: "100%",
          maxWidth: 448,
          p: { xs: 3, sm: 4 },
          boxShadow: 3,
          borderRadius: 2,
        }}
      >
        <Box component="form" noValidate onSubmit={handleSubmit}>
          <Stack spacing={2.25}>
            <Stack spacing={0.75}>
              <Typography variant="h4" component="h1">
                {copy.title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {copy.description}
              </Typography>
            </Stack>

            {isContextLoading ? (
              <Stack sx={{ alignItems: "center", py: 4 }}>
                <CircularProgress size={28} thickness={5} />
              </Stack>
            ) : null}

            {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}
            {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}

            {!isContextLoading && context ? (
              <>
                <TextField
                  id="setup-password-email"
                  label={t("auth.setupPassword.emailLabel")}
                  value={context.email}
                  disabled
                  fullWidth
                  size="small"
                />
                <Stack spacing={0}>
                  <TextField
                    id="setup-password"
                    label={t("auth.setupPassword.newPasswordLabel")}
                    type={showPassword ? "text" : "password"}
                    value={password}
                    onChange={(event) => handleFieldChange("password", event.target.value)}
                    autoComplete="new-password"
                    disabled={isSubmitting || Boolean(successMessage)}
                    error={Boolean(fieldErrors.password)}
                    helperText={fieldErrors.password ?? " "}
                    required
                    fullWidth
                    size="small"
                    sx={{
                      "& .MuiFormHelperText-root": {
                        mb: fieldErrors.password ? 0 : -1,
                      },
                    }}
                    slotProps={{
                      input: {
                        endAdornment: (
                          <InputAdornment position="end">
                            <IconButton
                              aria-label="toggle password visibility"
                              onClick={() => setShowPassword((show) => !show)}
                              edge="end"
                              size="small"
                            >
                              {showPassword ? (
                                <VisibilityOff fontSize="small" />
                              ) : (
                                <Visibility fontSize="small" />
                              )}
                            </IconButton>
                          </InputAdornment>
                        ),
                      },
                    }}
                  />
                  <PasswordChecklist password={password} />
                </Stack>
                <TextField
                  id="setup-confirm-password"
                  label={t("auth.setupPassword.confirmPasswordLabel")}
                  type={showConfirmPassword ? "text" : "password"}
                  value={confirmPassword}
                  onChange={(event) => handleFieldChange("confirmPassword", event.target.value)}
                  autoComplete="new-password"
                  disabled={isSubmitting || Boolean(successMessage)}
                  error={Boolean(fieldErrors.confirmPassword)}
                  helperText={fieldErrors.confirmPassword ?? " "}
                  required
                  fullWidth
                  size="small"
                  slotProps={{
                    input: {
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label="toggle confirm password visibility"
                            onClick={() => setShowConfirmPassword((show) => !show)}
                            edge="end"
                            size="small"
                          >
                            {showConfirmPassword ? (
                              <VisibilityOff fontSize="small" />
                            ) : (
                              <Visibility fontSize="small" />
                            )}
                          </IconButton>
                        </InputAdornment>
                      ),
                    },
                  }}
                />
                <Button
                  type="submit"
                  variant="contained"
                  disabled={isSubmitting || Boolean(successMessage)}
                  fullWidth
                  sx={{ minHeight: 40, textTransform: "none" }}
                >
                  {isSubmitting ? (
                    <CircularProgress color="inherit" size={20} thickness={5} />
                  ) : (
                    t("auth.setupPassword.submitButton")
                  )}
                </Button>
              </>
            ) : null}

            <Typography variant="body2" color="text.secondary" align="center">
              {t("auth.setupPassword.backTo")}{" "}
              <Link component={NextLink} href="/auth?mode=login" underline="hover">
                {t("auth.setupPassword.signIn")}
              </Link>
            </Typography>
          </Stack>
        </Box>
      </Card>
    </AuthLayout>
  );
}

/**
 * Validates the setup-password form.
 */
function validateSetupPassword(
  password: string,
  confirmPassword: string,
  t: (key: string) => string,
): SetupPasswordFieldErrors {
  const errors: SetupPasswordFieldErrors = {};

  if (password.length < 12) {
    errors.password = t("auth.register.validation.passwordLength");
  } else if (!/[A-Z]/.test(password)) {
    errors.password = t("auth.register.validation.passwordUppercase");
  } else if (!/[a-z]/.test(password)) {
    errors.password = t("auth.register.validation.passwordLowercase");
  } else if (!/[0-9]/.test(password)) {
    errors.password = t("auth.register.validation.passwordNumber");
  } else if (!/[^a-zA-Z0-9]/.test(password)) {
    errors.password = t("auth.register.validation.passwordSymbol");
  }

  if (confirmPassword !== password) {
    errors.confirmPassword = t("auth.register.validation.passwordsDontMatch");
  }

  return errors;
}

/**
 * Checks whether the setup-password validation result contains errors.
 */
function hasSetupErrors(errors: SetupPasswordFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

/**
 * Converts setup-password API failures into user-facing copy.
 */
function readSetupError(error: unknown, t: (key: string) => string): string {
  if (error instanceof ApiError && error.status === 400) {
    return readProblemDetailsMessage(error.details) ?? t("common.error");
  }

  return t("auth.setupPassword.invalidContext");
}

/**
 * Reads the most useful message from problem-details payloads.
 */
function readProblemDetailsMessage(details: unknown): string | null {
  if (!details || typeof details !== "object") {
    return null;
  }

  if (
    "errors" in details &&
    details.errors &&
    typeof details.errors === "object"
  ) {
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
  }

  if (
    "detail" in details &&
    typeof details.detail === "string" &&
    details.detail.trim().length > 0
  ) {
    return details.detail;
  }

  return null;
}
