using Drive.Application.Authentication.Interfaces;
using Drive.Application.Authentication.Options;
using Drive.Application.Authentication.Services;
using Drive.Application.Files.Interfaces;
using Drive.Application.Files.Services;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Folders.Services;
using Drive.Application.Sharing.Interfaces;
using Drive.Application.Sharing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Drive.Application.Billing.Common;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Plans.Services;
using Drive.Application.Billing.Services;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Billing.Subscriptions.Services;
using Drive.Application.Billing.Usage.Interfaces;
using Drive.Application.Billing.Usage.Services;

namespace Drive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IItemShareService, ItemShareService>();

        // Billing services
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IQuotaService, QuotaService>();

        return services;
    }
}

