using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngiFlow.Api.Controllers;

/// <summary>
/// Provides authentication endpoints for issuing EngiFlow bearer tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IApplicationMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="mediator">The EngiFlow application mediator used to dispatch auth use cases.</param>
    public AuthController(IApplicationMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT bearer token.
    /// </summary>
    /// <param name="request">The login credentials supplied by the client.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A bearer access token with its expiration timestamp.</returns>
    /// <response code="200">The credentials were valid and a token was issued.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">The credentials were invalid.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResultDto>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendQueryAsync<LoginQuery, LoginResultDto>(
                new LoginQuery(request.Email, request.Password),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
