using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;

namespace Internova.Api.Application.Interfaces;

public interface IInternshipApplicationRepository
{
    Task<InternshipApplication> AddAsync(InternshipApplication application);
    Task<IEnumerable<InternshipApplication>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<InternshipApplication>> GetByCompanyIdAsync(int companyId);
    Task<bool> UpdateStatusAsync(int applicationId, ApplicationStatus status);
    Task<IDictionary<string, int>> GetPipelineStatsAsync(int studentId);
    Task<IDictionary<string, string>> GetKpiStatsAsync(int studentId);
}
