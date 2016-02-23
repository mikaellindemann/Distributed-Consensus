using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.DTO.History;

namespace Event.Interfaces
{
    /// <summary>
    /// IEventHistoryStorage is an interface used to save details about the history of an Event.
    /// </summary>
    public interface IEventHistoryStorage : IDisposable
    {
        /// <summary>
        /// Saves the given HistoryModel to storage.
        /// </summary>
        /// <param name="toSave">The HistoryModel that is to be saved.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist.</exception>
        Task SaveHistory(HistoryModel toSave);

        /// <summary>
        /// Retrieves the History for a specified Event as an IQueryable.
        /// </summary>
        /// <param name="workflowId">EventId of the workflow, the Event belongs to.</param>
        /// <param name="eventId">EventId of the Event, whose history is to be retrieved.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">Thrown if the specified Event does not exist.</exception>
        Task<IQueryable<HistoryModel>> GetHistoryForEvent(string workflowId, string eventId);
    }
}
