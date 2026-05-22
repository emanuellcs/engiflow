using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Search.Dtos;
using EngiFlow.Application.Search.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides unified lookup and search execution for the application Command Palette.
/// </summary>
[ApiController]
[Authorize]
[Route("api/search")]
[Produces("application/json")]
public sealed class SearchController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchController"/> class.
    /// </summary>
    /// <param name="mediator">The EngiFlow application mediator used to dispatch CQRS requests.</param>
    public SearchController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Executes a global search across ECOs and Team Members for the current tenant.
    /// </summary>
    /// <param name="q">The search query term.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The aggregated search results.</returns>
    /// <response code="200">The search results were successfully retrieved.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(GlobalSearchResultDto), 200)]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        var result = await _mediator.SendQueryAsync<GlobalSearchQuery, GlobalSearchResultDto>(new GlobalSearchQuery(q), cancellationToken);
        return Ok(result);
    }
}
