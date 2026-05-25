"use client";

import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import type { PropsWithChildren } from "react";
import { I18nProvider } from "@/context/I18nContext";
import { AuthProvider } from "@/lib/auth/AuthContext";
import theme from "@/lib/theme";

/**
 * Installs shared client-side providers for the EngiFlow web app.
 *
 * @param props - Provider properties.
 * @param props.initialLocale - The initial locale detected by the server.
 * @param props.children - Route content that needs theme and auth context.
 * @returns Children wrapped with Material UI and authentication providers.
 */
export default function Providers({ 
  children, 
  initialLocale 
}: PropsWithChildren<{ initialLocale?: "en" | "pt-BR" }>) {
  return (
    <I18nProvider initialLocale={initialLocale}>
      <ThemeProvider theme={theme}>
        <LocalizationProvider dateAdapter={AdapterDayjs}>
          <CssBaseline />
          <AuthProvider>{children}</AuthProvider>
        </LocalizationProvider>
      </ThemeProvider>
    </I18nProvider>
  );
}
