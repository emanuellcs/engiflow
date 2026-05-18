using EngiFlow.Api.Auth;
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
[Authorize(Policy = EngiFlowAuthorizationPolicies.UserManagement)]
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
    /// <response code="403">The authenticated user is not allowed to manage users.</response>
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
    /// <response code="403">The authenticated user is not allowed to manage users.</response>
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

    /// <summary>
    /// Changes a tenant user's role.
    /// </summary>
    /// <remarks>
    /// Role changes enforce inter-management protections: users cannot change their own role,
    /// Owner users are immutable, and no endpoint can promote a user to Owner.
    /// </remarks>
    /// <param name="id">The target user identifier.</param>
    /// <param name="request">The replacement role.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The updated user summary.</returns>
    /// <response code="200">The role was updated.</response>
    /// <response code="400">The request failed application validation.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="403">The authenticated user is not allowed to manage the target user.</response>
    /// <response code="404">The target user was not found.</response>
    /// <response code="409">A role immutability rule rejected the request.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSummaryDto>> UpdateRoleAsync(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _mediator.SendCommandAsync<UpdateUserRoleCommand, UserSummaryDto>(
                new UpdateUserRoleCommand(id, request.Role),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(user);
    }

    /// <summary>
    /// Deactivates a tenant user without deleting their database row.
    /// </summary>
    /// <remarks>
    /// The command performs a soft delete by setting the user inactive. Inactive users are
    /// hidden by the EF Core global query filter and cannot authenticate.
    /// </remarks>
    /// <param name="id">The target user identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>No content when the user is deactivated.</returns>
    /// <response code="204">The user was deactivated.</response>
    /// <response code="400">The route identifier failed application validation.</response>
    /// <response code="401">A valid bearer token is required.</response>
    /// <response code="403">The authenticated user is not allowed to manage the target user.</response>
    /// <response code="404">The target user was not found.</response>
    /// <response code="409">A user immutability rule rejected the request.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.SendCommandAsync<DeactivateUserCommand, bool>(
                new DeactivateUserCommand(id),
                cancellationToken)
            .ConfigureAwait(false);

        return NoContent();
    }
}
