using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.History;
using Event.Interfaces;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Action = HistoryConsensus.Action;

namespace Event.Controllers
{
    /// <summary>
    /// HistoryController handles HTTP-request regarding Event History
    /// </summary>
    public class HistoryController : ApiController
    {
        private readonly IEventHistoryLogic _historyLogic;
        private readonly ILifecycleLogic _lifecycleLogic;
        private readonly IMaliciousLogic _maliciousLogic;

        /// <summary>
        /// Constructor used for dependency-injection
        /// </summary>
        /// <param name="historyLogic">Logic-layer implementing the IEventHistory interface</param>
        /// <param name="lifecycleLogic">Logic-layer implementing the ILifecycleLogic interface</param>
        /// <param name="maliciousLogic">Logic-layer implementing the IMaliciousLogic interface</param>
        public HistoryController(IEventHistoryLogic historyLogic, ILifecycleLogic lifecycleLogic, IMaliciousLogic maliciousLogic)
        {
            _historyLogic = historyLogic;
            _lifecycleLogic = lifecycleLogic;
            _maliciousLogic = maliciousLogic;
        }

        /// <summary>
        /// Get the entire History for a given Event.
        /// </summary>
        /// <param name="workflowId">The id of the Workflow in which the Event exists</param>
        /// <param name="eventId">The id of the Event, that you wish to get the history for.</param>
        /// <returns></returns>
        [Route("history/{workflowId}/{eventId}")]
        [HttpGet]
        public async Task<IEnumerable<ActionDto>> GetHistory(string workflowId, string eventId)
        {
            var history = await _historyLogic.GetHistoryForEvent(workflowId, eventId);
            if (await _maliciousLogic.IsMalicious(workflowId, eventId))
            {
                history = await _maliciousLogic.ApplyCheating(workflowId, eventId, history.ToList());
            }
            return history;
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
                case ActionType.CheckedConditon:
                    return Action.ActionType.CheckedCondition;
                case ActionType.ChecksConditon:
                    return Action.ActionType.ChecksCondition;
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
                new FSharpSet<Tuple<string, int>>(Enumerable.Empty<Tuple<string,int>>()) // Todo: Remember to add an edge to the resulting graph, from this action to the next.
            );
        }

        [HttpGet]
        [Route("history/{workflowId}/{eventId}/create")]
        public async Task<FSharpOption<Graph.Graph>> StartHistoryCreation(string workflowId, string eventId)
        {
            var history = await Produce(workflowId, eventId, new List<string>());

            if (FSharpOption<Graph.Graph>.get_IsNone(history))
            {
                return history;
            }

           return FSharpOption<Graph.Graph>.Some(History.simplify(history.Value));
        }

        [HttpGet]
        [Route("history/{workflowId}/{eventId}/collapse")]
        public async Task<FSharpOption<Graph.Graph>> Collapse(string workflowId, string eventId)
        {
            var history = await Produce(workflowId, eventId, new List<string>());

            if (FSharpOption<Graph.Graph>.get_IsNone(history))
            {
                return history;
            }

            return FSharpOption<Graph.Graph>.Some(History.collapse(history.Value));
        }

        [HttpGet]
        [Route("history/{workflowId}/{eventId}/local")]
        public async Task<Graph.Graph> GetLocal(string workflowId, string eventId)
        {
            var localHistory = (await GetHistory(workflowId, eventId)).Select(Convert).ToArray();

            var localHistoryGraph = Graph.empty;

            for (var i = 0; i < localHistory.Length; i++)
            {
                localHistoryGraph = Graph.addNode(localHistory[i], localHistoryGraph);
                if (i - 1 >= 0)
                    localHistoryGraph = Graph.addEdge(localHistory[i - 1].Id, localHistory[i].Id, localHistoryGraph);
            }

            return localHistoryGraph;
        }

        [HttpPost]
        [Route("history/{workflowId}/{eventId}/produce")]
        public async Task<FSharpOption<Graph.Graph>> Produce(string workflowId, string eventId, IEnumerable<string> traceList)
        {
            var localHistoryGraph = await GetLocal(workflowId, eventId);

            // HACK: We should have another way of fetching relations.
            var eventDto = await _lifecycleLogic.GetEventDto(workflowId, eventId);
            var relations =
                eventDto.Conditions
                .Union(eventDto.Responses)
                .Union(eventDto.Inclusions)
                .Union(eventDto.Exclusions)
                .Where(relation => !traceList.Contains(relation.Id) && relation.Id != eventId)
                .Select(relation => $"{relation.Uri.ToString()}history/{relation.WorkflowId}/{relation.Id}");
            
            return History.produce(workflowId, eventId, ToFSharpList(traceList), ToFSharpList(relations), localHistoryGraph);
        }

        /// <summary>
        /// Turns a typed IEnumerable into the corresponding FSharpList.
        /// 
        /// The list gets reversed up front because it is cons'ed together backwards.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        private static FSharpList<T> ToFSharpList<T>(IEnumerable<T> elements)
        {
            return elements.Reverse().Aggregate(FSharpList<T>.Empty, (current, element) => FSharpList<T>.Cons(element, current));
        }


        protected override void Dispose(bool disposing)
        {
            _historyLogic.Dispose();
            base.Dispose(disposing);
        }
    }
}
