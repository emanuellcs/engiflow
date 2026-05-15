using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides REST endpoints for creating, reviewing, approving, rejecting, and reading ECOs.
/// </summary>
/// <remarks>
/// Controllers stay intentionally thin: they translate HTTP route and body data into
/// application-layer commands or queries and delegate all business rules to the CQRS handlers.
/// </remarks>
[ApiController]
[Route("api/ecos")]
[Produces("application/json")]
public sealed class EcosController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EcosController"/> class.
    /// </summary>
    /// <param name="mediator">The EngiFlow application mediator used to dispatch CQRS requests.</param>
    public EcosController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new draft engineering change order.
    /// </summary>
    /// <remarks>
    /// The ECO is created in <c>Draft</c> status for the configured current tenant and actor.
    /// The current user must exist and be active until authentication-backed tenant context
    /// is introduced in a later step.
    /// </remarks>
    /// <param name="request">The ECO draft data supplied by the client.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created ECO including its initial audit event.</returns>
    /// <response code="201">The ECO was created.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="404">The configured current user was not found.</response>
    /// <response code="409">A domain business rule rejected the request.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> CreateAsync(
        [FromBody] CreateEcoRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _mediator.SendCommandAsync<CreateEcoCommand, EcoDetailsDto>(
                new CreateEcoCommand(request.Title, request.Description, request.Priority),
                cancellationToken)
            .ConfigureAwait(false);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

    /// <summary>
    /// Retrieves one engineering change order by identifier.
    /// </summary>
    /// <remarks>
    /// The returned detail view includes the ECO's chronological audit timeline. Tenant
    /// isolation is enforced by the persistence layer.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The requested ECO detail view.</returns>
    /// <response code="200">The ECO was found.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var eco = await _mediator.SendQueryAsync<GetEcoByIdQuery, EcoDetailsDto>(
                new GetEcoByIdQuery(id),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(eco);
    }

    /// <summary>
    /// Retrieves a page of engineering change order summaries.
    /// </summary>
    /// <remarks>
    /// Results are tenant-scoped and sorted by the repository's default ECO listing order.
    /// Page numbers are one-based, and page size is constrained by application validation.
    /// </remarks>
    /// <param name="pageNumber">The one-based page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">The number of ECOs to include in the page. Defaults to 20.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A page of ECO summaries and pagination metadata.</returns>
    /// <response code="200">The page was retrieved.</response>
    /// <response code="400">The pagination values failed application validation.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EcoSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<EcoSummaryDto>>> ListAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var page = await _mediator.SendQueryAsync<ListEcosQuery, PagedResult<EcoSummaryDto>>(
                new ListEcosQuery(pageNumber, pageSize),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(page);
    }

    /// <summary>
    /// Submits a draft engineering change order for formal review.
    /// </summary>
    /// <remarks>
    /// Only draft ECOs can be submitted. Invalid lifecycle transitions are returned as
    /// RFC 7807 conflict responses by the global exception handler.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO was submitted for review.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO cannot transition from its current status to under review.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/submit")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> SubmitAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<SubmitEcoCommand, EcoDetailsDto>(
                new SubmitEcoCommand(id),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Approves an engineering change order currently under review.
    /// </summary>
    /// <remarks>
    /// Approval is only valid after an ECO has been submitted for review. The application
    /// returns the updated ECO and audit timeline after the transition is saved.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO was approved.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO cannot transition from its current status to approved.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/approve")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<ApproveEcoCommand, EcoDetailsDto>(
                new ApproveEcoCommand(id),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Rejects an engineering change order currently under review.
    /// </summary>
    /// <remarks>
    /// Rejection is terminal in the current workflow foundation. The route identifier is
    /// authoritative; the request body supplies only the rejection reason.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The rejection reason supplied by the reviewer or approver.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO was rejected.</response>
    /// <response code="400">The route identifier or rejection reason failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO cannot transition from its current status to rejected.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> RejectAsync(
        Guid id,
        [FromBody] RejectEcoRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<RejectEcoCommand, EcoDetailsDto>(
                new RejectEcoCommand(id, request.Reason),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }
}
