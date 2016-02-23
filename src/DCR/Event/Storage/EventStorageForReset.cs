using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Interfaces;

namespace Event.Storage
{
    /// <summary>
    /// EventStorageForReset is the layer that rests on top of the database. It is used for resetting Events, and
    /// hence features brute implementation, that disrespects locking on Events. 
    /// </summary>
    public class EventStorageForReset : IEventStorageForReset
    {
        private readonly IEventContext _context;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The database-context, with which this layer communicates with.</param>
        public EventStorageForReset(IEventContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            _context = context;
        }

        public async Task ClearLock(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Clear any LockDto-element (should only exist a single)
            var @event = await _context.Events.SingleAsync(e => e.WorkflowId == workflowId && e.Id == eventId);

            @event.LockOwner = null;
            await _context.SaveChangesAsync();
        }

        public async Task ResetToInitialState(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            var @event = await _context.Events.SingleAsync(e => e.WorkflowId == workflowId && e.Id == eventId);

            @event.Executed = @event.InitialExecuted;
            @event.Included = @event.InitialIncluded;
            @event.Pending = @event.InitialPending;

            // Save changes
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Exists(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            return await _context.Events.AnyAsync(e => e.WorkflowId == workflowId && e.Id == eventId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}