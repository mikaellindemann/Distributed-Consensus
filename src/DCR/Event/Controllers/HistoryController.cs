using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.History;
using Event.Interfaces;
using Event.Logic;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Action = HistoryConsensus.Action;

namespace Event.Controllers
{
    /// <summary>
    /// HistoryController handles HTTP-request regarding Event History
    /// </summary>
    public class HistoryController : ApiController
    {
        private readonly IEventHistoryLogic _historyLogic;

        /// <summary>
        /// Default constructor; should be used during runtime
        /// </summary>
        public HistoryController()
        {
            _historyLogic = new EventHistoryLogic();
        }

        /// <summary>
        /// Constructor used for dependency-injection
        /// </summary>
        /// <param name="historyLogic">Logic-layer implementing the IEventHistory interface</param>
        public HistoryController(IEventHistoryLogic historyLogic)
        {
            _historyLogic = historyLogic;
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
            //try 
            //{
            var toReturn = await _historyLogic.GetHistoryForEvent(workflowId, eventId);
            //await _historyLogic.SaveSuccesfullCall("GET", "GetHistory", eventId, workflowId);


            return toReturn;
            //}

            //catch (Exception e) 
            //{
            //    await _historyLogic.SaveException(e, "GET", "GetHistory", eventId, workflowId);

            //    throw;
            //}
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
                    return Action.ActionType.CheckedConditon;
                case ActionType.ChecksConditon:
                    return Action.ActionType.ChecksConditon;
                case ActionType.Locks:
                    return Action.ActionType.Locks;
                case ActionType.LockedBy:
                    return Action.ActionType.LockedBy;
                case ActionType.Unlocks:
                    return Action.ActionType.Unlocks;
                case ActionType.UnlockedBy:
                    return Action.ActionType.UnlockedBy;
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
                action.CounterPartId,
                ConvertType(action.Type),
                FSharpList<Tuple<string, int>>.Empty // Todo: Remember to add an edge to the resulting graph, from this action to the next.
            );
        }

        [HttpGet]
        [Route("history/{workflowId}/{eventId}/produce")]
        public async Task<Graph.Graph> Produce(string workflowId, string eventId)
        {
            var localHistory = (await GetHistory(workflowId, eventId)).Select(Convert).ToArray();

            var localHistoryGraph = Graph.empty;

            for (var i = 0; i < localHistory.Length - 1; i++)
            {
                var old = localHistory[i];
                localHistory[i] = Action.create(
                    old.Id,
                    old.CounterpartEventId,
                    old.Type,
                    FSharpList<Tuple<string, int>>.Cons(localHistory[i + 1].Id, FSharpList<Tuple<string, int>>.Empty));

                localHistoryGraph = Graph.addNode(localHistory[i], localHistoryGraph);
            }

            localHistoryGraph = Graph.addNode(localHistory[localHistory.Length - 1], localHistoryGraph);

            // Todo: Fetch relations, and possibly convert them to a format, that can be used for further produce requests.
            return History.produce(FSharpList<string>.Empty,  localHistoryGraph);
        }


        protected override void Dispose(bool disposing)
        {
            _historyLogic.Dispose();
            base.Dispose(disposing);
        }
    }
}
