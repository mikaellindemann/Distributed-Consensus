using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.Exceptions;
using Event.Communicators;
using Event.Interfaces;
using Event.Logic;
using Event.Models;
using Event.Storage;

namespace Event.Controllers
{
    /// <summary>
    /// LockController handles HTTP-request regarding Event locks. 
    /// </summary>
    public class LockController : ApiController
    {
        private readonly ILockingLogic _lockLogic;
        private readonly IEventHistoryLogic _historyLogic;

        /// <summary>
        /// Default controller used by framework during runtime
        /// </summary>
        public LockController()
        {
            _lockLogic = new LockingLogic(new EventStorage(new EventContext()), new EventCommunicator());
            _historyLogic = new EventHistoryLogic();
        }

        /// <summary>
        /// Controller used to dependency-inject during testing
        /// </summary>
        /// <param name="lockLogic">LockLogic-layer implementing the ILockingLogic interface</param>
        /// <param name="historyLogic">HistoryLogic-layer implementing the IEventHistoryLogic interface</param>
        public LockController(ILockingLogic lockLogic, IEventHistoryLogic historyLogic)
        {
            _lockLogic = lockLogic;
            _historyLogic = historyLogic;
        }

        /// <summary>
        /// Will lock this Event if it is not already locked.
        /// This POST call should be received either a) from the Event itself (when it is about to execute) or when
        /// b) another Event (that has this Event in it's dependencies) asks it to lock.
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="lockDto">Contents should represent caller</param>
        /// <param name="eventId">The id of the Event, that caller wants to lock</param>
        [Route("events/{workflowId}/{eventId}/lock")]
        [HttpPost]
        public async Task Lock(string workflowId, string eventId, [FromBody] LockDto lockDto)
        {
            if (!ModelState.IsValid)
            {
                var toThrow = new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Provided input could not be mapped onto an instance of LockDto"));
                await _historyLogic.SaveException(toThrow, "POST", "Lock", eventId, workflowId);
                throw toThrow;
            }

            try
            {
                await _lockLogic.LockSelf(workflowId, eventId, lockDto);
                await _historyLogic.SaveSuccesfullCall("POST", "Lock", eventId, workflowId);
                return;
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "POST", "Lock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Lock: Seems input was not satisfactory"));
            }
            catch (LockedException e)
            {
                _historyLogic.SaveException(e, "POST", "Lock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Lock: Failed to lock: Event is currently locked by someone else"));
            }
            catch (NotFoundException e)
            {
                _historyLogic.SaveException(e, "POST", "Lock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Lock: Event seems not to exist"));
            }
            catch (IllegalStorageStateException e)
            {
                _historyLogic.SaveException(e, "POST", "Lock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Lock: Storage reported it is in a non-valid state"));
            }
        }

        /// <summary>
        /// Unlock will (attempt to) unlock this Event. May fail if Event is already locked
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="senderId">Should represent caller</param>
        /// <param name="eventId">The id of the Event, that caller seeks to unlock</param>
        [Route("events/{workflowId}/{eventId}/lock/{senderId}")]
        [HttpDelete]
        public async Task Unlock(string workflowId, string eventId, string senderId)
        {
            try
            {
                await _lockLogic.UnlockSelf(workflowId, eventId, senderId);
                await _historyLogic.SaveSuccesfullCall("DELETE", "Unlock", eventId, workflowId);
                return;
            }
            catch (ArgumentNullException e)
            {
                _historyLogic.SaveException(e, "DELETE", "Unlock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Unlock: Could not unlock: One or more of the provided arguments was null"));
            }
            catch (LockedException e)
            {
                _historyLogic.SaveException(e, "DELETE", "Unlock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict,
                    "Unlock: Could not unlock: Event is locked by someone else"));
            }
            catch (NotFoundException e)
            {
                _historyLogic.SaveException(e, "DELETE", "Unlock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "Unlock: Event seems not to exist"));
            }
            catch (IllegalStorageStateException e)
            {
                _historyLogic.SaveException(e, "DELETE", "Unlock");

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Unlock: Storage reported it is in a non-valid state"));
            }

        }

        protected override void Dispose(bool disposing)
        {
            _historyLogic.Dispose();
            _lockLogic.Dispose();
            base.Dispose(disposing);
        }
    }
}
