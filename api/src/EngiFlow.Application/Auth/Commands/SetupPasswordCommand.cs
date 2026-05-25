using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Users;
using FluentValidation;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that returns setup-password page context for a token and email pair.
/// </summary>
/// <param name="Token">The raw token from the email link.</param>
/// <param name="Email">The account email from the email link.</param>
public sealed record GetSetupPasswordContextCommand(string Token, string Email) : ICommand<SetupPasswordContextDto>;

/// <summary>
/// Command that consumes a setup/reset token and stores the new password hash.
/// </summary>
/// <param name="Token">The raw token from the email link.</param>
/// <param name="Email">The account email from the email link.</param>
/// <param name="Password">The new plain-text password.</param>
public sealed record SetupPasswordCommand(string Token, string Email, string Password) : ICommand<SetupPasswordResultDto>;

/// <summary>
/// Validates setup-password context requests.
/// </summary>
public sealed class GetSetupPasswordContextCommandValidator : AbstractValidator<GetSetupPasswordContextCommand>
{
    /// <summary>
    /// Initializes validation rules for setup-password context requests.
    /// </summary>
    public GetSetupPasswordContextCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().WithMessage("Token is required.");
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
/// Validates setup-password completion requests.
/// </summary>
public sealed class SetupPasswordCommandValidator : AbstractValidator<SetupPasswordCommand>
{
    /// <summary>
    /// Initializes validation rules for setup-password completion.
    /// </summary>
    public SetupPasswordCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().WithMessage("Token is required.");
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.")
            .EmailAddress()
            .WithMessage("Email is invalid.");
        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters.")
            .MaximumLength(256)
            .WithMessage("Password cannot exceed 256 characters.")
            .Matches("[A-Z]")
            .WithMessage("Password must include at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must include at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must include at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must include at least one symbol.");
    }
}

/// <summary>
/// Handles setup-password context validation.
/// </summary>
public sealed class GetSetupPasswordContextCommandHandler :
    ICommandHandler<GetSetupPasswordContextCommand, SetupPasswordContextDto>
{
    private readonly IPasswordSetupTokenRepository _passwordSetupTokens;
    private readonly IPasswordSetupTokenService _passwordSetupTokenService;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSetupPasswordContextCommandHandler"/> class.
    /// </summary>
    /// <param name="passwordSetupTokens">The password setup token repository.</param>
    /// <param name="passwordSetupTokenService">The password setup token hashing service.</param>
    /// <param name="users">The user repository.</param>
    public GetSetupPasswordContextCommandHandler(
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IUserRepository users)
    {
        _passwordSetupTokens = passwordSetupTokens;
        _passwordSetupTokenService = passwordSetupTokenService;
        _users = users;
    }

    /// <inheritdoc />
    public async Task<SetupPasswordContextDto> HandleAsync(
        GetSetupPasswordContextCommand command,
        CancellationToken cancellationToken = default)
    {
        var (token, user) = await SetupPasswordTokenResolver.ResolveUsableTokenAsync(
                command.Token,
                command.Email,
                _passwordSetupTokens,
                _passwordSetupTokenService,
                _users,
                cancellationToken)
            .ConfigureAwait(false);

        return new SetupPasswordContextDto(token.Purpose, user.Email);
    }
}

/// <summary>
/// Handles password setup and reset completion.
/// </summary>
public sealed class SetupPasswordCommandHandler : ICommandHandler<SetupPasswordCommand, SetupPasswordResultDto>
{
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPasswordSetupTokenRepository _passwordSetupTokens;
    private readonly IPasswordSetupTokenService _passwordSetupTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserEventRepository _userEvents;
    private readonly IUserRepository _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupPasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="passwordSetupTokens">The password setup token repository.</param>
    /// <param name="passwordSetupTokenService">The password setup token hashing service.</param>
    /// <param name="passwordHashService">The password hashing service.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="userEvents">The user lifecycle audit repository.</param>
    /// <param name="unitOfWork">The unit of work used to save credential changes.</param>
    public SetupPasswordCommandHandler(
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IPasswordHashService passwordHashService,
        IUserRepository users,
        IUserEventRepository userEvents,
        IUnitOfWork unitOfWork)
    {
        _passwordSetupTokens = passwordSetupTokens;
        _passwordSetupTokenService = passwordSetupTokenService;
        _passwordHashService = passwordHashService;
        _users = users;
        _userEvents = userEvents;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<SetupPasswordResultDto> HandleAsync(
        SetupPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var (token, user) = await SetupPasswordTokenResolver.ResolveUsableTokenAsync(
                command.Token,
                command.Email,
                _passwordSetupTokens,
                _passwordSetupTokenService,
                _users,
                cancellationToken)
            .ConfigureAwait(false);

        if (token.Purpose == PasswordSetupTokenPurpose.Invitation && user.Status != UserStatus.PendingActivation)
        {
            throw new AuthenticationFailedException();
        }

        await _users.SetPasswordAndActivateAsync(
                user.Id,
                _passwordHashService.HashPassword(user, command.Password),
                cancellationToken)
            .ConfigureAwait(false);
        token.Consume();
        await _userEvents.AddAsync(
                UserEvent.Create(
                    user.CompanyId,
                    user.Id,
                    user.Id,
                    token.Purpose == PasswordSetupTokenPurpose.Invitation
                        ? UserEventType.UserActivated
                        : UserEventType.PasswordResetRequested,
                    token.Purpose == PasswordSetupTokenPurpose.Invitation
                        ? "User activated account through invitation setup."
                        : "User completed password reset."),
                cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SetupPasswordResultDto();
    }

}

/// <summary>
/// Resolves password setup tokens shared by context and completion handlers.
/// </summary>
internal static class SetupPasswordTokenResolver
{
    /// <summary>
    /// Resolves a usable token and matching user for a public setup-password request.
    /// </summary>
    /// <param name="rawToken">The raw token supplied by the client.</param>
    /// <param name="email">The account email supplied by the client.</param>
    /// <param name="passwordSetupTokens">The token repository.</param>
    /// <param name="passwordSetupTokenService">The token hashing service.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="cancellationToken">A token that can cancel the lookup.</param>
    /// <returns>The token and matching user.</returns>
    public static async Task<(PasswordSetupToken Token, User User)> ResolveUsableTokenAsync(
        string rawToken,
        string email,
        IPasswordSetupTokenRepository passwordSetupTokens,
        IPasswordSetupTokenService passwordSetupTokenService,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var tokenHash = passwordSetupTokenService.HashToken(rawToken);
        var token = await passwordSetupTokens.GetByHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        if (token is null || !token.IsUsable(DateTimeOffset.UtcNow))
        {
            throw new AuthenticationFailedException();
        }

        var user = await users.GetByIdForAuthenticationAsync(token.UserId, cancellationToken).ConfigureAwait(false);

        if (user is null || !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthenticationFailedException();
        }

        return (token, user);
    }
}
