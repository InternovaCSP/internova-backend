using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface IInternshipRepository
{
    Task<Internship?> GetByIdAsync(int id);
    Task<IEnumerable<Internship>> GetAllAsync();
    Task<Internship> AddAsync(Internship internship);
    Task<bool> UpdateAsync(Internship internship);
    Task<bool> DeleteAsync(int id);
}
