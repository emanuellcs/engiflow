"use client";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import Slide from "@mui/material/Slide";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { TransitionGroup } from "react-transition-group";
import AuthLayout from "@/components/auth/AuthLayout";
import TenantList from "@/components/auth/TenantList";
import { useTranslation } from "@/context/I18nContext";
import LoginForm, {
  type WorkspaceSelectionChallenge,
} from "@/components/auth/LoginForm";
import RegisterForm from "@/components/auth/RegisterForm";
import { ApiError, apiFetch } from "@/lib/api/client";
import { type AuthSessionResult, useAuth } from "@/lib/auth/AuthContext";

/**
 * Identifies the visible auth hub panel.
 */
type AuthPanel = "login" | "register" | "workspacePicker";

/**
 * Renders the public auth hub with login, registration, and workspace selection panels.
 */
function AuthHubContent() {
  const searchParams = useSearchParams();
  const queryMode = searchParams.get("mode") === "register" ? "register" : "login";

  return <AuthHubPanels key={queryMode} initialPanel={queryMode} />;
}

/**
 * Describes the props used by the keyed auth panel state container.
 */
interface AuthHubPanelsProps {
  initialPanel: AuthPanel;
}

/**
 * Maintains the active auth panel state for the current URL mode.
 */
function AuthHubPanels({ initialPanel }: AuthHubPanelsProps) {
  const [panel, setPanel] = useState<AuthPanel>(initialPanel);
  const [workspaceChallenge, setWorkspaceChallenge] =
    useState<WorkspaceSelectionChallenge | null>(null);

  return (
    <AuthLayout>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: "1fr",
          width: "100%",
          maxWidth: 448,
        }}
      >
        <TransitionGroup component={null}>
          <Slide
            key={panel}
            direction={getSlideDirection(panel)}
            timeout={350}
            appear={false}
            mountOnEnter
            unmountOnExit
          >
            <Box sx={{ gridArea: "1 / 1", width: "100%" }}>
              {panel === "login" ? (
                <LoginForm
                  onWorkspaceSelectionRequired={(challenge) => {
                    setWorkspaceChallenge(challenge);
                    setPanel("workspacePicker");
                  }}
                />
              ) : null}
              {panel === "register" ? <RegisterForm /> : null}
              {panel === "workspacePicker" && workspaceChallenge ? (
                <WorkspacePicker
                  challenge={workspaceChallenge}
                  onBack={() => setPanel("login")}
                />
              ) : null}
            </Box>
          </Slide>
        </TransitionGroup>
      </Box>
    </AuthLayout>
  );
}

/**
 * Renders the public auth route.
 */
export default function AuthPage() {
  return (
    <Suspense>
      <AuthHubContent />
    </Suspense>
  );
}

/**
 * Selects the horizontal slide direction for an auth panel.
 */
function getSlideDirection(panel: AuthPanel): "left" | "right" {
  return panel === "login" ? "right" : "left";
}

/**
 * Describes the props used by the workspace picker panel.
 */
interface WorkspacePickerProps {
  challenge: WorkspaceSelectionChallenge;
  onBack: () => void;
}

/**
 * Renders the premium tenant picker using the official MUI List item structure.
 */
function WorkspacePicker({ challenge, onBack }: WorkspacePickerProps) {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [selectedTenantId, setSelectedTenantId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  /**
   * Exchanges the selected tenant and pre-auth token for the final auth session.
   */
  async function handleTenantSelect(tenantId: string) {
    if (selectedTenantId) {
      return;
    }

    setSelectedTenantId(tenantId);
    setErrorMessage(null);

    try {
      const response = await apiFetch<AuthSessionResult>("/api/auth/select-tenant", {
        method: "POST",
        skipAuth: true,
        body: {
          preAuthToken: challenge.preAuthToken,
          tenantId,
        },
      });
      login(response, challenge.rememberMe);
      window.location.assign("/");
    } catch (error) {
      setErrorMessage(readWorkspaceError(error, t));
      setSelectedTenantId(null);
    }
  }

  return (
    <Card
      variant="outlined"
      sx={{
        width: "100%",
        p: { xs: 3, sm: 4 },
        boxShadow: 3,
        borderRadius: 2,
      }}
    >
      <Stack spacing={2.5}>
        <Stack spacing={0.75}>
          <Typography variant="h4" component="h1">
            {t("auth.workspace.title")}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t("auth.workspace.subtitle")}
          </Typography>
        </Stack>

        {errorMessage ? (
          <Typography variant="body2" color="error.main">
            {errorMessage}
          </Typography>
        ) : null}

        <TenantList
          tenants={challenge.tenants}
          selectedTenantId={selectedTenantId}
          onSelect={handleTenantSelect}
        />

        <Button
          type="button"
          variant="text"
          startIcon={<ArrowBackIcon />}
          onClick={onBack}
          disabled={Boolean(selectedTenantId)}
          sx={{ alignSelf: "flex-start", textTransform: "none" }}
        >
          {t("auth.workspace.backToLogin")}
        </Button>
      </Stack>
    </Card>
  );
}

/**
 * Converts a workspace picker API error into user-facing copy.
 */
function readWorkspaceError(error: unknown, t: (key: string) => string): string {
  if (error instanceof ApiError) {
    return t("auth.workspace.error");
  }

  return t("auth.workspace.error");
}
