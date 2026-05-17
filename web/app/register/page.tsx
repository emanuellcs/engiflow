"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
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
import { type FormEvent, type ReactNode, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";

type RegisterFormState = {
  companyName: string;
  adminName: string;
  adminEmail: string;
  adminPassword: string;
};

type RegisterFieldErrors = Partial<Record<keyof RegisterFormState, string>>;

type RegisterCompanyResponse = {
  accessToken?: unknown;
};

const steps = ["Company Details", "Administrator Profile"] as const;
const initialFormState: RegisterFormState = {
  companyName: "",
  adminName: "",
  adminEmail: "",
  adminPassword: "",
};
const defaultRegisterError =
  "Unable to register your company. Review the details and try again.";
const invalidTokenError =
  "The server returned an invalid authentication response. Please try again.";
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const symbolPattern = /[^a-zA-Z0-9]/;

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
  const isSmallScreen = useMediaQuery(theme.breakpoints.down("sm"), {
    noSsr: true,
  });
  const [activeStep, setActiveStep] = useState(0);
  const [form, setForm] = useState<RegisterFormState>(initialFormState);
  const [fieldErrors, setFieldErrors] = useState<RegisterFieldErrors>({});
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);
  const isFinalStep = activeStep === steps.length - 1;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isPending) {
      return;
    }

    if (!isFinalStep) {
      handleNext();
      return;
    }

    const nextErrors = {
      ...validateStep(0, form),
      ...validateStep(1, form),
    };

    if (hasErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      return;
    }

    setIsPending(true);
    setErrorMessage(null);

    try {
      const response = await apiFetch<RegisterCompanyResponse>(
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
      const accessToken = readAccessToken(response);

      login(accessToken);
      router.replace("/");
    } catch (error) {
      setErrorMessage(getRegisterErrorMessage(error));
    } finally {
      setIsPending(false);
    }
  }

  function handleNext() {
    const nextErrors = validateStep(activeStep, form);

    if (hasErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      return;
    }

    setFieldErrors({});
    setErrorMessage(null);
    setActiveStep((current) => Math.min(current + 1, steps.length - 1));
  }

  function handleBack() {
    setFieldErrors({});
    setErrorMessage(null);
    setActiveStep((current) => Math.max(current - 1, 0));
  }

  function handleFieldChange(field: keyof RegisterFormState, value: string) {
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

  function renderStepFields(stepIndex: number): ReactNode {
    if (stepIndex === 0) {
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
            error={Boolean(fieldErrors.companyName)}
            helperText={fieldErrors.companyName}
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
          value={form.adminName}
          onChange={(event) => handleFieldChange("adminName", event.target.value)}
          autoComplete="name"
          disabled={isPending}
          error={Boolean(fieldErrors.adminName)}
          helperText={fieldErrors.adminName}
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
          error={Boolean(fieldErrors.adminEmail)}
          helperText={fieldErrors.adminEmail}
          required
          fullWidth
          size="small"
        />
        <TextField
          id="adminPassword"
          name="adminPassword"
          label="Password"
          type="password"
          value={form.adminPassword}
          onChange={(event) =>
            handleFieldChange("adminPassword", event.target.value)
          }
          autoComplete="new-password"
          disabled={isPending}
          error={Boolean(fieldErrors.adminPassword)}
          helperText={
            fieldErrors.adminPassword ??
            "At least 12 characters with uppercase, lowercase, number, and symbol."
          }
          required
          fullWidth
          size="small"
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
          onClick={isStepFinal ? undefined : handleNext}
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
      disableGutters
      sx={{
        minHeight: { xs: "calc(100vh - 160px)", sm: "calc(100vh - 176px)" },
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        px: { xs: 0, sm: 2 },
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
        <Box
          component="form"
          noValidate
          onSubmit={handleSubmit}
          sx={{ width: "100%" }}
        >
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
              <Alert severity="error" variant="outlined">
                {errorMessage}
              </Alert>
            ) : null}

            <Stepper
              activeStep={activeStep}
              orientation={isSmallScreen ? "vertical" : "horizontal"}
            >
              {steps.map((label, index) => (
                <Step key={label}>
                  <StepLabel error={stepHasErrors(index, fieldErrors)}>
                    {label}
                  </StepLabel>
                  {isSmallScreen ? (
                    <StepContent>
                      <Stack spacing={2} sx={{ pt: 1 }}>
                        {renderStepFields(index)}
                        {renderActions(index)}
                      </Stack>
                    </StepContent>
                  ) : null}
                </Step>
              ))}
            </Stepper>

            {!isSmallScreen ? (
              <Stack spacing={2.5}>
                {renderStepFields(activeStep)}
                {renderActions(activeStep)}
              </Stack>
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
    return form.companyName.trim()
      ? {}
      : { companyName: "Company name is required." };
  }

  const errors: RegisterFieldErrors = {};

  if (!form.adminName.trim()) {
    errors.adminName = "Full name is required.";
  }

  if (!form.adminEmail.trim()) {
    errors.adminEmail = "Email is required.";
  } else if (!emailPattern.test(form.adminEmail.trim())) {
    errors.adminEmail = "Enter a valid email address.";
  }

  if (!form.adminPassword) {
    errors.adminPassword = "Password is required.";
  } else if (form.adminPassword.length < 12) {
    errors.adminPassword = "Password must be at least 12 characters.";
  } else if (!/[A-Z]/.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one uppercase letter.";
  } else if (!/[a-z]/.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one lowercase letter.";
  } else if (!/[0-9]/.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one number.";
  } else if (!symbolPattern.test(form.adminPassword)) {
    errors.adminPassword = "Password must include at least one symbol.";
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

  return Boolean(errors.adminName || errors.adminEmail || errors.adminPassword);
}

function readAccessToken(
  response: RegisterCompanyResponse | null | undefined,
): string {
  if (
    !response ||
    typeof response !== "object" ||
    typeof response.accessToken !== "string" ||
    response.accessToken.trim().length === 0
  ) {
    throw new Error(invalidTokenError);
  }

  return response.accessToken.trim();
}

function getRegisterErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? defaultRegisterError;
  }

  if (error instanceof Error && error.message === invalidTokenError) {
    return invalidTokenError;
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
