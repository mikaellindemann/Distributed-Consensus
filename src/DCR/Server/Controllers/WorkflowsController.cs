using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;
using Server.Exceptions;
using Server.Interfaces;

namespace Server.Controllers
{
    /// <summary>
    /// WorkflowsController handles HTTP-request regarding workflows on Server
    /// </summary>
    public class WorkflowsController : ApiController
    {
        private readonly IServerLogic _logic;

        /// <summary>
        /// Constructor used for dependency-injection udring testing
        /// </summary>
        /// <param name="logic">Logic that handles logic for workflows operations</param>
        public WorkflowsController(IServerLogic logic)
        {
            _logic = logic;
        }

        #region GET requests
        /// <summary>
        /// Returns a list of all workflows currently held at this Server
        /// </summary>
        /// <returns>List of WorkflowDto</returns>
        [Route("workflows")]
        [HttpGet]
        public async Task<IEnumerable<WorkflowDto>> Get()
        {
            var toReturn = await _logic.GetAllWorkflows();
            //await _historyLogic.SaveNoneWorkflowSpecificHistory(new ActionModel
            //{
            //    HttpRequestType = "GET",
            //    Message = "Succesfully called: Get",
            //    MethodCalledOnSender = "Get"
            //});

            return toReturn;
        }

        /// <summary>
        /// Given an workflowId, this method returns all events within that workflow
        /// </summary>
        /// <param name="workflowId">Id of the requested workflow</param>
        /// <returns>IEnumerable of EventAddressDto</returns>
        [Route("workflows/{workflowId}")]
        [HttpGet]
        public async Task<IEnumerable<EventAddressDto>> Get(string workflowId)
        {
            try
            {
                var toReturn = await _logic.GetEventsOnWorkflow(workflowId);
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Succesfully called: Get",
                //    HttpRequestType = "GET",
                //    MethodCalledOnSender = "Get(" + workflowId + ")"
                //});

                return toReturn;
            }
            catch (ArgumentNullException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "GET",
                //    MethodCalledOnSender = "GET(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (NotFoundException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "GET",
                //    MethodCalledOnSender = "Get(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "The specified workflow could not be found"));
            }
            catch (Exception e)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "GET",
                //    MethodCalledOnSender = "Get(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, e.Message));
            }
        }
        #endregion

        #region POST requests
        /// <summary>
        /// PostNewWorkFlow adds a new workflow.
        /// </summary>
        /// <param name="workflowDto">Contains the information on the workflow, that is to be created at Server</param>
        [Route("Workflows")]
        [HttpPost]
        public async Task PostWorkFlow([FromBody] WorkflowDto workflowDto)
        {
            if (!ModelState.IsValid)
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Provided input could not be mapped onto an instance of WorkflowDto."));
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + toThrow.GetType(),
                //    MethodCalledOnSender = "PostWorkflow",
                //    WorkflowId = workflowDto.Id
                //});

                throw toThrow;
            }

