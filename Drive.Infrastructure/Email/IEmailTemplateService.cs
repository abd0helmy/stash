namespace Drive.Infrastructure.Email;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(string templateName, IDictionary<string, string> placeholders);
}
