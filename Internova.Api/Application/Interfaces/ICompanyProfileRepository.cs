using Internova.Api.Domain.Entities;
using Internova.Api.Domain.Enums;

namespace Internova.Api.Application.Interfaces;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile?> GetByCompanyIdAsync(int companyId);
    Task<IEnumerable<CompanyProfile>> GetPendingCompaniesAsync();
    Task<IEnumerable<CompanyProfile>> GetAllCompaniesAsync();
    Task<bool> UpdateStatusAsync(int companyId, CompanyStatus status);
    Task<CompanyProfile> AddAsync(CompanyProfile profile);
}
