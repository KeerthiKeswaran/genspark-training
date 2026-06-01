using System;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface IOperatorWorkflowService
    {
        Task<(bool Success, string Message)> DeactivateOperatorAsync(Guid operatorId);
        Task<(bool Success, string Message)> ReactivateOperatorAsync(Guid operatorId);
    }
}
