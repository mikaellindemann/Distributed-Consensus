using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Exceptions;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;

namespace Event.Controllers
{
    /// <summary>
    /// StateController handles HTTP-requests regarding State on an Event. 
    /// </summary>
    public class StateController : ApiController
    {
        private readonly IStateLogic _logic;
        private readonly IEventHistoryLogic _historyLogic;

        /// <summary>
        /// Constructor used for Dependency injection.
        /// </summary>
        /// <param name="logic">An implementation of IStateLogic.</param>
        /// <param name="historyLogic"></param>
        public StateController(IStateLogic logic, IEventHistoryLogic historyLogic)
        {
            _logic = logic;
            _historyLogic = historyLogic;
        }

        /// <summary>
        /// GetExecuted returns the Event's current (bool) Executed value. 
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="senderId">Content should represent the caller of this method</param>
        /// <param name="eventId">EventId of the Event, whose Executed value should be returned</param>
        /// <returns>Event's current Executed value</returns>
        [Route("events/{workflowId}/{eventId}/executed/{senderId}")]
        [HttpGet]
        public async Task<bool> GetExecuted(string workflowId, string eventId, string senderId)
        {
            try
            {
                return await _logic.IsExecuted(workflowId, eventId, senderId);
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Event is locked"));
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "GetExecuted: Seems input was not satisfactory"));
            }

        }

        [Route("events/{workflowId}/{eventId}/condition")]
        [HttpGet]
        public async Task<ConditionDto> GetCondition(string workflowId, string eventId, string senderId, int timestamp)
        {
            if (
                !await
                    _historyLogic.IsCounterpartTimeStampHigher(workflowId, eventId, senderId,
                        timestamp))
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdateIncluded: EventAddressDto Timestamp was lower than a previous timestamp from this event"));
                //await _historyLogic.SaveException(toThrow, "PUT", "UpdateIncluded", eventId, workflowId);
                throw toThrow;
            }
            try
            {
                var included = await _logic.IsIncluded(workflowId, eventId, senderId);
                var executed = await _logic.IsExecuted(workflowId, eventId, senderId);

                var localTimestamp = await _historyLogic.SaveSuccesfullCall(ActionType.CheckedCondition, eventId, workflowId, senderId, timestamp);

                if (included && !executed)
                {
                    return new ConditionDto {Condition = false, TimeStamp = localTimestamp};
                }

                return new ConditionDto {Condition = true, TimeStamp = localTimestamp};
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "GetIncluded: Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "GetIncluded: Event is locked"));
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "GetIncluded: Seems input was not satisfactory"));
            }
        }

        /// <summary>
        /// GetIncluded returns Event's current value for Included (bool). 
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="senderId">Content should represent caller of the method.</param>
        /// <param name="eventId">The id of the Event, whose Included value is to be returned</param>
        /// <returns>Current value of Event's (bool) Included value</returns>
        [Route("events/{workflowId}/{eventId}/included/{senderId}")]
        [HttpGet]
        public async Task<bool> GetIncluded(string workflowId, string senderId, string eventId)
        {
            try
            {
                return await _logic.IsIncluded(workflowId, eventId, senderId);
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "GetIncluded: Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                        "GetIncluded: Event is locked"));
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "GetIncluded: Seems input was not satisfactory"));
            }
        }

        /// <summary>
        /// Returns the current state of the event.
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="senderId">Content of this should represent caller</param>
        /// <param name="eventId">The id of the Event, whose StateDto is to be returned</param>
        /// <returns>A Task resulting in an EventStateDto which contains 3 
        /// booleans with the current state of the Event, plus a 4th boolean 
        /// which states whether the Event is currently executable</returns>
        [Route("events/{workflowId}/{eventId}/state/{senderId}")]
        [HttpGet]
        public async Task<EventStateDto> GetState(string workflowId, string eventId, string senderId)
        {
            try
            {
                return await _logic.GetStateDto(workflowId, eventId, senderId);
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "GetState: Seems input was not satisfactory"));
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "GetState: Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "GetState: Event is locked"));
            }
        }

        /// <summary>
        /// Updates Event's current (bool) value for Included
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventAddressDto">Content should represent caller. Used to identify caller.</param>
        /// <param name="boolValueForIncluded">The value that Included should be set to</param>
        /// <param name="eventId">The id of the Event, whose Included value is to be updated</param>
        [Route("events/{workflowId}/{eventId}/included/{boolValueForIncluded}")]
        [HttpPut]
        public async Task<int> UpdateIncluded(string workflowId, string eventId, bool boolValueForIncluded, [FromBody] EventAddressDto eventAddressDto)
        {
            // Check if provided input can be mapped onto an instance of EventAddressDto
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdateIncluded: Provided input could not be mapped onto an instance of EventAddressDto"));
            }
            if (
                !await
                    _historyLogic.IsCounterpartTimeStampHigher(workflowId, eventId, eventAddressDto.Id,
                        eventAddressDto.Timestamp))
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdateIncluded: EventAddressDto Timestamp was lower than a previous timestamp from this event"));
                //await _historyLogic.SaveException(toThrow, "PUT", "UpdateIncluded", eventId, workflowId);
                throw toThrow;
            }

            try
            {
                await _logic.SetIncluded(workflowId, eventId, eventAddressDto.Id, boolValueForIncluded);
                var timestamp = await _historyLogic.SaveSuccesfullCall(boolValueForIncluded ? ActionType.IncludedBy : ActionType.ExcludedBy, 
                    eventId, workflowId, eventAddressDto.Id, eventAddressDto.Timestamp);

                return timestamp;
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "UpdateIncluded: Not Found"));
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdateIncluded: Provided input was null"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "UpdateIncluded: Event is locked"));
            }
            catch (FailedToUpdateIncludedAtAnotherEventException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "UpdateIncluded: Failed to get Included from another Event"));
            }
        }

        /// <summary>
        /// Updates Event's current (bool) value for Pending
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventAddressDto">Content should represent caller.</param>
        /// <param name="boolValueForPending">The value Pending should be set to</param>
        /// <param name="eventId">The id of the Event, whose Pending value is to be set</param>
        [Route("events/{workflowId}/{eventId}/pending/{boolValueForPending}")]
        [HttpPut]
        public async Task<int> UpdatePending(string workflowId, string eventId, bool boolValueForPending, [FromBody] EventAddressDto eventAddressDto)
        {
            // Check to see whether caller provided a legal instance of an EventAddressDto
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdatePending: Provided input could not be mapped onto an instance of EventAddressDto"));
            }
            if (
                !await
                    _historyLogic.IsCounterpartTimeStampHigher(workflowId, eventId, eventAddressDto.Id,
                        eventAddressDto.Timestamp))
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdateIncluded: EventAddressDto Timestamp was lower than a previous timestamp from this event"));
                //await _historyLogic.SaveException(toThrow, "PUT", "UpdateIncluded", eventId, workflowId);
                throw toThrow;
            }

            try
            {
                await _logic.SetPending(workflowId, eventId, eventAddressDto.Id, boolValueForPending);
                var timestamp = await _historyLogic.SaveSuccesfullCall(ActionType.SetPendingBy, eventId, workflowId, eventAddressDto.Id, eventAddressDto.Timestamp);
                return timestamp;
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "UpdatePending: Provided input was null"));
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "UpdatePending: Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "UpdatePending: Event is locked"));
            }
            catch (FailedToUpdatePendingAtAnotherEventException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "UpdatePending: Failed to update Pending at another Event"));
            }

        }

        /// <summary>
        /// Executes this event. Only Clients should invoke this.
        /// </summary>
        /// <param name="executeDto">An executeDto with the roles of the given user wishing to execute.</param>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventId">The id of the Event, who is to be executed</param>
        /// <returns></returns>
        [Route("events/{workflowId}/{eventId}/executed")]
        [HttpPut]
        public async Task Execute(string workflowId, string eventId, [FromBody] RoleDto executeDto)
        {
            // Check that provided input can be mapped onto an instance of ExecuteDto
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Provided input could not be mapped onto an instance of ExecuteDto; " +
                    "No roles was provided"));
            }
            try
            {
                await _logic.Execute(workflowId, eventId, executeDto);
            }
            catch (NotFoundException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Execute: Not Found"));
            }
            catch (LockedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Event is locked"));
            }
            catch (UnauthorizedException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Unauthorized,
                    "You do not have permission to execute this event"));
            }
            catch (NotExecutableException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.PreconditionFailed,
                    "Event is not executable."));
            }
            catch (FailedToLockOtherEventException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Another event is locked"));
            }
            catch (FailedToUnlockOtherEventException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Could not unlock other events."));
            }
            catch (FailedToUpdateStateException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "State could not be saved!"));
            }
            catch (FailedToUpdateStateAtOtherEventException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Another event could not save state!"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            _logic.Dispose();
            _historyLogic.Dispose();
            base.Dispose(disposing);
        }
    }
}
