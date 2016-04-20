using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using ActionModel = Event.Models.ActionModel;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventHistoryStorage is an interface used to save details about the history of an Event.
    /// </summary>
    public interface IEventHistoryStorage : IDisposable
    {
        /// <summary>
        /// Saves the given ActionModel to storage.
        /// </summary>
        /// <param name="toSave">The ActionModel that is to be saved.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist.</exception>
        Task SaveHistory(ActionModel toSave);

        /// <summary>
        /// Retrieves the History for a specified Event as an IQueryable.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event, whose history is to be retrieved.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist.</exception>
        Task<IQueryable<ActionModel>> GetHistoryForEvent(string workflowId, string eventId);

        Task<ActionModel> ReserveNext(ActionModel model);
        Task UpdateHistory(ActionModel actionModel);

        Task<int> GetHighestCounterpartTimeStamp(string workflowId, string eventId, string counterpartId);
    }
}
