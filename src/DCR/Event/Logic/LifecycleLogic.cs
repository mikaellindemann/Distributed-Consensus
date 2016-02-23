using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Communicators;
using Event.Exceptions;
using Event.Interfaces;
using Event.Models;
using Event.Models.UriClasses;
using Event.Storage;

namespace Event.Logic
{
    /// <summary>
    /// LifecycleLogic is a logic-layer that handles logic involved in Event-lifecycle operations. 
    /// </summary>
    public class LifecycleLogic : ILifecycleLogic
    {
        private readonly IEventStorage _storage;
        private readonly IEventStorageForReset _resetStorage;
        private readonly ILockingLogic _lockLogic;

        /// <summary>
        /// Default constructor
        /// </summary>
        public LifecycleLogic()
        {
            var context = new EventContext();
            _storage = new EventStorage(context);
            _resetStorage = new EventStorageForReset(context);
            _lockLogic = new LockingLogic(_storage, new EventCommunicator());
        }

        /// <summary>
        /// Constructor to be used for dependency-injection
        /// </summary>
        /// <param name="storage">Storage-layer</param>
        /// <param name="resetStorage">Storage-layer used for reset-operations.</param>
        /// <param name="lockLogic">LockLogic-layer instance</param>
        public LifecycleLogic(IEventStorage storage, IEventStorageForReset resetStorage, ILockingLogic lockLogic)
        {
            _storage = storage;
            _resetStorage = resetStorage;
            _lockLogic = lockLogic;
        }

        public async Task CreateEvent(EventDto eventDto, Uri ownUri)
        {
            if (eventDto == null || ownUri == null || eventDto.WorkflowId == null || eventDto.EventId == null)
            {
                throw new ArgumentNullException();
            }

            // Check if Event already exists
            if (await _storage.Exists(eventDto.WorkflowId, eventDto.EventId))
            {
                throw new EventExistsException();
            }

            // #1. Make sure that server will accept our entry
            var dto = new EventAddressDto
            {
                WorkflowId = eventDto.WorkflowId,
                Id = eventDto.EventId,
                Uri = ownUri,
                Roles = eventDto.Roles
            };

#if DEBUG
            IServerFromEvent serverCommunicator = new ServerCommunicator("http://localhost:13768/", eventDto.EventId, eventDto.WorkflowId);
#else
            IServerFromEvent serverCommunicator = new ServerCommunicator("http://flowit.azurewebsites.net/", eventDto.EventId, eventDto.WorkflowId);
#endif

            // If the following fails, an exception will be thrown, 
            await serverCommunicator.PostEventToServer(dto);
            
            // Setup a new Event in own database.
            var @event = new EventModel
            {
                Id = eventDto.EventId,
                WorkflowId = eventDto.WorkflowId,
                Name = eventDto.Name,
                Roles = eventDto.Roles.Select(role => new EventRoleModel { WorkflowId = eventDto.WorkflowId, EventId = eventDto.EventId, Role = role }).ToList(),
                OwnUri = ownUri.AbsoluteUri,
                Executed = eventDto.Executed,
                Included = eventDto.Included,
                Pending = eventDto.Pending,
                ConditionUris = eventDto.Conditions.Select(condition => new ConditionUri { WorkflowId = eventDto.WorkflowId, EventId = eventDto.EventId, ForeignEventId = condition.Id, UriString = condition.Uri.AbsoluteUri }).ToList(),
                ResponseUris = eventDto.Responses.Select(response => new ResponseUri { WorkflowId = eventDto.WorkflowId, EventId = eventDto.EventId, ForeignEventId = response.Id, UriString = response.Uri.AbsoluteUri }).ToList(),
                InclusionUris = eventDto.Inclusions.Select(inclusion => new InclusionUri { WorkflowId = eventDto.WorkflowId, EventId = eventDto.EventId, ForeignEventId = inclusion.Id, UriString = inclusion.Uri.AbsoluteUri }).ToList(),
                ExclusionUris = eventDto.Exclusions.Select(exclusion => new ExclusionUri { WorkflowId = eventDto.WorkflowId, EventId = eventDto.EventId, ForeignEventId = exclusion.Id, UriString = exclusion.Uri.AbsoluteUri }).ToList(),
                LockOwner = null,
                InitialExecuted = eventDto.Executed,
                InitialIncluded = eventDto.Included,
                InitialPending = eventDto.Pending
            };

            try
            {
                await _storage.InitializeNewEvent(@event);
            }
            catch (EventExistsException)
            {
                throw;
            }
            catch (Exception)
            {
                // If initializing the Event in own storage fails, we'll delete it from Server
                serverCommunicator.DeleteEventFromServer().Wait();
                
                /* If DeleteEventFromServer fails, it will raise an FailedToDeleteEventFromServerException;
                *  and will propagate to LifecycleController, and will suppress the Exception from InitializeNewEvent,
                *  that got us here. 
                *  However, if DeleteEventFromServer succeeds, (i.e. no exception is thrown in DeleteEventFromServer), 
                *  but we still want to indicate to LifeCycleController (and end-user), that the Event was not created, 
                *  and hence we throw FailedToCreateEventException */
                throw new FailedToCreateEventException();
            }
        }

        public async Task DeleteEvent(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            // Notice that the following check will (should) fail, if this Event is locked by another Event
            if (! await _lockLogic.IsAllowedToOperate(workflowId, eventId, eventId))
            {
                throw new LockedException();
            }

#if DEBUG
            IServerFromEvent serverCommunicator = new ServerCommunicator("http://localhost:13768/", eventId, workflowId);
#else
            IServerFromEvent serverCommunicator = new ServerCommunicator("http://flowit.azurewebsites.net/", eventId, workflowId);
#endif
            await serverCommunicator.DeleteEventFromServer();

            // Delete Event from own Storage
            await _storage.DeleteEvent(workflowId, eventId);
        }

        public async Task ResetEvent(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException("workflowId", "workflowId was null");
            }

            // Clear lock
            await _resetStorage.ClearLock(workflowId, eventId);
            
            // Reset to initial state
            await _resetStorage.ResetToInitialState(workflowId, eventId);
        }

        public async Task<EventDto> GetEventDto(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            var returnValue = new EventDto
            {
                EventId = eventId,
                WorkflowId = workflowId,
                Name = await _storage.GetName(workflowId, eventId),
                Roles = await _storage.GetRoles(workflowId, eventId),
                Pending = await _storage.GetPending(workflowId, eventId),
                Executed = await _storage.GetExecuted(workflowId, eventId),
                Included = await _storage.GetIncluded(workflowId, eventId),
                Conditions = (await _storage.GetConditions(workflowId, eventId)).Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }),
                Exclusions = (await _storage.GetExclusions(workflowId, eventId)).Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }),
                Responses = (await _storage.GetResponses(workflowId, eventId)).Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }),
                Inclusions = (await _storage.GetInclusions(workflowId, eventId)).Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri })
            };
            
            return returnValue;
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}