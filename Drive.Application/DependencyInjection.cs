using Drive.Application.Authentication.Interfaces;
using Drive.Application.Authentication.Options;
using Drive.Application.Authentication.Services;
using Drive.Application.Files.Interfaces;
using Drive.Application.Files.Services;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Folders.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
