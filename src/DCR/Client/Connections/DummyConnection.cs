using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.DTO.Shared;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Action = HistoryConsensus.Action;

namespace Client.Connections
{
    public class DummyConnection : IEventConnection, IServerConnection
    {
        private int _idCounter;
        public int IdCounter => _idCounter++;

        private readonly Dictionary<ServerEventDto, ICollection<ActionDto>> _historyMap = new Dictionary<ServerEventDto, ICollection<ActionDto>>();
        public ICollection<WorkflowDto> WorkflowDtos { get; set; } = new List<WorkflowDto>();


        public DummyConnection()
        {
            Initialize();
        }

        private void Initialize()
        {
            ICollection<Tuple<ServerEventDto, ICollection<ActionDto>>> list = new List<Tuple<ServerEventDto, ICollection<ActionDto>>>();

            #region Workflow1 everything good
            {
                var workflowDto = new WorkflowDto { Name = "All good Workflow", Id = IdCounter.ToString() };
                var eventDto = new ServerEventDto
                {
                    WorkflowId = workflowDto.Id,
                    EventId = IdCounter.ToString(),
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "All good event",
                    Included = true,
                    Executed = true,
                    Pending = true,
                    Roles = new List<string>()
                };
                var actions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteStart,
                        TimeStamp = 1,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteFinished,
                        TimeStamp = 2,
                        CounterpartTimeStamp = -1
                    }
                };
                AddToLists(list, eventDto, actions);
                WorkflowDtos.Add(workflowDto);
            }
            #endregion Workflow1 everything good

