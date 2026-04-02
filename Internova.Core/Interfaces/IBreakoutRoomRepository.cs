using System.Collections.Generic;
using System.Threading.Tasks;
using Internova.Core.Entities;

namespace Internova.Core.Interfaces;

/// <summary>
/// Interface for Breakout Room repository/data-access.
/// </summary>
public interface IBreakoutRoomRepository
{
    Task<BreakoutRoom> AddAsync(BreakoutRoom room);
    Task<IEnumerable<BreakoutRoom>> GetActiveAsync();
    Task<BreakoutRoom?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(BreakoutRoom room);
    Task<bool> DeleteAsync(int id);
}
