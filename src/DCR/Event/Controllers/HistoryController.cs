using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Event.Interfaces;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Event.Controllers
{
    /// <summary>
    /// HistoryController handles HTTP-request regarding Event History
    /// </summary>
    public class HistoryController : ApiController
    {
        private readonly IEventHistoryLogic _historyLogic;
        private readonly ILifecycleLogic _lifecycleLogic;

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
            return await _historyLogic.GetHistory(workflowId, eventId);
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
