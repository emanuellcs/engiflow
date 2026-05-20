using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that exchanges a pre-authentication token and tenant selection for a final JWT.
/// </summary>
/// <param name="PreAuthToken">The short-lived pre-authentication token.</param>
/// <param name="TenantId">The selected tenant identifier.</param>
public sealed record SelectTenantCommand(string PreAuthToken, Guid TenantId) : ICommand<LoginResultDto>;

/// <summary>
/// Validates tenant selection requests.
/// </summary>
public sealed class SelectTenantCommandValidator : AbstractValidator<SelectTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant selection.
    /// </summary>
    public SelectTenantCommandValidator()
    {
        RuleFor(command => command.PreAuthToken)
            .NotEmpty()
            .WithMessage("Pre-auth token is required.");

        RuleFor(command => command.TenantId)
            .NotEmpty()
            .WithMessage("Tenant id is required.");
    }
}

/// <summary>
/// Handles final token issuance after a verified multi-tenant login selects a workspace.
/// </summary>
public sealed class SelectTenantCommandHandler : ICommandHandler<SelectTenantCommand, LoginResultDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPreAuthTokenService _preAuthTokenService;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="jwtTokenService">The JWT issuing service.</param>
    /// <param name="preAuthTokenService">The pre-authentication token service.</param>
    public SelectTenantCommandHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        IPreAuthTokenService preAuthTokenService)
    {
        _companies = companies;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _preAuthTokenService = preAuthTokenService;
    }

    /// <inheritdoc />
    public async Task<LoginResultDto> HandleAsync(
        SelectTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var payload = _preAuthTokenService.ValidatePreAuthToken(command.PreAuthToken);
        var tenantId = CompanyId.From(command.TenantId);

        if (!payload.TenantIds.Contains(tenantId))
        {
            throw new AuthenticationFailedException();
        }

        var candidates = await _users.ListByEmailForAuthenticationAsync(payload.NormalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        var user = candidates.SingleOrDefault(candidate =>
            candidate.CompanyId == tenantId &&
            candidate.IsActive &&
            !string.IsNullOrWhiteSpace(candidate.PasswordHash));

        if (user is null)
        {
            throw new AuthenticationFailedException();
        }

        var company = await _companies.GetByIdForAuthenticationAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null || !company.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        user.RecordSuccessfulLogin();
        await _users.RecordSuccessfulLoginAsync(user.Id, user.LastLoginAt!.Value, cancellationToken)
            .ConfigureAwait(false);

        var token = _jwtTokenService.CreateAccessToken(user, company.Name);
        return AuthResultFactory.Create(token, user, company);
    }
}
