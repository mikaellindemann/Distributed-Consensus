using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
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
            var eventModel = await GetEvent(workflowId, eventId);
            return eventModel.IsEvil;
        }

        public async Task<IEnumerable<CheatingType>> GetTypesOfCheating(string workflowId, string eventId)
        {
            var eventModel = await GetEvent(workflowId, eventId);
            return eventModel.TypesOfCheating;
        }

        public async Task<EventModel> GetEvent(string workflowId, string eventId)
        {
            if (string.IsNullOrWhiteSpace(workflowId) || string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException();
            }
            return await _context.Events.FindAsync(workflowId, eventId);
        }

        public async Task SaveEvent(EventModel eventModel)
        {
            if (string.IsNullOrWhiteSpace(eventModel.WorkflowId) || string.IsNullOrWhiteSpace(eventModel.Id))
            {
                throw new ArgumentException();
            }
            _context.Events.AddOrUpdate(eventModel);
            await _context.SaveChangesAsync();
        }
    }
}