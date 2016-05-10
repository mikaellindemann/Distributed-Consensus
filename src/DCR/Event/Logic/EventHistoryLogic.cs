using System;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Event.Interfaces;
using Event.Storage;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Action = HistoryConsensus.Action;
using Event.Models;

namespace Event.Logic
{
    /// <summary>
    /// EventHistoryLogic is a logic-layer, that handles logic regarding Event-history. 
    /// </summary>
    public class EventHistoryLogic : IEventHistoryLogic 
    {
        private readonly IEventHistoryStorage _storage;
        private readonly IMaliciousLogic _maliciousLogic;

        /// <summary>
        /// Default constructor
        /// </summary>
        public EventHistoryLogic(IEventHistoryStorage storage, IMaliciousLogic maliciousLogic)
        {
            _storage = storage;
            _maliciousLogic = maliciousLogic;
        }

        public async Task<Graph.Graph> GetHistory(string workflowId, string eventId)
        {
            var historyList = (await _storage.GetHistoryForEvent(workflowId, eventId));

            if (await _maliciousLogic.IsMalicious(workflowId, eventId))
            {
                historyList = await _maliciousLogic.ApplyCheating(workflowId, eventId, historyList.ToList());
            }

            var history = historyList.Select(Convert).ToArray();

            var graph = Graph.empty;

            for (var index = 0; index < history.Length; index++)
            {
                var action = history[index];
                graph = Graph.addNode(action, graph);

                if (index > 0)
                {
                    graph = Graph.addEdge(history[index - 1].Id, action.Id, graph);
                }
            }

            return graph;
        }

        public async Task<int> SaveSuccesfullCall(ActionType type, string eventId, string workflowId, string senderId, int senderTimeStamp)
        {
            var timestamp = senderTimeStamp != -1 ? await GetNextTimestamp(workflowId, eventId, senderTimeStamp) : await GetNextTimestamp(workflowId, eventId);
            var toSave = new ActionModel
            {
                Timestamp = timestamp,
                WorkflowId = workflowId,
                EventId = eventId,
                CounterpartId = senderId,
                CounterpartTimeStamp = senderTimeStamp,
                Type = type
            };

            try
            {
                await _storage.SaveHistory(toSave);
                return toSave.Timestamp;
            }

            catch (Exception)
            {
                throw new FailedToSaveHistoryException($"Failed to save history for action type: {type}");
            }
        }

        public async Task<int> GetNextTimestamp(string workflowId, string eventId, int counterPartTimestamp)
        {
            var currentMax = (await _storage.GetHistoryForEvent(workflowId, eventId)).MaxOrDefault(model => model.TimeStamp);
            return Math.Max(currentMax, counterPartTimestamp) + 1;
        }
        public async Task<int> GetNextTimestamp(string workflowId, string eventId)
        {
            var currentMax = (await _storage.GetHistoryForEvent(workflowId, eventId)).MaxOrDefault(model => model.TimeStamp);
            return currentMax + 1;
        }

        public async Task<ActionDto> ReserveNext(ActionType type, string workflowId, string eventId, string counterpartId)
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

        public async Task UpdateAction(ActionDto dto)
        {
            await _storage.UpdateHistory(new ActionModel
            {
                Timestamp = dto.TimeStamp,
                EventId = dto.EventId,
                WorkflowId = dto.WorkflowId,
                CounterpartTimeStamp = dto.CounterpartTimeStamp,
                CounterpartId = dto.CounterpartId,
                Type = dto.Type
            });
        }

        public bool IsCounterpartTimeStampHigher(string workflowId, string eventId, string counterpartId, int timestamp)
        {
            var highestTimestampForCounterpart = _storage.GetHighestCounterpartTimeStamp(workflowId, eventId, counterpartId);
            if (eventId == counterpartId)
            {
                return highestTimestampForCounterpart <= timestamp;
            }
            return highestTimestampForCounterpart < timestamp;
        }


        private static Action.ActionType ConvertType(ActionType type)
        {
            switch (type)
            {
                case ActionType.Includes:
                    return Action.ActionType.Includes;
                case ActionType.IncludedBy:
                    return Action.ActionType.IncludedBy;
                case ActionType.Excludes:
                    return Action.ActionType.Excludes;
                case ActionType.ExcludedBy:
                    return Action.ActionType.ExcludedBy;
                case ActionType.SetsPending:
                    return Action.ActionType.SetsPending;
                case ActionType.SetPendingBy:
                    return Action.ActionType.SetPendingBy;
                case ActionType.CheckedConditionBy:
                    return Action.ActionType.CheckedConditionBy;
                case ActionType.ChecksCondition:
                    return Action.ActionType.ChecksCondition;
                case ActionType.CheckedMilestoneBy:
                    return Action.ActionType.CheckedMilestoneBy;
                case ActionType.ChecksMilestone:
                    return Action.ActionType.ChecksMilestone;
                case ActionType.ExecuteStart:
                    return Action.ActionType.ExecuteStart;
                case ActionType.ExecuteFinished:
                    return Action.ActionType.ExecuteFinish;
                default:
                    throw new InvalidOperationException("Update actiontypes!");
            }
        }

        private static Action.Action Convert(ActionDto action)
        {
            return Action.create(
                new Tuple<string, int>(action.EventId, action.TimeStamp),
                new Tuple<string, int>(action.CounterpartId, action.CounterpartTimeStamp),
                ConvertType(action.Type),
                new FSharpSet<Tuple<string, int>>(Enumerable.Empty<Tuple<string, int>>()) // Todo: Remember to add an edge to the resulting graph, from this action to the next.
            );
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}