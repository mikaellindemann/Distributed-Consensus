using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Models;

namespace Event.Interfaces
{
    /// <summary>
    /// ILockingLogic handles the logic involved in operations on Event locks. 
    /// </summary>
    public interface ILockingLogic : IDisposable
    {
        Task LockSelf(string workflowId, string eventId, LockDto lockDto);

        /// <summary>
        /// Tries to unlock the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <param name="callerId">Represents caller</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either of the input arguments are null</exception>
        /// <exception cref="LockedException">Thrown if the Event is currently locked by someone else</exception>
        Task UnlockSelf(string workflowId, string eventId, string callerId);

        /// <summary>
        /// LockAllForExecute attempts to lockall related Events for the specified Event
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<bool> LockAllForExecute(string workflowId, string eventId);

        /// <summary>
        /// UnlockAllForExecute attempts to unlock all related events for the specified Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns>False if it fails to unlock other Events</returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NullReferenceException">Thrown if Storage layer returns null-relations.</exception>
        Task<bool> UnlockAllForExecute(string workflowId, string eventId);

        /// <summary>
        /// Will determine, on basis of the provided arguments, if caller is allowed to operate on the target Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the target Event belongs to</param>
        /// <param name="eventId">EventId of the target Event</param>
        /// <param name="callerId">EventId of the Event, that wishes to operate on the target Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<bool> IsAllowedToOperate(string workflowId, string eventId, string callerId);


        Task<bool> LockList(SortedDictionary<string, RelationToOtherEventModel> list, string eventId);
        Task<bool> UnlockList(SortedDictionary<string, RelationToOtherEventModel> list, string eventId);
        Task WaitForMyTurn(string workflowId, string eventId, LockDto lockDto);
    }
}
