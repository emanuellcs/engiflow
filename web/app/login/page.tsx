"use client";

import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import AssignmentTurnedInIcon from "@mui/icons-material/AssignmentTurnedIn";
import HistoryEduIcon from "@mui/icons-material/HistoryEdu";
import Visibility from "@mui/icons-material/Visibility";
import VisibilityOff from "@mui/icons-material/VisibilityOff";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogContentText from "@mui/material/DialogContentText";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import IconButton from "@mui/material/IconButton";
import InputAdornment from "@mui/material/InputAdornment";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { type AuthSessionResult, useAuth } from "@/lib/auth/AuthContext";

type LoginFieldErrors = {
  email?: string;
  password?: string;
};

const featureItems = [
  {
    icon: <AssignmentTurnedInIcon sx={{ color: "primary.light" }} />,
    title: "ECO Workflow",
    description: "Create, submit, approve, and reject controlled changes with clear ownership.",
  },
  {
    icon: <HistoryEduIcon sx={{ color: "primary.light" }} />,
    title: "Audit Trails",
    description: "Capture immutable activity history for every material workflow decision.",
  },
  {
    icon: <AdminPanelSettingsIcon sx={{ color: "primary.light" }} />,
    title: "Role-Based Access",
    description: "Separate requesters, approvers, and administrators across each tenant.",
  },
];

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const defaultLoginError =
  "Unable to sign in. Check your email and password, then try again.";
const invalidAuthResponseError =
  "The server returned an invalid authentication response. Please try again.";
