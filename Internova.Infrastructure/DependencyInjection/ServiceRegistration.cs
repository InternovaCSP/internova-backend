using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Internova.Infrastructure.Repositories;
using Internova.Infrastructure.Services;
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

        // --- ADO.NET connection factory (reused by raw-SQL repositories) ---
        services.AddScoped<DbConnectionFactory>();

        // --- Repositories ---
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();

        // --- Azure Blob Storage service ---
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        return services;
    }
}
