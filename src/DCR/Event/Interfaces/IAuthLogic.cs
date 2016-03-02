using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Event.Interfaces
{
    /// <summary>
    /// IAuthLogic is a logic-layer, that determines successfull authorization among roles and events.  
    /// </summary>
    public interface IAuthLogic : IDisposable
    {
        /// <summary>
        /// IsAuthorized will, if provided a set of roles when called, determine whether at least one of the 
        /// provided roles match the roles needed to execute the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="roles">The set of roles, that you may have, but you wish to check is authorized to execute the specified Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<bool> IsAuthorized(string workflowId, string eventId, IEnumerable<string> roles);
    }
}