const forgotPasswordSuccess =
  "If an account exists, a reset link has been sent";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(true);
  const [fieldErrors, setFieldErrors] = useState<LoginFieldErrors>({});
  const [isPending, setIsPending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isForgotPasswordOpen, setIsForgotPasswordOpen] = useState(false);
  const [isFirstAccessOpen, setIsFirstAccessOpen] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isPending) {
      return;
    }

    const nextErrors = validateLogin(email, password);

    if (hasErrors(nextErrors)) {
      setFieldErrors(nextErrors);
      return;
    }

    setIsPending(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    try {
      const response = await apiFetch<AuthSessionResult>("/api/auth/login", {
        method: "POST",
        skipAuth: true,
        body: {
          email: email.trim(),
          password,
        },
      });

      login(response, rememberMe);
      router.replace("/");
    } catch (error) {
      setErrorMessage(getLoginErrorMessage(error));
    } finally {
      setIsPending(false);
    }
  }

  function handleFieldChange(field: "email" | "password", value: string) {
    if (field === "email") {
      setEmail(value);
    } else {
      setPassword(value);
    }

    setFieldErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  return (
    <Box
      component="main"
      sx={{
        minHeight: "100vh",
        display: "flex",
        bgcolor: "background.default",
      }}
    >
      <Box
        component="section"
        sx={{
          display: { xs: "none", md: "flex" },
          flex: "1 1 50%",
          minWidth: 0,
          bgcolor: "secondary.dark",
          color: "secondary.contrastText",
          px: { md: 6, lg: 9 },
          py: 6,
          alignItems: "center",
        }}
      >
        <Stack spacing={4} sx={{ maxWidth: 520 }}>
          <Stack spacing={1.5}>
            <Typography variant="h3" component="p" sx={{ fontWeight: 500 }}>
              EngiFlow
            </Typography>
            <Typography variant="h6" component="h1" sx={{ color: "grey.100" }}>
              Engineering change control for modern B2B teams.
            </Typography>
          </Stack>
          <Stack spacing={3}>
            {featureItems.map((item) => (
              <Stack key={item.title} direction="row" spacing={2}>
                <Box sx={{ pt: 0.25 }}>{item.icon}</Box>
                <Box>
                  <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
                    {item.title}
                  </Typography>
                  <Typography variant="body2" sx={{ color: "grey.300" }}>
                    {item.description}
                  </Typography>
                </Box>
              </Stack>
            ))}
          </Stack>
        </Stack>
      </Box>

      <Stack
        component="section"
        sx={{
          flex: "1 1 50%",
          minWidth: 0,
          justifyContent: "center",
          alignItems: "center",
          px: { xs: 2, sm: 4, lg: 8 },
          py: { xs: 4, md: 6 },
        }}
      >
        <Card
          variant="outlined"
          sx={{
            width: "100%",
            maxWidth: 448,
            p: { xs: 3, sm: 4 },
            boxShadow: 1,
          }}
        >
          <Box
            component="form"
            noValidate
            onSubmit={handleSubmit}
            sx={{ width: "100%" }}
          >
            <Stack spacing={2}>
              <Stack spacing={0.75}>
                <Typography variant="h4" component="h1">
                  Sign in
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Access your EngiFlow workspace.
                </Typography>
              </Stack>

              {errorMessage ? (
                <Alert severity="error">
                  {errorMessage}
                </Alert>
              ) : null}
              {successMessage ? (
                <Alert severity="success">
                  {successMessage}
                </Alert>
              ) : null}

              <Stack spacing={1}>
                <TextField
                  id="outlined-basic-email"
                  name="email"
                  label="Email"
                  variant="outlined"
                  type="email"
                  value={email}
                  onChange={(event) => handleFieldChange("email", event.target.value)}
                  autoComplete="email"
                  autoFocus
                  disabled={isPending}
                  required
                  fullWidth
                  size="small"
                  error={Boolean(fieldErrors.email)}
                  helperText={fieldErrors.email}
                />
                <TextField
                  id="outlined-basic-password"
                  name="password"
                  label="Password"
                  variant="outlined"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(event) =>
                    handleFieldChange("password", event.target.value)
                  }
                  autoComplete="current-password"
                  disabled={isPending}
                  required
                  fullWidth
                  size="small"
                  error={Boolean(fieldErrors.password)}
                  helperText={fieldErrors.password}
                  slotProps={{
                    input: {
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label="toggle password visibility"
                            onClick={() => setShowPassword((show) => !show)}
                            onMouseDown={(event) => event.preventDefault()}
                            onMouseUp={(event) => event.preventDefault()}
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
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    gap: 2,
                  }}
                >
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={rememberMe}
                        onChange={(event) => setRememberMe(event.target.checked)}
                        size="small"
                      />
                    }
                    label="Remember me"
                    slotProps={{
                      typography: { variant: "body2" },
                    }}
                    sx={{ m: 0 }}
                  />
                  <Box sx={{ display: "flex", gap: 1 }}>
                    <Link
                      component="button"
                      type="button"
                      variant="body2"
                      onClick={() => {
                        setSuccessMessage(null);
                        setIsForgotPasswordOpen(true);
                      }}
                      sx={{ whiteSpace: "nowrap" }}
                    >
                      Forgot password?
                    </Link>
                    <Typography variant="body2" color="text.disabled">
                      •
                    </Typography>
                    <Link
                      component="button"
                      type="button"
                      variant="body2"
                      onClick={() => {
                        setSuccessMessage(null);
                        setIsFirstAccessOpen(true);
                      }}
                      sx={{ whiteSpace: "nowrap" }}
                    >
                      First access?
                    </Link>
                  </Box>
                </Box>
              </Stack>

              <Button
                type="submit"
                variant="contained"
                disabled={isPending}
                fullWidth
                sx={{ minHeight: 40, textTransform: "none" }}
              >
                {isPending ? (
                  <CircularProgress color="inherit" size={20} thickness={5} />
                ) : (
                  "Sign in"
                )}
              </Button>

              <Typography variant="body2" color="text.secondary" align="center">
                Don&apos;t have an account?{" "}
                <Link component={NextLink} href="/register" underline="hover">
                  Register your company
                </Link>
              </Typography>
            </Stack>
          </Box>
        </Card>
      </Stack>

      {isForgotPasswordOpen ? (
        <ForgotPasswordDialog
          open={isForgotPasswordOpen}
          initialEmail={email}
          onClose={() => setIsForgotPasswordOpen(false)}
          onSuccess={() => {
            setIsForgotPasswordOpen(false);
            setSuccessMessage(forgotPasswordSuccess);
            setErrorMessage(null);
          }}
        />
      ) : null}

      {isFirstAccessOpen ? (
        <FirstAccessDialog
          open={isFirstAccessOpen}
          initialEmail={email}
          onClose={() => setIsFirstAccessOpen(false)}
          onSuccess={() => {
            setIsFirstAccessOpen(false);
            setSuccessMessage("First access instructions sent to your email.");
            setErrorMessage(null);
          }}
        />
      ) : null}
    </Box>
  );
}

type FirstAccessDialogProps = {
  open: boolean;
  initialEmail: string;
  onClose: () => void;
  onSuccess: () => void;
};

