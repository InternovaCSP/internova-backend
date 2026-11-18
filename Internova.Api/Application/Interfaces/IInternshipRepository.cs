using Internova.Api.Domain.Entities;

namespace Internova.Api.Application.Interfaces;

public interface IInternshipRepository
{
    Task<Internship?> GetByIdAsync(int id);
    Task<IEnumerable<Internship>> GetAllAsync();
    Task<IEnumerable<Internship>> GetByCompanyIdAsync(int companyId);
    Task<Internship> AddAsync(Internship internship);
    Task<bool> UpdateAsync(Internship internship);
    Task<bool> DeleteAsync(int id);
}
