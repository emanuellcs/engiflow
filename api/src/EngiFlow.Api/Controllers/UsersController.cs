using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Users.Commands;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Application.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides administrator user-management endpoints for the current tenant.
/// </summary>
[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="mediator">The EngiFlow application mediator used to dispatch user use cases.</param>
    public UsersController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lists active users for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The active tenant users.</returns>
    /// <response code="200">The tenant users were returned.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="403">The authenticated user is not an administrator.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var users = await _mediator.SendQueryAsync<ListUsersQuery, IReadOnlyList<UserSummaryDto>>(
                new ListUsersQuery(),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(users);
    }

    /// <summary>
    /// Creates an active user for the current tenant.
    /// </summary>
    /// <param name="request">The user invitation details supplied by an administrator.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created user summary.</returns>
    /// <response code="201">The user was created.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="403">The authenticated user is not an administrator.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSummaryDto>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.SendCommandAsync<CreateUserCommand, UserSummaryDto>(
                new CreateUserCommand(
                    request.Name,
                    request.Email,
                    request.Password,
                    request.Role),
                cancellationToken)
            .ConfigureAwait(false);

        return Created("/api/users", user);
    }
}
