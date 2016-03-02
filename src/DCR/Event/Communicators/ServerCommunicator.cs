using System;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Tools;
using Event.Exceptions.ServerInteraction;
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
        public readonly HttpClientToolbox HttpClient;

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
            HttpClient = new HttpClientToolbox(baseAddress);
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
            HttpClient = httpClient;
        }

        public async Task PostEventToServer(EventAddressDto addressDto)
        {
            var path = $"workflows/{_workflowId}";
            try
            {
                await HttpClient.Create(path, addressDto);
            }
            catch (Exception)
            {
                throw new FailedToPostEventAtServerException();
            }
        }

        public async Task DeleteEventFromServer()
        {
            var path = $"workflows/{_workflowId}/{_eventId}";

            try
            {
                await HttpClient.Delete(path);
            }
            catch (Exception)
            {
                throw new FailedToDeleteEventFromServerException();
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}