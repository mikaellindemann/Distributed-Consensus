using System;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Tools;
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
        public readonly HttpClientToolbox HttpClient;

        /// <summary>
        /// Create a new EventCommunicator with no outgoing communication addresses.
        /// </summary>
        public EventCommunicator()
        {
            HttpClient = new HttpClientToolbox();
        }

        /// <summary>
        /// For testing purposes; (inject a mocked HttpClientToolbox).
        /// </summary>
        /// <param name="toolbox"> The HttpClientToolbox to use for testing purposes.</param>
        public EventCommunicator(HttpClientToolbox toolbox)
        {
            HttpClient = toolbox;
        }

        public async Task<bool> IsExecuted(Uri targetEventUri, string targetWorkflowId, string targetId, string ownId)
        {
            HttpClient.SetBaseAddress(targetEventUri);

            try
            {
                return await HttpClient.Read<bool>($"events/{targetWorkflowId}/{targetId}/executed/{ownId}");
            }
            catch (Exception)
            {
                throw new FailedToGetExecutedFromAnotherEventException();
            }
            
        }

        public async Task<bool> CheckCondition(Uri targetEventUri, string targetWorkflowId, string targetEventId, string ownId)
        {
            HttpClient.SetBaseAddress(targetEventUri);

            try
            {
                return await HttpClient.Read<bool>($"events/{targetWorkflowId}/{targetEventId}/condition/{ownId}");
            }
            catch (Exception)
            {
                throw new FailedToGetConditionFromAnotherEventException();
            }
        }

        public async Task<bool> IsIncluded(Uri targetEventUri, string targetWorkflowId, string targetId, string ownId)
        {
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                return await HttpClient.Read<bool>($"events/{targetWorkflowId}/{targetId}/included/{ownId}");
            }
            catch (Exception)
            {
                throw new FailedToGetIncludedFromAnotherEventException();
            }
        }

        public async Task SendPending(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                await HttpClient.Update($"events/{targetWorkflowId}/{targetId}/pending/true", lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdatePendingAtAnotherEventException();
            }
            
        }

        public async Task SendIncluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                await HttpClient.Update($"events/{targetWorkflowId}/{targetId}/included/true", lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdateIncludedAtAnotherEventException();
            }
            
        }

        public async Task SendExcluded(Uri targetEventUri, EventAddressDto lockDto, string targetWorkflowId, string targetId)
        {
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                await HttpClient.Update($"events/{targetWorkflowId}/{targetId}/included/false", lockDto);
            }
            catch (Exception)
            {
                throw new FailedToUpdateExcludedAtAnotherEventException();
            }  
        }

        public async Task Lock(Uri targetEventUri, LockDto lockDto, string targetWorkflowId, string targetId)
        {
            //long oldTimeout = HttpClient.HttpClient.Timeout.Ticks;
            //HttpClient.HttpClient.Timeout = new TimeSpan(0,0,10);
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                await HttpClient.Create($"events/{targetWorkflowId}/{targetId}/lock", lockDto);
            }
            catch (Exception)
            {
                throw new FailedToLockOtherEventException();
            }
            //HttpClient.HttpClient.Timeout = new TimeSpan(oldTimeout);
        }

        public async Task Unlock(Uri targetEventUri, string targetWorkflowId, string targetId, string unlockId)
        {
            HttpClient.SetBaseAddress(targetEventUri);
            try
            {
                await HttpClient.Delete($"events/{targetWorkflowId}/{targetId}/lock/{unlockId}");
            }
            catch (Exception)
            {
                throw new FailedToUnlockOtherEventException();
            }
        }

        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }

    public class FailedToGetConditionFromAnotherEventException : Exception
    {
    }
}