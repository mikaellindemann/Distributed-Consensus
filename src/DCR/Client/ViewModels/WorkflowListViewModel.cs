using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Client.Connections;
using Client.Exceptions;
using Common.DTO.Event;

namespace Client.ViewModels
{
    public class WorkflowListViewModel : ViewModelBase, IWorkflowListViewModel
    {
        public Action CloseAction { get; set; }
        public ObservableCollection<WorkflowViewModel> WorkflowList { get; set; }

        private WorkflowViewModel _selecteWorkflowViewModel;
        private string _status;
        private readonly Dictionary<string, ICollection<string>> _rolesForWorkflows;
        private readonly IServerConnection _serverConnection;

        public WorkflowListViewModel()
        {

        }

        public WorkflowListViewModel(Dictionary<string, ICollection<string>> rolesForWorkflows)
        {
            if (rolesForWorkflows == null)
            {
                throw new ArgumentNullException(nameof(rolesForWorkflows));
            }
            WorkflowList = new ObservableCollection<WorkflowViewModel>();

            var settings = Settings.LoadSettings();
            _rolesForWorkflows = rolesForWorkflows;

            _serverConnection = new ServerConnection(new Uri(settings.ServerAddress));

            GetWorkflows();
        }

        public WorkflowListViewModel(IServerConnection serverConnection, Dictionary<string, ICollection<string>> rolesForWorkflows, ObservableCollection<WorkflowViewModel> workflowList)
        {
            WorkflowList = workflowList;
            _rolesForWorkflows = rolesForWorkflows;
            _serverConnection = serverConnection;
        }

        #region Databindings
        public WorkflowViewModel SelectedWorkflowViewModel
        {
            get { return _selecteWorkflowViewModel; }
            set
            {
                _selecteWorkflowViewModel = value;
                NotifyPropertyChanged();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }

        public Dictionary<string, ICollection<string>> RolesForWorkflows => _rolesForWorkflows;

        #endregion

        #region Actions

        /// <summary>
        /// Is called to get all the workflows on the server. Events on the workflows are not retrieved.
        /// The method is called when the button "Refresh" is cliWcked.
        /// </summary>
        public async void GetWorkflows()
        {
            Status = "";
            SelectedWorkflowViewModel = null;
            WorkflowList.Clear();

            IEnumerable<WorkflowDto> workflows;
            try
            {
                workflows = await _serverConnection.GetWorkflows();
            }
            catch (HostNotFoundException)
            {
                Status = "The host of the server was not found. If the problem persists, contact you Flow administrator";
                return;
            }
            catch (Exception e)
            {
                Status = e.Message;
                return;
            }

            WorkflowList = new ObservableCollection<WorkflowViewModel>();

            foreach (var workflowDto in workflows)
            {
                ICollection<string> roles;
                if (!_rolesForWorkflows.TryGetValue(workflowDto.Id, out roles))
                {
                    // The user has no roles associated with this workflow.
                    continue;
                }
                WorkflowList.Add(new WorkflowViewModel(this, workflowDto, roles));
            }

            SelectedWorkflowViewModel = WorkflowList.FirstOrDefault();

            NotifyPropertyChanged("");
        }

        /// <summary>
        /// This method is called when the selection on the workflowList is changed.
        /// It gets all the event and in the end their states on the given workflow.
        /// </summary>
        public void GetEventsOnWorkflow()
        {
            SelectedWorkflowViewModel?.GetEvents();
        }

        #endregion
    }
}
