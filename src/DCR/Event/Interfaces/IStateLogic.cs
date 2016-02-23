using System;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Exceptions;
using Common.DTO.Event;
using Event.Exceptions.EventInteraction;

namespace Event.Interfaces
{
    /// <summary>
    /// IStateLogic is a logic-layer, that handles logic involved in operations on an Event's state. 
    /// </summary>
    public interface IStateLogic : IDisposable
    {
        /// <summary>
        /// IsExecuted returns the executed value for the specified Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="senderId">EventId of the one, who wants this information.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="LockedException">Thrown if the Event is locked by someone else than caller</exception>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<bool> IsExecuted(string workflowId, string eventId, string senderId);

        /// <summary>
        /// IsIncluded returns the included value for the specified Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="senderId">EventId of the one, who wants this information.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="LockedException">Thrown if the Event is locked by someone else than caller</exception>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<bool> IsIncluded(string workflowId, string eventId, string senderId);

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
        Task<EventStateDto> GetStateDto(string workflowId, string eventId, string senderId);

        /// <summary>
        /// SetIncluded sets the specified Event's Included value to the provided value. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="senderId">EventId of the one, who wants this information.</param>
        /// <param name="newIncludedValue">The value that the Event's Included value should be set to</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the string-type arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="LockedException">Thrown if the specified Event is currently locked</exception>
        Task SetIncluded(string workflowId, string eventId, string senderId, bool newIncludedValue);

        /// <summary>
        /// SetPending sets the specified Event's Pending value to the provided value. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="senderId">EventId of the one, who wants this information.</param>
        /// <param name="newPendingValue">The value that the Event's Included value should be set to</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the string-type arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="LockedException">Thrown if the specified Event is currently locked</exception>
        /// <returns></returns>
        Task SetPending(string workflowId, string eventId, string senderId, bool newPendingValue);

        /// <summary>
        /// Execute attempts to Execute the specified Event. The process includes locking the other events, and updating their state. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="executeDto">Contains the roles, that caller has.</param>
        /// <exception cref="LockedException">Thrown if the specified Event is currently locked by someone else</exception>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist</exception>
        /// <exception cref="FailedToLockOtherEventException">Thrown if locking of an other (dependent) Event failed.</exception>
        /// <exception cref="FailedToUpdateStateAtOtherEventException">Thrown if updating of another Event's state failed</exception>
        /// <exception cref="FailedToUnlockOtherEventException">Thrown if unlocking of another Event fails.</exception>
        Task Execute(string workflowId, string eventId, RoleDto executeDto);

    }
}
