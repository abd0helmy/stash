using Amazon.S3;
using Drive.Application.Authentication.Interfaces;
using Drive.Application.Files.Interfaces;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Application.Sharing.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Caching;
using Drive.Infrastructure.Caching.Options;
using Drive.Infrastructure.Email;
using Drive.Infrastructure.Email.Options;
using Drive.Infrastructure.Persistence;
using Drive.Infrastructure.Repositories;
using Drive.Infrastructure.Storage;
using Drive.Infrastructure.Storage.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Drive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();
        services.AddScoped<Drive.Application.Authentication.Interfaces.IPasswordHasher, Identity.PasswordHasher>();

        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration.GetSection(RedisOptions.SectionName).GetValue<string>("ConnectionString")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ITokenService, Identity.TokenService>();

        services
            .AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddDbContext<DriveDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IFolderRepository, FolderRepository>();
        services.AddScoped<IFileRepository, FileRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services
            .AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(S3Options.SectionName)
            .Get<S3Options>()!;

        services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(
                options.AccessKey,
                options.SecretKey,
                new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    ForcePathStyle = true
                }));

        services.AddScoped<IObjectStorage, S3ObjectStorage>();
        services.AddScoped<IItemShareRepository, ItemShareRepository>();

        // Billing Repositories
        services.AddScoped<Drive.Application.Billing.Plans.Interfaces.IPlanRepository, PlanRepository>();
        services.AddScoped<Drive.Application.Billing.Subscriptions.Interfaces.ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<Drive.Application.Billing.Usage.Interfaces.IUsageRepository, UsageRepository>();

        // Payment Provider
        services
            .AddOptions<Billing.Stripe.StripeOptions>()
            .Bind(configuration.GetSection(Billing.Stripe.StripeOptions.SectionName));
        services.AddScoped<Drive.Application.Billing.Common.IPaymentService, Billing.Stripe.StripePaymentService>();
        services.AddScoped<Drive.Application.Billing.Common.IStripeWebhookService, Billing.Stripe.StripeWebhookService>();

        return services;
    }
}