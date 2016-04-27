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

        /// <summary>
        /// Checks if the event (workflowId,eventId) is malicious (has any cheatingTypes on it)
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public async Task<bool> IsMalicious(string workflowId, string eventId)
        {
            return await _storage.IsMalicious(workflowId, eventId);
        }

        /// <summary>
        /// Applies the types of cheating of the event to the history list, implying that data is not modified in the database but only the history which is sent to the requester.
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ActionDto>> ApplyCheating(string workflowId, string eventId, IList<ActionDto> history)
        {
            var cheatingTypes = (await _storage.GetTypesOfCheating(workflowId, eventId)).Select(type => type.Type);
            var eventModel = await _storage.GetEvent(workflowId, eventId);

            var relationOut = FindARelationOutType(eventModel);
            var relationIn = FindARelationInType(history);
            foreach (var cheatingType in cheatingTypes)
            {
                
                switch (cheatingType)
                {
                    case CheatingTypeEnum.HistoryAboutOthers:
                        // maybe do so that it is placed in an execution.
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationOut.Item2,
                            CounterpartId = relationOut.Item1,
                            TimeStamp = history.Max(dto => dto.TimeStamp)+1,
                            CounterpartTimeStamp = history.Max(dto => dto.CounterpartTimeStamp)+1,
                            EventId = "Cheating"
                        });
                        break;
                    case CheatingTypeEnum.LocalTimestampOutOfOrder:
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationOut.Item2,
                            CounterpartId = relationOut.Item1,
                            TimeStamp = history.Max(dto => dto.TimeStamp)+2,
                            CounterpartTimeStamp = history.Max(dto => dto.CounterpartTimeStamp) + 1,
                            EventId = eventId
                        });
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationOut.Item2,
                            CounterpartId = relationOut.Item1,
                            TimeStamp = history.Max(dto => dto.TimeStamp)+1,
                            CounterpartTimeStamp = history.Max(dto => dto.CounterpartTimeStamp) + 2,
                            EventId = eventId
                        });
                        break;
                    case CheatingTypeEnum.CounterpartTimestampOutOfOrder:
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationOut.Item2,
                            CounterpartId = relationOut.Item1,
                            TimeStamp = history.Max(dto => dto.TimeStamp) + 1,
                            CounterpartTimeStamp = history.Max(dto => dto.CounterpartTimeStamp) + 2,
                            EventId = eventId
                        });
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationOut.Item2,
                            CounterpartId = relationOut.Item1,
                            TimeStamp = history.Max(dto => dto.TimeStamp) + 2,
                            CounterpartTimeStamp = history.Max(dto => dto.CounterpartTimeStamp) + 1,
                            EventId = eventId
                        });
                        break;
                    case CheatingTypeEnum.IncomingChangesWhileExecuting:
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = ActionType.ExecuteStart,
                            CounterpartId = "",
                            TimeStamp = history.Max(dto => dto.TimeStamp) + 1,
                            CounterpartTimeStamp = -1,
                            EventId = eventId
                        });
                        var time = Math.Max(history.Max(dto => dto.TimeStamp), history.Max(dto => dto.CounterpartTimeStamp))+2;
                        history.Add(new ActionDto
                        {
                            WorkflowId = workflowId,
                            Type = relationIn.Item2,
                            CounterpartId = relationIn.Item1,
                            TimeStamp = time+1,
                            CounterpartTimeStamp = time,
                            EventId = eventId
                        });
                        history.Add(new ActionDto
                        {

                            WorkflowId = workflowId,
                            Type = ActionType.ExecuteFinished,
                            CounterpartId = "",
                            TimeStamp = time+2,
                            CounterpartTimeStamp = -1,
                            EventId = eventId
                        });
                        break;
                    default:
                        break;
                }
            }
            return history;
        }

        /// <summary>
        /// Finds a relation out type (includes, setspending, excludes, checkcondition)
        /// </summary>
        /// <param name="eventModel"></param>
        /// <returns></returns>
        private Tuple<string, ActionType> FindARelationOutType(EventModel eventModel)
        {
            ActionType type = ActionType.ExecuteStart;
            string id = null;
            if (eventModel.ConditionUris.Count != 0)
            {
                type = ActionType.ChecksCondition;
                id = eventModel.ConditionUris.First().EventId;
            }
            else if (eventModel.InclusionUris.Count != 0)
            {
                type = ActionType.Includes;
                id = eventModel.InclusionUris.First().EventId;
            }
            else if (eventModel.ExclusionUris.Count != 0)
            {
                type = ActionType.Excludes;
                id = eventModel.ExclusionUris.First().EventId;
            }
            else if (eventModel.ResponseUris.Count != 0)
            {
                type = ActionType.SetsPending;
                id = eventModel.ResponseUris.First().EventId;
            }
            return new Tuple<string, ActionType>(id,type);
        }
        /// <summary>
        /// Finds a relation in type (includedBy, setPendingBy, excludedBy, checkedConditionBy)
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        private Tuple<string, ActionType> FindARelationInType(IList<ActionDto> history)
        {
            ActionType type = ActionType.ExecuteStart;
            string id = null;
            var actions = history.Where(dto => dto.Type == ActionType.CheckedConditionBy).ToList();
            if (actions.Count()!=0)
            {
                type = ActionType.ChecksCondition;
                id = actions[0].EventId;
                return new Tuple<string, ActionType>(id,type);
            }
            actions = history.Where(dto => dto.Type == ActionType.IncludedBy).ToList();
            if (actions.Count() != 0)
            {
                type = ActionType.Includes;
                id = actions[0].EventId;
                return new Tuple<string, ActionType>(id, type);
            }
            actions = history.Where(dto => dto.Type == ActionType.ExcludedBy).ToList();
            if (actions.Count() != 0)
            {
                type = ActionType.Excludes;
                id = actions[0].EventId;
                return new Tuple<string, ActionType>(id, type);
            }
            actions = history.Where(dto => dto.Type == ActionType.SetPendingBy).ToList();
            if (actions.Count() != 0)
            {
                type = ActionType.SetsPending;
                id = actions[0].EventId;
                return new Tuple<string, ActionType>(id, type);
            }
            return new Tuple<string, ActionType>(id, type);
        }

        /// <summary>
        /// Adds the cheatingType in cheatingDto to the database of event (workflowid,eventid).
        /// </summary>
        /// <param name="workflowId"></param>
        /// <param name="eventId"></param>
        /// <param name="cheatingDto"></param>
        /// <returns></returns>
        public async Task AddCheatingType(string workflowId, string eventId, CheatingDto cheatingDto)
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