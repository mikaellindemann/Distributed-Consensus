using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.DTO.Shared;
using Common.Exceptions;

namespace Client.Connections
{
    public interface IServerConnection : IDisposable
    {
        /// <summary>
        /// Login to the Flow-system.
        /// </summary>
        /// <param name="username">The username of the chosen user to log in.</param>
        /// <param name="password">The password that matches the username</param>
        /// <returns>A dto containing relation between workflows and the roles the logged in user has access to.</returns>
        /// <exception cref="LoginFailedException">If the username and password does not match a user</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task<RolesOnWorkflowsDto> Login(string username, string password);

        /// <summary>
        /// Retrieve the Workflows currently held by the server.
        /// </summary>
        /// <returns>A list of information about workflows, which can be shown on the UI.</returns>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task<IEnumerable<WorkflowDto>> GetWorkflows();

        /// <summary>
        /// Retrieve the events of a workflow.
        /// </summary>
        /// <param name="workflowId">The Id of the workflow we want to retrieve events from.</param>
        /// <returns>A list of information about how to contact the events in the given workflow.</returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        Task<IEnumerable<ServerEventDto>> GetEventsFromWorkflow(string workflowId);
    }
}
