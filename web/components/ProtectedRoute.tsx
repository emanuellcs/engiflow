"use client";

import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import type { PropsWithChildren } from "react";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";

/**
 * Guards authenticated routes by redirecting anonymous users to the login page
 * and rendering a loading state while auth state is being evaluated.
 *
 * @param props - Protected route properties.
 * @param props.children - Authenticated route content to render after auth passes.
 * @returns The protected children or an authentication loading indicator.
 */
export default function ProtectedRoute({ children }: PropsWithChildren) {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/auth?mode=login");
    }
  }, [isAuthenticated, isLoading, router]);

  if (isLoading || !isAuthenticated) {
    return <ProtectedRouteLoading />;
  }

  return <>{children}</>;
}

/**
 * Renders the centered progress indicator shown while protected content is
 * waiting for authentication state.
 *
 * @returns A status region containing a Material UI circular progress indicator.
 */
function ProtectedRouteLoading() {
  return (
    <Box
      role="status"
      aria-live="polite"
      sx={{
        minHeight: "calc(100vh - 192px)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <CircularProgress aria-label="Loading workspace" size={36} thickness={4} />
    </Box>
  );
}
