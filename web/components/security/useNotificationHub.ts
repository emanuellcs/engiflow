"use client";

import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useEffect, useState, useCallback, useRef } from "react";
import { apiFetch } from "@/lib/api/client";

/**
 * Defines the priority and context categories for user notifications.
 */
export type NotificationCategory = "ActionRequired" | "Update" | "Activity";

/**
 * Describes a user notification for display in the real-time Topbar popover.
 */
export interface NotificationDto {
  /** The unique notification identifier. */
  id: string;
  /** The brief alert summary. */
  title: string;
  /** The detailed alert body. */
  message: string;
  /** The semantic category. */
  category: NotificationCategory;
  /** True if the notification has been acknowledged. */
  isRead: boolean;
  /** The optional navigation target. */
  deepLink: string | null;
  /** The UTC timestamp when the alert was issued. */
  createdAt: string;
}

/**
 * Describes the possible SignalR connection states for the notification hub.
 */
export type NotificationHubStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

/**
 * Configures the real-time notification connection.
 */
export type UseNotificationHubOptions = {
  /** Bearer token used for SignalR authentication. */
  token: string | null;
  /** Current tenant identifier, used to clear state on swap. */
  tenantId: string | null;
};

/**
 * Provides notification state and management operations to application components.
 */
export type UseNotificationHubResult = {
  /** Current SignalR connection status. */
  status: NotificationHubStatus;
  /** The most recent notifications. */
  notifications: NotificationDto[];
  /** Count of unread notifications. */
  unreadCount: number;
  /** Marks a single notification as read. */
  markAsRead: (id: string) => Promise<void>;
  /** Marks all notifications as read. */
  markAllAsRead: () => Promise<void>;
  /** Clears all notifications from local state. */
  clear: () => void;
};

/**
 * Connects to the user-scoped notification hub and maintains unread alert state.
 *
 * @param options - Hub connection options.
 * @returns Notification state and management handlers.
 */
export function useNotificationHub({ token, tenantId }: UseNotificationHubOptions): UseNotificationHubResult {
  const [status, setStatus] = useState<NotificationHubStatus>("idle");
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const initializedRef = useRef(false);

  /**
   * Loads the initial TOP 20 notifications from the persistent store.
   */
  const fetchRecent = useCallback(async () => {
    try {
      const recent = await apiFetch<NotificationDto[]>("/api/notifications");
      setNotifications(recent);
    } catch (error) {
      console.error("Failed to fetch recent notifications:", error);
    }
  }, []);

  /**
   * Resets local notification state.
   */
  const clear = useCallback(() => {
    setNotifications([]);
  }, []);

  // Sync initialization state when tenant changes
  useEffect(() => {
    initializedRef.current = false;
  }, [tenantId]);

  useEffect(() => {
    if (!token || !tenantId) {
      return;
    }

    if (!initializedRef.current) {
      void fetchRecent();
      initializedRef.current = true;
    }
let isDisposed = false;

const connection = new HubConnectionBuilder()
  .withUrl(resolveNotificationHubUrl(), {
    accessTokenFactory: () => token,
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .configureLogging(LogLevel.Warning)
  .build();

const onReceive = (notification: NotificationDto) => {
  setNotifications((prev) => [notification, ...prev].slice(0, 20));
};

connection.on("ReceiveNotification", onReceive);

connection.onreconnecting(() => !isDisposed && setStatus("reconnecting"));
connection.onreconnected(() => !isDisposed && setStatus("connected"));
connection.onclose(() => !isDisposed && setStatus("disconnected"));

async function startConnection(): Promise<void> {
  setStatus("connecting");
  try {
    await connection.start();
    if (!isDisposed && connection.state === HubConnectionState.Connected) {
      setStatus("connected");
    }
  } catch {
    if (!isDisposed) setStatus("disconnected");
  }
}

    void startConnection();

    return () => {
      isDisposed = true;
      connection.off("ReceiveNotification", onReceive);
      void connection.stop();
    };
  }, [token, tenantId, fetchRecent]);

  /**
   * Synchronizes a single read acknowledgement with the backend.
   * @param id - The notification identifier.
   */
  const markAsRead = async (id: string) => {
    try {
      await apiFetch(`/api/notifications/${id}/read`, { method: "PUT" });
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
    } catch (error) {
      console.error("Failed to mark notification as read:", error);
    }
  };

  /**
   * Synchronizes bulk read acknowledgement with the backend.
   */
  const markAllAsRead = async () => {
    try {
      await apiFetch("/api/notifications/read-all", { method: "PUT" });
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    } catch (error) {
      console.error("Failed to mark all notifications as read:", error);
    }
  };

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return { status, notifications, unreadCount, markAsRead, markAllAsRead, clear };
}

/**
 * Resolves the absolute WebSocket URL for the notification hub.
 */
function resolveNotificationHubUrl(): string {
  const baseUrl = (
    process.env.NEXT_PUBLIC_API_URL ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    "http://localhost:8080"
  ).replace(/\/+$/, "");

  return `${baseUrl}/hubs/notifications`;
}
