using System;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;
using Event.Models;

namespace Event.Logic
{
    /// <summary>
    /// StateLogic is a logic-layer that handles logic involved in operations on an Event's state. 
    /// </summary>
    public class StateLogic : IStateLogic
    {
        private readonly IEventStorage _storage;
        private readonly ILockingLogic _lockingLogic;
        private readonly IAuthLogic _authLogic;
        private readonly IEventFromEvent _eventCommunicator;
        private readonly IEventHistoryLogic _historyLogic;

        /// <summary>
        /// Constructor used for dependency injection.
        /// </summary>
        /// <param name="storage">An implementation of IEventStorage</param>
        /// <param name="lockingLogic">An implementation of ILockingLogic</param>
        /// <param name="authLogic">An implementation of IAuthLogic</param>
        /// <param name="eventCommunicator">An implementation of IEventFromEvent</param>
        /// <param name="eventHistory"></param>
        public StateLogic(IEventStorage storage, ILockingLogic lockingLogic, IAuthLogic authLogic, IEventFromEvent eventCommunicator, IEventHistoryLogic eventHistory)
        {
            if (storage == null || lockingLogic == null || authLogic == null || eventCommunicator == null || eventHistory == null)
            {
                throw new ArgumentNullException();
            }
            _storage = storage;
            _lockingLogic = lockingLogic;
            _authLogic = authLogic;
            _eventCommunicator = eventCommunicator;
            _historyLogic = eventHistory;
        }

        public async Task<bool> IsExecuted(string workflowId, string eventId, string senderId)
        {
            if (workflowId == null || eventId == null || senderId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Check is made to see if caller is allowed to execute this method at the moment. 
            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, senderId))
            {
                await _lockingLogic.WaitForMyTurn(workflowId, eventId, new LockDto
                {
                    WorkflowId = workflowId,
                    LockOwner = senderId,
                    EventId = eventId
                });
            }

            return await _storage.GetExecuted(workflowId, eventId);
        }

        public async Task<bool> IsIncluded(string workflowId, string eventId, string senderId)
        {
            if (workflowId == null || eventId == null || senderId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Check is made to see if caller is allowed to execute this method
            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, senderId))
            {
                await _lockingLogic.WaitForMyTurn(workflowId, eventId, new LockDto
                {
                    WorkflowId = workflowId,
                    LockOwner = senderId,
                    EventId = eventId
                });
            }

