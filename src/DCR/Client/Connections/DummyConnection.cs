using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.DTO.Shared;
using Newtonsoft.Json;

namespace Client.Connections
{
    public class DummyConnection : IEventConnection, IServerConnection
    {
        private int _idCounter;

        public int IdCounter
        {
            get
            {
                _idCounter++;
                return _idCounter;
            }
        }

        private readonly Dictionary<ServerEventDto, ICollection<ActionDto>> _historyMap = new Dictionary<ServerEventDto, ICollection<ActionDto>>();
        public ICollection<WorkflowDto> WorkflowDtos { get; set; } = new List<WorkflowDto>();


        public DummyConnection()
        {
            Initialize();
        }

        private void Initialize()
        {
            ICollection<Tuple<ServerEventDto, ICollection<ActionDto>>> list = new List<Tuple<ServerEventDto, ICollection<ActionDto>>>();

            #region Workflow1
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
                AddToLists(workflowDto, list, eventDto, actions);
            }
            #endregion Workflow1

            #region Workflow2
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
                AddToLists(workflowDto, list, eventDto, actions);
            }
            #endregion Workflow2

            AddWorkflowDtoToMap(list);
        }

        private void AddToLists(WorkflowDto workflowDto, ICollection<Tuple<ServerEventDto, ICollection<ActionDto>>> list, ServerEventDto eventDto, List<ActionDto> actions)
        {
            WorkflowDtos.Add(workflowDto);
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

        public Task<IEnumerable<ActionDto>> GetHistory(Uri uri, string workflowId, string eventId)
        {
            return Task.Run(()=>(IEnumerable<ActionDto>)_historyMap.FirstOrDefault(pair => pair.Key.WorkflowId == workflowId && pair.Key.EventId == eventId).Value);
        }

        public Task<string> GetLocalHistory(Uri uri, string workflowId, string eventId)
        {
            var history =
                _historyMap.FirstOrDefault(pair => pair.Key.WorkflowId == workflowId && pair.Key.EventId == eventId)
                    .Value;
            return Task.Run(() => JsonConvert.SerializeObject(history));
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
            return Task.Delay(0);
        }

        public Task Unlock(Uri uri, string workflowId, string id)
        {
            return Task.Delay(0);
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
            return Task.Run(() => (IEnumerable<ServerEventDto>) _historyMap.Where(pair => pair.Key.WorkflowId == workflowId).Select(pair => pair.Key));
        }

        public Task<IEnumerable<ActionDto>> GetHistory(string workflowId)
        {
            return Task.Run(() => (IEnumerable<ActionDto>)_historyMap.Where(pair => pair.Key.WorkflowId == workflowId).Select(pair => pair.Value));
        }

        public void Dispose()
        {

        }
    }
}