            try
            {
                // Add this Event to the specified workflow
                await _logic.AddNewWorkflow(workflowDto);
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Succesfully called: PostWorkflow",
                //    MethodCalledOnSender = "PostWorkflow",
                //    WorkflowId = workflowDto.Id
                //});
            }
            catch (ArgumentNullException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostWorkflow",
                //    WorkflowId = workflowDto != null ? workflowDto.Id : ""
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (WorkflowAlreadyExistsException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostWorkflow",
                //    WorkflowId = workflowDto.Id
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "A workflow with that id exists!"));
            }
            catch (Exception e)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostWorkflow",
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e));
            }
        }

        /// <summary>
        /// Will add an Event to a workflow. 
        /// </summary>
        /// <param name="workflowId">The id of the workflow, that the Event is to be added to</param>
        /// <param name="eventToAddDto">Contains information about the Event</param>
        /// <returns></returns>
        [Route("Workflows/{workflowId}")]
        [HttpPost]
        public async Task PostEventToWorkFlow(string workflowId, [FromBody] EventAddressDto eventToAddDto)
        {
            if (!ModelState.IsValid)
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                                "Provided input could not be mapped onto EventAddressDto"));
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + toThrow.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow",
                //    WorkflowId = workflowId,
                //    EventId = eventToAddDto.Id
                //});

                throw toThrow;
            }

            try
            {
                // Add this Event to the specified workflow
                await _logic.AddEventToWorkflow(workflowId, eventToAddDto);
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    EventId = eventToAddDto.Id,
                //    Message = "Succesfully called: PostEventWorkflow",
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    HttpRequestType = "POST",
                //    WorkflowId = workflowId
                //});
            }
            catch (ArgumentNullException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    WorkflowId = workflowId
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (NotFoundException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    WorkflowId = workflowId
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "The workflow was not found at Server"));
            }
            catch (EventExistsException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    WorkflowId = workflowId
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "The event already exists at Server. You may wish to update the Event instead, using a PUT call"));
            }
            catch (IllegalStorageStateException e)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    WorkflowId = workflowId
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e));
            }
            catch (Exception e)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    HttpRequestType = "POST",
                //    Message = "Threw: " + e.GetType(),
                //    MethodCalledOnSender = "PostEventWorkflow(" + workflowId + ")",
                //    WorkflowId = workflowId
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e));
            }
        }
        #endregion

        #region DELETE requests
        /// <summary>
        /// Will delete a specified Event from a specified workflow
        /// </summary>
        /// <param name="workflowId">Id of the workflow, the Event belongs to</param>
        /// <param name="eventId">Id of the Event, that is to be deleted</param>
        [Route("Workflows/{workflowId}/{eventId}")]
        [HttpDelete]
        public void DeleteEventFromWorkflow(string workflowId, string eventId)
        {
            try
            {
                // Delete the given event id from the list of workflow-events.
                _logic.RemoveEventFromWorkflow(workflowId, eventId);
                //_historyLogic.SaveHistory(new ActionModel
                //{
                //    EventId = eventId,
                //    WorkflowId = workflowId,
                //    Message = "Succesfully called: DeleteEventFromWorkflow",
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteEventFromWorkflow(" + workflowId + ", " + eventId + ")",
                //});
            }
            catch (ArgumentNullException)
            {
                //_historyLogic.SaveHistory(new ActionModel
                //{
                //    EventId = eventId,
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteEventFromWorkflow(" + workflowId + ", " + eventId + ")",
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (NotFoundException)
            {
                //_historyLogic.SaveHistory(new ActionModel
                //{
                //    EventId = eventId,
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteEventFromWorkflow(" + workflowId + ", " + eventId + ")",
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "Either the event or the workflow was not found at Server"));
            }
            catch (Exception e)
            {
                //_historyLogic.SaveHistory(new ActionModel
                //{
                //    EventId = eventId,
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteEventFromWorkflow(" + workflowId + ", " + eventId + ")",
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server: Failed to remove Event from workflow", e));
            }
        }

        /// <summary>
        /// Deletes the specified workflow
        /// </summary>
        /// <param name="workflowId">Id of the workflow, that is to be deleted</param>
        /// <returns></returns>
        [Route("Workflows/{workflowId}")]
        [HttpDelete]
        public async Task DeleteWorkflow(string workflowId)
        {
            try
            {
                await _logic.RemoveWorkflow(workflowId);
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Succesfully called: DeleteWorkflow",
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteWorkflow(" + workflowId + ")",
                //});
            }
            catch (ArgumentNullException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteWorkflow(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Seems input was not satisfactory"));
            }
            catch (NotFoundException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteWorkflow(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound,
                    "The workflow could not be found"));
            }
            catch (IllegalStorageStateException)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteWorkflow(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server: Storage was found in an illegal state"));
            }
            catch (Exception e)
            {
                //await _historyLogic.SaveHistory(new ActionModel
                //{
                //    WorkflowId = workflowId,
                //    Message = "Threw: " + e.GetType(),
                //    HttpRequestType = "DELETE",
                //    MethodCalledOnSender = "DeleteWorkflow(" + workflowId + ")"
                //});

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Server: Failed to remove workflow", e));
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            _logic.Dispose();
            base.Dispose(disposing);
        }
    }
}
