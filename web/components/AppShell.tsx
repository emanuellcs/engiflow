"use client";

import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import type { PropsWithChildren } from "react";
import { useAuth } from "@/lib/auth/AuthContext";

export default function AppShell({ children }: PropsWithChildren) {
  const { isAuthenticated, logout, user } = useAuth();

  return (
    <Box sx={{ bgcolor: "background.default", minHeight: "100vh" }}>
      <AppBar position="static" color="primary" elevation={4}>
        <Toolbar>
          <Typography
            variant="h6"
            component="div"
            sx={{ flexGrow: 1, letterSpacing: 0 }}
          >
            EngiFlow
          </Typography>
          {isAuthenticated && user ? (
            <Stack
              direction="row"
              spacing={2}
              sx={{ alignItems: "center", minWidth: 0 }}
            >
              <Typography
                variant="body2"
                color="inherit"
                noWrap
                sx={{ maxWidth: { xs: 96, sm: 220 } }}
              >
                {user.role}
              </Typography>
              <Button color="inherit" size="small" onClick={logout}>
                Logout
              </Button>
            </Stack>
          ) : null}
        </Toolbar>
      </AppBar>
      <Container component="main" maxWidth="lg" sx={{ py: 4 }}>
        {children}
      </Container>
    </Box>
  );
}
