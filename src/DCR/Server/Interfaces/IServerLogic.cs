using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.DTO.Event;
using Common.DTO.Server;
using Common.DTO.Shared;
using Common.Exceptions;

namespace Server.Interfaces
{
    /// <summary>
    /// IServerLogic is a logic-layer that handles logic related to users-, login- and workflow operations.
    /// </summary>
    public interface IServerLogic : IDisposable
    {
        /// <summary>
        /// Tries to log in/return all the roles a given user (stored in a LoginDto) has on all workflows.
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns>The roles of the user which is logged, which can then be returned to the client for usage.</returns>
        Task<RolesOnWorkflowsDto> Login(LoginDto loginDto);

        /// <summary>
        /// Add a user.
        /// </summary>
        /// <param name="userDto">Contains information about the user.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either of the provided arguments are null.</exception>
        /// <exception cref="NotFoundException">Thrown if a role in the provided UserDto could not be found at the Server.</exception>
        Task AddUser(UserDto userDto);

        /// <summary>
        /// Adds the given roles to the given username, if not already included.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="roles">The roles to add to the user.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the username or the roles are null.</exception>
        /// <exception cref="NotFoundException">If the username does not correspond to a user,
        /// or if a role does not exist at some event in the given workflow.</exception>
        Task AddRolesToUser(string username, IEnumerable<WorkflowRole> roles);

        /// <summary>
        /// Returns a list of Events for the specified workflow.
        /// </summary>
        /// <param name="workflowId">Id of the workflow.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if workflowId is null.</exception>
        Task<IEnumerable<EventAddressDto>> GetEventsOnWorkflow(string workflowId);

        /// <summary>
        /// Adds an Event to the specified workflow.
        /// </summary>
        /// <param name="workflowToAttachToId">Id of the workflow that the Event should be added to.</param>
        /// <param name="eventToBeAddedDto">Contains information about the Event that is to be added.</param>
        /// <returns></returns>
        Task AddEventToWorkflow(string workflowToAttachToId, EventAddressDto eventToBeAddedDto);

        /// <summary>
        /// Will delete an Event from a specified workflow. 
        /// </summary>
        /// <param name="workflowId">Id of the target workflow.</param>
        /// <param name="eventId">Id of the target Event.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if either of the arguments are null.</exception>
        Task RemoveEventFromWorkflow(string workflowId, string eventId);

        /// <summary>
        /// Returns all workflows. May return an empty list. 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<WorkflowDto>> GetAllWorkflows();

        /// <summary>
        /// Returns information about the specified workflow. 
        /// </summary>
        /// <param name="workflowId">Id of the workflow</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided argument is null</exception>
        Task<WorkflowDto> GetWorkflow(string workflowId);

        /// <summary>
        /// Adds a new workflow
        /// </summary>
        /// <param name="workflow">Contains information about the workflow</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null</exception>
        Task AddNewWorkflow(WorkflowDto workflow);

        /// <summary>
        /// Deletes the specified workflow
        /// </summary>
        /// <param name="workflowId">Id of the workflow to be deleted</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null</exception>
        Task RemoveWorkflow(string workflowId);
    }
}
