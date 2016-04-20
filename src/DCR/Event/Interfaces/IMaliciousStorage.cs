using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Interfaces
{
    public interface IMaliciousStorage : IDisposable
    {
        Task<bool> IsMalicious(string workflowId, string eventId);
        Task<IEnumerable<CheatingType>> GetTypesOfCheating(string workflowId, string eventId);
    }
}
