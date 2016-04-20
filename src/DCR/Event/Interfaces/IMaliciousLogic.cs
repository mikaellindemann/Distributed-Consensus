using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.History;

namespace Event.Interfaces
{
    public interface IMaliciousLogic : IDisposable
    {
        Task<bool> IsMalicious(string workflowId, string eventId);
        Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IEnumerable<ActionDto> history);
    }
}
