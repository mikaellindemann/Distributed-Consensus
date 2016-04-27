using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Exceptions;
using Event.Interfaces;
using Event.Models;
using ActionModel = Event.Models.ActionModel;

namespace Event.Storage
{
    public static class HistoryExtension
    {
        public static async Task<int> MaxOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector)
        {
            return await source.Select(selector).MaxAsync(i => (int?) i) ?? default(int);
        }
    }
    /// <summary>
    /// EventStorage is the application-layer that rests on top of the actual storage-facility (a database)
    /// EventStorage implements IEventStorage and IEventHistoryStorage interfaces.
    /// </summary>
    public class EventStorage : IEventStorage, IEventHistoryStorage
    {
        private readonly IEventContext _context;

        /// <summary>
        /// Constructor used for dependency injection (used for testing purposes)
        /// </summary>
        /// <param name="context">Context to be used by EventStorage</param>
        public EventStorage(IEventContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        public async Task InitializeNewEvent(EventModel eventModel)
        {
            if (eventModel == null)
            {
                throw new ArgumentNullException(nameof(eventModel), "eventModel was null");
            }

            if (await Exists(eventModel.WorkflowId, eventModel.Id))
            {
                throw new EventExistsException();
            }

            _context.Events.Add(eventModel);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEvent(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                // Discussion on why, we throw an exception, as opposed to saying "great, it's gone, in any case". 
                // http://lists.w3.org/Archives/Public/ietf-http-wg/2007JulSep/0347.html
                throw new NotFoundException();
            }

            _context.Conditions.RemoveRange(_context.Conditions.Where(c => c.WorkflowId == workflowId && c.EventId == eventId));
            _context.Exclusions.RemoveRange(_context.Exclusions.Where(e => e.WorkflowId == workflowId && e.EventId == eventId));
            _context.Inclusions.RemoveRange(_context.Inclusions.Where(i => i.WorkflowId == workflowId && i.EventId == eventId));
            _context.Responses.RemoveRange(_context.Responses.Where(r => r.WorkflowId == workflowId && r.EventId == eventId));

            _context.Events.Remove(_context.Events.Single(e => e.WorkflowId == workflowId && e.Id == eventId));

            await _context.SaveChangesAsync();
        }

        public async Task Reload(string workflowId, string eventId)
        {
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }
            await _context.Entry(await _context.Events.SingleAsync(e => e.WorkflowId == workflowId && e.Id == eventId)).ReloadAsync();
        }

        #region Properties
        public async Task<bool> Exists(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            return await _context.Events.AnyAsync(e => e.WorkflowId == workflowId && e.Id == eventId);
        }

        public async Task<Uri> GetUri(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);
            return new Uri((await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).OwnUri);
        }

        public async Task<string> GetName(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Name;
        }

        public async Task<IEnumerable<string>> GetRoles(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Roles.Select(role => role.Role);
        }

        public async Task<bool> GetExecuted(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Executed;
        }

        public async Task SetExecuted(string workflowId, string eventId, bool executedValue)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            try
            {
                (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Executed = executedValue;
            }
            catch (Exception)
            {
                throw new FailedToUpdateStateException();
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> GetIncluded(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Included;
        }

        public async Task SetIncluded(string workflowId, string eventId, bool includedValue)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            var @event = await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId);

            @event.Included = includedValue;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> GetPending(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Pending;
        }

        public async Task<bool> GetIsEvil(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            return (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).IsEvil;
        }

        public async Task SetPending(string workflowId, string eventId, bool pendingValue)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            (await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId)).Pending = pendingValue;
            await _context.SaveChangesAsync();
        }

        public async Task<LockDto> GetLockDto(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);
            // SingleOrDeafult will return either null or the actual single element in set. 
            var @event =
                await
                    _context.Events.SingleOrDefaultAsync(model => model.WorkflowId == workflowId && model.Id == eventId);
            if (@event.LockOwner == null) return null;

            return new LockDto
            {
                WorkflowId = @event.WorkflowId,
                EventId = @event.Id,
                LockOwner = @event.LockOwner
            };
        }

        public async Task SetLock(string workflowId, string eventId, string lockOwner)
        {
            if (workflowId == null || eventId == null || lockOwner == null)
            {
                throw new ArgumentNullException();
            }

            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            await EventIsInALegalState(workflowId, eventId);

            var @event =
                await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId);
            if (@event.LockOwner != null && @event.LockOwner != lockOwner)
            {
                throw new LockedException();
            }

            @event.LockOwner = lockOwner;

            await _context.SaveChangesAsync();
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

            await EventIsInALegalState(workflowId, eventId);

            var @event = await _context.Events.SingleAsync(model => model.WorkflowId == workflowId && model.Id == eventId);

            @event.LockOwner = null;
            _context.Entry(@event).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

        public async Task<HashSet<RelationToOtherEventModel>> GetConditions(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }
            var dbset = _context.Conditions.Where(model => model.WorkflowId == workflowId && model.EventId == eventId);
            var hashSet = new HashSet<RelationToOtherEventModel>();

            foreach (var element in dbset)
            {
                hashSet.Add(new RelationToOtherEventModel
                {
                    Uri = new Uri(element.UriString),
                    EventId = element.ForeignEventId,
                    WorkflowId = element.WorkflowId
                });
            }
            return hashSet;
        }

        public async Task<HashSet<RelationToOtherEventModel>> GetResponses(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            var dbset = _context.Responses.Where(model => model.WorkflowId == workflowId && model.EventId == eventId);
            var hashSet = new HashSet<RelationToOtherEventModel>();

            foreach (var element in dbset)
            {
                hashSet.Add(new RelationToOtherEventModel
                {
                    Uri = new Uri(element.UriString),
                    EventId = element.ForeignEventId,
                    WorkflowId = element.WorkflowId
                });
            }

            return hashSet;
        }

        public async Task<HashSet<RelationToOtherEventModel>> GetExclusions(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }


            var dbset = _context.Exclusions.Where(model => model.WorkflowId == workflowId && model.EventId == eventId);
            var hashSet = new HashSet<RelationToOtherEventModel>();

            foreach (var element in dbset)
            {
                hashSet.Add(new RelationToOtherEventModel
                {
                    Uri = new Uri(element.UriString),
                    EventId = element.ForeignEventId,
                    WorkflowId = element.WorkflowId
                });
            }

            return hashSet;
        }

        public async Task<HashSet<RelationToOtherEventModel>> GetInclusions(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }


            var dbset = _context.Inclusions.Where(model => model.WorkflowId == workflowId && model.EventId == eventId);
            var hashSet = new HashSet<RelationToOtherEventModel>();

            foreach (var element in dbset)
            {
                hashSet.Add(new RelationToOtherEventModel
                {
                    Uri = new Uri(element.UriString),
                    EventId = element.ForeignEventId,
                    WorkflowId = element.WorkflowId
                });
            }

            return hashSet;
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Disposes this context. (New Controllers are created for each HTTP-request, and hence, also disposed of
        /// when the HTTP-request is executed, and hence, this EventStorage is also disposed of)
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// EventIdentificationIsInALegalState makes two checks on EventIdentification-set,
        /// that when combined ensures that EventIdentification only has a single element. 
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null.</exception>
        private async Task EventIsInALegalState(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            // Check that there's currently only a single element in database
            if (await _context.Events.CountAsync(model => model.WorkflowId == workflowId && model.Id == eventId) > 1)
            {
                throw new IllegalStorageStateException();
            }

            if (!await _context.Events.AnyAsync(model => model.WorkflowId == workflowId && model.Id == eventId))
            {
                throw new IllegalStorageStateException();
            }
        }
        #endregion

        public async Task SaveHistory(ActionModel toSave)
        {
            if (toSave == null)
            {
                throw new ArgumentNullException(nameof(toSave));
            }
            if (!await Exists(toSave.WorkflowId, toSave.EventId))
            {
                throw new NotFoundException();
            }

            _context.History.Add(toSave);
            await _context.SaveChangesAsync();
        }

        public async Task<IQueryable<ActionModel>> GetHistoryForEvent(string workflowId, string eventId)
        {
            if (!await Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            return _context.History.Where(h => h.EventId == eventId && h.WorkflowId == workflowId);
        }

        public async Task<ActionModel> ReserveNext(ActionModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (!await Exists(model.WorkflowId, model.EventId))
            {
                throw new NotFoundException();
            }

            model.Timestamp = await (await GetHistoryForEvent(model.WorkflowId, model.EventId)).MaxOrDefaultAsync(action => action.Timestamp) + 1;

            _context.History.Add(model);
            await _context.SaveChangesAsync();
            
            return model;
        }

        public async Task UpdateHistory(ActionModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (!await Exists(model.WorkflowId, model.EventId))
            {
                throw new NotFoundException();
            }

            var dbModel =
                await
                    (await GetHistoryForEvent(model.WorkflowId, model.EventId)).SingleOrDefaultAsync(
                        m => m.Timestamp == model.Timestamp && m.CounterpartId == model.CounterpartId && m.Type == model.Type);
            dbModel.CounterpartTimeStamp = model.CounterpartTimeStamp;

            await _context.SaveChangesAsync();
        }

        public Task<int> GetHighestCounterpartTimeStamp(string workflowId, string eventId, string counterpartId)
        {
            return _context.History
                .Where(
                    action =>
                        action.WorkflowId == workflowId
                        && action.EventId == eventId
                        && action.CounterpartId == counterpartId)
                .MaxOrDefaultAsync(action => action.CounterpartTimeStamp);
        }
    }
}