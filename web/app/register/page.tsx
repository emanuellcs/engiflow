"use client";

import CheckCircle from "@mui/icons-material/CheckCircle";
import RadioButtonUnchecked from "@mui/icons-material/RadioButtonUnchecked";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Step from "@mui/material/Step";
import StepContent from "@mui/material/StepContent";
import StepLabel from "@mui/material/StepLabel";
import Stepper from "@mui/material/Stepper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useTheme } from "@mui/material/styles";
import useMediaQuery from "@mui/material/useMediaQuery";
import { useRouter } from "next/navigation";
import { type FormEvent, type ReactNode, useEffect, useRef, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { type AuthSessionResult, useAuth } from "@/lib/auth/AuthContext";

type RegisterFormState = {
  companyName: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
  adminConfirmPassword: string;
};

type RegisterFieldErrors = Partial<Record<keyof RegisterFormState, string>>;

const steps = ["Company Details", "Administrator Profile"] as const;
const initialFormState: RegisterFormState = {
  companyName: "",
  adminName: "",
  adminEmail: "",
  adminPassword: "",
  adminConfirmPassword: "",
};
const defaultRegisterError =
  "Unable to register your company. Review the details and try again.";
const invalidAuthResponseError =
  "The server returned an invalid authentication response. Please try again.";
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const passwordPatterns = {
  length: /^.{12,}$/,
  uppercase: /[A-Z]/,
  lowercase: /[a-z]/,
  number: /[0-9]/,
  symbol: /[^a-zA-Z0-9]/,
};

/**
 * Renders the public company registration wizard and auto-signs in the first
 * administrator after successful tenant creation.
 *
 * @returns The self-service onboarding page for new EngiFlow companies.
 */
export default function RegisterPage() {
  const router = useRouter();
  const { login } = useAuth();
  const theme = useTheme();
  const isSmallScreen = useMediaQuery(theme.breakpoints.down("md"), {
    noSsr: true,
  });
  const adminNameRef = useRef<HTMLInputElement>(null);
  const [activeStep, setActiveStep] = useState(0);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [form, setForm] = useState<RegisterFormState>(initialFormState);
  const [fieldErrors, setFieldErrors] = useState<RegisterFieldErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    if (activeStep === 1) {
      // Small timeout ensures the DOM has rendered the new step's fields
      const timer = setTimeout(() => {
        adminNameRef.current?.focus();
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [activeStep]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isPending) {
      return;
    }

    setSubmitted(true);

    // Final comprehensive validation
    const step0Errors = validateStep(0, form);
    const step1Errors = validateStep(1, form);
    const nextErrors: RegisterFieldErrors = {
      ...step0Errors,
      ...step1Errors,
    };

    if (hasErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      // If Step 0 has errors and we are on Step 1, go back to Step 0
      if (hasErrors(step0Errors)) {
        setActiveStep(0);
      }
      return;
    }

    setIsPending(true);
    setErrorMessage(null);

    try {
      const response = await apiFetch<AuthSessionResult>(
        "/api/auth/register-company",
        {
          method: "POST",
          skipAuth: true,
          body: {
            companyName: form.companyName.trim(),
            adminName: form.adminName.trim(),
            adminEmail: form.adminEmail.trim(),
            adminPassword: form.adminPassword,
          },
        },
      );

      login(response, true);
      router.replace("/");
    } catch (error) {
      setErrorMessage(getRegisterErrorMessage(error));
    } finally {
      setIsPending(false);
    }
  }

  function handleNextClick() {
    if (isPending) return;
    handleNext();
  }

  function handleNextSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    handleNext();
  }

  function handleNext() {
    setErrorMessage(null);
    setActiveStep((current) => Math.min(current + 1, steps.length - 1));
  }

  function handleBack() {
    setErrorMessage(null);
    setActiveStep((current) => Math.max(current - 1, 0));
  }

  function handleFieldChange(field: keyof RegisterFormState, value: string) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
    setFieldErrors((current) => {
      if (!current[field]) return current;
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  function renderStepFields(stepIndex: number): ReactNode {
    if (stepIndex === 0) {
      const hasError = submitted && Boolean(fieldErrors.companyName);
      return (
        <Stack spacing={2}>
          <TextField
            id="companyName"
            name="companyName"
            label="Company name"
            value={form.companyName}
            onChange={(event) =>
              handleFieldChange("companyName", event.target.value)
            }
            autoComplete="organization"
            disabled={isPending}
            error={hasError}
            helperText={hasError ? fieldErrors.companyName : ""}
            required
            fullWidth
            size="small"
          />
        </Stack>
      );
    }

    return (
      <Stack spacing={2}>
        <TextField
          id="adminName"
          name="adminName"
          label="Full name"
          inputRef={adminNameRef}
          value={form.adminName}
          onChange={(event) => handleFieldChange("adminName", event.target.value)}
          autoComplete="name"
          disabled={isPending}
          error={submitted && Boolean(fieldErrors.adminName)}
          helperText={submitted ? fieldErrors.adminName : ""}
          required
          fullWidth
          size="small"
        />
        <TextField
          id="adminEmail"
          name="adminEmail"
          label="Email"
          type="email"
          value={form.adminEmail}
          onChange={(event) =>
            handleFieldChange("adminEmail", event.target.value)
          }
          autoComplete="email"
          disabled={isPending}
          error={submitted && Boolean(fieldErrors.adminEmail)}
          helperText={submitted ? fieldErrors.adminEmail : ""}
          required
          fullWidth
          size="small"
        />
        <Stack spacing={1}>
          <TextField
            id="adminPassword"
            name="adminPassword"
            label="Password"
            type={showPassword ? "text" : "password"}
            value={form.adminPassword}
            onChange={(event) =>
              handleFieldChange("adminPassword", event.target.value)
            }
            autoComplete="new-password"
            disabled={isPending}
            error={submitted && Boolean(fieldErrors.adminPassword)}
            helperText={submitted ? fieldErrors.adminPassword : ""}
            required
            fullWidth
            size="small"
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

          <Box sx={{ pl: 0.5 }}>
            <Stack spacing={0.5}>
              {[
                { label: "12+ characters", met: passwordPatterns.length.test(form.adminPassword) },
                { label: "1 Uppercase", met: passwordPatterns.uppercase.test(form.adminPassword) },
                { label: "1 Lowercase", met: passwordPatterns.lowercase.test(form.adminPassword) },
                { label: "1 Number", met: passwordPatterns.number.test(form.adminPassword) },
                {
                  label: "1 Symbol",
                  met: passwordPatterns.symbol.test(form.adminPassword),
                },
              ].map((criterion) => (
                <Stack
                  key={criterion.label}
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: "center" }}
                >
                  {criterion.met ? (
                    <CheckCircle sx={{ fontSize: 16, color: "success.main" }} />
                  ) : (
                    <RadioButtonUnchecked
                      sx={{ fontSize: 16, color: "text.disabled" }}
                    />
                  )}
                  <Typography
                    variant="caption"
                    color={criterion.met ? "success.main" : "text.secondary"}
                  >
                    {criterion.label}
                  </Typography>
                </Stack>
              ))}
            </Stack>
          </Box>
        </Stack>

        <TextField
          id="adminConfirmPassword"
          name="adminConfirmPassword"
          label="Confirm password"
          type={showConfirmPassword ? "text" : "password"}
          value={form.adminConfirmPassword}
          onChange={(event) =>
            handleFieldChange("adminConfirmPassword", event.target.value)
          }
          autoComplete="new-password"
          disabled={isPending}
          error={submitted && Boolean(fieldErrors.adminConfirmPassword)}
          helperText={submitted ? fieldErrors.adminConfirmPassword : ""}
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
      </Stack>
    );
  }

  function renderActions(stepIndex: number): ReactNode {
    const isStepFinal = stepIndex === steps.length - 1;

    return (
      <Stack
        direction={{ xs: "column-reverse", sm: "row" }}
        spacing={1.5}
        sx={{ pt: 1 }}
      >
        <Button
          type="button"
          variant="outlined"
          disabled={isPending || stepIndex === 0}
          onClick={handleBack}
          sx={{ textTransform: "none" }}
        >
          Back
        </Button>
        <Button
          type={isStepFinal ? "submit" : "button"}
          variant="contained"
          disabled={isPending}
          onClick={isStepFinal ? undefined : handleNextClick}
          sx={{
            minHeight: 40,
            textTransform: "none",
          }}
        >
          {isPending && isStepFinal ? (
            <CircularProgress color="inherit" size={20} thickness={5} />
          ) : isStepFinal ? (
            "Create account"
          ) : (
            "Next"
          )}
        </Button>
      </Stack>
    );
  }

  return (
    <Container
      maxWidth="md"
      sx={{
        minHeight: { xs: "calc(100vh - 160px)", sm: "calc(100vh - 176px)" },
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        px: { xs: 2, sm: 3 },
        py: { xs: 3, sm: 6 },
      }}
    >
      <Paper
        component="section"
        elevation={2}
        sx={{
          width: "100%",
          maxWidth: 720,
          p: { xs: 3, sm: 4 },
        }}
      >
        <Box sx={{ width: "100%" }}>
          <Stack spacing={3}>
            <Stack spacing={1}>
              <Typography variant="h4" component="h1" sx={{ fontWeight: 500 }}>
                Register company
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Create a company workspace and first administrator.
              </Typography>
            </Stack>

            {errorMessage ? (
              <Alert severity="error">
                {errorMessage}
              </Alert>
            ) : null}

            <Stepper
              activeStep={activeStep}
              orientation={isSmallScreen ? "vertical" : "horizontal"}
            >
              {steps.map((label, index) => (
                <Step key={label}>
                  <StepLabel
                    error={submitted && stepHasErrors(index, fieldErrors)}
                  >
                    {label}
                  </StepLabel>
                  {isSmallScreen ? (
                    <StepContent>
                      <Box
                        component="form"
                        noValidate
                        onSubmit={index === steps.length - 1 ? handleSubmit : handleNextSubmit}
                        sx={{ pt: 1, display: "flex", flexDirection: "column", gap: 2 }}
                      >
                        {renderStepFields(index)}
                        {renderActions(index)}
                      </Box>
                    </StepContent>
                  ) : null}
                </Step>
              ))}
            </Stepper>

            {!isSmallScreen ? (
              <>
                {steps.map((label, index) => {
                  if (activeStep !== index) return null;
                  return (
                    <Box
                      key={label}
                      component="form"
                      noValidate
                      onSubmit={index === steps.length - 1 ? handleSubmit : handleNextSubmit}
                      sx={{ display: "flex", flexDirection: "column", gap: 2.5 }}
                    >
                      {renderStepFields(index)}
                      {renderActions(index)}
                    </Box>
                  );
                })}
              </>
            ) : null}

            <Typography variant="body2" color="text.secondary" align="center">
              Already have an account?{" "}
              <Link component={NextLink} href="/login" underline="hover">
                Sign in
              </Link>
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Container>
  );
}

function validateStep(
  stepIndex: number,
  form: RegisterFormState,
): RegisterFieldErrors {
  if (stepIndex === 0) {
    if (!form.companyName.trim()) {
      return { companyName: "Company name is required." };
    }
    // RegEx: At least 2 characters, supports unicode letters, numbers, spaces, and basic punctuation
    if (!/^[\p{L}\p{N}\s.,&'-]{2,}$/u.test(form.companyName.trim())) {
      return { companyName: "Enter a valid company name (min 2 characters)." };
    }
    return {};
  }

  const errors: RegisterFieldErrors = {};

  if (!form.adminName.trim()) {
    errors.adminName = "Full name is required.";
  } else if (!/^[\p{L}\s.'-]{2,}$/u.test(form.adminName.trim())) {
    errors.adminName = "Enter a valid name (min 2 characters).";
  }

  if (!form.adminEmail.trim()) {
    errors.adminEmail = "Email is required.";
  } else if (!emailPattern.test(form.adminEmail.trim())) {
    errors.adminEmail = "Enter a valid email address.";
  }

  if (!form.adminPassword) {
    errors.adminPassword = "Password is required.";
  } else if (!passwordPatterns.length.test(form.adminPassword)) {
    errors.adminPassword = "Password must be at least 12 characters.";
  } else if (!passwordPatterns.uppercase.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one uppercase letter.";
  } else if (!passwordPatterns.lowercase.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one lowercase letter.";
  } else if (!passwordPatterns.number.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one number.";
  } else if (!passwordPatterns.symbol.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one symbol.";
  }

  if (form.adminPassword && form.adminConfirmPassword !== form.adminPassword) {
    errors.adminConfirmPassword = "Passwords do not match.";
  }

  return errors;
}

function hasErrors(errors: RegisterFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

function stepHasErrors(
  stepIndex: number,
  errors: RegisterFieldErrors,
): boolean {
  if (stepIndex === 0) {
    return Boolean(errors.companyName);
  }

  return Boolean(
    errors.adminName ||
      errors.adminEmail ||
      errors.adminPassword ||
      errors.adminConfirmPassword,
  );
}

function getRegisterErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? defaultRegisterError;
  }

  if (
    error instanceof Error &&
    error.message === "The server returned an invalid authentication response."
  ) {
    return invalidAuthResponseError;
  }

  return defaultRegisterError;
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
