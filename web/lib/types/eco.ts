/**
 * Centralized Engineering Change Order contracts shared by EngiFlow ECO pages,
 * workflow controls, and reusable UI components.
 */

/**
 * Lifecycle states returned by the EngiFlow ECO API.
 */
export type EcoStatus =
  | "Draft"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "Implemented";

/**
 * Priority values returned by the EngiFlow ECO API.
 */
export type EcoPriority = "Low" | "Medium" | "High" | "Critical";

/**
 * Priority values available in the current ECO creation form.
 */
export type CreateEcoPriority = Extract<EcoPriority, "Low" | "Medium" | "High">;

/**
 * Immutable audit event classifications returned on detailed ECO responses.
 */
export type EcoEventType =
  | "Created"
  | "DetailsUpdated"
  | "SubmittedForReview"
  | "Approved"
  | "Rejected"
  | "Implemented";

/**
 * Paginated API response shape used by tenant-scoped ECO list endpoints.
 */
export interface PagedResult<TItem> {
  /** Page items returned for the requested page. */
  items: TItem[];
  /** One-based page number returned by the API. */
  pageNumber: number;
  /** Number of records requested for each page. */
  pageSize: number;
  /** Total records matching the query. */
  totalCount: number;
  /** Total pages available for the query. */
  totalPages: number;
  /** Whether a previous page exists. */
  hasPreviousPage: boolean;
  /** Whether a next page exists. */
  hasNextPage: boolean;
}

/**
 * Summary DTO returned by the paginated ECO list endpoint.
 */
export interface EcoSummaryDto {
  /** ECO identifier. */
  id: string;
  /** Tenant identifier that owns the ECO. */
  companyId: string;
  /** Short business title of the engineering change. */
  title: string;
  /** Operational urgency assigned to the ECO. */
  priority: EcoPriority;
  /** Current lifecycle status. */
  status: EcoStatus;
  /** User identifier for the creator. */
  createdByUserId: string;
  /** UTC timestamp when the ECO was created. */
  createdAt: string;
  /** UTC timestamp when the ECO was last updated. */
  updatedAt: string;
}

/**
 * Audit timeline entry returned on detailed ECO responses.
 */
export interface EcoEventDto {
  /** Audit event identifier. */
  id: string;
  /** Tenant identifier that owns the event. */
  companyId: string;
  /** ECO identifier that produced the event. */
  engineeringChangeOrderId: string;
  /** User identifier for the actor who performed the event. */
  actorUserId: string;
  /** Event classification. */
  eventType: EcoEventType;
  /** Human-readable audit description. */
  description: string;
  /** Previous ECO status when the event is a transition. */
  oldStatus: EcoStatus | null;
  /** New ECO status when the event is a transition. */
  newStatus: EcoStatus | null;
  /** UTC timestamp when the audited action occurred. */
  occurredAt: string;
}

/**
 * Detailed ECO DTO returned by create, read, and workflow transition endpoints.
 */
export interface EcoDetailsDto extends EcoSummaryDto {
  /** Detailed engineering change description. */
  description: string;
  /** Chronological audit event history for the ECO. */
  events: EcoEventDto[];
}

/**
 * Request body used to create a draft ECO.
 */
export type CreateEcoRequest = {
  /** Short business title of the requested engineering change. */
  title: string;
  /** Detailed engineering change description. */
  description: string;
  /** Operational priority assigned to the request. */
  priority: CreateEcoPriority;
};

/**
 * Request body used to reject an ECO currently under review.
 */
export type RejectEcoRequest = {
  /** Business justification for rejecting the ECO. */
  reason: string;
};
