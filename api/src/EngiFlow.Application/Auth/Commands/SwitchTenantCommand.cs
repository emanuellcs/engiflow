using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.ValueObjects;
using FluentValidation;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that exchanges an active session for a new JWT in a different tenant.
/// </summary>
/// <param name="TenantId">The target tenant identifier.</param>
public sealed record SwitchTenantCommand(Guid TenantId) : ICommand<LoginResultDto>;

/// <summary>
/// Validates tenant switching requests.
/// </summary>
public sealed class SwitchTenantCommandValidator : AbstractValidator<SwitchTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant switching.
    /// </summary>
    public SwitchTenantCommandValidator()
    {
        RuleFor(command => command.TenantId)
            .NotEmpty()
            .WithMessage("Tenant id is required.");
    }
}

/// <summary>
/// Handles session migration for authenticated multi-tenant users.
/// </summary>
public sealed class SwitchTenantCommandHandler : ICommandHandler<SwitchTenantCommand, LoginResultDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchTenantCommandHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="jwtTokenService">The JWT issuing service.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    public SwitchTenantCommandHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IJwtTokenService jwtTokenService,
        ITenantProvider tenantProvider)
    {
        _companies = companies;
        _users = users;
        _jwtTokenService = jwtTokenService;
        _tenantProvider = tenantProvider;
    }

    /// <inheritdoc />
    public async Task<LoginResultDto> HandleAsync(
        SwitchTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _users.GetByIdForAuthenticationAsync(_tenantProvider.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var tenantId = CompanyId.From(command.TenantId);
        var candidates = await _users.ListByEmailForAuthenticationAsync(currentUser.Email, cancellationToken)
            .ConfigureAwait(false);
        
        var targetUser = candidates.SingleOrDefault(candidate =>
            candidate.CompanyId == tenantId &&
            candidate.IsActive &&
            !string.IsNullOrWhiteSpace(candidate.PasswordHash));

        if (targetUser is null)
        {
            throw new AuthenticationFailedException();
        }

        var company = await _companies.GetByIdForAuthenticationAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null || !company.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        targetUser.RecordSuccessfulLogin();
        await _users.RecordSuccessfulLoginAsync(targetUser.Id, targetUser.LastLoginAt!.Value, cancellationToken)
            .ConfigureAwait(false);

        var token = _jwtTokenService.CreateAccessToken(targetUser, company.Name);
        return AuthResultFactory.Create(token, targetUser, company);
    }
}
