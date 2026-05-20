using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Application.Exceptions;
using EngiFlow.Application.Users;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Users.Commands;

/// <summary>
/// Command that invites a new pending activation user in the current tenant.
/// </summary>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Role">The user's role.</param>
public sealed record CreateUserCommand(
    string Name,
    string Email,
    UserRole Role) : ICommand<UserSummaryDto>;

/// <summary>
/// Validates <see cref="CreateUserCommand"/> requests before user creation.
/// </summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes validation rules for administrator-created users.
    /// </summary>
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters.");

        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email is invalid.");

        RuleFor(command => command.Role)
            .Must(role => role is UserRole.Administrator or UserRole.Approver or UserRole.Requester or UserRole.Viewer)
            .WithMessage("Role must be Administrator, Approver, Requester, or Viewer.");
    }
}

/// <summary>
/// Handles administrator user creation inside the current tenant boundary.
/// </summary>
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserSummaryDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IConfiguration _configuration;
    private readonly IPasswordResetEmailSender _emailSender;
    private readonly IPasswordSetupTokenRepository _passwordSetupTokens;
    private readonly IPasswordSetupTokenService _passwordSetupTokenService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserEventRepository _userEvents;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserCommandHandler"/> class.
    /// </summary>
    /// <param name="companies">The company repository used to validate the current tenant.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="passwordSetupTokens">The password setup token repository.</param>
    /// <param name="passwordSetupTokenService">The password setup token generator.</param>
    /// <param name="emailSender">The setup email sender.</param>
    /// <param name="userEvents">The user lifecycle audit repository.</param>
    /// <param name="tenantProvider">The current tenant provider.</param>
    /// <param name="unitOfWork">The unit of work used to save the new user.</param>
    /// <param name="configuration">The application configuration.</param>
    public CreateUserCommandHandler(
        ICompanyRepository companies,
        IUserRepository users,
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IPasswordResetEmailSender emailSender,
        IUserEventRepository userEvents,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _companies = companies;
        _users = users;
        _passwordSetupTokens = passwordSetupTokens;
        _passwordSetupTokenService = passwordSetupTokenService;
        _emailSender = emailSender;
        _userEvents = userEvents;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<UserSummaryDto> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var actor = await UserManagementRules.GetActiveCurrentUserAsync(
                _users,
                _tenantProvider,
                cancellationToken)
            .ConfigureAwait(false);
        UserManagementRules.EnsureCanManageUsers(actor);

        var existingUsers = await _users.ListByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (existingUsers.Any(user => user.CompanyId == _tenantProvider.CurrentCompanyId))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserCommand.Email)] = ["Email is already registered."]
            });
        }

        var company = await _companies.GetByIdAsync(_tenantProvider.CurrentCompanyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
        {
            throw new EntityNotFoundException("Company", _tenantProvider.CurrentCompanyId.Value);
        }

        var user = company.RegisterPendingUser(normalizedEmail, command.Name, command.Role);
        var rawToken = _passwordSetupTokenService.GenerateToken();
        var token = PasswordSetupToken.Create(
            user.Id,
            PasswordSetupTokenPurpose.Invitation,
            _passwordSetupTokenService.HashToken(rawToken),
            DateTimeOffset.UtcNow.AddHours(48));
        var setupLink = BuildSetupPasswordLink(rawToken, normalizedEmail);

        await _passwordSetupTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _userEvents.AddAsync(
                UserEvent.Create(
                    user.CompanyId,
                    user.Id,
                    actor.Id,
                    UserEventType.UserInvited,
                    $"Invitation issued by {actor.DisplayName}."),
                cancellationToken)
            .ConfigureAwait(false);
        await _emailSender.SendPasswordSetupAsync(
                normalizedEmail,
                setupLink,
                company.Name,
                actor.DisplayName,
                cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return user.ToSummaryDto();
    }

    /// <summary>
    /// Builds the public setup-password URL for an invitation token.
    /// </summary>
    /// <param name="token">The raw token value.</param>
    /// <param name="email">The normalized recipient email.</param>
    /// <returns>The absolute setup URL.</returns>
    private string BuildSetupPasswordLink(string token, string email)
    {
        var baseUrl = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        return $"{baseUrl}/auth/setup-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
    }
}
