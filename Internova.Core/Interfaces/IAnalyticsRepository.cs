using Internova.Core.DTOs;

namespace Internova.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task<AdminStatsDto> GetAdminStatsAsync();
}
