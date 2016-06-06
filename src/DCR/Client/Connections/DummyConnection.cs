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
            #region Workflow4 - Simulation error
            {
                var workflow = new WorkflowDto { Name = "Simulation error", Id = IdCounter.ToString() };

                var aId = "A";
                var bId = "B";
                var cId = "C";
                var dId = "D";

                var a = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = aId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>
                    {
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = bId
                        },
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = cId
                        }
                    },
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "A",
                    Included = true,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var b = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = bId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "B",
                    Included = false,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var c = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = cId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "C",
                    Included = false,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var d = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = dId,
                    Conditions = new List<EventAddressDto>
                    {
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = bId
                        },
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = cId
                        }
                    },
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "D",
                    Included = true,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var aActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 1,
                        Type = ActionType.ExecuteStart,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 2,
                        CounterpartId = bId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.Includes
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 3,
                        CounterpartId = cId,
                        CounterpartTimeStamp = 4,
                        Type = ActionType.Includes
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 4,
                        Type = ActionType.ExecuteFinished,
                        CounterpartTimeStamp = -1
                    },
                };

                var bActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 3,
                        CounterpartId = aId,
                        CounterpartTimeStamp = 2,
                        Type = ActionType.IncludedBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 4,
                        CounterpartId = dId,
                        CounterpartTimeStamp = 2,
                        Type = ActionType.CheckedConditionBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 5,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 6,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    }
                };
                var cActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 4,
                        CounterpartId = aId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.IncludedBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 5,
                        CounterpartId = dId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.CheckedConditionBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 6,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 7,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    }
                };

                var dActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 1,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 2,
                        CounterpartId = bId,
                        CounterpartTimeStamp = 4,
                        Type = ActionType.ChecksCondition
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 3,
                        CounterpartId = cId,
                        CounterpartTimeStamp = 5,
                        Type = ActionType.ChecksCondition
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 4,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    }
                };


                WorkflowDtos.Add(workflow);
                AddToLists(list, a, aActions);
                AddToLists(list, b, bActions);
                AddToLists(list, c, cActions);
                AddToLists(list, d, dActions);
            }
            #endregion

            #region Workflow4 - Correct
            {
                var workflow = new WorkflowDto { Name = "Correct", Id = IdCounter.ToString() };

                var aId = "A";
                var bId = "B";
                var cId = "C";
                var dId = "D";

                var a = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = aId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>
                    {
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = bId
                        },
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = cId
                        }
                    },
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "A",
                    Included = true,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var b = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = bId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "B",
                    Included = false,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var c = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = cId,
                    Conditions = new List<EventAddressDto>(),
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "C",
                    Included = false,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var d = new ServerEventDto
                {
                    WorkflowId = workflow.Id,
                    EventId = dId,
                    Conditions = new List<EventAddressDto>
                    {
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = bId
                        },
                        new EventAddressDto
                        {
                            WorkflowId = workflow.Id,
                            Id = cId
                        }
                    },
                    Exclusions = new List<EventAddressDto>(),
                    Inclusions = new List<EventAddressDto>(),
                    Responses = new List<EventAddressDto>(),
                    Milestones = new List<EventAddressDto>(),
                    Name = "D",
                    Included = true,
                    Executed = false,
                    Pending = false,
                    Roles = new List<string>()
                };

                var aActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 1,
                        Type = ActionType.ExecuteStart,
                        CounterpartTimeStamp = -1
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 2,
                        CounterpartId = bId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.Includes
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 3,
                        CounterpartId = cId,
                        CounterpartTimeStamp = 4,
                        Type = ActionType.Includes
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = aId,
                        TimeStamp = 4,
                        Type = ActionType.ExecuteFinished,
                        CounterpartTimeStamp = -1
                    },
                };

                var bActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 3,
                        CounterpartId = aId,
                        CounterpartTimeStamp = 2,
                        Type = ActionType.IncludedBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 4,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 5,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = bId,
                        TimeStamp = 6,
                        CounterpartId = dId,
                        CounterpartTimeStamp = 2,
                        Type = ActionType.CheckedConditionBy
                    },
                };
                var cActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 4,
                        CounterpartId = aId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.IncludedBy
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 5,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 6,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = cId,
                        TimeStamp = 7,
                        CounterpartId = dId,
                        CounterpartTimeStamp = 3,
                        Type = ActionType.CheckedConditionBy
                    }
                };

                var dActions = new List<ActionDto>
                {
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 1,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteStart
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 2,
                        CounterpartId = bId,
                        CounterpartTimeStamp = 6,
                        Type = ActionType.ChecksCondition
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 3,
                        CounterpartId = cId,
                        CounterpartTimeStamp = 7,
                        Type = ActionType.ChecksCondition
                    },
                    new ActionDto
                    {
                        WorkflowId = workflow.Id,
                        EventId = dId,
                        TimeStamp = 4,
                        CounterpartTimeStamp = -1,
                        Type = ActionType.ExecuteFinished
                    }
                };


                WorkflowDtos.Add(workflow);
                AddToLists(list, a, aActions);
                AddToLists(list, b, bActions);
                AddToLists(list, c, cActions);
                AddToLists(list, d, dActions);
            }
            #endregion

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
