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
  | "Canceled"
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
  | "ReviewDecisionSubmitted"
  | "Approved"
  | "ChangesRequested"
  | "AffectedItemAdded"
  | "AffectedItemRemoved"
  | "CommentAdded"
  | "AttachmentAdded"
  | "Canceled"
  | "Rejected"
  | "Implemented";

/**
 * ECO affected-item action values returned by the API.
 */
export type EcoAffectedItemAction = "Add" | "Modify" | "Remove";

/**
 * ECO approval decision values returned by review decision endpoints.
 */
export type EcoApprovalDecision = "Approve" | "RequestChanges";

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
  /** Active review round number. Draft ECOs use zero. */
  reviewRound: number;
  /** Count of current-round approval decisions. */
  currentRoundApprovalCount: number;
  /** Count of current-round request-changes decisions. */
  currentRoundRequestChangesCount: number;
}

/**
 * Audit timeline entry returned on detailed ECO responses.
 */
export interface EcoEventDto {
  /** Audit event identifier. */
  id: string;
  /** Tenant identifier that owns the event. */
  companyId?: string;
  /** ECO identifier that produced the event. */
  engineeringChangeOrderId?: string;
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
  /** PostgreSQL xmin row version surfaced by the API. */
  rowVersion: number;
  /** Affected engineering item diff rows. */
  affectedItems: EcoAffectedItemDto[];
  /** Review decisions across all review rounds. */
  approvals: EcoApprovalDto[];
  /** Attachment metadata stored for this ECO. */
  attachments: EcoAttachmentDto[];
  /** User-authored timeline comments. */
  comments: EcoCommentDto[];
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

/**
 * Request body used to submit a review decision.
 */
export type SubmitReviewDecisionRequest = {
  /** Approver decision for the active review round. */
  decision: EcoApprovalDecision;
  /** Optional markdown comment stored alongside the decision. */
  comment?: string | null;
};

/**
 * Request body used to add a comment to the ECO conversation.
 */
export type AddCommentRequest = {
  /** Markdown comment body. */
  body: string;
};

/**
 * Request body used to add an affected item to a draft ECO.
 */
export type AddAffectedItemRequest = {
  /** Affected part or document number. */
  partNumber: string;
  /** Human-readable affected item description. */
  description: string;
  /** Current controlled revision. */
  currentRevision: string;
  /** Proposed revision. */
  newRevision: string;
  /** Engineering diff action. */
  action: EcoAffectedItemAction;
};

/**
 * Affected engineering item diff row.
 */
export interface EcoAffectedItemDto {
  /** Affected item identifier. */
  id: string;
  /** Part or document number. */
  partNumber: string;
  /** Affected item description. */
  description: string;
  /** Current controlled revision. */
  currentRevision: string;
  /** Proposed revision. */
  newRevision: string;
  /** Engineering diff action. */
  action: EcoAffectedItemAction;
  /** User identifier that added the item. */
  createdByUserId: string;
  /** UTC timestamp when the item was added. */
  createdAt: string;
}

/**
 * ECO review decision.
 */
export interface EcoApprovalDto {
  /** Approval identifier. */
  id: string;
  /** Approver user identifier. */
  approverUserId: string;
  /** Approval decision. */
  decision: EcoApprovalDecision;
  /** Review round for this decision. */
  reviewRound: number;
  /** UTC timestamp when the decision was first created. */
  createdAt: string;
  /** UTC timestamp when the decision was last updated. */
  updatedAt: string;
}

/**
 * ECO attachment metadata.
 */
export interface EcoAttachmentDto {
  /** Attachment identifier. */
  id: string;
  /** Original file name. */
  fileName: string;
  /** File size in bytes. */
  fileSize: number;
  /** Object storage key. */
  objectKey: string;
  /** Validated MIME type. */
  mimeType: string;
  /** Uploader user identifier. */
  uploadedByUserId: string;
  /** UTC timestamp when uploaded. */
  uploadedAt: string;
}

/**
 * ECO conversation comment.
 */
export interface EcoCommentDto {
  /** Comment identifier. */
  id: string;
  /** Author user identifier. */
  authorUserId: string;
  /** Markdown body. */
  body: string;
  /** UTC timestamp when authored. */
  createdAt: string;
}

/**
 * Tenant user display information needed by ECO screens.
 */
export interface EcoUserDto {
  /** User identifier. */
  id: string;
  /** Display name. */
  name: string;
  /** Email address. */
  email: string;
  /** Role name. */
  role: string;
}

/**
 * Review context for PR-like ECO screens.
 */
export interface EcoReviewContextDto {
  /** Tenant quorum setting. */
  minApprovalsRequired: number;
  /** Active tenant users visible to ECO screens. */
  users: EcoUserDto[];
}

/**
 * Short-lived attachment download response.
 */
export interface EcoAttachmentDownloadDto {
  /** Pre-signed download URL. */
  url: string;
  /** UTC expiration timestamp. */
  expiresAtUtc: string;
}

/**
 * SignalR ECO update payload emitted by the API.
 */
export interface EcoRealtimeUpdate {
  /** Tenant identifier. */
  companyId: string;
  /** ECO identifier. */
  ecoId: string;
  /** Current ECO status. */
  status: EcoStatus;
  /** Current review round. */
  reviewRound: number;
  /** Current audit event timeline. */
  events: EcoEventDto[];
}
