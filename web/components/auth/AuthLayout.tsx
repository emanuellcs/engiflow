"use client";

import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import AssignmentTurnedInIcon from "@mui/icons-material/AssignmentTurnedIn";
import HistoryEduIcon from "@mui/icons-material/HistoryEdu";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMemo, type ReactNode } from "react";
import LanguageSwitcher from "@/components/ui/LanguageSwitcher";
import { useTranslation } from "@/context/I18nContext";

/**
 * Describes one branding feature rendered in the public auth split screen.
 */
type AuthFeatureItem = {
  icon: ReactNode;
  title: string;
  description: string;
};

/**
 * Describes the props accepted by the shared public auth layout.
 */
interface AuthLayoutProps {
  children: ReactNode;
}

/**
 * Renders the shared EngiFlow public auth layout.
 */
export default function AuthLayout({ children }: AuthLayoutProps) {
  const { t } = useTranslation();

  const featureItems = useMemo<AuthFeatureItem[]>(() => [
    {
      icon: <AssignmentTurnedInIcon sx={{ color: "primary.light" }} />,
      title: t("auth.layout.features.workflow.title"),
      description: t("auth.layout.features.workflow.description"),
    },
    {
      icon: <HistoryEduIcon sx={{ color: "primary.light" }} />,
      title: t("auth.layout.features.audit.title"),
      description: t("auth.layout.features.audit.description"),
    },
    {
      icon: <AdminPanelSettingsIcon sx={{ color: "primary.light" }} />,
      title: t("auth.layout.features.access.title"),
      description: t("auth.layout.features.access.description"),
    },
  ], [t]);

  return (
    <Box
      component="main"
      sx={{
        minHeight: "100vh",
        display: "flex",
        flexDirection: { xs: "column", md: "row" },
        bgcolor: "background.default",
        position: "relative",
      }}
    >
      <Box
        sx={{
          position: "absolute",
          top: 16,
          right: 16,
          zIndex: 100,
          // Mobile visibility: make icon white on small screens to contrast with dark green background
          "& .MuiButton-root": {
            color: { xs: "common.white", md: "text.secondary" },
            "&:hover": {
              color: { xs: "common.white", md: "primary.main" },
              bgcolor: { xs: "rgba(255, 255, 255, 0.08)", md: "action.hover" },
            },
          },
        }}
      >
        <LanguageSwitcher />
      </Box>
      <Box
        component="section"
        sx={{
          display: "flex",
          flex: { xs: "0 0 auto", md: "1 1 50%" },
          minHeight: { xs: "30vh", md: "100vh" },
          bgcolor: "secondary.dark",
          color: "secondary.contrastText",
          px: { xs: 3, sm: 6, lg: 9 },
          pt: { xs: 4, md: 6 },
          pb: { xs: 10, md: 6 },
          alignItems: { xs: "flex-start", md: "center" },
          justifyContent: { xs: "center", md: "flex-start" },
          position: "relative",
          zIndex: { xs: 5, md: 20 },
        }}
      >
        <Stack spacing={{ xs: 3, md: 4 }} sx={{ maxWidth: 520, width: "100%" }}>
          <Stack spacing={1.5}>
            <Typography
              variant="h3"
              component="p"
              sx={{
                fontWeight: 500,
                fontSize: { xs: "1.75rem", sm: "2.5rem", md: "3rem" },
              }}
            >
              EngiFlow
            </Typography>
            <Typography
              variant="h6"
              component="h1"
              sx={{
                color: "grey.100",
                fontSize: { xs: "0.875rem", md: "1.25rem" },
                opacity: 0.9,
              }}
            >
              {t("auth.layout.mission")}
            </Typography>
          </Stack>

          <Stack spacing={{ xs: 2, md: 3 }} sx={{ mt: { xs: 0.5, md: 0 } }}>
            {featureItems.map((item) => (
              <Stack
                key={item.title}
                direction="row"
                spacing={2}
                sx={{ alignItems: "flex-start" }}
              >
                <Box
                  sx={{
                    pt: 0.25,
                    "& svg": { fontSize: { xs: 18, md: 24 } },
                    color: "primary.light",
                  }}
                >
                  {item.icon}
                </Box>
                <Box>
                  <Typography
                    variant="subtitle1"
                    sx={{
                      fontWeight: 500,
                      fontSize: { xs: "0.8125rem", md: "1rem" },
                      lineHeight: 1.2,
                    }}
                  >
                    {item.title}
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      color: "grey.400",
                      fontSize: { xs: "0.75rem", md: "0.875rem" },
                    }}
                  >
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
          justifyContent: { xs: "flex-start", md: "center" },
          alignItems: "center",
          px: { xs: 2, sm: 4, lg: 8 },
          py: { xs: 0, md: 6 },
          mt: { xs: -6, md: 0 },
          position: "relative",
          zIndex: 10,
          width: "100%",
        }}
      >
        {children}
      </Stack>
    </Box>
  );
}
