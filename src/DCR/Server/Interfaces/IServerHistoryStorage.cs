using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Common.DTO.History;

namespace Server.Interfaces
{
    /// <summary>
    /// IServerHistoryStorage is a Storage used for saving logging history data.
    /// </summary>
    public interface IServerHistoryStorage : IDisposable
    {
        /// <summary>
        /// Save a given HistoryModel to storage.
        /// </summary>
        /// <param name="toSave"></param>
        Task SaveHistory(HistoryModel toSave);

        Task SaveNonWorkflowSpecificHistory(HistoryModel toSave);

        /// <summary>
        /// Returns the history at the Server for the specified workflow.
        /// </summary>
        /// <param name="workflowId">Id of the workflow at the Server, whose history is to be obtained.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the argument is null.</exception>
        /// <exception cref="NotFoundException">Thrown if the workflow could not be found.</exception>
        Task<IQueryable<HistoryModel>> GetHistoryForWorkflow(string workflowId);
    }
}
