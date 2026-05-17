"use client";

import type { ReactNode } from "react";
import AppShell from "@/components/AppShell";
import ProtectedRoute from "@/components/ProtectedRoute";

type AuthenticatedLayoutProps = {
  children: ReactNode;
};

/**
 * Authenticated workspace layout that protects all grouped routes and wraps
 * them in the shared EngiFlow navigation shell.
 *
 * @param props - Layout properties supplied by the Next.js App Router.
 * @param props.children - The protected workspace route content.
 * @returns Protected route content inside the responsive application shell.
 */
export default function AuthenticatedLayout({
  children,
}: Readonly<AuthenticatedLayoutProps>) {
  return (
    <ProtectedRoute>
      <AppShell>{children}</AppShell>
    </ProtectedRoute>
  );
}
