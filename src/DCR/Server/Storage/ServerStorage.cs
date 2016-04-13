using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Server.Exceptions;
using Server.Interfaces;
using Server.Models;

namespace Server.Storage
{
    /// <summary>
    /// ServerStorage is the layer that rests on top of the actual database. 
    /// </summary>
    public class ServerStorage : IServerStorage, IServerHistoryStorage
    {
        private readonly IServerContext _db;

        public ServerStorage(IServerContext context)
        {
            _db = context;
        }

        public async Task<ServerUserModel> GetUser(string username, string password)
        {
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException();
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => string.Equals(u.Name, username));

            if (user == null) return null;

            return PasswordHasher.VerifyHashedPassword(password, user.Password) ? user : null;
        }

        public async Task AddRolesToUser(string username, IEnumerable<ServerRoleModel> roles)
        {
            if (username == null || roles == null)
            {
                throw new ArgumentNullException();
            }

            var user = await _db.Users.SingleOrDefaultAsync(u => string.Equals(u.Name, username));

            if (user == null)
            {
                throw new NotFoundException();
            }

            foreach (var role in roles)
            {
                var serverRole = await GetRole(role.Id, role.ServerWorkflowModelId);
                if (!user.ServerRolesModels.Contains(serverRole))
                {
                    user.ServerRolesModels.Add(serverRole);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<ICollection<ServerRoleModel>> Login(ServerUserModel userModel)
        {
            if (userModel == null)
            {
                throw new ArgumentNullException();
            }

            if (userModel.ServerRolesModels != null) return userModel.ServerRolesModels;

            var user = await _db.Users.FindAsync(userModel.Name);
            return user?.ServerRolesModels;
        }

        public async Task<IEnumerable<ServerEventModel>> GetEventsFromWorkflow(string workflowId)
        {
            if (workflowId == null)
            {
                throw new ArgumentNullException();
            }

            // Check whether workflow exists
            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }

            var events = from e in _db.Events
                         where workflowId == e.ServerWorkflowModelId
                         select e;
            return await events.ToListAsync();
        }

        public async Task<IEnumerable<ServerRoleModel>> AddRolesToWorkflow(IEnumerable<ServerRoleModel> roles)
        {
            if (roles == null)
            {
                throw new ArgumentNullException();
            }

            // Result contains the ServerRoleModels as EntityFramework sees them.
            var result = new List<ServerRoleModel>();
            foreach (var role in roles)
            {
                if (!await RoleExists(role))
                {
                    // We add the result of the Add call to result, because we don't want entityFramework
                    // To create another identical role when the roles are added to the events.
                    result.Add(_db.Roles.Add(role));
                }
                else
                {
                    // ReSharper says that the following two statements are required in order to do what we want.
                    var roleId = role.Id;
                    var workflowId = role.ServerWorkflowModelId;
                    // We have to find the server-representation of these roles
                    result.Add(await _db.Roles.SingleAsync(r => r.Id == roleId && r.ServerWorkflowModelId == workflowId));
                }
            }
            await _db.SaveChangesAsync();
            return result;
        }

        public async Task<ServerRoleModel> GetRole(string rolename, string workflowId)
        {
            if (rolename == null || workflowId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }

            return await _db.Roles.SingleOrDefaultAsync(role => role.Id.Equals(rolename) && role.ServerWorkflowModelId.Equals(workflowId));
        }

        public async Task<bool> UserExists(string username)
        {
            if (username == null)
            {
                throw new ArgumentNullException();
            }

            return await _db.Users.AnyAsync(u => u.Name.Equals(username));
        }

        public async Task<bool> RoleExists(ServerRoleModel role)
        {
            if (role == null)
            {
                throw new ArgumentNullException();
            }

            return await _db.Roles.AnyAsync(rr => rr.Id == role.Id
                && rr.ServerWorkflowModelId == role.ServerWorkflowModelId);
        }

        public async Task AddUser(ServerUserModel user)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            if (await UserExists(user.Name))
            {
                throw new UserExistsException();
            }

            user.Password = PasswordHasher.HashPassword(user.Password);

            _db.Users.Add(user);

            await _db.SaveChangesAsync();
        }

        public async Task AddEventToWorkflow(ServerEventModel eventToBeAddedDto)
        {
            if (eventToBeAddedDto == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(eventToBeAddedDto.ServerWorkflowModelId))
            {
                throw new NotFoundException();
            }
            if (await EventExists(eventToBeAddedDto.ServerWorkflowModelId, eventToBeAddedDto.Id))
            {
                throw new EventExistsException();
            }

            var workflows = from w in _db.Workflows
                            where eventToBeAddedDto.ServerWorkflowModelId == w.Id
                            select w;

            if (workflows.Count() != 1)
            {
                throw new IllegalStorageStateException();
            }

            _db.Events.Add(eventToBeAddedDto);

            await _db.SaveChangesAsync();
        }

        public async Task RemoveEventFromWorkflow(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }
            if (!await EventExists(workflowId, eventId))
            {
                throw new NotFoundException();
            }

            var events = from e in _db.Events
                         where e.Id == eventId
                         select e;

            var eventToRemove = await events.SingleOrDefaultAsync();

            _db.Events.Remove(eventToRemove);
            await _db.SaveChangesAsync();
        }

