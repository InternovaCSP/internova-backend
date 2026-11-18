using Internova.Api.Domain.Entities;

namespace Internova.Api.Application.Interfaces;

public interface ICompetitionRepository
{
    Task<Competition?> GetByIdAsync(int id);
    Task<IEnumerable<Competition>> GetAllAsync();
    Task<Competition> AddAsync(Competition competition);
    Task<bool> UpdateAsync(Competition competition);
    Task<bool> DeleteAsync(int id);
}
