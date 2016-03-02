using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public interface IWorkflowViewModel
    {
        string Status { get; set; }
        string WorkflowId { get; }
        IEnumerable<string> Roles { get; }
        Task DisableExecuteButtons();
        void RefreshEvents();
    }
}
