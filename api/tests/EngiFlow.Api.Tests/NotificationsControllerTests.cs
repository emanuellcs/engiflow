using EngiFlow.Api.Controllers;
using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Mediation;
using EngiFlow.Application.Notifications.Commands;
using EngiFlow.Application.Notifications.Dtos;
using EngiFlow.Application.Notifications.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Tests;

public sealed class NotificationsControllerTests
{
    [Fact]
    public void Controller_RequiresAuthorization()
    {
        var attribute = typeof(NotificationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true);
        
        Assert.NotEmpty(attribute);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsOkWithNotifications()
    {
        // Arrange
        var notifications = new List<NotificationDto>
        {
            new(Guid.NewGuid(), "Title", "Message", "Update", false, null, DateTimeOffset.UtcNow)
        };
        var mediator = new FakeApplicationMediator { Dispatch = _ => notifications };
        var controller = new NotificationsController(mediator);

        // Act
        var result = await controller.GetRecentAsync(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(notifications, okResult.Value);
        Assert.IsType<GetMyNotificationsQuery>(mediator.LastRequest);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mediator = new FakeApplicationMediator { Dispatch = _ => Unit.Value };
        var controller = new NotificationsController(mediator);

        // Act
        var result = await controller.MarkAsReadAsync(id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var command = Assert.IsType<MarkNotificationAsReadCommand>(mediator.LastRequest);
        Assert.Equal(id, command.Id);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ReturnsNoContent()
    {
        // Arrange
        var mediator = new FakeApplicationMediator { Dispatch = _ => Unit.Value };
        var controller = new NotificationsController(mediator);

        // Act
        var result = await controller.MarkAllAsReadAsync(CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.IsType<MarkAllNotificationsAsReadCommand>(mediator.LastRequest);
    }

    private sealed class FakeApplicationMediator : IApplicationMediator
    {
        public object? LastRequest { get; private set; }
        public Func<object, object>? Dispatch { get; init; }

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(
            TQuery query,
            CancellationToken cancellationToken = default) where TQuery : IQuery<TResponse>
        {
            LastRequest = query;
            return Task.FromResult((TResponse)Dispatch!(query));
        }

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(
            TCommand command,
            CancellationToken cancellationToken = default) where TCommand : ICommand<TResponse>
        {
            LastRequest = command;
            return Task.FromResult((TResponse)Dispatch!(command));
        }

        public Task SendCommandAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default) where TCommand : ICommand<Unit>
        {
            LastRequest = command;
            Dispatch!(command);
            return Task.CompletedTask;
        }

        public Task PublishEventAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default) where TEvent : class, INotification
        {
            throw new NotImplementedException();
        }
    }
}
