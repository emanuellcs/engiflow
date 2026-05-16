import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import AppShell from "@/components/AppShell";
import Providers from "@/app/providers";
import "./globals.css";

export const metadata: Metadata = {
  title: "EngiFlow",
  description: "Engineering change management for B2B teams.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <AppRouterCacheProvider options={{ key: "mui" }}>
          <Providers>
            <AppShell>{children}</AppShell>
          </Providers>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
