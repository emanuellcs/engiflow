using EngiFlow.Domain.Notifications;

namespace EngiFlow.Application.Notifications.Dtos;

/// <summary>
/// Provides translation from internal notification entities to application DTOs.
/// </summary>
public static class NotificationMappingExtensions
{
    /// <summary>
    /// Converts a notification domain entity to its serializable data transfer object.
    /// </summary>
    /// <param name="notification">The source notification entity.</param>
    /// <returns>A mapped notification DTO.</returns>
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto(
            notification.Id.Value,
            notification.Title,
            notification.Message,
            notification.Category.ToString(),
            notification.IsRead,
            notification.DeepLink,
            notification.CreatedAt);
    }

    /// <summary>
    /// Converts a collection of notification entities to a list of DTOs.
    /// </summary>
    /// <param name="notifications">The source notifications.</param>
    /// <returns>A list of mapped DTOs.</returns>
    public static IReadOnlyList<NotificationDto> ToDtoList(this IEnumerable<Notification> notifications)
    {
        return notifications.Select(ToDto).ToArray();
    }
}
