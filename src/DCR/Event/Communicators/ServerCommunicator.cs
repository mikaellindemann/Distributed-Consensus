using System;
using System.Threading.Tasks;
using Common;
using Common.DTO.Shared;
using Common.Tools;
using Event.Exceptions;
using Event.Interfaces;

namespace Event.Communicators
{
    /// <summary>
    /// ServerCommunicator is the module through which Event has its outgoing communication with Server.
    /// Notice that a ServerCommunicator instance is 'per-event- specific. 
    /// ServerCommunicator implements the IServerFromEvent interface
    /// </summary>
    public class ServerCommunicator : IServerFromEvent
    {
        public readonly HttpClientToolbox _httpClient;

        // _eventId represents this Event's id, and _workflowId the workflow that this Event is a part of.
        private readonly string _eventId;
        private readonly string _workflowId;


        public ServerCommunicator(string baseAddress, string eventId, string workFlowId)
        {
            if (baseAddress == null || eventId == null || workFlowId == null)
            {
                throw new ArgumentNullException();
            }

            _workflowId = workFlowId;
             _eventId = eventId;
            _httpClient = new HttpClientToolbox(baseAddress);
        }

        ///<summary>
    	///A constructor with dependency injection for testing uses.
    	///</summary>
        public ServerCommunicator(string eventId, string workFlowId, HttpClientToolbox httpClient)
        {
            if (eventId == null || workFlowId == null || httpClient == null)
            {
                throw new ArgumentNullException();
            }

            _workflowId = workFlowId;
            _eventId = eventId;
            _httpClient = httpClient;
        }

        public async Task PostEventToServer(EventAddressDto addressDto)
        {
            var path = string.Format("workflows/{0}", _workflowId);
            try
            {
                await _httpClient.Create(path, addressDto);
            }
            catch (Exception)
            {
                throw new FailedToPostEventAtServerException();
            }
        }

        public async Task DeleteEventFromServer()
        {
            var path = string.Format("workflows/{0}/{1}", _workflowId, _eventId);

            try
            {
                await _httpClient.Delete(path);
            }
            catch (Exception)
            {
                throw new FailedToDeleteEventFromServerException();
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}