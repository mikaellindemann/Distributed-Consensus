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

        public async Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IList<ActionDto> history)
        {
            var cheatingTypes = (await _storage.GetTypesOfCheating(workflowId, eventId)).Select(type => type.Type);
            foreach (var cheatingType in cheatingTypes)
            {
                switch (cheatingType)
                {
                    case CheatingTypeEnum.HistoryAboutOthers:
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = ActionType.Excludes,
                            CounterpartId = eventId,
                            TimeStamp = 100,
                            CounterpartTimeStamp = 101,
                            EventId = "Cheating"
                        });
                        break;
                    case CheatingTypeEnum.LocalTimestampOutOfOrder:
                        if (history.Count < 2)
                        {
                            history.Add(new ActionDto
                            {
                                WorkflowId = workflowId,
                                Type = ActionType.Excludes,
                                CounterpartId = eventId,
                                TimeStamp = 100,
                                CounterpartTimeStamp = 101,
                                EventId = "Cheating"
                            });
                            history.Add(new ActionDto
                            {
                                WorkflowId = eventId,
                                Type = ActionType.Excludes,
                                CounterpartId = eventId,
                                TimeStamp = 98,
                                CounterpartTimeStamp = 102,
                                EventId = "Cheating"
                            });
                        }
                        else
                        {
                            var temp = history[0];
                            history[0] = history[1];
                            history[1] = temp;
                        }
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