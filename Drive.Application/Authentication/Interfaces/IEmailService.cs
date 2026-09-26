namespace Drive.Application.Authentication.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string toEmail,
        string verificationToken,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        CancellationToken cancellationToken = default);

    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
