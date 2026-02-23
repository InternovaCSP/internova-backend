using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Internova.Infrastructure.Repositories;
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
        // --- Database (ADO.NET) ---
        services.AddSingleton<DbConnectionFactory>();

        // --- Repositories ---
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
