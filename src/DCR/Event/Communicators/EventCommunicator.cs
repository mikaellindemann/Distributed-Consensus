using System;
using System.Threading.Tasks;
using Common;
using Common.DTO.Shared;
using Common.Tools;
using Event.Exceptions;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;
using Event.Models;

namespace Event.Communicators
{
    /// <summary>
    /// EventCommunicator handles the outgoing communication from an Event to another Event.
    /// </summary>
    public class EventCommunicator : IEventFromEvent
    {
        public readonly HttpClientToolbox _httpClient;

        /// <summary>
        /// Create a new EventCommunicator with no outgoing communication addresses.
        /// </summary>
        public EventCommunicator()
        {
            _httpClient = new HttpClientToolbox();
        }

        /// <summary>
        /// For testing purposes; (inject a mocked HttpClientToolbox).
        /// </summary>
        /// <param name="toolbox"> The HttpClientToolbox to use for testing purposes.</param>
        public EventCommunicator(HttpClientToolbox toolbox)
        {
            _httpClient = toolbox;
        }

        public async Task<bool> IsExecuted(Uri targetEventUri, string targetWorkflowId, string targetId, string ownId)
        {
            _httpClient.SetBaseAddress(targetEventUri);

            try
            {
                return await _httpClient.Read<bool>(String.Format("events/{0}/{1}/executed/{2}", targetWorkflowId, targetId, ownId));
            }
            catch (Exception)
            {
                throw new FailedToGetExecutedFromAnotherEventException();
            }
            
        }

        public async Task<bool> IsIncluded(Uri targetEventUri, string targetWorkflowId, string targetId, string ownId)
        {
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                return await _httpClient.Read<bool>(String.Format("events/{0}/{1}/included/{2}", targetWorkflowId, targetId, ownId));
            }
            catch (Exception)
            {
                throw new FailedToGetIncludedFromAnotherEventException();
            }
        }

        public async Task SendPending(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                await _httpClient.Update(String.Format("events/{0}/{1}/pending/true", targetWorkflowId, targetId), lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdatePendingAtAnotherEventException();
            }
            
        }

        public async Task SendIncluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                await _httpClient.Update(String.Format("events/{0}/{1}/included/true", targetWorkflowId, targetId), lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdateIncludedAtAnotherEventException();
            }
            
        }

        public async Task SendExcluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                await _httpClient.Update(string.Format("events/{0}/{1}/included/false", targetWorkflowId, targetId), lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdateExcludedAtAnotherEventException();
            }  
        }

        public async Task Lock(Uri targetEventUri, LockDto lockDto, string targetWorkflowId, string targetId)
        {
            //long oldTimeout = _httpClient.HttpClient.Timeout.Ticks;
            //_httpClient.HttpClient.Timeout = new TimeSpan(0,0,10);
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                await _httpClient.Create(string.Format("events/{0}/{1}/lock", targetWorkflowId, targetId), lockDto);
            }
            catch (Exception)
            {
                throw new FailedToLockOtherEventException();
            }
            //_httpClient.HttpClient.Timeout = new TimeSpan(oldTimeout);
        }

        public async Task Unlock(Uri targetEventUri, string targetWorkflowId, string targetId, string unlockId)
        {
            _httpClient.SetBaseAddress(targetEventUri);
            try
            {
                await _httpClient.Delete(String.Format("events/{0}/{1}/lock/{2}", targetWorkflowId, targetId, unlockId));
            }
            catch (Exception)
            {
                throw new FailedToUnlockOtherEventException();
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}