using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Ecos;

/// <summary>
/// User-authored timeline comment attached to an engineering change order.
/// </summary>
public sealed class EcoComment : ITenantScoped
{
    private EcoComment()
    {
    }

    private EcoComment(
        EcoCommentId id,
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId authorUserId,
        string body,
        DateTimeOffset createdAt)
    {
        Id = id;
        CompanyId = companyId;
        EngineeringChangeOrderId = engineeringChangeOrderId;
        AuthorUserId = authorUserId;
        Body = body;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the comment identifier.
    /// </summary>
    public EcoCommentId Id { get; private set; }

    /// <inheritdoc />
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the ECO identifier this comment belongs to.
    /// </summary>
    public EngineeringChangeOrderId EngineeringChangeOrderId { get; private set; }

    /// <summary>
    /// Gets the user who authored the comment.
    /// </summary>
    public UserId AuthorUserId { get; private set; }

    /// <summary>
    /// Gets the validated comment body.
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the comment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    internal static EcoComment Create(
        CompanyId companyId,
        EngineeringChangeOrderId engineeringChangeOrderId,
        UserId authorUserId,
        string body,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(engineeringChangeOrderId, nameof(engineeringChangeOrderId));
        DomainGuard.AgainstDefault(authorUserId, nameof(authorUserId));

        return new EcoComment(
            EcoCommentId.New(),
            companyId,
            engineeringChangeOrderId,
            authorUserId,
            DomainGuard.Required(body, nameof(body), 4_000),
            DomainGuard.UtcTimestamp(createdAt));
    }
}
