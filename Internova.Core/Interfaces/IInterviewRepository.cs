using Internova.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Internova.Core.Interfaces;

public interface IInterviewRepository
{
    Task<Interview> AddAsync(Interview interview);
    Task<IEnumerable<Interview>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<Interview>> GetByCompanyIdAsync(int companyId);
    Task<Interview?> GetByApplicationIdAsync(int applicationId);
}