function FirstAccessDialog({
  open,
  initialEmail,
  onClose,
  onSuccess,
}: FirstAccessDialogProps) {
  const [email, setEmail] = useState(initialEmail);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextError = validateEmail(email);

    if (nextError) {
      setFieldError(nextError);
      return;
    }

    setIsPending(true);
    setFieldError(null);
    setSubmitError(null);

    try {
      // Adaptive usage of the reset-password endpoint or placeholder for first-access
      await apiFetch("/api/auth/first-access", {
        method: "POST",
        skipAuth: true,
        body: {
          email: email.trim(),
        },
      });
      onSuccess();
    } catch (error) {
      setSubmitError(
        readProblemDetailsMessage(error) ?? "Unable to submit first access request.",
      );
    } finally {
      setIsPending(false);
    }
  }

  return (
    <Dialog
      open={open}
      onClose={isPending ? undefined : onClose}
      slotProps={{
        paper: {
          sx: { width: "100%", maxWidth: 440 },
        },
      }}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle>First Access</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <DialogContentText>
            Invited by an admin? Enter your email to receive a secure link to
            set up your account password.
          </DialogContentText>
          {submitError ? (
            <Alert severity="error">
              {submitError}
            </Alert>
          ) : null}
          <TextField
            autoFocus
            required
            id="first-access-email"
            name="email"
            label="Email"
            variant="outlined"
            type="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              setFieldError(null);
            }}
            error={Boolean(fieldError)}
            helperText={fieldError ?? " "}
            disabled={isPending}
            fullWidth
            size="small"
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={onClose}
            disabled={isPending}
            sx={{ textTransform: "none" }}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isPending}
            sx={{ minWidth: 96, textTransform: "none" }}
          >
            {isPending ? (
              <CircularProgress color="inherit" size={18} thickness={5} />
            ) : (
              "Send Link"
            )}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

type ForgotPasswordDialogProps = {
  open: boolean;
  initialEmail: string;
  onClose: () => void;
  onSuccess: () => void;
};

function ForgotPasswordDialog({
  open,
  initialEmail,
  onClose,
  onSuccess,
}: ForgotPasswordDialogProps) {
  const [email, setEmail] = useState(initialEmail);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isPending, setIsPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const nextError = validateEmail(email);

    if (nextError) {
      setFieldError(nextError);
      return;
    }

    setIsPending(true);
    setFieldError(null);
    setSubmitError(null);

    try {
      await apiFetch("/api/auth/forgot-password", {
        method: "POST",
        skipAuth: true,
        body: {
          email: email.trim(),
        },
      });
      onSuccess();
    } catch (error) {
      setSubmitError(readProblemDetailsMessage(error) ?? "Unable to submit reset request.");
    } finally {
      setIsPending(false);
    }
  }

  return (
    <Dialog
      open={open}
      onClose={isPending ? undefined : onClose}
      slotProps={{
        paper: {
          sx: { width: "100%", maxWidth: 440 },
        },
      }}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle>Reset password</DialogTitle>
        <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <DialogContentText>
            Enter your account email address and EngiFlow will send reset
            instructions if the account exists.
          </DialogContentText>
          {submitError ? (
            <Alert severity="error">
              {submitError}
            </Alert>
          ) : null}
          <TextField
            autoFocus
            required
            id="forgot-password-email"
            name="email"
            label="Email"
            variant="outlined"
            type="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              setFieldError(null);
            }}
            error={Boolean(fieldError)}
            helperText={fieldError ?? " "}
            disabled={isPending}
            fullWidth
            size="small"
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={onClose}
            disabled={isPending}
            sx={{ textTransform: "none" }}
          >
            Cancel
          </Button>
          <Button
            type="submit"
            variant="contained"
            disabled={isPending}
            sx={{ minWidth: 96, textTransform: "none" }}
          >
            {isPending ? (
              <CircularProgress color="inherit" size={18} thickness={5} />
            ) : (
              "Continue"
            )}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

function validateLogin(email: string, password: string): LoginFieldErrors {
  const errors: LoginFieldErrors = {};
  const emailError = validateEmail(email);

  if (emailError) {
    errors.email = emailError;
  }

  if (!password) {
    errors.password = "Password is required.";
  }

  return errors;
}

function validateEmail(email: string): string | null {
  if (!email.trim()) {
    return "Email is required.";
  }

  if (!emailPattern.test(email.trim())) {
    return "Enter a valid email address.";
  }

  return null;
}

function hasErrors(errors: LoginFieldErrors): boolean {
  return Object.values(errors).some(Boolean);
}

function getLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error) ?? defaultLoginError;
  }

  if (
    error instanceof Error &&
    error.message === "The server returned an invalid authentication response."
  ) {
    return invalidAuthResponseError;
  }

  return defaultLoginError;
}

function readProblemDetailsMessage(error: unknown): string | null {
  const details = error instanceof ApiError ? error.details : error;

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
