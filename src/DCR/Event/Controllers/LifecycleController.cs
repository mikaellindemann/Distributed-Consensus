using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Common;
using Common.DTO.Event;
using Common.Exceptions;
using Event.Exceptions;
using Event.Interfaces;
using Event.Logic;

namespace Event.Controllers
{
    /// <summary>
    /// LifecycleController handles handles HTTP-request regarding Event lifecycle.
    /// </summary>
    public class LifecycleController : ApiController
    {
        private readonly ILifecycleLogic _logic;
        private readonly IEventHistoryLogic _historyLogic;

        /// <summary>
        /// Default constructor. Will be used by ASP.NET during runtime.
        /// </summary>
        public LifecycleController()
        {
            _logic = new LifecycleLogic();
            _historyLogic = new EventHistoryLogic();
        }

        /// <summary>
        /// Constructor used for dependency-injection
        /// </summary>
        /// <param name="logic">Logic-layer implementing the ILifecycleLogic interface</param>
        /// <param name="historyLogic">Historylogic-layer implementing the IEventHistoryLogic interface</param>
        public LifecycleController(ILifecycleLogic logic, IEventHistoryLogic historyLogic)
        {
            _logic = logic;
            _historyLogic = historyLogic;
        }

        /// <summary>
        /// Sets up an Event at this WebAPI. It will attempt to post the (needed details of the) Event to Server.  
        /// </summary>
        /// <param name="eventDto">Containts the data, this Event should be initially set to</param>
        /// <returns></returns>
        [Route("events")]
        [HttpPost]
        public async Task CreateEvent([FromBody] EventDto eventDto)
        {

            // Check that provided input can be mapped onto an instance of EventDto
            if (!ModelState.IsValid)
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Provided input could not be mapped onto an instance of EventDto."));
                await _historyLogic.SaveException(toThrow, eventDto.EventId, eventDto.WorkflowId);
                throw toThrow;
            }


            // Prepare for method-call: Gets own URI
            var s = string.Format("{0}://{1}", Request.RequestUri.Scheme, Request.RequestUri.Authority);
            var ownUri = new Uri(s);

            try
            {
                await _logic.CreateEvent(eventDto, ownUri);
                await _historyLogic.SaveSuccesfullCall("POST", "CreateEvent", eventDto.EventId, eventDto.WorkflowId);
                return; // Important that we return here. 
            }
            catch (EventExistsException e)
            {
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "CreateEvent: Event already exists"));
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "CreateEvent: Seems input was not satisfactory"));
            }
            catch (FailedToPostEventAtServerException e)
            {
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "CreateEvent: Failed to Post Event at Server"));
            }
            catch (FailedToDeleteEventFromServerException e)
            {
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                // Is thrown if we somehow fail to PostEventToServer
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "CreateEvent: Failed to delete Event from Server. " +
                    "The deletion was attempted because, posting the Event to Server failed. "));
            }
            catch (FailedToCreateEventException e)
            {
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "CreateEvent: Failed to create Event locally "));
            }
            catch (Exception e)
            {
                // Will catch any other Exception
                _historyLogic.SaveException(e, "POST", "CreateEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Create Event: An un-expected exception arose"));
            }
        }

        /// <summary>
        /// DeleteEvent will delete an Event at this Event-machine, and attempt aswell to delete the Event from Server.
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventId">The id of the Event to be deleted</param>
        /// <returns></returns>
        [Route("events/{workflowId}/{eventId}")]
        [HttpDelete]
        public async Task DeleteEvent(string workflowId, string eventId)
        {
            try
            {
                await _logic.DeleteEvent(workflowId, eventId);
                await _historyLogic.SaveSuccesfullCall("DELETE", "DeleteEvent", eventId, workflowId);
                return;
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "DELETE", "DeleteEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "DeleteEvent: Seems input was not satisfactory"));
            }
            catch (LockedException e)
            {
                _historyLogic.SaveException(e, "DELETE", "DeleteEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "DeleteEvent: Event is currently locked by someone else"));    
            }
            catch (NotFoundException e)
            {
                _historyLogic.SaveException(e, "DELETE", "DeleteEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "DeleteEvent: Event does not exist"));
            }
            catch (FailedToDeleteEventFromServerException e)
            {
                _historyLogic.SaveException(e, "DELETE", "DeleteEvent");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "DeleteEvent: Failed to delete Event from Server"));
            }
        }


        /// <summary>
        /// This method resets an Event. Note, that this will reset the three bool-values of the Event
        /// to their initial values, and reset any locks!. 
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventId">EventId of the Event, that is to be reset</param>
        /// <returns></returns>
        [Route("events/{workflowId}/{eventId}/reset")]
        [HttpPut]
        public async Task ResetEvent(string workflowId, string eventId)
        {
            try
            {
                await _logic.ResetEvent(workflowId, eventId);
                await _historyLogic.SaveSuccesfullCall("PUT", "ResetEvent", eventId, workflowId);
                return;
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "PUT", "ResetEvent", eventId, workflowId);

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "ResetEvent: Seems input was not satisfactory"));
            }
            catch (NotFoundException e)
            {
                _historyLogic.SaveException(e, "PUT", "ResetEvent", eventId, workflowId);

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "ResetEvent: Event seems not to exist"));
            }
            catch (Exception e)
            {
                _historyLogic.SaveException(e, "PUT", "ResetEvent", eventId, workflowId);

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "An unexpected exception was thrown"));
            }
        }

        /// <summary>
        /// Get the entire Event, (namely rules and state for this Event)
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventId">The id of the Event, that you wish to get an EventDto representation of</param>
        /// <returns>A task containing a single EventDto which represents the Events current state.</returns>
        [Route("events/{workflowId}/{eventId}")]
        [HttpGet]
        public async Task<EventDto> GetEvent(string workflowId, string eventId)
        {
            try
            {
                var toReturn = await _logic.GetEventDto(workflowId, eventId);
                await _historyLogic.SaveSuccesfullCall("GET", "GetEvent", eventId, workflowId);

                return toReturn;
            }
            catch (NotFoundException e)
            {
                _historyLogic.SaveException(e, "GET", "GetEvent", eventId, workflowId);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                        workflowId + "." + eventId + " not found"));
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "GET", "GetEvent", eventId, workflowId);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (Exception e)
            {
                _historyLogic.SaveException(e, "GET", "GetEvent", eventId, workflowId);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "An unexpected exception was thrown"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            _historyLogic.Dispose();
            _logic.Dispose();
            base.Dispose(disposing);
        }
    }
}
