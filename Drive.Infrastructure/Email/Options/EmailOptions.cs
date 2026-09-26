using System.ComponentModel.DataAnnotations;

namespace Drive.Infrastructure.Email.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 587;

    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = "noreply@drive.com";

    public string SenderName { get; set; } = "Drive";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public string ClientAppUrl { get; set; } = "http://localhost:3000";
}
