using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Internova.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Internova.Infrastructure.DependencyInjection;

/// <summary>
/// Centralises registration of all Infrastructure-layer services.
/// Called once from Program.cs via builder.Services.AddInfrastructure(config).
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Database (local MySQL via EF Core + Pomelo) ---
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Add it to appsettings.Development.json.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // --- Repositories ---
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
