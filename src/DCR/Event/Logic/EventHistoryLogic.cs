using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Event.Interfaces;
using Event.Storage;

namespace Event.Logic
{
    /// <summary>
    /// EventHistoryLogic is a logic-layer, that handles logic regarding Event-history. 
    /// </summary>
    public class EventHistoryLogic : IEventHistoryLogic 
    {
        private readonly IEventHistoryStorage _storage;

        /// <summary>
        /// Default constructor
        /// </summary>
        public EventHistoryLogic()
        {
            _storage = new EventStorage();
        }

        public Task SaveException(Exception ex, ActionType type, string eventId = "", string workflowId = "", string counterpartId = "")
        {
            // Todo: Remove this method.
            return Task.Delay(0);/*
            //Don't save a null reference.
            if (ex == null) return;

            var toSave = new ActionModel
            {
                WorkflowId = workflowId,
                EventId = eventId,
                CounterpartId = null
            };

            await _storage.SaveHistory(toSave);*/
        }

        public async Task<IEnumerable<ActionDto>> GetHistoryForEvent(string workflowId, string eventId)
        {
            var models = (await _storage.GetHistoryForEvent(workflowId, eventId)).ToList();
            return models.Select(model => new ActionDto(model));
        }

        public async Task<int> SaveSuccesfullCall(ActionType type, string eventId = "", string workflowId = "", string senderId = "", int senderTimeStamp = -1)
        {
            var toSave = new ActionModel
            {
                WorkflowId = workflowId,
                EventId = eventId,
                CounterpartId = senderId,
                CounterpartTimeStamp = senderTimeStamp,
                Type = type
            };

            await _storage.SaveHistory(toSave);
            return toSave.Id;
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}