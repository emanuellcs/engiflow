using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;
using FluentValidation;

namespace EngiFlow.Application.Auth.Queries;

/// <summary>
/// Query that authenticates a user and returns a JWT bearer token.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's plain-text password for verification.</param>
public sealed record LoginQuery(string Email, string Password) : ICommand<LoginResultDto>;

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
public sealed class LoginQueryHandler : ICommandHandler<LoginQuery, LoginResultDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginQueryHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository used to resolve tenant display metadata.</param>
    /// <param name="users">The user repository used for authentication lookup.</param>
    /// <param name="passwordHashService">The password hash verification service.</param>
    /// <param name="jwtTokenService">The JWT issuing service.</param>
    public LoginQueryHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService)
    {
        _companies = companies;
        _users = users;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<LoginResultDto> HandleAsync(
        LoginQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(query.Email);
        var user = await _users.GetByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (user is null
            || !user.IsActive
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !_passwordHashService.VerifyPassword(user, query.Password))
        {
            throw new AuthenticationFailedException();
        }

        var company = await _companies.GetByIdAsync(user.CompanyId, cancellationToken)
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

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
