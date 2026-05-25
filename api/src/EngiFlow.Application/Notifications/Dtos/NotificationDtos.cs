namespace EngiFlow.Application.Notifications.Dtos;

/// <summary>
/// Describes a user notification for display in the real-time Topbar popover.
/// </summary>
/// <param name="Id">The unique notification identifier.</param>
/// <param name="Title">The brief alert summary.</param>
/// <param name="Message">The detailed alert body.</param>
/// <param name="Category">The semantic category (ActionRequired, Update, Activity).</param>
/// <param name="IsRead">True if the notification has been acknowledged.</param>
/// <param name="DeepLink">The optional navigation target.</param>
/// <param name="CreatedAt">The UTC timestamp when the alert was issued.</param>
public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Category,
    bool IsRead,
    string? DeepLink,
    DateTimeOffset CreatedAt);
