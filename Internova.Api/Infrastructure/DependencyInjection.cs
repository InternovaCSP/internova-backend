using Dapper;
using Internova.Api.Application.Interfaces;
using Internova.Api.Infrastructure.Data;
using Internova.Api.Infrastructure.Repositories;
using Internova.Api.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Internova.Api.Infrastructure;

/// <summary>
/// Centralises registration of all Infrastructure-layer services and repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<DbConnectionFactory>();
        services.AddHttpClient();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IInternshipRepository, InternshipRepository>();
        services.AddScoped<IInternshipApplicationRepository, InternshipApplicationRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
        services.AddScoped<IBreakoutRoomRepository, BreakoutRoomRepository>();
        services.AddScoped<ISeminarRepository, SeminarRepository>();
        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
