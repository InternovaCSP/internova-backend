using Internova.Core.Entities;
using Internova.Core.Enums;

namespace Internova.Core.Interfaces;

public interface ICompanyProfileRepository
{
    Task<CompanyProfile?> GetByCompanyIdAsync(int companyId);
    Task<IEnumerable<CompanyProfile>> GetPendingCompaniesAsync();
    Task<IEnumerable<CompanyProfile>> GetAllCompaniesAsync();
    Task<bool> UpdateStatusAsync(int companyId, CompanyStatus status);
    Task<CompanyProfile> AddAsync(CompanyProfile profile);
}
