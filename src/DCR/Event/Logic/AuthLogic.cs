using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Exceptions;
using Event.Interfaces;

namespace Event.Logic
{
    /// <summary>
    /// AuthLogic is a logic-layer, that determines successfull authorization among roles and events.  
    /// </summary>
    public class AuthLogic : IAuthLogic
    {
        private readonly IEventStorage _storage;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="storage">An instance of an IEventStorage-implementation</param>
        public AuthLogic(IEventStorage storage)
        {
            _storage = storage;
        }

        public async Task<bool> IsAuthorized(string workflowId, string eventId, IEnumerable<string> roles)
        {
            if (eventId == null || workflowId == null || roles == null)
            {
                throw new ArgumentNullException("eventId", "eventId was null");
            }

            var eventRoles = await _storage.GetRoles(workflowId, eventId);

            return eventRoles.Intersect(roles).Any();
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}