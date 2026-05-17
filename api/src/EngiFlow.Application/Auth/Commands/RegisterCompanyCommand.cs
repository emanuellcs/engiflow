using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Domain.Companies;
using EngiFlow.Domain.Users;
using FluentValidation;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that creates a new company tenant and first administrator account.
/// </summary>
/// <param name="CompanyName">The company display name for the new tenant.</param>
/// <param name="AdminName">The display name for the first company administrator.</param>
/// <param name="AdminEmail">The email address used by the first company administrator to sign in.</param>
/// <param name="AdminPassword">The plain-text password to hash for the first company administrator.</param>
public sealed record RegisterCompanyCommand(
    string CompanyName,
    string AdminName,
    string AdminEmail,
    string AdminPassword) : ICommand<LoginResultDto>;

/// <summary>
/// Validates <see cref="RegisterCompanyCommand"/> requests before tenant bootstrap.
/// </summary>
public sealed class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    /// <summary>
    /// Initializes validation rules for self-service company registration.
    /// </summary>
    public RegisterCompanyCommandValidator()
    {
        RuleFor(command => command.CompanyName)
            .NotEmpty()
            .WithMessage("Company name is required.")
            .MaximumLength(200)
            .WithMessage("Company name cannot exceed 200 characters.");

        RuleFor(command => command.AdminName)
            .NotEmpty()
            .WithMessage("Administrator name is required.")
            .MaximumLength(200)
            .WithMessage("Administrator name cannot exceed 200 characters.");

        RuleFor(command => command.AdminEmail)
            .NotEmpty()
            .WithMessage("Administrator email is required.")
            .MaximumLength(320)
            .WithMessage("Administrator email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Administrator email is invalid.");

        RuleFor(command => command.AdminPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Administrator password is required.")
            .MinimumLength(12)
            .WithMessage("Administrator password must be at least 12 characters.")
            .MaximumLength(256)
            .WithMessage("Administrator password cannot exceed 256 characters.")
            .Matches("[A-Z]")
            .WithMessage("Administrator password must include at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Administrator password must include at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Administrator password must include at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Administrator password must include at least one symbol.");
    }
}

/// <summary>
/// Handles tenant bootstrap, administrator credential creation, and immediate token issuance.
/// </summary>
public sealed class RegisterCompanyCommandHandler : ICommandHandler<RegisterCompanyCommand, LoginResultDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterCompanyCommandHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository used to persist the new tenant root.</param>
    /// <param name="users">The user repository used to enforce global email uniqueness.</param>
    /// <param name="passwordHashService">The password hashing service.</param>
    /// <param name="jwtTokenService">The JWT issuing service.</param>
    /// <param name="unitOfWork">The unit of work used to save the company and administrator atomically.</param>
    public RegisterCompanyCommandHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IPasswordHashService passwordHashService,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _companies = companies;
        _users = users;
        _passwordHashService = passwordHashService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<LoginResultDto> HandleAsync(
        RegisterCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(command.AdminEmail);
        var existingUser = await _users.GetByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (existingUser is not null)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(RegisterCompanyCommand.AdminEmail)] =
                    ["Administrator email is already registered."]
            });
        }

        var company = Company.Create(command.CompanyName);
        var admin = company.RegisterUser(
            normalizedEmail,
            command.AdminName,
            UserRole.Administrator);
        admin.SetPasswordHash(_passwordHashService.HashPassword(admin, command.AdminPassword));

        await _companies.AddAsync(company, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var token = _jwtTokenService.CreateAccessToken(admin);
        return new LoginResultDto(token.AccessToken, "Bearer", token.ExpiresAtUtc);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
