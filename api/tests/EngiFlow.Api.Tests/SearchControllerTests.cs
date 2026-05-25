using EngiFlow.Api.Controllers;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Search.Dtos;
using EngiFlow.Application.Search.Queries;
using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Tests;

public sealed class SearchControllerTests
{
    [Fact]
    public async Task Search_DispatchesQueryAndReturnsOk()
    {
        var searchResult = new GlobalSearchResultDto(
            [new EcoSearchResultDto(Guid.NewGuid(), "Aluminum Bracket", "Update material", EcoStatus.Draft)],
            [new UserSearchResultDto(Guid.NewGuid(), "Search User", "search@example.test", UserRole.Viewer)]
        );
        var mediator = new FakeApplicationMediator { Dispatch = _ => searchResult };
        var controller = new SearchController(mediator);

        var result = await controller.Search("Search", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(searchResult, ok.Value);

        var query = Assert.IsType<GlobalSearchQuery>(mediator.LastRequest);
        Assert.Equal("Search", query.QueryTerm);
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
