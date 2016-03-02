using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.History;
using Common.Exceptions;

namespace Client.Connections
{
    /// <summary>
    /// Connection to an event
    /// </summary>
    public interface IEventConnection : IDisposable
    {
        /// <summary>
        /// Get the state of a task
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="LockedException">If an event is locked</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task<EventStateDto> GetState(Uri uri, string workflowId, string eventId);

        /// <summary>
        /// Returns the history of the event.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task<IEnumerable<HistoryDto>> GetHistory(Uri uri, string workflowId, string eventId);

        /// <summary>
        /// Reset an event. Only to be used for testing!
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task ResetEvent(Uri uri, string workflowId, string eventId);

        /// <summary>
        /// Execute a task
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="UnauthorizedException">If the user does not have the right access rights</exception>
        /// <exception cref="LockedException">If an event is locked</exception>
        /// <exception cref="NotExecutableException">If an event is not executable, when execute is pressed</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task Execute(Uri uri, string workflowId, string eventId, IEnumerable<string> roles);
    }
}
