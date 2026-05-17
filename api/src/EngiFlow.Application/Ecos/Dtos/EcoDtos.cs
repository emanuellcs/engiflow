using EngiFlow.Domain.Ecos;

namespace EngiFlow.Application.Ecos.Dtos;

/// <summary>
/// Summarizes an engineering change order for list views.
/// </summary>
public sealed record EcoSummaryDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    EcoPriority Priority,
    EcoStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ReviewRound = 0,
    int CurrentRoundApprovalCount = 0,
    int CurrentRoundRequestChangesCount = 0);

/// <summary>
/// Describes one engineering change order with its timeline and review artifacts.
/// </summary>
public sealed record EcoDetailsDto(
    Guid Id,
    Guid CompanyId,
    string Title,
    string Description,
    EcoPriority Priority,
    EcoStatus Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EcoEventDto> Events,
    int ReviewRound = 0,
    uint RowVersion = 0,
    IReadOnlyList<EcoAffectedItemDto>? AffectedItems = null,
    IReadOnlyList<EcoApprovalDto>? Approvals = null,
    IReadOnlyList<EcoAttachmentDto>? Attachments = null,
    IReadOnlyList<EcoCommentDto>? Comments = null);

/// <summary>
/// Describes an immutable ECO audit event.
/// </summary>
public sealed record EcoEventDto(
    Guid Id,
    Guid ActorUserId,
    EcoEventType EventType,
    string Description,
    EcoStatus? OldStatus,
    EcoStatus? NewStatus,
    DateTimeOffset OccurredAt);

/// <summary>
/// Describes an ECO affected item diff row.
/// </summary>
public sealed record EcoAffectedItemDto(
    Guid Id,
    string PartNumber,
    string Description,
    string CurrentRevision,
    string NewRevision,
    EcoAffectedItemAction Action,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Describes an ECO review decision.
/// </summary>
public sealed record EcoApprovalDto(
    Guid Id,
    Guid ApproverUserId,
    EcoApprovalDecision Decision,
    int ReviewRound,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Describes attachment metadata stored for an ECO.
/// </summary>
public sealed record EcoAttachmentDto(
    Guid Id,
    string FileName,
    long FileSize,
    string ObjectKey,
    string MimeType,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAt);

/// <summary>
/// Describes a user-authored ECO timeline comment.
/// </summary>
public sealed record EcoCommentDto(
    Guid Id,
    Guid AuthorUserId,
    string Body,
    DateTimeOffset CreatedAt);

/// <summary>
/// Describes a tenant user needed by ECO review and identity display widgets.
/// </summary>
public sealed record EcoUserDto(
    Guid Id,
    string Name,
    string Email,
    string Role);

/// <summary>
/// Provides UI context needed to render PR-like ECO review state.
/// </summary>
public sealed record EcoReviewContextDto(
    int MinApprovalsRequired,
    IReadOnlyList<EcoUserDto> Users);

/// <summary>
/// Describes a short-lived attachment download link.
/// </summary>
public sealed record EcoAttachmentDownloadDto(
    string Url,
    DateTimeOffset ExpiresAtUtc);
