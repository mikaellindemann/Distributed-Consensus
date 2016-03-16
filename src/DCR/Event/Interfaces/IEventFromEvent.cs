using System;
using System.Threading.Tasks;
using Common.DTO.Event;
using Event.Exceptions;
using Event.Exceptions.EventInteraction;
using Common.DTO.Shared;
using Event.Models;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventFromEvent handles the outgoing communication from an Event to another Event.
    /// </summary>
    public interface IEventFromEvent : IDisposable
    {
        /// <summary>
        /// Asks the specified target Event whether it is executed.
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event, whose Executed value is asked for</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <param name="ownId">EventId of the calling Event.</param>
        /// <returns></returns>
        /// <exception cref="FailedToGetExecutedFromAnotherEventException">Thrown if method fails to retrieve Executed value from the target Event</exception>
        Task<bool> IsExecuted(Uri targetEventUri, string targetWorkflowId, string targetEventId, string ownId);

        Task<ConditionDto> CheckCondition(Uri targetEventUri, string targetWorkflowId, string targetEventId, string ownId);

        /// <summary>
        /// Will determine if the target event is included (true) or not (false). 
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <param name="ownId">EventId of the calling Event.</param>
        /// <returns></returns>
        /// <exception cref="FailedToGetIncludedFromAnotherEventException">Thrown if Included could not be retrieved from the target Event</exception>
        Task<bool> IsIncluded(Uri targetEventUri, string targetWorkflowId, string targetEventId, string ownId);

        /// <summary>
        /// SendExcluded attempts on updating the Pending value on the target Event
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="lockDto">Should describe caller of this method.</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <returns></returns>
        /// <exception cref="FailedToUpdatePendingAtAnotherEventException">Thrown if Pending value failed to be updated at the target Event</exception>
        Task<int> SendPending(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetEventId);

        /// <summary>
        /// SendIncluded attempts on updating the Included value on the target Event
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="lockDto">Should describe caller of this method.</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <returns></returns>
        /// <exception cref="FailedToUpdateIncludedAtAnotherEventException">Thrown if Included value failed to be updated at the target Event</exception>
        Task<int> SendIncluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetEventId);

        /// <summary>
        /// SendExcluded attempts on updating the Excluded value on the target Event
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="lockDto">Contents should describe caller of this method.</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <returns></returns>
        /// <exception cref="FailedToUpdateExcludedAtAnotherEventException">Thrown if Excluded value failed to be updated at the target Event</exception>
        Task<int> SendExcluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetEventId);

        /// <summary>
        /// Tries to lock target event
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="lockDto">Should describe caller of this method.</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <returns></returns>
        /// <exception cref="FailedToLockOtherEventException">Thrown if this method fails to lock the target Event</exception>
        Task<int> Lock(Uri targetEventUri, LockDto lockDto, string targetWorkflowId, string targetEventId);

        /// <summary>
        /// Attempts on unlocking the target Event
        /// </summary>
        /// <param name="targetEventUri">URI of the target Event</param>
        /// <param name="targetWorkflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="targetEventId">EventId of the target Event</param>
        /// <param name="unlockId"></param>
        /// <returns></returns>
        /// <exception cref="FailedToUnlockOtherEventException">Thrown if this method fails to unlock the target Event</exception>
        Task<int> Unlock(Uri targetEventUri, string targetWorkflowId, string targetEventId, string unlockId);
    }
}
