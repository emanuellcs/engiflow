using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Application.Auth.Dtos;
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
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordCommandHandler"/> class.
    /// </summary>
    /// <param name="resetEmailSender">The email sender used to deliver reset links.</param>
    /// <param name="configuration">The application configuration.</param>
    public ForgotPasswordCommandHandler(
        IPasswordResetEmailSender resetEmailSender,
        IConfiguration configuration)
    {
        _resetEmailSender = resetEmailSender;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<ForgotPasswordResultDto> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var baseUrl = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(normalizedEmail)}&token=mock-{Guid.NewGuid():N}";

        await _resetEmailSender.SendPasswordResetAsync(normalizedEmail, resetLink, cancellationToken)
            .ConfigureAwait(false);

        return new ForgotPasswordResultDto();
    }
}
