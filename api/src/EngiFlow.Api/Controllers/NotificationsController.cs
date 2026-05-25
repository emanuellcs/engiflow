using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Mediation;
using EngiFlow.Application.Notifications.Commands;
using EngiFlow.Application.Notifications.Dtos;
using EngiFlow.Application.Notifications.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides endpoints for managing user notifications.
/// </summary>
[Authorize]
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsController"/> class.
    /// </summary>
    /// <param name="mediator">The application mediator.</param>
    public NotificationsController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves the most recent notifications for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A list of recent notifications.</returns>
    /// <response code="200">The recent notifications were returned.</response>
    /// <response code="401">A valid bearer token is required.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetRecentAsync(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendQueryAsync<GetMyNotificationsQuery, IReadOnlyList<NotificationDto>>(
                new GetMyNotificationsQuery(),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A success response when the notification is marked as read.</returns>
    /// <response code="204">The notification was successfully updated.</response>
    /// <response code="404">The notification was not found or belongs to another user.</response>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.SendCommandAsync<MarkNotificationAsReadCommand, Unit>(
                new MarkNotificationAsReadCommand(id),
                cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Marks all unread notifications for the user as read.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A success response when the batch update is complete.</returns>
    /// <response code="204">All notifications were successfully updated.</response>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsReadAsync(
        CancellationToken cancellationToken)
    {
        await _mediator.SendCommandAsync<MarkAllNotificationsAsReadCommand, Unit>(
                new MarkAllNotificationsAsReadCommand(),
                cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }
}
