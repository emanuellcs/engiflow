using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
using FluentValidation;

namespace EngiFlow.Application.Auth.Commands;

/// <summary>
/// Command that accepts a forgot-password request and logs an MVP reset link.
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
/// Handles accepted forgot-password requests for the MVP reset flow.
/// </summary>
public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResultDto>
{
    private readonly IPasswordResetLinkLogger _resetLinkLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="resetLinkLogger">The logger used to write mock reset links.</param>
    public ForgotPasswordCommandHandler(IPasswordResetLinkLogger resetLinkLogger)
    {
        _resetLinkLogger = resetLinkLogger;
    }

    /// <inheritdoc />
    public async Task<ForgotPasswordResultDto> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var resetLink = $"https://engiflow.local/reset-password?email={Uri.EscapeDataString(normalizedEmail)}&token=mock-{Guid.NewGuid():N}";

        await _resetLinkLogger.LogMockResetLinkAsync(normalizedEmail, resetLink, cancellationToken)
            .ConfigureAwait(false);

        return new ForgotPasswordResultDto();
    }
}
