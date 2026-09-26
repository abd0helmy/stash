using System.Collections.Concurrent;
using System.Reflection;

namespace Drive.Infrastructure.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private static readonly ConcurrentDictionary<string, string> TemplateCache = new();
    private static readonly Assembly Assembly = typeof(EmailTemplateService).Assembly;

    public async Task<string> RenderTemplateAsync(
        string templateName,
        IDictionary<string, string> placeholders)
    {
        var rawTemplate = await GetTemplateContentAsync(templateName);

        var rendered = rawTemplate;
        foreach (var (key, value) in placeholders)
        {
            rendered = rendered.Replace($"{{{{{key}}}}}", value);
        }

        return rendered;
    }

    private static async Task<string> GetTemplateContentAsync(string templateName)
    {
        if (TemplateCache.TryGetValue(templateName, out var cached))
        {
            return cached;
        }

        // 1. Try to read from embedded resource
        var resourceName = Assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith($"{templateName}.html", StringComparison.OrdinalIgnoreCase));

        if (resourceName is not null)
        {
            await using var stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                TemplateCache[templateName] = content;
                return content;
            }
        }

        // 2. Fallback: try loading from disk
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Email", "Templates", $"{templateName}.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "Email", "Templates", $"{templateName}.html"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Drive.Infrastructure", "Email", "Templates", $"{templateName}.html")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path);
                TemplateCache[templateName] = content;
                return content;
            }
        }

        throw new FileNotFoundException($"Email template '{templateName}.html' was not found as an embedded resource or on disk.");
    }
}
