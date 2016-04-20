using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.History;
using Common.DTO.Shared;

namespace Event.Interfaces
{
    public interface IMaliciousLogic : IDisposable
    {
        Task<bool> IsMalicious(string workflowId, string eventId);
        Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IList<ActionDto> history);
        Task ApplyCheatingType(string workflowId, string eventId, CheatingDto cheatingDto);

    }
}
