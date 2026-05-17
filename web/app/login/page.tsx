"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import NextLink from "@/components/ui/NextLink";
import { ApiError, apiFetch } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthContext";

type LoginResponse = {
  accessToken?: unknown;
};

const defaultLoginError =
  "Unable to sign in. Check your email and password, then try again.";
const invalidTokenError =
  "The server returned an invalid authentication response. Please try again.";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isPending, setIsPending] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isPending) {
      return;
    }

    setIsPending(true);
    setErrorMessage(null);

    try {
      const response = await apiFetch<LoginResponse>("/api/auth/login", {
        method: "POST",
        skipAuth: true,
        body: {
          email: email.trim(),
          password,
        },
      });
      const accessToken = readAccessToken(response);

      login(accessToken);
      router.replace("/");
    } catch (error) {
      setErrorMessage(getLoginErrorMessage(error));
    } finally {
      setIsPending(false);
    }
  }

  return (
    <Container
      maxWidth="sm"
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
          maxWidth: 480,
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
                Sign in
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Access your EngiFlow workspace.
              </Typography>
            </Stack>

            {errorMessage ? (
              <Alert severity="error" variant="outlined">
                {errorMessage}
              </Alert>
            ) : null}

            <Stack spacing={2}>
              <TextField
                id="email"
                name="email"
                label="Email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
                disabled={isPending}
                required
                fullWidth
              />
              <TextField
                id="password"
                name="password"
                label="Password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
                disabled={isPending}
                required
                fullWidth
              />
            </Stack>

            <Button
              type="submit"
              variant="contained"
              size="large"
              disabled={isPending}
              fullWidth
              sx={{
                minHeight: 48,
                textTransform: "none",
              }}
            >
              {isPending ? (
                <CircularProgress color="inherit" size={22} thickness={5} />
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
      </Paper>
    </Container>
  );
}

function readAccessToken(response: LoginResponse | null | undefined): string {
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

function getLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return readProblemDetailsMessage(error.details) ?? defaultLoginError;
  }

  if (error instanceof Error && error.message === invalidTokenError) {
    return invalidTokenError;
  }

  return defaultLoginError;
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
