using Drive.Application.Authentication.Interfaces;
using Drive.Application.Folders.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Drive.Infrastructure.Repositories;
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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}