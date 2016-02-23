using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Exceptions;
using Server.Exceptions;
using Server.Models;

namespace Server.Interfaces
{
    /// <summary>
    /// IServerStorage is the layer that rests on top of a storage-facility. 
    /// </summary>
    public interface IServerStorage : IDisposable
    {
        #region Workflow related
        /// <summary>
        /// Gets the Events within the specified workflow.
        /// </summary>
        /// <param name="workflowId">The id of the workflow, whose Events are to be returned</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided argument is null</exception>
        /// <exception cref="NotFoundException">Thrown if the workflow does not exist at Server</exception>
        Task<IEnumerable<ServerEventModel>> GetEventsFromWorkflow(string workflowId);

        /// <summary>
        /// Adds the given roles to a workflow, and returns the server-representation of the added roles.
        /// </summary>
        /// <param name="roles">The roles to add to a workflow.</param>
        /// <returns>A collection of the server-representation of the workflows.</returns>
        Task<IEnumerable<ServerRoleModel>> AddRolesToWorkflow(IEnumerable<ServerRoleModel> roles);

        /// <summary>
        /// Adds an Event to a specified workflow. The information needed to identify what workflow, the Event should be added to,
        /// is held within argument. 
        /// </summary>
        /// <param name="eventToBeAddedDto">Holds information about a) the Event, that is to be added and 
        /// b) the workflow, the Event should be added to.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Trhown if the provided argument is null</exception>
        /// <exception cref="NotFoundException">Thrown if the workflow could not be found</exception>
        /// <exception cref="EventExistsException">Thrown if the Event already exists</exception>
        /// <exception cref="IllegalStorageStateException">Thrown if the storage was found to be in an illegal state</exception>
        Task AddEventToWorkflow(ServerEventModel eventToBeAddedDto);

        /// <summary>
        /// Will delete the Event from the specified workflow
        /// </summary>
        /// <param name="workflowId">Id if the workflow</param>
        /// <param name="eventId">Id of the Event</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either of the arguments are null</exception>
        /// <exception cref="NotFoundException">Thrown if either the workflow or the event could not be found</exception>
        Task RemoveEventFromWorkflow(string workflowId, string eventId);

        /// <summary>
        /// Returns all workflows held in storage. 
        /// </summary>
        /// <returns></returns>
        Task<ICollection<ServerWorkflowModel>> GetAllWorkflows();

        /// <summary>
        /// Determines whether a workflow exists in storage
        /// </summary>
        /// <param name="workflowId">Id of the workflow</param>
        /// <returns></returns>
        Task<bool> WorkflowExists(string workflowId);

        /// <summary>
        /// Checks whether an Event exists or not in the database
        /// </summary>
        /// <param name="workflowId">Id of the workflow, the Event belongs to</param>
        /// <param name="eventId">Id of the Event</param>
        /// <returns></returns>
        Task<bool> EventExists(string workflowId, string eventId);

        /// <summary>
        /// Returns the specified workflow. 
        /// </summary>
        /// <param name="workflowId"></param>
        /// <returns></returns>
        Task<ServerWorkflowModel> GetWorkflow(string workflowId);

        /// <summary>
        /// Adds a new workflow. 
        /// </summary>
        /// <param name="workflow">Represents the workflow that is to be added</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided ServerWorkflowModel is null</exception>
        /// <exception cref="WorkflowAlreadyExistsException">Thrown if the workflow already exists</exception>
        Task AddNewWorkflow(ServerWorkflowModel workflow);

        /// <summary>
        /// Updates a workflow
        /// </summary>
        /// <param name="workflow">The replacing workflow</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided argument is null</exception>
        /// <exception cref="NotFoundException">Thrown if the workflow was not found</exception>
        Task UpdateWorkflow(ServerWorkflowModel workflow);

        /// <summary>
        /// Deletes a workflow
        /// </summary>
        /// <param name="workflowId">Id of the workflow to be deleted</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided argument is null</exception>
        /// <exception cref="NotFoundException">Thrown if the workflow was not found</exception>
        /// <exception cref="IllegalStorageStateException">Thrown if the storage was found to be in an illegal state</exception>
        Task RemoveWorkflow(string workflowId);

        #endregion

        #region User related
        /// <summary>
        /// Adds a user to Server. Server holds a hashed value for the password. 
        /// </summary>
        /// <param name="user">Holds the logininformation about the user</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided input is null</exception>
        /// <exception cref="UserExistsException">Thrown if the user already exists at Server</exception>
        Task AddUser(ServerUserModel user);

        /// <summary>
        /// States whether the given role exists on the server already or not.
        /// </summary>
        /// <param name="role">The role to test</param>
        /// <returns>True if the role exists on the server, false otherwise.</returns>
        Task<bool> RoleExists(ServerRoleModel role);

        /// <summary>
        /// States whether a given username already exists at Server
        /// </summary>
        /// <param name="username">The username to check for</param>
        /// <returns></returns>
        Task<bool> UserExists(string username);

        /// <summary>
        /// Get the server-representation of a role
        /// </summary>
        /// <param name="rolename">The name of the role. This is what identifies the role.</param>
        /// <param name="workflowId">The Id of the Workflow the role belongs to.</param>
        /// <returns>If found, the server-representation of the role. null otherwise.</returns>
        Task<ServerRoleModel> GetRole(string rolename, string workflowId);

        /// <summary>
        /// Returns the user, if he/she exists, and the provided password matches. 
        /// Returns null if no user is found. 
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Claimed password of the user</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null</exception>
        Task<ServerUserModel> GetUser(string username, string password);

        /// <summary>
        /// Adds the given roles to the user with username, if the user does not already have them.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="roles">The roles to add to the user.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If username or roles are null.</exception>
        /// <exception cref="NotFoundException">If username does not match a user.</exception>
        Task AddRolesToUser(string username, IEnumerable<ServerRoleModel> roles);

        /// <summary>
        /// Attempts to login using the provided ServerUserModel
        /// </summary>
        /// <param name="userModel">Represents the user</param>
        /// <returns>The roles associated with the user.</returns>
        Task<ICollection<ServerRoleModel>> Login(ServerUserModel userModel);
        #endregion
    }
}
