using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

public interface ISeminarRepository
{
    Task<IEnumerable<SeminarRequest>> GetAllAsync();
    Task<SeminarRequest?> GetByIdAsync(int id);
    Task<int> CreateAsync(SeminarRequest request);
    Task<bool> VoteAsync(int requestId, int studentId);
    Task<int> GetVoteCountAsync(int requestId);
    Task<bool> HasStudentVotedAsync(int requestId, int studentId);
    Task<bool> UpdateStatusAsync(int id, string status);
}
