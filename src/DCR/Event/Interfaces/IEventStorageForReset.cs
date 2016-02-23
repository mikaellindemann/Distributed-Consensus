using System;
using System.Threading.Tasks;
using Common.Exceptions;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventStorageForReset is the layer that rests on top of the actual storage. It is used for resetting Events, and
    /// hence features brute implementation, that disrespects locking on Events. 
    /// </summary>
    public interface IEventStorageForReset : IDisposable
    {
        /// <summary>
        /// ClearLock clears the Lock on the specified Event, if possible. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task ClearLock(string workflowId, string eventId);

        /// <summary>
        /// Will restore the Event to it's initial state. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event, that is to be reset.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if the Event does not exist</exception>
        Task ResetToInitialState(string workflowId, string eventId);

        /// <summary>
        /// Determines whether a specified Event exists in the database. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to</param>
        /// <param name="eventId">EventId of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the specified Event does not exist</exception>
        Task<bool> Exists(string workflowId, string eventId);
    }
}
