using EngiFlow.Api.Models;
using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Auth.Commands;
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
    /// Authenticates a user and returns either a JWT bearer token or a tenant selection challenge.
    /// </summary>
    /// <param name="request">The login credentials supplied by the client.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A bearer access token or tenant selection options.</returns>
    /// <response code="200">The credentials were valid and a token or tenant challenge was returned.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">The credentials were invalid.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<LoginQuery, LoginResponseDto>(
                new LoginQuery(request.Email, request.Password),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Exchanges a pre-authentication token and selected tenant for the final application JWT.
    /// </summary>
    /// <param name="request">The tenant selection request.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A bearer access token with its expiration timestamp.</returns>
    /// <response code="200">The tenant selection was accepted and a token was issued.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">The pre-authentication token was invalid or expired.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("select-tenant")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResultDto>> SelectTenantAsync(
        [FromBody] SelectTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<SelectTenantCommand, LoginResultDto>(
                new SelectTenantCommand(request.PreAuthToken, request.TenantId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Registers a new company tenant, creates its first administrator, and returns a JWT bearer token.
    /// </summary>
    /// <param name="request">The company and administrator details supplied by the client.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A bearer access token with its expiration timestamp for the new administrator.</returns>
    /// <response code="200">The company and administrator were created and a token was issued.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="409">A domain business rule was violated.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("register-company")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResultDto>> RegisterCompanyAsync(
        [FromBody] RegisterCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<RegisterCompanyCommand, LoginResultDto>(
                new RegisterCompanyCommand(
                    request.CompanyName,
                    request.AdminName,
                    request.AdminEmail,
                    request.AdminPassword),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Accepts a forgot-password request and sends setup-password reset links to matching active accounts.
    /// </summary>
    /// <param name="request">The account email address supplied by the client.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A result indicating success or a tenant selection challenge.</returns>
    /// <response code="200">The reset request was accepted.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForgotPasswordResultDto>> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<ForgotPasswordCommand, ForgotPasswordResultDto>(
                new ForgotPasswordCommand(request.Email, request.TenantId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Resolves the public setup-password page context for a token and email pair.
    /// </summary>
    /// <param name="request">The setup-password token and email.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The setup-password token purpose.</returns>
    /// <response code="200">The token context was returned.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">The token was invalid or expired.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("setup-password/context")]
    [ProducesResponseType(typeof(SetupPasswordContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SetupPasswordContextDto>> SetupPasswordContextAsync(
        [FromBody] SetupPasswordContextRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<GetSetupPasswordContextCommand, SetupPasswordContextDto>(
                new GetSetupPasswordContextCommand(request.Token, request.Email),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    /// Completes public invitation setup or password reset.
    /// </summary>
    /// <param name="request">The token, email, and new password.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>An empty success response when the password is stored.</returns>
    /// <response code="200">The password was set.</response>
    /// <response code="400">The request body failed application validation.</response>
    /// <response code="401">The token was invalid or expired.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("setup-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetupPasswordAsync(
        [FromBody] SetupPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.SendCommandAsync<SetupPasswordCommand, SetupPasswordResultDto>(
                new SetupPasswordCommand(request.Token, request.Email, request.Password),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok();
    }

    /// <summary>
    /// Resends setup instructions for pending first-access users.
    /// </summary>
    /// <param name="request">The pending account email address.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>A result indicating success or a tenant selection challenge.</returns>
    /// <response code="200">The first-access request was accepted.</response>
    /// <response code="400">The account is already active or the request failed validation.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [AllowAnonymous]
    [HttpPost("first-access")]
    [ProducesResponseType(typeof(FirstAccessResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FirstAccessResultDto>> FirstAccessAsync(
        [FromBody] FirstAccessRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendCommandAsync<FirstAccessCommand, FirstAccessResultDto>(
                new FirstAccessCommand(request.Email, request.TenantId),
                cancellationToken)
            .ConfigureAwait(false);

        return Ok(result);
    }
}
