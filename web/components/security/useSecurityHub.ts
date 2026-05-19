"use client";

import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect } from "react";
import {
  clearStoredAuthToken,
  updateStoredAuthSessionRoles,
} from "@/lib/auth/token-storage";

type UseSecurityHubOptions = {
  token: string | null;
  currentUserId: string | undefined;
};

export function useSecurityHub({ token, currentUserId }: UseSecurityHubOptions): void {
  useEffect(() => {
    if (!token || !currentUserId) {
      return undefined;
    }

    const activeUserId = currentUserId;
    let isDisposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl(resolveSecurityHubUrl(), {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    function handlePermissionsChanged(userId: string, newRole: string): void {
      if (!isSameUser(userId, activeUserId)) {
        return;
      }

      updateStoredAuthSessionRoles([newRole]);
    }

    function handleUserDeactivated(userId: string): void {
      if (!isSameUser(userId, activeUserId)) {
        return;
      }

      clearStoredAuthToken();
      window.alert("Your EngiFlow account has been deactivated.");
      window.location.assign("/auth?mode=login");
    }

    connection.on("UserPermissionsChanged", handlePermissionsChanged);
    connection.on("UserDeactivated", handleUserDeactivated);

    async function startConnection(): Promise<void> {
      try {
        await connection.start();
      } catch {
        if (!isDisposed && connection.state !== HubConnectionState.Connected) {
          window.setTimeout(() => {
            if (!isDisposed) {
              void startConnection();
            }
          }, 5000);
        }
      }
    }

    void startConnection();

    return () => {
      isDisposed = true;
      connection.off("UserPermissionsChanged", handlePermissionsChanged);
      connection.off("UserDeactivated", handleUserDeactivated);
      void connection.stop();
    };
  }, [currentUserId, token]);
}

function resolveSecurityHubUrl(): string {
  const baseUrl = (
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    "http://localhost:8080"
  ).replace(/\/+$/, "");

  return `${baseUrl}/hubs/security`;
}

function isSameUser(receivedUserId: string, currentUserId: string): boolean {
  return receivedUserId.toLowerCase() === currentUserId.toLowerCase();
}
