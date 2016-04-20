using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.Server;
using Common.Exceptions;
using Common.Tools;

namespace Client.Services
{
    public class EventUploader
    {
        private readonly string _workflow;
        private readonly string _serverAddress;
        private readonly Dictionary<string, string> _ips;
        public EventUploader(string workflow, string serverAddress, Dictionary<string, string> ips)
        {
            _workflow = workflow;
            _serverAddress = serverAddress;
            _ips = ips;
        }

        public async Task CreateWorkflow(string workflowDescription)
        {
            var tool = new HttpClientToolbox(_serverAddress);
            await tool.Create("workflows", new WorkflowDto { Id = _workflow, Name = workflowDescription });
        }

        public async Task Upload(IList<EventDto> events)
        {
            foreach (var e in events)
            {
                var tool = new HttpClientToolbox(_ips[e.EventId]);
                await tool.Create("events", e);
            }
        }
        //THIS MUST HAPPEN after Upload()
        public async Task<bool> UploadUsers(IEnumerable<string> roles, string password)
        {
            var usersCreated = true;
            var tool = new HttpClientToolbox(_serverAddress);
            foreach (var user in roles.Select(r => new UserDto
            {
                Name = r,
                Password = password,
                Roles = new List<WorkflowRole>
                {
                    new WorkflowRole
                    {
                        Role = r, 
                        Workflow = _workflow
                    }
                }
                
            }))
            {
                var userCreated = true;
                try
                {
                    await tool.Create("users", user);
                }
                catch (LockedException)
                {
                    // LockedException in this case means that a user with that username is exiting.
                    // Therefore we add the roles instead.
                    userCreated = false;
                    usersCreated = false;
                }
                if (!userCreated)
                {
                    await AddRolesToUser(tool, user.Name, user.Roles);
                }
            }
            return usersCreated;
        }

        private static async Task AddRolesToUser(HttpClientToolbox tool, string username, IEnumerable<WorkflowRole> roles)
        {
            await tool.Create($"users/{username}/roles", roles);
        }
    }
}
