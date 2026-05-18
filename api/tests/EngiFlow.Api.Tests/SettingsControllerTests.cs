using EngiFlow.Api.Controllers;
using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Settings.Commands;
using EngiFlow.Application.Settings.Dtos;
using EngiFlow.Application.Settings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Tests;

public sealed class SettingsControllerTests
{
    [Fact]
    public async Task GetAsync_DispatchesSettingsQueryAndReturnsOk()
    {
        var dto = new CompanySettingsDto(2, DateTimeOffset.Parse("2026-05-18T00:00:00Z"));
        var mediator = new FakeApplicationMediator { Dispatch = _ => dto };
        var controller = new SettingsController(mediator);

        var result = await controller.GetAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        Assert.IsType<GetCompanySettingsQuery>(mediator.LastRequest);
    }

    [Fact]
    public async Task UpdateAsync_DispatchesSettingsCommandAndReturnsOk()
    {
        var dto = new CompanySettingsDto(3, DateTimeOffset.Parse("2026-05-18T00:00:00Z"));
        var mediator = new FakeApplicationMediator { Dispatch = _ => dto };
        var controller = new SettingsController(mediator);

        var result = await controller.UpdateAsync(
            new UpdateSettingsRequest(3),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        var command = Assert.IsType<UpdateCompanySettingsCommand>(mediator.LastRequest);
        Assert.Equal(3, command.MinApprovalsRequired);
    }

    [Fact]
    public void Controller_RequiresOwnerOrAdministratorRoles()
    {
        var authorize = Assert.Single(typeof(SettingsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal("Owner,Administrator", authorize.Roles);
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
