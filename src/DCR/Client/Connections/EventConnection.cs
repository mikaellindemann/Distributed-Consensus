using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Shared;
using Common.Exceptions;
using Common.Tools;
using HistoryConsensus;

namespace Client.Connections
{
    public class EventConnection : IEventConnection
    {
        private readonly HttpClientToolbox _httpClient;

        /// <summary>
        /// This constructor is used forwhen the connection should have knowlegde about roles.
        /// </summary>
        public EventConnection()
        {
            var client = new HttpClient {Timeout = TimeSpan.FromHours(1)};
            _httpClient = new HttpClientToolbox(new HttpClientWrapper(client));
        }


        /// <summary>
        /// For testing purposes (dependency injection of mocked Toolbox).
        /// </summary>
        /// <param name="toolbox"></param>
        public EventConnection(HttpClientToolbox toolbox)
        {
            _httpClient = toolbox;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="LockedException">If an event is locked</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task<EventStateDto> GetState(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return
                    await _httpClient.Read<EventStateDto>($"{uri}events/{workflowId}/{eventId}/state/-1");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task<IEnumerable<ActionDto>> GetHistory(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return await _httpClient.ReadList<ActionDto>($"{uri}history/{workflowId}/{eventId}");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
            
        }

        public async Task<string> GetLocalHistory(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return await _httpClient.Read<string>($"{uri}history/{workflowId}/{eventId}/local");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task ResetEvent(Uri uri, string workflowId, string eventId)
        {
            try
            {
                await
                    _httpClient.Update($"{uri}events/{workflowId}/{eventId}/reset", (object) null);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotFoundException">If the resource isn't found</exception>
        /// <exception cref="UnauthorizedException">If the user does not have the right access rights</exception>
        /// <exception cref="LockedException">If an event is locked</exception>
        /// <exception cref="NotExecutableException">If an event is not executable, when execute is pressed</exception>
        /// <exception cref="HostNotFoundException">If the host wasn't found.</exception>
        /// <exception cref="Exception">If an unexpected error happened</exception>
        public async Task Execute(Uri uri, string workflowId, string eventId, IEnumerable<string> roles)
        {
            try
            {
                await
                    _httpClient.Update($"{uri}events/{workflowId}/{eventId}/executed/",
                        new RoleDto {Roles = roles});
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public async Task<string> Produce(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return await
                    _httpClient.Create<object, string>($"{uri}history/{workflowId}/{eventId}/produce/",
                        new object[0]);
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public async Task<string> Collapse(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return await
                    _httpClient.Read<string>($"{uri}history/{workflowId}/{eventId}/collapse/");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public async Task<string> Create(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return await
                    _httpClient.Read<string>($"{uri}history/{workflowId}/{eventId}/create/");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public Task Lock(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return
                    _httpClient.Create($"{uri}events/{workflowId}/{eventId}/lock",
                        new LockDto {EventId = eventId, WorkflowId = workflowId, LockOwner = "-1"});
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public Task Unlock(Uri uri, string workflowId, string eventId)
        {
            try
            {
                return
                    _httpClient.Delete($"{uri}events/{workflowId}/{eventId}/lock/-1");
            }
            catch (HttpRequestException e)
            {
                throw new HostNotFoundException(e);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
