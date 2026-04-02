using Internova.Core.Interfaces;
using Internova.Infrastructure.Data;
using Internova.Infrastructure.Repositories;
using Internova.Infrastructure.Services;
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
        // --- ADO.NET connection factory (reused by raw-SQL repositories) ---
        services.AddScoped<DbConnectionFactory>();
        services.AddHttpClient();

        // --- Repositories ---
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
        services.AddScoped<IBreakoutRoomRepository, BreakoutRoomRepository>();

        // --- Services ---
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
