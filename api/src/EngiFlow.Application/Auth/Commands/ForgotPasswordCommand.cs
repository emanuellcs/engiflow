using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that accepts a forgot-password request and sends a password reset email.
/// </summary>
/// <param name="Email">The account email address requesting a password reset.</param>
public sealed record ForgotPasswordCommand(string Email) : ICommand<ForgotPasswordResultDto>;

/// <summary>
/// Validates <see cref="ForgotPasswordCommand"/> requests.
/// </summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>
    /// Initializes validation rules for forgot-password requests.
    /// </summary>
    public ForgotPasswordCommandValidator()
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
/// Handles accepted forgot-password requests for the SMTP reset flow.
/// </summary>
public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResultDto>
{
    private readonly IPasswordResetEmailSender _resetEmailSender;
    private readonly IPasswordSetupTokenRepository _passwordSetupTokens;
    private readonly IPasswordSetupTokenService _passwordSetupTokenService;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserEventRepository _userEvents;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="resetEmailSender">The email sender used to deliver reset links.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="passwordSetupTokens">The password setup token repository.</param>
    /// <param name="passwordSetupTokenService">The password setup token service.</param>
    /// <param name="userEvents">The user lifecycle audit repository.</param>
    /// <param name="unitOfWork">The unit of work used to save reset tokens.</param>
    public ForgotPasswordCommandHandler(
        IPasswordResetEmailSender resetEmailSender,
        IConfiguration configuration,
        IUserRepository users,
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IUserEventRepository userEvents,
        IUnitOfWork unitOfWork)
    {
        _resetEmailSender = resetEmailSender;
        _configuration = configuration;
        _users = users;
        _passwordSetupTokens = passwordSetupTokens;
        _passwordSetupTokenService = passwordSetupTokenService;
        _userEvents = userEvents;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ForgotPasswordResultDto> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var baseUrl = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        var activeUsers = (await _users.ListByEmailForAuthenticationAsync(normalizedEmail, cancellationToken)
                .ConfigureAwait(false))
            .Where(user => user.Status == UserStatus.Active)
            .ToArray();

        foreach (var user in activeUsers)
        {
            var rawToken = _passwordSetupTokenService.GenerateToken();
            var resetLink = $"{baseUrl}/auth/setup-password?email={Uri.EscapeDataString(normalizedEmail)}&token={Uri.EscapeDataString(rawToken)}";

            await _passwordSetupTokens.AddAsync(
                    PasswordSetupToken.Create(
                        user.Id,
                        PasswordSetupTokenPurpose.Reset,
                        _passwordSetupTokenService.HashToken(rawToken),
                        DateTimeOffset.UtcNow.AddHours(2)),
                    cancellationToken)
                .ConfigureAwait(false);
            await _userEvents.AddAsync(
                    UserEvent.Create(
                        user.CompanyId,
                        user.Id,
                        user.Id,
                        UserEventType.PasswordResetRequested,
                        "Password reset requested."),
                    cancellationToken)
                .ConfigureAwait(false);
            await _resetEmailSender.SendPasswordResetAsync(normalizedEmail, resetLink, cancellationToken)
                .ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ForgotPasswordResultDto();
    }
}
