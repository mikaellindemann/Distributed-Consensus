using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Common;
using Common.DTO.History;
using Common.DTO.Shared;
using Event.Interfaces;
using Event.Models;

namespace Event.Logic
{
    public class MaliciousLogic : IMaliciousLogic
    {
        private readonly IMaliciousStorage _storage;

        public MaliciousLogic(IMaliciousStorage storage)
        {
            _storage = storage;
        }
        public void Dispose()
        {
            _storage.Dispose();
        }

        public async Task<bool> IsMalicious(string workflowId, string eventId)
        {
            return await _storage.IsMalicious(workflowId, eventId);
        }

        public async Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IEnumerable<ActionDto> history)
        {
            var cheatingTypes = (await _storage.GetTypesOfCheating(workflowId, eventId)).Select(type => type.Type);
            foreach (var cheatingType in cheatingTypes)
            {
                switch (cheatingType)
                {
                    case CheatingTypeEnum.ConterpartTimestampOutOfOrder:
                        break;
                    default:
                        break;
                }
            }
            return history;
        }

        public async Task ApplyCheatingType(string workflowId, string eventId, CheatingDto cheatingDto)
        {
            var eventModel = await _storage.GetEvent(workflowId, eventId);
            eventModel.IsEvil = true;
            // Check if the cheatingType is not already added on the event
            if (eventModel.TypesOfCheating.All(type => type.Type != cheatingDto.CheatingTypeEnum))
            {
                eventModel.TypesOfCheating.Add(new CheatingType
                {
                    Event = eventModel,
                    WorkflowId = workflowId,
                    EventId = eventId,
                    Type = cheatingDto.CheatingTypeEnum
                });
            }
            await _storage.SaveEvent(eventModel);
        }
    }
}