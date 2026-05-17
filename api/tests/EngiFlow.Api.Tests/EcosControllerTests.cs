using EngiFlow.Api.Controllers;
using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Domain.Ecos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Tests;

public sealed class EcosControllerTests
{
    [Fact]
    public async Task CreateAsync_DispatchesCreateCommandAndReturnsCreatedAtRoute()
    {
        var eco = CreateEcoDetails();
        var mediator = new FakeApplicationMediator { Dispatch = _ => eco };
        var controller = new EcosController(mediator);

        var result = await controller.CreateAsync(
            new CreateEcoRequest(
                "Use aluminum bracket",
                "Update load-bearing bracket material from steel to aluminum.",
                EcoPriority.High),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal("GetEcoById", created.RouteName);
        Assert.Equal(eco.Id, created.RouteValues?["id"]);
        Assert.Same(eco, created.Value);

        var command = Assert.IsType<CreateEcoCommand>(mediator.LastRequest);
        Assert.Equal("Use aluminum bracket", command.Title);
        Assert.Equal("Update load-bearing bracket material from steel to aluminum.", command.Description);
        Assert.Equal(EcoPriority.High, command.Priority);
    }

    [Fact]
    public async Task GetByIdAsync_DispatchesQueryAndReturnsOk()
    {
        var ecoId = Guid.NewGuid();
        var eco = CreateEcoDetails(ecoId);
        var mediator = new FakeApplicationMediator { Dispatch = _ => eco };
        var controller = new EcosController(mediator);

        var result = await controller.GetByIdAsync(ecoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(eco, ok.Value);

        var query = Assert.IsType<GetEcoByIdQuery>(mediator.LastRequest);
        Assert.Equal(ecoId, query.EcoId);
    }

    [Fact]
    public async Task ListAsync_DispatchesQueryAndReturnsOk()
    {
        var page = new PagedResult<EcoSummaryDto>(
            [CreateEcoSummary()],
            PageNumber: 2,
            PageSize: 25,
            TotalCount: 50);
        var mediator = new FakeApplicationMediator { Dispatch = _ => page };
        var controller = new EcosController(mediator);

        var result = await controller.ListAsync(2, 25, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(page, ok.Value);

        var query = Assert.IsType<ListEcosQuery>(mediator.LastRequest);
        Assert.Equal(2, query.PageNumber);
        Assert.Equal(25, query.PageSize);
    }

    [Fact]
    public async Task SubmitAsync_DispatchesSubmitCommandAndReturnsOk()
    {
        var ecoId = Guid.NewGuid();
        var eco = CreateEcoDetails(ecoId, EcoStatus.UnderReview);
        var mediator = new FakeApplicationMediator { Dispatch = _ => eco };
        var controller = new EcosController(mediator);

        var result = await controller.SubmitAsync(ecoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(eco, ok.Value);

        var command = Assert.IsType<SubmitEcoCommand>(mediator.LastRequest);
        Assert.Equal(ecoId, command.EcoId);
    }

    [Fact]
    public async Task ApproveAsync_DispatchesApproveCommandAndReturnsOk()
    {
        var ecoId = Guid.NewGuid();
        var eco = CreateEcoDetails(ecoId, EcoStatus.Approved);
        var mediator = new FakeApplicationMediator { Dispatch = _ => eco };
        var controller = new EcosController(mediator);

        var result = await controller.ApproveAsync(ecoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(eco, ok.Value);

        var command = Assert.IsType<ApproveEcoCommand>(mediator.LastRequest);
        Assert.Equal(ecoId, command.EcoId);
    }

    [Fact]
    public async Task RejectAsync_UsesRouteIdAndBodyReason()
    {
        var ecoId = Guid.NewGuid();
        var eco = CreateEcoDetails(ecoId, EcoStatus.Rejected);
        var mediator = new FakeApplicationMediator { Dispatch = _ => eco };
        var controller = new EcosController(mediator);

        var result = await controller.RejectAsync(
            ecoId,
            new RejectEcoRequest("Specification is incomplete."),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(eco, ok.Value);

        var command = Assert.IsType<RejectEcoCommand>(mediator.LastRequest);
        Assert.Equal(ecoId, command.EcoId);
        Assert.Equal("Specification is incomplete.", command.Reason);
    }

    [Fact]
    public void Controller_IsSecuredAndUsesEcoRolePolicies()
    {
        var controllerAuthorize = typeof(EcosController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>();
        Assert.Contains(controllerAuthorize, attribute => attribute.Policy is null);

        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.CreateAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.UpdateDetailsAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.AddAffectedItemAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.RemoveAffectedItemAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.UploadAttachmentAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.SubmitAsync)));
        Assert.Equal("EcoAuthoring", GetAuthorizePolicy(nameof(EcosController.CancelAsync)));
        Assert.Equal("EcoApproval", GetAuthorizePolicy(nameof(EcosController.SubmitReviewDecisionAsync)));
        Assert.Equal("EcoApproval", GetAuthorizePolicy(nameof(EcosController.ApproveAsync)));
        Assert.Equal("EcoApproval", GetAuthorizePolicy(nameof(EcosController.RejectAsync)));
    }

    private static EcoDetailsDto CreateEcoDetails(Guid? id = null, EcoStatus status = EcoStatus.Draft)
    {
        var timestamp = DateTimeOffset.Parse("2026-05-15T00:00:00Z");
        return new EcoDetailsDto(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            status,
            Guid.NewGuid(),
            timestamp,
            timestamp,
            []);
    }

    private static EcoSummaryDto CreateEcoSummary()
    {
        var timestamp = DateTimeOffset.Parse("2026-05-15T00:00:00Z");
        return new EcoSummaryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Use aluminum bracket",
            EcoPriority.Medium,
            EcoStatus.Draft,
            Guid.NewGuid(),
            timestamp,
            timestamp);
    }

    private static string? GetAuthorizePolicy(string methodName)
    {
        return typeof(EcosController)
            .GetMethod(methodName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;
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
