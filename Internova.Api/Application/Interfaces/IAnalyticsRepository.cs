using Internova.Api.Application.DTOs;

namespace Internova.Api.Application.Interfaces;

public interface IAnalyticsRepository
{
    Task<AdminStatsDto> GetAdminStatsAsync();
}
