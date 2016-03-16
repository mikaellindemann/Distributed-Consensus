using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.DTO.Shared;
using Common.Exceptions;
using Common.Tools;

namespace Client.Connections
{
    public class ServerConnection : IServerConnection
    {
        private readonly HttpClientToolbox _http;


        public ServerConnection(Uri uri)
        {
            _http = new HttpClientToolbox(uri);
        }

        /// <summary>
        /// For testing purposes (dependency injection of mocked Toolbox).
        /// </summary>
        /// <param name="toolbox"></param>
        public ServerConnection(HttpClientToolbox toolbox)
        {
            _http = toolbox;
        }

        /// <summary>
        /// Login to the Flow-system.
        /// </summary>
        /// <param name="username">The username of the chosen user to log in.</param>
        /// <param name="password">The password that matches the username</param>
        /// <returns>A dto containing relation between workflows and the roles the logged in user has access to.</returns>
        /// <exception cref="LoginFailedException">If the username and password does not match a user</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task<RolesOnWorkflowsDto> Login(string username, string password)
        {
            try
            {
                return
                    await
                        _http.Create<LoginDto, RolesOnWorkflowsDto>("login",
                            new LoginDto {Username = username, Password = password});
            }
            catch (UnauthorizedException e)
            {
                throw new LoginFailedException(e);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        /// <summary>
        /// Retrieve the Workflows currently held by the server.
        /// </summary>
        /// <returns>A list of information about workflows, which can be shown on the UI.</returns>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task<IEnumerable<WorkflowDto>> GetWorkflows()
        {
            try
            {
                return await _http.ReadList<WorkflowDto>("workflows");
            }
            catch (HttpRequestException ex)
            {
                throw new HostNotFoundException(ex);
            }

        }

        /// <summary>
        /// Retrieve the events of a workflow.
        /// </summary>
        /// <param name="workflowId">The Id of the workflow we want to retrieve events from.</param>
        /// <returns>A list of information about how to contact the events in the given workflow.</returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public Task<IEnumerable<EventAddressDto>> GetEventsFromWorkflow(string workflowId)
        {
            try
            {
                return _http.ReadList<EventAddressDto>($"workflows/{workflowId}");
            }
            catch (HttpRequestException ex)
            {
                throw new HostNotFoundException(ex);
            }
        }

        /// <summary>
        /// Retrieve the worklfow history from the server.
        /// </summary>
        /// <param name="workflowId">The Id of the workflow we want to see the history of.</param>
        /// <returns>A list of history-entries.</returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task<IEnumerable<ActionDto>> GetHistory(string workflowId)
        {
            try
            {
                return await _http.ReadList<ActionDto>($"history/{workflowId}");
            }
            catch (HttpRequestException ex)
            {
                throw new HostNotFoundException(ex);
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
