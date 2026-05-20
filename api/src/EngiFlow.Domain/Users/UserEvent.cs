using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Users;

/// <summary>
/// Immutable audit entry for user lifecycle and account-security actions.
/// </summary>
public sealed class UserEvent
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="UserEvent"/> class for EF Core materialization.
    /// </summary>
    private UserEvent()
    {
    }

    private UserEvent(
        UserEventId id,
        CompanyId companyId,
        UserId userId,
        UserId actorId,
        UserEventType eventType,
        DateTimeOffset occurredAt,
        string? reason)
    {
        Id = id;
        CompanyId = companyId;
        UserId = userId;
        ActorId = actorId;
        EventType = eventType;
        OccurredAt = occurredAt;
        Reason = reason;
    }

    /// <summary>
    /// Gets the unique user event identifier.
    /// </summary>
    public UserEventId Id { get; private set; }

    /// <summary>
    /// Gets the tenant identifier that owns the event.
    /// </summary>
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the user account affected by the event.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the actor that caused the event.
    /// </summary>
    public UserId ActorId { get; private set; }

    /// <summary>
    /// Gets the audited event type.
    /// </summary>
    public UserEventType EventType { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Gets the compliance reason associated with the event, when required.
    /// </summary>
    public string? Reason { get; private set; }

    /// <summary>
    /// Creates a validated user lifecycle audit event.
    /// </summary>
    /// <param name="companyId">The tenant identifier that owns the event.</param>
    /// <param name="userId">The affected user identifier.</param>
    /// <param name="actorId">The actor user identifier.</param>
    /// <param name="eventType">The audited event type.</param>
    /// <param name="reason">The optional compliance reason.</param>
    /// <param name="occurredAt">Optional timestamp used for deterministic tests or imports.</param>
    /// <returns>A validated immutable user event.</returns>
    public static UserEvent Create(
        CompanyId companyId,
        UserId userId,
        UserId actorId,
        UserEventType eventType,
        string? reason = null,
        DateTimeOffset? occurredAt = null)
    {
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstDefault(userId, nameof(userId));
        DomainGuard.AgainstDefault(actorId, nameof(actorId));
        DomainGuard.AgainstInvalidEnum(eventType, nameof(eventType));

        return new UserEvent(
            UserEventId.New(),
            companyId,
            userId,
            actorId,
            eventType,
            DomainGuard.UtcTimestamp(occurredAt),
            string.IsNullOrWhiteSpace(reason) ? null : DomainGuard.Required(reason, nameof(reason), 1000));
    }
}
