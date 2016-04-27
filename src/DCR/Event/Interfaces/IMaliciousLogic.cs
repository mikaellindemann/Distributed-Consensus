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
        /// <summary>
        /// Checks if the event (workflowId,eventId) is malicious (has any cheatingTypes on it)
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        Task<bool> IsMalicious(string workflowId, string eventId);
        /// <summary>
        /// Applies the types of cheating of the event to the history list, implying that data is not modified in the database but only the history which is sent to the requester.
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IList<ActionDto> history);
        /// <summary>
        /// Adds the cheatingType in cheatingDto to the database of event (workflowid,eventid).
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <param name="cheatingDto"></param>
        /// <returns></returns>
        Task AddCheatingType(string workflowId, string eventId, CheatingDto cheatingDto);

    }
}
