using EngiFlow.Api.Controllers;
using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Users.Commands;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Application.Users.Queries;
using EngiFlow.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Tests;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task ListAsync_DispatchesListUsersQueryAndReturnsOk()
    {
        var users = new UserSummaryDto[]
        {
            new(Guid.NewGuid(), "Ada Lovelace", "ada@acme.example", nameof(UserRole.Administrator))
        };
        var mediator = new FakeApplicationMediator { Dispatch = _ => users };
        var controller = new UsersController(mediator);

        var result = await controller.ListAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(users, ok.Value);
        Assert.IsType<ListUsersQuery>(mediator.LastRequest);
    }

    [Fact]
    public async Task CreateAsync_DispatchesCreateUserCommandAndReturnsCreated()
    {
        var createdUser = new UserSummaryDto(
            Guid.NewGuid(),
            "Grace Hopper",
            "grace@acme.example",
            nameof(UserRole.Approver));
        var mediator = new FakeApplicationMediator { Dispatch = _ => createdUser };
        var controller = new UsersController(mediator);

        var result = await controller.CreateAsync(
            new CreateUserRequest(
                "Grace Hopper",
                "grace@acme.example",
                "StrongPass123!",
                UserRole.Approver),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("/api/users", created.Location);
        Assert.Same(createdUser, created.Value);

        var command = Assert.IsType<CreateUserCommand>(mediator.LastRequest);
        Assert.Equal("Grace Hopper", command.Name);
        Assert.Equal("grace@acme.example", command.Email);
        Assert.Equal("StrongPass123!", command.Password);
        Assert.Equal(UserRole.Approver, command.Role);
    }

    [Fact]
    public void Controller_IsSecuredForAdministrators()
    {
        var authorize = Assert.Single(typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(nameof(UserRole.Administrator), authorize.Roles);
    }

    private sealed class FakeApplicationMediator : IApplicationMediator
    {
        public Func<object, object>? Dispatch { get; init; }

        public object? LastRequest { get; private set; }

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse>
        {
            LastRequest = command;
            return Task.FromResult(Resolve<TResponse>(command));
        }

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(
            TQuery query,
            CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResponse>
        {
            LastRequest = query;
            return Task.FromResult(Resolve<TResponse>(query));
        }

        private TResponse Resolve<TResponse>(object request)
        {
            if (Dispatch is null)
            {
                throw new InvalidOperationException("No mediator dispatch delegate was configured.");
            }

            return (TResponse)Dispatch(request);
        }
    }
}