            #region Workflow2 local timestamp mess up
            {
                var workflowDto = new WorkflowDto { Name = "local Timestamp mixup", Id = IdCounter.ToString() };
                var eventDto = new ServerEventDto
                {
                    WorkflowId = workflowDto.Id,
                    EventId = IdCounter.ToString(),
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "local Timestamp mixup",
                    Included = true,
                    Executed = true,
                    Pending = true,
                    Roles = new List<string>()
                };
                var actions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteStart,
                        TimeStamp = 2,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteFinished,
                        TimeStamp = 1,
                        CounterpartTimeStamp = -1
                    }
                };
                AddToLists(list, eventDto, actions);
                eventDto = new ServerEventDto
                {
                    WorkflowId = workflowDto.Id,
                    EventId = IdCounter.ToString(),
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "local Timestamp mixup",
                    Included = true,
                    Executed = true,
                    Pending = true,
                    Roles = new List<string>()
                };
                actions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteStart,
                        TimeStamp = 2,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteFinished,
                        TimeStamp = 2,
                        CounterpartTimeStamp = -1
                    }
                };
                AddToLists(list, eventDto, actions);
                WorkflowDtos.Add(workflowDto);
            }
            #endregion Workflow2

            #region Workflow3 Counterpart timestamp mess up
            {
                var workflowDto = new WorkflowDto { Name = "counterpart Timestamp mixup", Id = IdCounter.ToString() };
                var eventDto = new ServerEventDto
                {
                    WorkflowId = workflowDto.Id,
                    EventId = IdCounter.ToString(),
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "local Timestamp mixup",
                    Included = true,
                    Executed = true,
                    Pending = true,
                    Roles = new List<string>()
                };
                var eventDto2 = new ServerEventDto
                {
                    WorkflowId = workflowDto.Id,
                    EventId = IdCounter.ToString(),
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto> { new EventAddressDto { WorkflowId = workflowDto.Id, Id = eventDto.EventId, Roles = new List<string>() } },
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "local Timestamp mixup",
                    Included = true,
                    Executed = true,
                    Pending = true,
                    Roles = new List<string>()
                };
                var actions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        CounterpartId = eventDto2.EventId,
                        Type = ActionType.IncludedBy,
                        TimeStamp = 3,
                        CounterpartTimeStamp = 5
                    },
                    new ActionDto
                    {
                        EventId = eventDto.EventId,
                        WorkflowId = workflowDto.Id,
                        CounterpartId = eventDto2.EventId,
                        Type = ActionType.IncludedBy,
                        TimeStamp = 6,
                        CounterpartTimeStamp = 2
                    }
                };
                var actions2 = new List<ActionDto>
                {
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteStart,
                        TimeStamp = 1,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        CounterpartId = eventDto.EventId,
                        Type = ActionType.Includes,
                        TimeStamp = 2,
                        CounterpartTimeStamp = 3
                    },
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteFinished,
                        TimeStamp = 3,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteStart,
                        TimeStamp = 4,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        CounterpartId = eventDto.EventId,
                        Type = ActionType.Includes,
                        TimeStamp = 5,
                        CounterpartTimeStamp = 6
                    },
                    new ActionDto
                    {
                        EventId = eventDto2.EventId,
                        WorkflowId = workflowDto.Id,
                        Type = ActionType.ExecuteFinished,
                        TimeStamp = 6,
                        CounterpartTimeStamp = -1
                    }
                };
                WorkflowDtos.Add(workflowDto);
                AddToLists(list, eventDto, actions);
                AddToLists(list, eventDto2, actions2);
            }
            #endregion Workflow3

            AddWorkflowDtoToMap(list);
        }

        private void AddToLists(ICollection<Tuple<ServerEventDto, ICollection<ActionDto>>> list, ServerEventDto eventDto, List<ActionDto> actions)
        {
            list.Add(new Tuple<ServerEventDto, ICollection<ActionDto>>(eventDto, actions));
        }

        private void AddWorkflowDtoToMap(ICollection<Tuple<ServerEventDto, ICollection<ActionDto>>> serverEventDtos)
        {
            foreach (var serverEventDto in serverEventDtos)
            {
                _historyMap.Add(serverEventDto.Item1, serverEventDto.Item2);
            }
        }


        // EVENT CONNECTION
        
        public Task<EventStateDto> GetState(Uri uri, string workflowId, string eventId)
        {
            throw new NotImplementedException();
        }

        public Task<Graph.Graph> GetLocalHistory(Uri uri, string workflowId, string eventId)
        {
            return Task.Run(() =>
            {

                var history =
                    _historyMap.FirstOrDefault(pair => pair.Key.WorkflowId == workflowId && pair.Key.EventId == eventId)
                        .Value.ToArray();

                var graph = Graph.empty;

                for (var index = 0; index < history.Length; index++)
                {
                    var actionDto = history[index];
                    var action = Action.create(
                        new Tuple<string, int>(actionDto.EventId, actionDto.TimeStamp),
                        new Tuple<string, int>(actionDto.CounterpartId, actionDto.CounterpartTimeStamp),
                        ConvertActionType(actionDto.Type),
                        new FSharpSet<Tuple<string, int>>(Enumerable.Empty<Tuple<string, int>>()));

                    graph = Graph.addNode(action, graph);
                    if (index > 0)
                    {
                        graph =
                            Graph.addEdge(
                                new Tuple<string, int>(history[index - 1].EventId, history[index - 1].TimeStamp),
                                action.Id, graph);
                    }
                }

                return graph;
            });
        }

        public Task ResetEvent(Uri uri, string workflowId, string eventId)
        {
            throw new NotImplementedException();
        }

        public Task Execute(Uri uri, string workflowId, string eventId, IEnumerable<string> roles)
        {
            throw new NotImplementedException();
        }

        public Task<string> Produce(Uri uri, string workflowId, string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> Collapse(Uri uri, string workflowId, string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> Create(Uri uri, string workflowId, string id)
        {
            throw new NotImplementedException();
        }

        public Task Lock(Uri uri, string workflowId, string id)
        {
            return Task.CompletedTask;
        }

        public Task Unlock(Uri uri, string workflowId, string id)
        {
            return Task.CompletedTask;
        }


        // SERVER CONNECTION

        public Task<RolesOnWorkflowsDto> Login(string username, string password)
        {
            throw new NotImplementedException();
        }


        public Task<IEnumerable<WorkflowDto>> GetWorkflows()
        {
            return Task.Run(() => (IEnumerable<WorkflowDto>)WorkflowDtos);
        }

        public Task<IEnumerable<ServerEventDto>> GetEventsFromWorkflow(string workflowId)
        {
            return Task.Run(() => _historyMap.Where(pair => pair.Key.WorkflowId == workflowId).Select(pair => pair.Key));
        }

        public void Dispose()
        {

        }

        private Action.ActionType ConvertActionType(ActionType type)
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
    }
}
