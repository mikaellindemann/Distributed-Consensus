using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.DTO.History;
using Server.Models;

namespace Server.Interfaces
{
    /// <summary>
    /// Logic layer that handles operations related to history ('logging')
    /// </summary>
    public interface IWorkflowHistoryLogic : IDisposable
    {
        /// <summary>
        /// Returns the Server-history for the specified workflow. 
        /// </summary>
        /// <param name="workflowId">Id of the workflow, whose history is to be obtained</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null</exception>
        Task<IEnumerable<ActionDto>> GetHistoryForWorkflow(string workflowId);

        /// <summary>
        /// Saves the history given in the provided toSave.
        /// </summary>
        /// <param name="toSave">Contains the information about the history that should be saved</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null</exception>
        Task SaveHistory(ActionModel toSave);

        /// <summary>
        /// Saves a history that is non-specific to a workflow. 
        /// </summary>
        /// <param name="toSave">Information to be saved</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null</exception>
        Task SaveNoneWorkflowSpecificHistory(ActionModel toSave);
    }
}
