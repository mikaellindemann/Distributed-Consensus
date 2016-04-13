using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Event.Interfaces;
using ActionModel = Event.Models.ActionModel;

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
        public EventHistoryLogic(IEventHistoryStorage storage)
        {
            _storage = storage;
        }

        public Task SaveException(Exception ex, ActionType type, string eventId = "", string workflowId = "", string counterpartId = "")
        {
            // Todo: Remove this method.
            return Task.CompletedTask;/*
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
            return models.Select(model => model.ToActionDto());
        }

        public async Task<int> SaveSuccesfullCall(ActionType type, string eventId = "", string workflowId = "", string senderId = "", int senderTimeStamp = -1)
        {
            var timestamp = senderTimeStamp != -1 ? await GetNextTimestamp(workflowId, eventId, senderTimeStamp) : await GetNextTimestamp(workflowId,eventId);
            var toSave = new ActionModel
            {
                Timestamp = timestamp,
                WorkflowId = workflowId,
                EventId = eventId,
                CounterpartId = senderId,
                CounterpartTimeStamp = senderTimeStamp,
                Type = type
            };

            await _storage.SaveHistory(toSave);
            return toSave.Timestamp;
        }

        public async Task<int> GetNextTimestamp(string workflowId, string eventId, int counterPartTimestamp)
        {
            var currentMax = (await _storage.GetHistoryForEvent(workflowId, eventId)).Max(model => model.Timestamp);
            return Math.Max(currentMax, counterPartTimestamp) + 1;
        }
        public async Task<int> GetNextTimestamp(string workflowId, string eventId)
        {
            var currentMax = (await _storage.GetHistoryForEvent(workflowId, eventId)).Max(model => model.Timestamp);
            return currentMax + 1;
        }

        public async Task<ActionDto> ReserveNext(ActionType type, string workflowId = "", string eventId = "", string counterpartId = "")
        {
            var model = await _storage.ReserveNext(new ActionModel
            {
                Type = type,
                WorkflowId = workflowId,
                EventId = eventId,
                CounterpartId = counterpartId
            });

            return model.ToActionDto();
        }

        public Task UpdateAction(ActionDto dto)
        {
            return _storage.UpdateHistory(new ActionModel
            {
                Timestamp = dto.TimeStamp,
                EventId = dto.EventId,
                WorkflowId = dto.WorkflowId,
                CounterpartTimeStamp = dto.CounterpartTimeStamp,
                CounterpartId = dto.CounterpartId,
                Type = dto.Type
            });
        }

        public async Task<bool> IsCounterpartTimeStampHigher(string workflowId, string eventId, string counterpartId, int timestamp)
        {
            return await _storage.GetHighestCounterpartTimeStamp(workflowId, eventId, counterpartId) < timestamp;
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}