using System;
using System.Threading.Tasks;
using Event.Exceptions;
using Common.DTO.Shared;

namespace Event.Interfaces
{
    /// <summary>
    /// IServerFromEvent is the module through which Event has its outgoing communication with Server.
    /// </summary>
    interface IServerFromEvent : IDisposable
    {
        /// <summary>
        /// Attempts to Delete an Event from Server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FailedToDeleteEventFromServerException">Thrown if deletion of Event at Server fails</exception>
        Task DeleteEventFromServer();

        /// <summary>
        /// Attempts to Post an Event to Server
        /// </summary>
        /// <param name="dto">Contains the information about the Event that is to be posted to Server</param>
        /// <returns></returns>
        /// <exception cref="FailedToPostEventAtServerException">Thrown if posting of Event at Server fails.</exception>
        Task PostEventToServer(EventAddressDto dto);
    }
}
