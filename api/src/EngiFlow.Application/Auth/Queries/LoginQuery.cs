using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Users;
using FluentValidation;

namespace EngiFlow.Application.Auth.Queries;

/// <summary>
/// Query that authenticates a user and either returns a JWT bearer token or a tenant selection challenge.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plain-text password for verification.</param>
public sealed record LoginQuery(string Email, string Password) : ICommand<LoginResponseDto>;

/// <summary>
/// Validates <see cref="LoginQuery"/> requests before credential verification.
/// </summary>
public sealed class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    /// <summary>
    /// Initializes validation rules for login requests.
    /// </summary>
    public LoginQueryValidator()
    {
        RuleFor(query => query.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email is invalid.");

        RuleFor(query => query.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MaximumLength(256)
            .WithMessage("Password cannot exceed 256 characters.");
    }
}

/// <summary>
/// Handles credential validation and token issuance for login requests.
/// </summary>
public sealed class LoginQueryHandler : ICommandHandler<LoginQuery, LoginResponseDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPreAuthTokenService _preAuthTokenService;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginQueryHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository used to resolve tenant display metadata.</param>
    /// <param name="users">The user repository used for authentication lookup.</param>
    /// <param name="passwordHashService">The password hash verification service.</param>
    /// <param name="jwtTokenService">The JWT issuing service.</param>
    /// <param name="preAuthTokenService">The pre-auth token issuing service.</param>
    public LoginQueryHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        IPreAuthTokenService preAuthTokenService)
    {
        _companies = companies;
        _users = users;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _preAuthTokenService = preAuthTokenService;
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> HandleAsync(
        LoginQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(query.Email);
        var users = await _users.ListByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        var validMemberships = new List<(User User, Company Company)>();

        foreach (var candidate in users)
        {
            if (!candidate.IsActive
                || string.IsNullOrWhiteSpace(candidate.PasswordHash)
                || !_passwordHashService.VerifyPassword(candidate, query.Password))
            {
                continue;
            }

            var candidateCompany = await _companies.GetByIdForAuthenticationAsync(candidate.CompanyId, cancellationToken)
                .ConfigureAwait(false);

            if (candidateCompany is not null && candidateCompany.IsActive)
            {
                validMemberships.Add((candidate, candidateCompany));
            }
        }

        if (validMemberships.Count == 0)
        {
            throw new AuthenticationFailedException();
        }

        if (validMemberships.Count > 1)
        {
            var preAuthToken = _preAuthTokenService.CreatePreAuthToken(
                normalizedEmail,
                validMemberships.Select(membership => membership.User.CompanyId).ToArray(),
                TimeSpan.FromMinutes(5));
            var tenants = await BuildTenantSelectionAsync(validMemberships, cancellationToken).ConfigureAwait(false);
            return LoginResponseDto.TenantSelection(preAuthToken.Token, preAuthToken.ExpiresAtUtc, tenants);
        }

        var (user, company) = validMemberships[0];
        user.RecordSuccessfulLogin();
        await _users.RecordSuccessfulLoginAsync(user.Id, user.LastLoginAt!.Value, cancellationToken)
            .ConfigureAwait(false);

        var token = _jwtTokenService.CreateAccessToken(user, company.Name);
        return LoginResponseDto.Authenticated(AuthResultFactory.Create(token, user, company));
    }

    /// <summary>
    /// Normalizes an email address for authentication lookup.
    /// </summary>
    /// <param name="email">The candidate email address.</param>
    /// <returns>The normalized email address.</returns>
    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Builds tenant selection metadata for the verified memberships.
    /// </summary>
    /// <param name="memberships">The valid memberships.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The tenant selection options.</returns>
    private async Task<IReadOnlyList<TenantSelectionDto>> BuildTenantSelectionAsync(
        IReadOnlyCollection<(User User, Company Company)> memberships,
        CancellationToken cancellationToken)
    {
        var tenants = new List<TenantSelectionDto>();

        foreach (var (_, company) in memberships.OrderBy(membership => membership.Company.Name))
        {
            var owner = await _users.GetOwnerByCompanyIdForAuthenticationAsync(company.Id, cancellationToken)
                .ConfigureAwait(false);

            tenants.Add(new TenantSelectionDto(
                company.Id.Value,
                company.Name,
                company.ContactEmail ?? owner?.Email ?? string.Empty,
                owner?.DisplayName ?? "Workspace Owner",
                owner?.Email ?? company.ContactEmail ?? string.Empty));
        }

        return tenants;
    }
}
