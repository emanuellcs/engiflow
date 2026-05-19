"use client";

import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import AssignmentTurnedInIcon from "@mui/icons-material/AssignmentTurnedIn";
import HistoryEduIcon from "@mui/icons-material/HistoryEdu";
import Box from "@mui/material/Box";
import Slide from "@mui/material/Slide";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { TransitionGroup } from "react-transition-group";
import LoginForm from "@/components/auth/LoginForm";
import RegisterForm from "@/components/auth/RegisterForm";

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

function AuthHubContent() {
  const searchParams = useSearchParams();
  const mode = searchParams.get("mode") === "register" ? "register" : "login";

  return (
    <Box
      component="main"
      sx={{
        minHeight: "100vh",
        display: "flex",
        flexDirection: { xs: "column", md: "row" },
        bgcolor: "background.default",
      }}
    >
      {/* Branding Section */}
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
          zIndex: { xs: 5, md: 20 }, // Elevated on desktop to allow forms to slide "under" it
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
              Engineering change control for modern B2B teams.
            </Typography>
          </Stack>

          {/* Features - Full descriptions restored for all devices */}
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

      {/* Auth Form Section */}
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
          zIndex: 10, // Sits below branding on desktop, but above branding section's base on mobile due to stacking order
          width: "100%",
        }}
      >
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
              key={mode}
              direction={mode === "login" ? "right" : "left"}
              timeout={350}
              appear={false}
              mountOnEnter
              unmountOnExit
            >
              <Box
                sx={{
                  gridArea: "1 / 1",
                  width: "100%",
                  zIndex: mode === "login" ? 1 : 2,
                }}
              >
                {mode === "login" ? <LoginForm /> : <RegisterForm />}
              </Box>
            </Slide>
          </TransitionGroup>
        </Box>
      </Stack>
    </Box>
  );
}

export default function AuthPage() {
  return (
    <Suspense>
      <AuthHubContent />
    </Suspense>
  );
}
