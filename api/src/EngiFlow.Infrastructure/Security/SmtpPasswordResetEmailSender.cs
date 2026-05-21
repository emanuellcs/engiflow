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

        var safeResetLink = WebUtility.HtmlEncode(resetLink);
        var message = CreateMessage(
            email,
            "Reset your EngiFlow password",
            $"""
            We received a request to reset your EngiFlow password.

            Open this link to continue:
            {resetLink}

            If you did not request this reset, you can ignore this email.
            """,
            $"""
            <div style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
              <h2 style="margin:0 0 12px">Reset your EngiFlow password</h2>
              <p>We received a request to reset your EngiFlow password.</p>
              <p>Click the button below to choose a new password. This link will expire in 2 hours.</p>
              <p>
                <a href="{safeResetLink}" style="display:inline-block;background:#1976d2;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none">
                  Reset password
                </a>
              </p>
              <p style="color:#6b7280;font-size:13px">If you did not request this reset, you can ignore this email.</p>
            </div>
            """);

        await SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendPasswordSetupAsync(
        string email,
        string setupLink,
        string companyName,
        string invitedByName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(setupLink);
        ArgumentException.ThrowIfNullOrWhiteSpace(companyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(invitedByName);

        var safeCompanyName = WebUtility.HtmlEncode(companyName);
        var safeInvitedByName = WebUtility.HtmlEncode(invitedByName);
        var safeSetupLink = WebUtility.HtmlEncode(setupLink);
        var message = CreateMessage(
            email,
            "Activate your EngiFlow workspace access",
            $"""
            {invitedByName} invited you to {companyName} in EngiFlow.

            Create your password within 48 hours:
            {setupLink}

            If you were not expecting this invitation, contact your workspace administrator.
            """,
            $"""
            <div style="font-family:Arial,sans-serif;color:#1f2937;line-height:1.5">
              <h2 style="margin:0 0 12px">Activate your EngiFlow workspace access</h2>
              <p>{safeInvitedByName} invited you to <strong>{safeCompanyName}</strong> in EngiFlow.</p>
              <p>Create your password within 48 hours to activate your workspace access.</p>
              <p>
                <a href="{safeSetupLink}" style="display:inline-block;background:#1976d2;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none">
                  Set up password
                </a>
              </p>
              <p style="color:#6b7280;font-size:13px">If you were not expecting this invitation, contact your workspace administrator.</p>
            </div>
            """);

        await SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a MIME message with consistent EngiFlow sender metadata.
    /// </summary>
    /// <param name="email">The recipient email address.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="textBody">The plain-text body.</param>
    /// <param name="htmlBody">The HTML body.</param>
    /// <returns>The prepared MIME message.</returns>
    private MimeMessage CreateMessage(string email, string subject, string textBody, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody
        }.ToMessageBody();

        return message;
    }

    /// <summary>
    /// Sends a prepared MIME message through the configured SMTP server.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token that can cancel the send operation.</param>
    private async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
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
