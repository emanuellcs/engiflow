using System.Net;
using EngiFlow.Application.Abstractions.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EngiFlow.Infrastructure.Security;

/// <summary>
/// MailKit SMTP password-reset email sender.
/// </summary>
internal sealed class SmtpPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly SmtpEmailOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpPasswordResetEmailSender"/> class.
    /// </summary>
    /// <param name="options">The configured SMTP options.</param>
    public SmtpPasswordResetEmailSender(IOptions<SmtpEmailOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    /// <inheritdoc />
    public async Task SendPasswordResetAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetLink);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset your EngiFlow password";
        message.Body = new BodyBuilder
        {
            TextBody = $"""
                We received a request to reset your EngiFlow password.

                Open this link to continue:
                {resetLink}

                If you did not request this reset, you can ignore this email.
                """,
            HtmlBody = $"""
                <p>We received a request to reset your EngiFlow password.</p>
                <p><a href="{WebUtility.HtmlEncode(resetLink)}">Reset your password</a></p>
                <p>If you did not request this reset, you can ignore this email.</p>
                """
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();
        var secureSocketOptions = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                secureSocketOptions,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await smtpClient.AuthenticateAsync(
                    _options.Username,
                    _options.Password ?? string.Empty,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await smtpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }
}
