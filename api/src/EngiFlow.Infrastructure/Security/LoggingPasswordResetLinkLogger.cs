using EngiFlow.Application.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace EngiFlow.Infrastructure.Security;

/// <summary>
/// Logs MVP password reset links through the configured application logger.
/// </summary>
internal sealed class LoggingPasswordResetLinkLogger : IPasswordResetLinkLogger
{
    private readonly ILogger<LoggingPasswordResetLinkLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingPasswordResetLinkLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger that writes reset links to the console in local development.</param>
    public LoggingPasswordResetLinkLogger(ILogger<LoggingPasswordResetLinkLogger> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task LogMockResetLinkAsync(
        string normalizedEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Mock password reset link for {Email}: {ResetLink}",
            normalizedEmail,
            resetLink);

        return Task.CompletedTask;
    }
}
