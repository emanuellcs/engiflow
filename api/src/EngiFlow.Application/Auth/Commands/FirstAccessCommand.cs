using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Domain.Companies;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that resends first-access setup links for pending invited users.
/// </summary>
/// <param name="Email">The pending account email address.</param>
/// <param name="TenantId">The optional tenant identifier for multi-tenant accounts.</param>
public sealed record FirstAccessCommand(string Email, Guid? TenantId = null) : ICommand<FirstAccessResultDto>;

/// <summary>
/// Validates first-access resend requests.
/// </summary>
public sealed class FirstAccessCommandValidator : AbstractValidator<FirstAccessCommand>
{
    /// <summary>
    /// Initializes validation rules for first-access requests.
    /// </summary>
    public FirstAccessCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email is invalid.");
    }
}

/// <summary>
/// Handles regeneration and delivery of first-access invitation links.
/// </summary>
public sealed class FirstAccessCommandHandler : ICommandHandler<FirstAccessCommand, FirstAccessResultDto>
{
    private readonly ICompanyRepository _companies;
    private readonly IConfiguration _configuration;
    private readonly IPasswordResetEmailSender _emailSender;
    private readonly IPasswordSetupTokenRepository _passwordSetupTokens;
    private readonly IPasswordSetupTokenService _passwordSetupTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserEventRepository _userEvents;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstAccessCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository.</param>
    /// <param name="companies">The company repository.</param>
    /// <param name="passwordSetupTokens">The password setup token repository.</param>
    /// <param name="passwordSetupTokenService">The password setup token service.</param>
    /// <param name="emailSender">The setup email sender.</param>
    /// <param name="userEvents">The user lifecycle audit repository.</param>
    /// <param name="unitOfWork">The unit of work used to save regenerated tokens.</param>
    /// <param name="configuration">The application configuration.</param>
    public FirstAccessCommandHandler(
        IUserRepository users,
        ICompanyRepository companies,
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IPasswordResetEmailSender emailSender,
        IUserEventRepository userEvents,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _users = users;
        _companies = companies;
        _passwordSetupTokens = passwordSetupTokens;
        _passwordSetupTokenService = passwordSetupTokenService;
        _emailSender = emailSender;
        _userEvents = userEvents;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<FirstAccessResultDto> HandleAsync(
        FirstAccessCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var matchingUsers = await _users.ListByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
            .ConfigureAwait(false);

        if (command.TenantId.HasValue)
        {
            matchingUsers = matchingUsers
                .Where(user => user.CompanyId.Value == command.TenantId.Value)
                .ToArray();
        }

        var pendingUsers = matchingUsers
            .Where(user => user.Status == UserStatus.PendingActivation)
            .ToArray();

        if (pendingUsers.Length == 0 && matchingUsers.Any(user => user.Status == UserStatus.Active))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                [nameof(FirstAccessCommand.Email)] = ["This account is already active. Use forgot password if you cannot sign in."]
            });
        }

        if (pendingUsers.Length > 1)
        {
            var memberships = new List<(User User, Company Company)>();
            foreach (var user in pendingUsers)
            {
                var company = await _companies.GetByIdForAuthenticationAsync(user.CompanyId, cancellationToken)
                    .ConfigureAwait(false);
                if (company is not null && company.IsActive)
                {
                    memberships.Add((user, company));
                }
            }

            if (memberships.Count > 1)
            {
                var tenants = await BuildTenantSelectionAsync(memberships, cancellationToken).ConfigureAwait(false);
                return FirstAccessResultDto.Challenge(tenants);
            }

            pendingUsers = memberships.Select(m => m.User).ToArray();
        }

        foreach (var user in pendingUsers)
        {
            var company = await _companies.GetByIdForAuthenticationAsync(user.CompanyId, cancellationToken)
                .ConfigureAwait(false);

            if (company is null || !company.IsActive)
            {
                continue;
            }

            var rawToken = _passwordSetupTokenService.GenerateToken();
            await _passwordSetupTokens.AddAsync(
                    PasswordSetupToken.Create(
                        user.Id,
                        PasswordSetupTokenPurpose.Invitation,
                        _passwordSetupTokenService.HashToken(rawToken),
                        DateTimeOffset.UtcNow.AddHours(48)),
                    cancellationToken)
                .ConfigureAwait(false);
            await _userEvents.AddAsync(
                    UserEvent.Create(
                        user.CompanyId,
                        user.Id,
                        user.Id,
                        UserEventType.FirstAccessInvitationResent,
                        "First access invitation link regenerated."),
                    cancellationToken)
                .ConfigureAwait(false);
            await _emailSender.SendPasswordSetupAsync(
                    normalizedEmail,
                    BuildSetupPasswordLink(rawToken, normalizedEmail),
                    company.Name,
                    "EngiFlow",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return FirstAccessResultDto.Success();
    }

    /// <summary>
    /// Builds tenant selection metadata for the verified pending accounts.
    /// </summary>
    /// <param name="memberships">The matching pending user memberships.</param>
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

    /// <summary>
    /// Builds the public setup-password URL for a regenerated invitation token.
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
