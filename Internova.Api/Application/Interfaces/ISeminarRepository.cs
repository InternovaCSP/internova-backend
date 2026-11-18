using Internova.Api.Domain.Entities;

namespace Internova.Api.Application.Interfaces;

public interface ISeminarRepository
{
    Task<IEnumerable<SeminarRequest>> GetAllAsync();
    Task<SeminarRequest?> GetByIdAsync(int id);
    Task<int> CreateAsync(SeminarRequest request);
    Task<bool> VoteAsync(int requestId, int studentId);
    Task<int> GetVoteCountAsync(int requestId);
    Task<bool> HasStudentVotedAsync(int requestId, int studentId);
    Task<IReadOnlySet<int>> GetVotedRequestIdsAsync(int studentId);
    Task<bool> UpdateStatusAsync(int id, string status);
}
