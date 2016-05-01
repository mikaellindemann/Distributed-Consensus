using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.DTO.Shared;

namespace Client.Connections
{
    public class DummyConnection : IEventConnection, IServerConnection
    {
        public DummyConnection()
        {
            
        }

        public IEnumerable<WorkflowDto> DummyWorkflows { get; set; } = new List<WorkflowDto>();

        // EVENT CONNECTION
        
        public Task<EventStateDto> GetState(Uri uri, string workflowId, string eventId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ActionDto>> GetHistory(Uri uri, string workflowId, string eventId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetLocalHistory(Uri uri, string workflowId, string eventId)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Task Unlock(Uri uri, string workflowId, string id)
        {
            throw new NotImplementedException();
        }


        // SERVER CONNECTION

        public Task<RolesOnWorkflowsDto> Login(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkflowDto>> GetWorkflows()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ServerEventDto>> GetEventsFromWorkflow(string workflowId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ActionDto>> GetHistory(string workflowId)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }
    }
}
