using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DTO.History;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventHistoryLogic is a logic-layer that handles logic regarding retrieving and saving Event-history data. 
    /// </summary>
    public interface IEventHistoryLogic : IDisposable
    {
        /// <summary>
        /// Returns the logging history for the specified Event. 
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the specified Event.</param>
        /// <returns></returns>
        Task<IEnumerable<ActionDto>> GetHistoryForEvent(string workflowId, string eventId);

        /// <summary>
        /// Will save a thrown Exception for this event. Should be used if an operation throws an exception.   
        /// </summary>
        /// <param name="ex">Exception that was thrown.</param>
        /// <param name="type">Type of the action to save.</param>
        /// <param name="eventId">EventId of the Event, that was involved in the operation that caused the exception.</param>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="counterpartId"></param>
        /// <returns></returns>
        Task SaveException(Exception ex, ActionType type, string eventId = "", string workflowId = "", string counterpartId  = "");
        
        /// <summary>
        /// Will save a succesfull method call for this event. Should be used when an operation was carried out succesfully.
        /// </summary>
        /// <param name="type">The type of action to save.</param>
        /// <param name="eventId">>EventId of the Event, that was involved in the operation.</param>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="counterpartId"></param>
        /// <param name="senderTimeStamp"></param>
        /// <returns></returns>
        Task<int> SaveSuccesfullCall(ActionType type, string eventId = "", string workflowId = "", string counterpartId = "", int senderTimeStamp = -1);
    }
}
