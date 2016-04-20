using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Server.Interfaces;
using Server.Models;
using Server.Storage;

namespace Server.Logic
{
    /// <summary>
    /// Logic layer that handles operations related to history ('logging')
    /// </summary>
    public class WorkflowHistoryLogic : IWorkflowHistoryLogic
    {
        private readonly IServerHistoryStorage _storage;

        /// <summary>
        /// Dependency injection constructor for testing purposes. 
        /// </summary>
        public WorkflowHistoryLogic(IServerHistoryStorage storage)
        {
            _storage = storage;
        }

        public async Task<IEnumerable<ActionDto>> GetHistoryForWorkflow(string workflowId)
        {
            if (workflowId == null)
            {
                throw new ArgumentNullException();
            }

            var models = (await _storage.GetHistoryForWorkflow(workflowId)).ToList();
            return models.Select(model => model.ToActionDto());
        }

        public async Task SaveHistory(ActionModel toSave)
        {
            if (toSave == null)
            {
                throw new ArgumentNullException();
            }

            await _storage.SaveHistory(toSave);
        }

        public async Task SaveNoneWorkflowSpecificHistory(ActionModel toSave)
        {
            if (toSave == null)
            {
                throw new ArgumentNullException();
            }

            await _storage.SaveNonWorkflowSpecificHistory(toSave);
        }

        public void Dispose()
        {
            _storage.Dispose();
        }
    }
}