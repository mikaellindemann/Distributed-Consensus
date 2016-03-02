using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Models;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventStorage is the application-layer that rests on top of the actual storage-facility.
    /// </summary>
    public interface IEventStorage : IDisposable
    {
        #region Ids
        /// <summary>
        /// Tells whether an Event exists in the storage.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event to check for.</param>
        /// <returns>True if the an Event with eventId exists at workflow with workflowId.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either eventId or workFlowId is null.</exception>
        Task<bool> Exists(string workflowId, string eventId);

        /// <summary>
        /// Returns the URI-address of the Event belonging to the given workflowId and identified by eventId.
        /// </summary>
        /// <param name="workflowId">Identifies the workflow the event belongs to.</param>
        /// <param name="eventId">EventId of the Event to get.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either eventId or workFlowId is null.</exception>
        /// <exception cref="NotFoundException">Thrown if no event matches the identifying arguments.</exception>
        Task<Uri> GetUri(string workflowId, string eventId);

        /// <summary>
        /// Returns the name of an Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null.</exception>
        /// <exception cref="NotFoundException">Thrown if no Event exists with the given workflowId and EventId.</exception>
        Task<string> GetName(string workflowId, string eventId);

        /// <summary>
        /// GetRoles returns the Roles that are allowed to execute an Event.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null.</exception>
        /// <exception cref="NotFoundException">Thrown when no Event matches the provided workflowId and eventId.</exception>
        Task<IEnumerable<string>> GetRoles(string workflowId, string eventId);
        #endregion

        /// <summary>
        /// Initializes a new Event based on the provided EventModel.
        /// </summary>
        /// <param name="eventModel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when an Event with the same id already exists.</exception>
        /// <exception cref="ArgumentNullException">Thrown when provided EventModel was null.</exception>
        Task InitializeNewEvent(EventModel eventModel);

        /// <summary>
        /// DeleteEvent deletes the Event, belonging to the given workflowId and with the given eventId.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event to be deleted.</param>
        /// <returns>Task</returns>
        /// <exception cref="ArgumentNullException">Will be thrown if either workflowId or eventId is null.</exception>
        /// <exception cref="NotFoundException">Thrown if no event matches the identifying arguments.</exception>
        Task DeleteEvent(string workflowId, string eventId);

        /// <summary>
        /// Reload the workflow identified by the arguments.
        /// This ensures that any changes from other controller-calls are updated in memory.
        /// </summary>
        /// <param name="workflowId">The workflow in which the event belongs.</param>
        /// <param name="eventId">The id of the event to reload.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If an event with the given ids does not exist.</exception>
        Task Reload(string workflowId, string eventId);

        #region State
        /// <summary>
        /// Returns the Executed value for the specified event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the event does not exist in the storage</exception>
        Task<bool> GetExecuted(string workflowId, string eventId);

        /// <summary>
        /// Sets the Executed value for the specified Event.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event.</param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either workflowId or eventId is null.</exception>
        /// <exception cref="NotFoundException">Thrown if the event does not exist in the storage.</exception>
        Task SetExecuted(string workflowId, string eventId, bool value);

        /// <summary>
        /// Returns the Included value of the specified Event.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the event does not exist in the storage</exception>
        Task<bool> GetIncluded(string workflowId, string eventId);

        /// <summary>
        /// Sets the Included value for the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="value">The value that included should be set to</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the provided arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the event does not exist in the storage</exception>
        Task SetIncluded(string workflowId, string eventId, bool value);

        /// <summary>
        /// Returns the Included value of the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the Event does not exist</exception>
        Task<bool> GetPending(string workflowId, string eventId);

        /// <summary>
        /// Sets the Pending value for the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task SetPending(string workflowId, string eventId, bool value);
        #endregion

        #region Locking
        /// <summary>
        /// Returns the LockDto for the specified Event.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if an Event with the specified ids does not exist.</exception>
        Task<LockDto> GetLockDto(string workflowId, string eventId);

        /// <summary>
        /// The setter for this property should not be used to unlock the Event. If setter is provided with a null-value
        /// an ArgumentNullException will be raised. Instead, use ClearLock()-method to remove any Lock on this Event.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="lockOwner">EventId of the lockowner</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if an Event with the specified ids does not exist.</exception>
        Task SetLock(string workflowId, string eventId, string lockOwner);

        /// <summary>
        /// This method should be used for unlocking an Event as opposed to using the setter for LockDto
        /// (Setter for LockDto will raise an ArgumentNullException if provided a null-value)
        /// The method simply removes all (should be either 1 or 0) LockDto element(s) held in database. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if an Event with the specified ids does not exist.</exception>
        Task ClearLock(string workflowId, string eventId);
        #endregion

        #region Rules
        /// <summary>
        /// Returns the Condition-relations for the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<HashSet<RelationToOtherEventModel>> GetConditions(string workflowId, string eventId);

        /// <summary>
        /// GetResponses returns a HashSet containing the response relations for the provided event.
        /// Notice, that this method will not return null, but may return an empty set.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<HashSet<RelationToOtherEventModel>> GetResponses(string workflowId, string eventId);

        /// <summary>
        /// Returns the Exclusion-relations on the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<HashSet<RelationToOtherEventModel>> GetExclusions(string workflowId, string eventId);

        /// <summary>
        /// GetResponses returns a HashSet containing the inclusion relations for the provided event.
        /// Notice, that this method will not return null, but may return an empty set.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<HashSet<RelationToOtherEventModel>> GetInclusions(string workflowId, string eventId);
        #endregion
    }
}