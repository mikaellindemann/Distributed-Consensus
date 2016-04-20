using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Common.DTO.History;
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

        public async Task HistoryAboutOthers(string workflowId, string eventId)
        {
            var eventModel = await _storage.GetEvent(workflowId, eventId);
            eventModel.IsEvil = true;
            await _storage.SaveEvent(eventModel);
        }

        public async Task MixUpLocalTimestamp(string workflowId, string eventId)
        {
            var eventModel = await _storage.GetEvent(workflowId, eventId);
            eventModel.IsEvil = true;
            await _storage.SaveEvent(eventModel);
        }
    }
}