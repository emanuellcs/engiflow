import { apiFetch } from "./client";

/**
 * Represents a single numerical metric displayed on the dashboard.
 */
export interface DashboardMetric {
  /** The descriptive label for the metric (e.g., "Waiting for your review"). */
  label: string;
  /** The numerical count. */
  value: number;
  /** A small caption footer (e.g., "Just now"). */
  footer: string;
}

/**
 * Represents an actionable item requiring user attention on the dashboard.
 */
export interface DashboardAttentionItem {
  /** The unique identifier of the entity (e.g., ECO ID). */
  id: string;
  /** A descriptive title for the action item. */
  title: string;
  /** A muted subtitle providing context (e.g., priority or status). */
  subtitle: string;
  /** The category of the item for deep-linking (e.g., "ECO"). */
  type: string;
  /** The specific path for immediate navigation (e.g., "/ecos/ECO-2026-004"). */
  deepLink: string;
  /** The name of the user who performed the action, if applicable. */
  userName?: string;
  /** The abbreviated title of the related ECO, if applicable. */
  ecoTitle?: string;
  /** The UTC timestamp when the activity occurred, if applicable. */
  occurredAt?: string;
}

/**
 * Aggregates all data required to render the role-based dashboard view.
 */
export interface DashboardData {
  /** The list of numerical metrics tailored to the user's role. */
  metrics: DashboardMetric[];
  /** The list of actionable items requiring user attention. */
  attentionItems: DashboardAttentionItem[];
}

/**
 * Retrieves the role-tailored dashboard payload for the current user and tenant.
 *
 * @param signal An optional abort signal to cancel the request.
 * @returns A promise resolving to the dashboard data payload.
 */
export async function getDashboardData(signal?: AbortSignal): Promise<DashboardData> {
  return apiFetch<DashboardData>("/api/dashboard", { signal });
}
