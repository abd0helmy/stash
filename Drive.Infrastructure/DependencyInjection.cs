using Amazon.S3;
using Drive.Application.Authentication.Interfaces;
using Drive.Application.Files.Interfaces;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Drive.Infrastructure.Repositories;
using Drive.Infrastructure.Storage;
using Drive.Infrastructure.Storage.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

        return services;
    }
}