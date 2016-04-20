using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Event.Interfaces;
using Event.Models;

namespace Event.Storage
{
    public class MaliciousStorage : IMaliciousStorage
    {
        private readonly IEventContext _context;

        /// <summary>
        /// Constructor used for dependency injection (used for testing purposes)
        /// </summary>
        /// <param name="context">Context to be used by EventStorage</param>
        public MaliciousStorage(IEventContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();

        }

        public async Task<bool> IsMalicious(string workflowId, string eventId)
        {
            return (await _context.Events.FindAsync(workflowId, eventId)).IsEvil;
        }

        public async Task<IEnumerable<CheatingType>> GetTypesOfCheating(string workflowId, string eventId)
        {
            var eventModel = (await _context.Events.FindAsync(workflowId, eventId));
            return eventModel.TypesOfCheating;
        }
    }
}