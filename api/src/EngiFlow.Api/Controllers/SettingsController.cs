using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Settings.Commands;
using EngiFlow.Application.Settings.Dtos;
using EngiFlow.Application.Settings.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides tenant governance settings endpoints.
/// </summary>
[ApiController]
[Authorize(Roles = "Owner,Administrator")]
[Route("api/settings")]
[Produces("application/json")]
public sealed class SettingsController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    public SettingsController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets current workflow governance settings for the tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanySettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _mediator
            .SendQueryAsync<GetCompanySettingsQuery, CompanySettingsDto>(
                new GetCompanySettingsQuery(),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(settings);
    }

    /// <summary>
    /// Updates current workflow governance settings for the tenant.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CompanySettingsDto>> UpdateAsync(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await _mediator
            .SendCommandAsync<UpdateCompanySettingsCommand, CompanySettingsDto>(
                new UpdateCompanySettingsCommand(request.MinApprovalsRequired),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(settings);
    }
}
