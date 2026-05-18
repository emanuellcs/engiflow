"use client";

import AccountTreeIcon from "@mui/icons-material/AccountTree";
import ManageAccountsIcon from "@mui/icons-material/ManageAccounts";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import type { ReactNode } from "react";
import { usePathname } from "next/navigation";
import NextLink from "@/components/ui/NextLink";

type SettingsLayoutProps = {
  children: ReactNode;
};

const settingsTabs = [
  {
    label: "Team Management",
    href: "/settings/users",
    icon: <ManageAccountsIcon fontSize="small" />,
  },
  {
    label: "Workflow Policies",
    href: "/settings/workflow-policies",
    icon: <AccountTreeIcon fontSize="small" />,
  },
];

export default function SettingsLayout({
  children,
}: Readonly<SettingsLayoutProps>) {
  const pathname = usePathname() ?? "/settings/users";
  const currentTab = settingsTabs.some((tab) => pathname.startsWith(tab.href))
    ? settingsTabs.find((tab) => pathname.startsWith(tab.href))?.href
    : settingsTabs[0].href;

  return (
    <Stack
      direction={{ xs: "column", md: "row" }}
      spacing={{ xs: 2, md: 3 }}
      sx={{ alignItems: "stretch", minHeight: "calc(100vh - 132px)" }}
    >
      <Box
        component="aside"
        sx={{
          width: { xs: "100%", md: 232 },
          flexShrink: 0,
          borderRight: { md: 1 },
          borderBottom: { xs: 1, md: 0 },
          borderColor: "divider",
          pr: { md: 2 },
          pb: { xs: 1, md: 0 },
        }}
      >
        <Tabs
          value={currentTab}
          orientation="vertical"
          variant="scrollable"
          aria-label="Settings sections"
          sx={{
            display: { xs: "none", md: "flex" },
            "& .MuiTab-root": {
              alignItems: "flex-start",
              justifyContent: "flex-start",
              minHeight: 44,
              textTransform: "none",
            },
          }}
        >
          {settingsTabs.map((tab) => (
            <Tab
              key={tab.href}
              component={NextLink}
              href={tab.href}
              icon={tab.icon}
              iconPosition="start"
              label={tab.label}
              value={tab.href}
            />
          ))}
        </Tabs>

        <Tabs
          value={currentTab}
          variant="scrollable"
          scrollButtons="auto"
          aria-label="Settings sections"
          sx={{
            display: { xs: "flex", md: "none" },
            minHeight: 44,
            "& .MuiTab-root": {
              minHeight: 44,
              textTransform: "none",
            },
          }}
        >
          {settingsTabs.map((tab) => (
            <Tab
              key={tab.href}
              component={NextLink}
              href={tab.href}
              icon={tab.icon}
              iconPosition="start"
              label={tab.label}
              value={tab.href}
            />
          ))}
        </Tabs>
      </Box>

      <Divider flexItem sx={{ display: { xs: "none", md: "block" } }} />

      <Box sx={{ flex: 1, minWidth: 0 }}>{children}</Box>
    </Stack>
  );
}
