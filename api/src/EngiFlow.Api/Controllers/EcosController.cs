using EngiFlow.Api.Auth;
using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Domain.Ecos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides REST endpoints for creating, reviewing, approving, requesting changes on, and reading ECOs.
/// </summary>
/// <remarks>
/// Controllers stay intentionally thin: they translate HTTP route and body data into
/// application-layer commands or queries and delegate all business rules to the CQRS handlers.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/ecos")]
[Produces("application/json")]
public sealed class EcosController : ControllerBase
{
    private const string GetByIdRouteName = "GetEcoById";

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
    /// The ECO is created in <c>Draft</c> status for the authenticated user's tenant and actor.
    /// The current user must exist and be active inside the tenant supplied by the bearer token.
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
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
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

        return CreatedAtRoute(GetByIdRouteName, new { id = created.Id }, created);
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
    [HttpGet("{id:guid}", Name = GetByIdRouteName)]
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
    /// <param name="search">Optional text search across ECO title and description.</param>
    /// <param name="status">Optional lifecycle status filter.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="createdFrom">Optional inclusive created-at lower bound.</param>
    /// <param name="createdTo">Optional inclusive created-at upper bound.</param>
    /// <param name="createdByMe">Whether to include only ECOs created by the current actor.</param>
    /// <param name="awaitingMyReview">Whether to include under-review ECOs awaiting the current actor's review.</param>
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
        [FromQuery] string? search = null,
        [FromQuery] EcoStatus? status = null,
        [FromQuery] EcoPriority? priority = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        [FromQuery] bool createdByMe = false,
        [FromQuery] bool awaitingMyReview = false,
        CancellationToken cancellationToken = default)
    {
        var page = await _mediator.SendQueryAsync<ListEcosQuery, PagedResult<EcoSummaryDto>>(
                new ListEcosQuery(
                    pageNumber,
                    pageSize,
                    search,
                    status,
                    priority,
                    createdFrom,
                    createdTo,
                    createdByMe,
                    awaitingMyReview),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(page);
    }

    /// <summary>
    /// Retrieves tenant users and approval quorum settings needed by ECO review UI.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The review context for the current tenant.</returns>
    /// <response code="200">The review context was returned.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("review-context")]
    [ProducesResponseType(typeof(EcoReviewContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoReviewContextDto>> GetReviewContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _mediator
            .SendQueryAsync<GetEcoReviewContextQuery, EcoReviewContextDto>(
                new GetEcoReviewContextQuery(),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(context);
    }

    /// <summary>
    /// Updates the title, description, and priority of a draft engineering change order.
    /// </summary>
    /// <remarks>
    /// ECO details can be changed only while the ECO is in <c>Draft</c>. Once submitted for
    /// review, the ECO must be returned to draft before details can be edited again.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The replacement ECO details.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO details were updated.</response>
    /// <response code="400">The request failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not editable in its current status.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/details")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> UpdateDetailsAsync(
        Guid id,
        [FromBody] UpdateEcoDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<UpdateEcoDetailsCommand, EcoDetailsDto>(
                new UpdateEcoDetailsCommand(id, request.Title, request.Description, request.Priority),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Adds an affected engineering item to a draft engineering change order.
    /// </summary>
    /// <remarks>
    /// Affected items model the engineering diff associated with an ECO. They can be added
    /// only while the ECO is in <c>Draft</c>.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The affected item data.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The affected item was added.</response>
    /// <response code="400">The request failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not editable in its current status.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("{id:guid}/affected-items")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> AddAffectedItemAsync(
        Guid id,
        [FromBody] AddAffectedItemRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<AddAffectedItemCommand, EcoDetailsDto>(
                new AddAffectedItemCommand(
                    id,
                    request.PartNumber,
                    request.Description,
                    request.CurrentRevision,
                    request.NewRevision,
                    request.Action),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Removes an affected engineering item from a draft engineering change order.
    /// </summary>
    /// <remarks>
    /// Removing affected items is allowed only in <c>Draft</c>, preserving the reviewed diff
    /// after submission.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="itemId">The affected item identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The affected item was removed.</response>
    /// <response code="400">The route identifiers failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not editable in its current status.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpDelete("{id:guid}/affected-items/{itemId:guid}")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> RemoveAffectedItemAsync(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<RemoveAffectedItemCommand, EcoDetailsDto>(
                new RemoveAffectedItemCommand(id, itemId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Adds a user-authored comment to an engineering change order timeline.
    /// </summary>
    /// <remarks>
    /// Comments are accepted for every non-canceled ECO state and broadcast to tenant SignalR
    /// subscribers after the database transaction commits.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The comment body.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The comment was added.</response>
    /// <response code="400">The request failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is canceled and cannot receive comments.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> AddCommentAsync(
        Guid id,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<AddCommentCommand, EcoDetailsDto>(
                new AddCommentCommand(id, request.Body),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Uploads an attachment file and records its metadata on a draft engineering change order.
    /// </summary>
    /// <remarks>
    /// The upload uses multipart form data with a single <c>file</c> field. The application
    /// accepts only approved engineering attachment types up to 25 MB and removes the S3 object
    /// if the database transaction fails after upload.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="file">The uploaded attachment file.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The attachment was uploaded and recorded.</response>
    /// <response code="400">The multipart upload failed validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not editable in its current status or the attachment type is not allowed.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("{id:guid}/attachments")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> UploadAttachmentAsync(
        Guid id,
        [FromForm(Name = "file")] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            ModelState.AddModelError("file", "Attachment file is required.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        var updated = await _mediator.SendCommandAsync<UploadAttachmentCommand, EcoDetailsDto>(
                new UploadAttachmentCommand(
                    id,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Creates a short-lived pre-signed download URL for one ECO attachment.
    /// </summary>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A pre-signed download URL for the attachment.</returns>
    /// <response code="200">The download URL was generated.</response>
    /// <response code="400">The route identifiers failed validation.</response>
    /// <response code="404">No tenant-scoped ECO or attachment exists for the supplied identifiers.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/download")]
    [ProducesResponseType(typeof(EcoAttachmentDownloadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoAttachmentDownloadDto>> GetAttachmentDownloadUrlAsync(
        Guid id,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var download = await _mediator
            .SendQueryAsync<GetEcoAttachmentDownloadUrlQuery, EcoAttachmentDownloadDto>(
                new GetEcoAttachmentDownloadUrlQuery(id, attachmentId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(download);
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
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
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
    /// Submits an approver decision for the active ECO review round.
    /// </summary>
    /// <remarks>
    /// <c>Approve</c> records the current user's vote and promotes the ECO to <c>Approved</c>
    /// when the tenant quorum is met. <c>RequestChanges</c> records the vote and returns the
    /// ECO to <c>Draft</c> for revision.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The review decision and optional comment.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The review decision was accepted.</response>
    /// <response code="400">The request failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not under review.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("{id:guid}/review-decisions")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoApproval)]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> SubmitReviewDecisionAsync(
        Guid id,
        [FromBody] SubmitReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<SubmitReviewDecisionCommand, EcoDetailsDto>(
                new SubmitReviewDecisionCommand(id, request.Decision, request.Comment),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Cancels a draft or under-review engineering change order.
    /// </summary>
    /// <remarks>
    /// Canceled ECOs are terminal and no longer accept comments, attachments, affected items,
    /// detail edits, submissions, or review decisions.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO was canceled.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is already terminal.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoAuthoring)]
    [ProducesResponseType(typeof(EcoDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EcoDetailsDto>> CancelAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.SendCommandAsync<CancelEcoCommand, EcoDetailsDto>(
                new CancelEcoCommand(id),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(updated);
    }

    /// <summary>
    /// Approves an engineering change order currently under review through the compatibility route.
    /// </summary>
    /// <remarks>
    /// Approval is only valid after an ECO has been submitted for review. The application
    /// records the current user's active-round vote and approves the ECO only when quorum is met.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The approval vote was recorded.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not under review.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoApproval)]
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
    /// Requests changes on an engineering change order currently under review through the compatibility route.
    /// </summary>
    /// <remarks>
    /// This legacy route now records a <c>RequestChanges</c> decision and returns the ECO to
    /// <c>Draft</c>. The route identifier is authoritative; the request body supplies the reason.
    /// </remarks>
    /// <param name="id">The ECO identifier.</param>
    /// <param name="request">The request-changes reason supplied by the approver.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated ECO detail view.</returns>
    /// <response code="200">The ECO was returned to draft.</response>
    /// <response code="400">The route identifier or request-changes reason failed application validation.</response>
    /// <response code="404">No tenant-scoped ECO exists for the supplied identifier.</response>
    /// <response code="409">The ECO is not under review.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Policy = EngiFlowAuthorizationPolicies.EcoApproval)]
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
