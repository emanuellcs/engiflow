using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Dashboard.Dtos;
using EngiFlow.Application.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides REST endpoints for aggregating role-based workspace metrics and attention feeds.
/// </summary>
/// <remarks>
/// This controller is the primary data source for the authenticated landing experience.
/// It uses dynamic role tailoring to ensure users see only relevant operational counters.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="mediator">The EngiFlow application mediator used to dispatch CQRS requests.</param>
    public DashboardController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves the aggregated dashboard payload for the current user.
    /// </summary>
    /// <remarks>
    /// The payload structure (metrics and attention items) is dynamically shaped based on
    /// the authenticated user's assigned role within their active company tenant.
    /// </remarks>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A role-tailored dashboard data transfer object.</returns>
    /// <response code="200">The dashboard data was successfully compiled.</response>
    /// <response code="401">The request is missing a valid bearer token.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardDataDto>> GetAsync(CancellationToken cancellationToken)
    {
        var data = await _mediator.SendQueryAsync<GetDashboardDataQuery, DashboardDataDto>(
                new GetDashboardDataQuery(),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(data);
    }
}
