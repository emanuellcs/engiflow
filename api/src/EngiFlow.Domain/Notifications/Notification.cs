using EngiFlow.Domain.Abstractions;
using EngiFlow.Domain.Guards;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Notifications;

/// <summary>
/// A tenant-scoped user notification delivered in real-time or as an activity alert.
/// </summary>
public sealed class Notification : ITenantScoped
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="Notification"/> class for EF Core materialization.
    /// </summary>
    private Notification()
    {
    }

    /// <summary>
    /// Initializes a new tenant-scoped notification.
    /// </summary>
    private Notification(
        NotificationId id,
        UserId userId,
        CompanyId companyId,
        string title,
        string message,
        NotificationCategory category,
        string? deepLink,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        CompanyId = companyId;
        Title = title;
        Message = message;
        Category = category;
        DeepLink = deepLink;
        CreatedAt = createdAt;
        IsRead = false;
    }

    /// <summary>
    /// Gets the unique notification identifier.
    /// </summary>
    public NotificationId Id { get; private set; }

    /// <summary>
    /// Gets the user who should receive the notification.
    /// </summary>
    public UserId UserId { get; private set; }

    /// <summary>
    /// Gets the company tenant that owns the notification.
    /// </summary>
    public CompanyId CompanyId { get; private set; }

    /// <summary>
    /// Gets the brief, high-level summary of the alert.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the detailed notification body content.
    /// </summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the category that determines the visual styling and priority of the notification.
    /// </summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the notification has been acknowledged by the user.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Gets the optional relative or absolute URL the user should be directed to upon interaction.
    /// </summary>
    public string? DeepLink { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the notification was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new notification for a specific user and tenant.
    /// </summary>
    /// <param name="userId">The recipient user identifier.</param>
    /// <param name="companyId">The tenant identifier.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification body.</param>
    /// <param name="category">The notification category.</param>
    /// <param name="deepLink">Optional navigation target.</param>
    /// <param name="createdAt">Optional explicit creation timestamp.</param>
    /// <returns>A new unread notification.</returns>
    public static Notification Create(
        UserId userId,
        CompanyId companyId,
        string title,
        string message,
        NotificationCategory category,
        string? deepLink = null,
        DateTimeOffset? createdAt = null)
    {
        DomainGuard.AgainstDefault(userId, nameof(userId));
        DomainGuard.AgainstDefault(companyId, nameof(companyId));
        DomainGuard.AgainstInvalidEnum(category, nameof(category));

        return new Notification(
            NotificationId.New(),
            userId,
            companyId,
            DomainGuard.Required(title, nameof(title), 200),
            DomainGuard.Required(message, nameof(message), 1000),
            category,
            deepLink,
            DomainGuard.UtcTimestamp(createdAt));
    }

    /// <summary>
    /// Marks the notification as read.
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }
}
