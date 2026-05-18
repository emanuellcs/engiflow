"use client";

import Box from "@mui/material/Box";
import type { ReactNode } from "react";

type SettingsLayoutProps = {
  children: ReactNode;
};

export default function SettingsLayout({
  children,
}: Readonly<SettingsLayoutProps>) {
  return (
    <Box sx={{ flexGrow: 1, display: "flex", flexDirection: "column", minHeight: 0 }}>
      {children}
    </Box>
  );
}

