using System.Net;
using System.Net.Mail;
using Drive.Application.Authentication.Interfaces;
using Drive.Infrastructure.Email.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drive.Infrastructure.Email;

public class EmailService(
    IOptions<EmailOptions> options,
    IEmailTemplateService templateService,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendVerificationEmailAsync(
        string toEmail,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_options.ClientAppUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(verificationToken)}";

        var placeholders = new Dictionary<string, string>
        {
            ["VerificationUrl"] = verificationUrl,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var subject = "Verify your email address - Drive";
        var htmlBody = await templateService.RenderTemplateAsync("EmailVerification", placeholders);

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_options.ClientAppUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        var placeholders = new Dictionary<string, string>
        {
            ["ResetUrl"] = resetUrl,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var subject = "Reset your password - Drive";
        var htmlBody = await templateService.RenderTemplateAsync("PasswordReset", placeholders);

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'", toEmail, subject);
            // In development or if SMTP is not configured, we do not crash the user action
        }
    }
}
