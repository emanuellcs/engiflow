"use client";

import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useState } from "react";
import type { EcoRealtimeUpdate } from "@/lib/types/eco";

export type EcoHubConnectionStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

export type UseEcoHubOptions = {
  /** Bearer token used for SignalR authentication. */
  token: string | null;
  /** Handler invoked when the hub broadcasts an ECO update. */
  onEcoChanged: (update: EcoRealtimeUpdate) => void;
};

export type UseEcoHubResult = {
  /** Current SignalR connection status. */
  status: EcoHubConnectionStatus;
  /** Last connection error surfaced by SignalR. */
  errorMessage: string | null;
};

/**
 * Connects to the tenant-scoped ECO SignalR hub and cleans up handlers on unmount.
 *
 * @param options - Hub connection options.
 * @returns Current hub status and error state.
 */
export function useEcoHub({ token, onEcoChanged }: UseEcoHubOptions): UseEcoHubResult {
  const [status, setStatus] = useState<EcoHubConnectionStatus>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!token) {
      const timeoutId = window.setTimeout(() => {
        setStatus("idle");
        setErrorMessage(null);
      }, 0);

      return () => window.clearTimeout(timeoutId);
    }

    let isDisposed = false;
    const connection = new HubConnectionBuilder()
      .withUrl(resolveEcoHubUrl(), {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("EcoChanged", onEcoChanged);
    connection.onreconnecting((error) => {
      if (!isDisposed) {
        setStatus("reconnecting");
        setErrorMessage(getSignalRErrorMessage(error));
      }
    });
    connection.onreconnected(() => {
      if (!isDisposed) {
        setStatus("connected");
        setErrorMessage(null);
      }
    });
    connection.onclose((error) => {
      if (!isDisposed) {
        setStatus("disconnected");
        setErrorMessage(getSignalRErrorMessage(error));
      }
    });

    async function startConnection(): Promise<void> {
      setStatus("connecting");
      setErrorMessage(null);

      try {
        await connection.start();
        if (!isDisposed && connection.state === HubConnectionState.Connected) {
          setStatus("connected");
        }
      } catch (error) {
        if (!isDisposed) {
          setStatus("disconnected");
          setErrorMessage(getSignalRErrorMessage(error));
        }
      }
    }

    void startConnection();

    return () => {
      isDisposed = true;
      connection.off("EcoChanged", onEcoChanged);
      void connection.stop();
    };
  }, [onEcoChanged, token]);

  return { status, errorMessage };
}

function resolveEcoHubUrl(): string {
  const baseUrl = (
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    "http://localhost:8080"
  ).replace(/\/+$/, "");

  return `${baseUrl}/hubs/ecos`;
}

function getSignalRErrorMessage(error: unknown): string | null {
  if (!error) {
    return null;
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return "The real-time ECO connection is unavailable.";
}