        public async Task<ICollection<ServerWorkflowModel>> GetAllWorkflows()
        {
            var workflows = from w in _db.Workflows select w;

            return await workflows.ToListAsync();
        }

        /// <summary>
        /// Determines whether a workflow exists in storage
        /// </summary>
        /// <param name="workflowId">Id of the workflow</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when provided argument is null</exception>
        public async Task<bool> WorkflowExists(string workflowId)
        {
            if (String.IsNullOrEmpty(workflowId))
            {
                throw new ArgumentNullException();
            }
            return await _db.Workflows.AnyAsync(workflow => workflow.Id == workflowId);
        }

        public async Task<bool> EventExists(string workflowId, string eventId)
        {
            if (workflowId == null || eventId == null)
            {
                throw new ArgumentNullException();
            }

            // Returns true if any Event matches both the workflowId and the eventId, otherwise false.
            return await _db.Events.AnyAsync(x => x.Id == eventId && x.ServerWorkflowModel.Id == workflowId);
        }

        public async Task<ServerWorkflowModel> GetWorkflow(string workflowId)
        {
            if (workflowId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }

            var workflows = from w in _db.Workflows
                            where w.Id == workflowId
                            select w;

            return await workflows.SingleOrDefaultAsync();
        }

        public async Task AddNewWorkflow(ServerWorkflowModel workflowToAdd)
        {
            if (workflowToAdd == null)
            {
                throw new ArgumentNullException();
            }
            if (await WorkflowExists(workflowToAdd.Id))
            {
                throw new WorkflowAlreadyExistsException();
            }

            _db.Workflows.Add(workflowToAdd);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWorkflow(ServerWorkflowModel replacingWorkflow)
        {
            if (replacingWorkflow == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(replacingWorkflow.Id))
            {
                throw new NotFoundException();
            }

            var workflows = from w in _db.Workflows
                            where w.Id == replacingWorkflow.Id
                            select w;

            var tempWorkflow = workflows.Single();
            tempWorkflow.Name = replacingWorkflow.Name;

            await _db.SaveChangesAsync();
        }

        public async Task RemoveWorkflow(string workflowId)
        {
            if (workflowId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }

            var workflows =
                from w in _db.Workflows
                where w.Id == workflowId
                select w;

            if (workflows.Count() > 1)
            {
                // Because workflowId's are unique identifiers, and hence, there should be only a single element in workflows. 
                throw new IllegalStorageStateException();
            }

            _db.Workflows.Remove(await workflows.SingleAsync());
            await _db.SaveChangesAsync();
        }




        public void Dispose()
        {
            _db.Dispose();
        }

        public async Task SaveHistory(ActionModel toSave)
        {
            if (toSave == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(toSave.WorkflowId))
            {
                throw new NotFoundException();
            }
            _db.History.Add(toSave);
            await _db.SaveChangesAsync();
        }

        public async Task SaveNonWorkflowSpecificHistory(ActionModel toSave)
        {
            if (toSave == null)
            {
                throw new ArgumentNullException();
            }

            _db.History.Add(toSave);
            await _db.SaveChangesAsync();
        }

        public async Task<IQueryable<ActionModel>> GetHistoryForWorkflow(string workflowId)
        {
            if (workflowId == null)
            {
                throw new ArgumentNullException();
            }

            if (!await WorkflowExists(workflowId))
            {
                throw new NotFoundException();
            }

            return _db.History.Where(h => h.WorkflowId == workflowId);
        }
    }
}