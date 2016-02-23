using System;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.DTO.Event;

namespace Event.Interfaces
{
    /// <summary>
    /// ILifecycleLogic is a logic-layer, that handles logic involved in Event-lifecycle operations. 
    /// </summary>
    public interface ILifecycleLogic : IDisposable
    {
        /// <summary>
        /// Creates an Event based on the provided EventDto.
        /// </summary>
        /// <param name="eventDto">Contains the information about the Event, that is to be created.</param>
        /// <param name="ownUri">Represents the address at which the Event is located / hosted at.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="EventExistsException">Thrown if an Event already is created with that EventId and WorkflowId</exception>
        Task CreateEvent(EventDto eventDto, Uri ownUri);

        /// <summary>
        /// Will attempt to Delete the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event to be deleted</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="LockedException">Thrown if the Event is currently locked</exception>
        Task DeleteEvent(string workflowId, string eventId);

        /// <summary>
        /// ResetEvent will bruteforce reset this Event, regardless of whether it is currently locked
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event to be deleted</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        Task ResetEvent(string workflowId, string eventId);

        /// <summary>
        /// GetEventDto returns an EventDto representing the Event matching the given workflowId and EventId.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event to be deleted</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if no Event is found, that matches the arguments</exception>
        Task<EventDto> GetEventDto(string workflowId, string eventId);
    }
}
