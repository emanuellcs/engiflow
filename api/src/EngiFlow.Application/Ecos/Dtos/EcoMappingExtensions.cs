using EngiFlow.Domain.Ecos;

namespace EngiFlow.Application.Ecos.Dtos;

/// <summary>
/// Converts ECO domain entities to application DTOs.
/// </summary>
internal static class EcoMappingExtensions
{
    /// <summary>
    /// Converts an ECO aggregate to a list summary.
    /// </summary>
    /// <param name="eco">The ECO aggregate to convert.</param>
    /// <returns>The summary DTO.</returns>
    public static EcoSummaryDto ToSummaryDto(this EngineeringChangeOrder eco)
    {
        return new EcoSummaryDto(
            eco.Id.Value,
            eco.CompanyId.Value,
            eco.Title,
            eco.Priority,
            eco.Status,
            eco.CreatedByUserId.Value,
            eco.CreatedAt,
            eco.UpdatedAt,
            eco.ReviewRound,
            eco.Approvals.Count(approval =>
                approval.ReviewRound == eco.ReviewRound &&
                approval.Decision == EcoApprovalDecision.Approve),
            eco.Approvals.Count(approval =>
                approval.ReviewRound == eco.ReviewRound &&
                approval.Decision == EcoApprovalDecision.RequestChanges));
    }

    /// <summary>
    /// Converts an ECO aggregate to a detailed DTO.
    /// </summary>
    /// <param name="eco">The ECO aggregate to convert.</param>
    /// <returns>The detail DTO.</returns>
    public static EcoDetailsDto ToDetailsDto(this EngineeringChangeOrder eco)
    {
        return new EcoDetailsDto(
            eco.Id.Value,
            eco.CompanyId.Value,
            eco.Title,
            eco.Description,
            eco.Priority,
            eco.Status,
            eco.CreatedByUserId.Value,
            eco.CreatedAt,
            eco.UpdatedAt,
            eco.Events
                .OrderBy(ecoEvent => ecoEvent.OccurredAt)
                .ThenBy(ecoEvent => ecoEvent.Id.Value)
                .Select(ecoEvent => ecoEvent.ToDto())
                .ToArray(),
            eco.ReviewRound,
            eco.RowVersion,
            eco.AffectedItems
                .OrderBy(item => item.PartNumber)
                .ThenBy(item => item.CreatedAt)
                .Select(item => item.ToDto())
                .ToArray(),
            eco.Approvals
                .OrderBy(approval => approval.ReviewRound)
                .ThenBy(approval => approval.CreatedAt)
                .Select(approval => approval.ToDto())
                .ToArray(),
            eco.Attachments
                .OrderBy(attachment => attachment.UploadedAt)
                .ThenBy(attachment => attachment.FileName)
                .Select(attachment => attachment.ToDto())
                .ToArray(),
            eco.Comments
                .OrderBy(comment => comment.CreatedAt)
                .ThenBy(comment => comment.Id.Value)
                .Select(comment => comment.ToDto())
                .ToArray());
    }

    private static EcoEventDto ToDto(this EcoEvent ecoEvent)
    {
        return new EcoEventDto(
            ecoEvent.Id.Value,
            ecoEvent.ActorUserId.Value,
            ecoEvent.EventType,
            ecoEvent.Description,
            ecoEvent.OldStatus,
            ecoEvent.NewStatus,
            ecoEvent.OccurredAt);
    }

    private static EcoAffectedItemDto ToDto(this EcoAffectedItem item)
    {
        return new EcoAffectedItemDto(
            item.Id.Value,
            item.PartNumber,
            item.Description,
            item.CurrentRevision,
            item.NewRevision,
            item.Action,
            item.CreatedByUserId.Value,
            item.CreatedAt);
    }

    private static EcoApprovalDto ToDto(this EcoApproval approval)
    {
        return new EcoApprovalDto(
            approval.Id.Value,
            approval.ApproverUserId.Value,
            approval.Decision,
            approval.ReviewRound,
            approval.CreatedAt,
            approval.UpdatedAt);
    }

    private static EcoAttachmentDto ToDto(this EcoAttachment attachment)
    {
        return new EcoAttachmentDto(
            attachment.Id.Value,
            attachment.FileName,
            attachment.FileSize,
            attachment.ObjectKey,
            attachment.MimeType,
            attachment.UploadedByUserId.Value,
            attachment.UploadedAt);
    }

    private static EcoCommentDto ToDto(this EcoComment comment)
    {
        return new EcoCommentDto(
            comment.Id.Value,
            comment.AuthorUserId.Value,
            comment.Body,
            comment.CreatedAt);
    }
}