            var b = await _storage.GetIncluded(workflowId, eventId);
            return b;
        }

        /// <summary>
        /// GetStateDto returns an EventStateDto for the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="senderId">EventId of the one, who wants this information.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="LockedException">Thrown if the Event is currently locked</exception>
        public async Task<EventStateDto> GetStateDto(string workflowId, string eventId, string senderId)
        {
            // Input check
            if (workflowId == null || eventId == null || senderId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }



            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, senderId))
            {
                await _lockingLogic.WaitForMyTurn(workflowId, eventId, new LockDto
                {
                    EventId = eventId,
                    WorkflowId = workflowId,
                    LockOwner = senderId
                });
            }

            var name = await _storage.GetName(workflowId, eventId);
            var executed = await _storage.GetExecuted(workflowId, eventId);
            var included = await _storage.GetIncluded(workflowId, eventId);
            var pending = await _storage.GetPending(workflowId, eventId);

            var eventStateDto = new EventStateDto
            {
                Id = eventId,
                Name = name,
                Executed = executed,
                Included = included,
                Pending = pending,
                Executable = await IsExecutable(workflowId, eventId, false)
            };

            return eventStateDto;
        }

        /// <summary>
        /// Determines whether an Event can be executed at the moment. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="log"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        private async Task<bool> IsExecutable(string workflowId, string eventId, bool log)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.GetIncluded(workflowId, eventId))
            {
                //If this event is excluded, return false.
                return false;
            }

            var conditionRelations = await _storage.GetConditions(workflowId, eventId);

            foreach (var condition in conditionRelations)
            {
                if (log)
                {
                    var actionDto = await _historyLogic.ReserveNext(ActionType.ChecksConditon, eventId, workflowId,
                        condition.EventId);

                    var cond =
                        await
                            _eventCommunicator.CheckCondition(condition.Uri, condition.WorkflowId, condition.EventId,
                                eventId, actionDto.TimeStamp);

                    actionDto.CounterpartTimeStamp = cond.TimeStamp;

                    await _historyLogic.UpdateAction(actionDto);

                    // If the condition-event is not executed and currently included.
                    if (!cond.Condition) return false;
                }
                else
                {
                    var executed = await _eventCommunicator.IsExecuted(condition.Uri, condition.WorkflowId, condition.EventId, eventId);
                    var included = await _eventCommunicator.IsIncluded(condition.Uri, condition.WorkflowId, condition.EventId, eventId);

                    // If the condition-event is not executed and currently included.
                    if (included && !executed)
                    {
                        return false;
                    }
                }
            }
            return true; // If all conditions are executed or excluded.
        }

        public async Task SetIncluded(string workflowId, string eventId, string senderId, bool newIncludedValue)
        {
            if (workflowId == null || eventId == null || senderId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Check to see if caller is currently allowed to execute this method
            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, senderId))
            {
                throw new LockedException();
            }

            await _storage.SetIncluded(workflowId, eventId, newIncludedValue);
        }

        public async Task SetPending(string workflowId, string eventId, string senderId, bool newPendingValue)
        {
            if (workflowId == null || eventId == null || senderId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Check if caller is allowed to execute this method at the moment
            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, senderId))
            {
                throw new LockedException();
            }
            await _storage.SetPending(workflowId, eventId, newPendingValue);
        }

        /// <summary>
        /// Execute attempts to Execute the specified Event. The process includes locking the other events, and updating their state. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="executeDto">Contains the roles, that caller has.</param>
        /// <returns></returns>
        /// <exception cref="LockedException">Thrown if the specified Event is currently locked by someone else</exception>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="FailedToLockOtherEventException">Thrown if locking of an other (dependent) Event failed.</exception>
        /// <exception cref="FailedToUpdateStateAtOtherEventException">Thrown if updating of another Event's state failed</exception>
        /// <exception cref="FailedToUnlockOtherEventException">Thrown if unlocking of another Event fails.</exception>
        public async Task Execute(string workflowId, string eventId, RoleDto executeDto)
        {
            if (workflowId == null || eventId == null || executeDto == null)
            {
                throw new ArgumentNullException();
            }

            if (!await _storage.Exists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            // Check that caller claims the right role for executing this Event
            if (!await _authLogic.IsAuthorized(workflowId, eventId, executeDto.Roles))
            {
                throw new UnauthorizedException();
            }

            // Check if Event is currently locked
            if (!await _lockingLogic.IsAllowedToOperate(workflowId, eventId, eventId))
            {
                throw new LockedException();
            }
            // Check whether Event can be executed at the moment
            if (!await IsExecutable(workflowId, eventId, true))
            {
                throw new NotExecutableException();
            }

            // Lock all dependent Events (including one-self)
            if (!await _lockingLogic.LockAllForExecute(workflowId, eventId))
            {
                throw new FailedToLockOtherEventException();
            }

            var allOk = true;
            FailedToUpdateStateAtOtherEventException exception = null;
            try
            {
                var addressDto = new EventAddressDto
                {
                    WorkflowId = workflowId,
                    Id = eventId,
                    Uri = await _storage.GetUri(workflowId, eventId),
                    Timestamp = -1
                };
                foreach (var pending in await _storage.GetResponses(workflowId, eventId))
                {
                    var action = await _historyLogic.ReserveNext(ActionType.SetsPending, eventId, workflowId, pending.EventId);
                    addressDto.Timestamp = action.TimeStamp;
                    action.CounterpartTimeStamp = await _eventCommunicator.SendPending(pending.Uri, addressDto, pending.WorkflowId, pending.EventId);
                    await _historyLogic.UpdateAction(action);
                }
                foreach (var inclusion in await _storage.GetInclusions(workflowId, eventId))
                {
                    var action = await _historyLogic.ReserveNext(ActionType.Includes, eventId, workflowId, inclusion.EventId);
                    addressDto.Timestamp = action.TimeStamp;
                    action.CounterpartTimeStamp = await
                        _eventCommunicator.SendIncluded(inclusion.Uri, addressDto, inclusion.WorkflowId,
                            inclusion.EventId);
                    await _historyLogic.UpdateAction(action);
                }
                foreach (var exclusion in await _storage.GetExclusions(workflowId, eventId))
                {
                    var action = await _historyLogic.ReserveNext(ActionType.Excludes, eventId, workflowId, exclusion.EventId);
                    addressDto.Timestamp = action.TimeStamp;
                    action.CounterpartTimeStamp = await
                        _eventCommunicator.SendExcluded(exclusion.Uri, addressDto, exclusion.WorkflowId,
                            exclusion.EventId);
                    await _historyLogic.UpdateAction(action);
                }
                // There might have been made changes on the entity itself in another controller-call
                // Therefore we have to reload the state from database.
                await _storage.Reload(workflowId, eventId);

                await _storage.SetExecuted(workflowId, eventId, true);
                await _storage.SetPending(workflowId, eventId, false);
            }
            catch (Exception)
            {
                /*  This will catch any of FailedToUpdate<Excluded|Pending|Executed>AtAnotherEventExceptions
                 *  plus other unexpected thrown Exceptions */
                allOk = false;
                exception = new FailedToUpdateStateAtOtherEventException();
            }

            if (!await _lockingLogic.UnlockAllForExecute(workflowId, eventId))
            {
                // If we cannot even unlock, we give up!
                throw new FailedToUnlockOtherEventException();
            }
            if (allOk)
            {
                return;
            }
            throw exception;
        }

        public void Dispose()
        {
            _storage.Dispose();
            _lockingLogic.Dispose();
            _authLogic.Dispose();
            _eventCommunicator.Dispose();
        }
    }
}