using System.Collections.ObjectModel;

namespace Client.ViewModels
{
    public interface IWorkflowListViewModel
    {
        string Status { get; set; }
        ObservableCollection<WorkflowViewModel> WorkflowList { get; set; }
    }
}
