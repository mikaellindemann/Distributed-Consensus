using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Client.Views;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;

namespace Client.ViewModels
{
    public class WorkflowViewModel : ViewModelBase, IWorkflowViewModel
    {
        private readonly WorkflowDto _workflowDto;
        private bool _resetEventRuns;
        private readonly IWorkflowListViewModel _parent;
        private readonly IEventConnection _eventConnection;
        private readonly IServerConnection _serverConnection;

        public string WorkflowId => _workflowDto.Id;

        public WorkflowViewModel(IWorkflowListViewModel parent, WorkflowDto workflowDto, IEnumerable<string> roles,
            IEventConnection eventConnection, IServerConnection serverConnection, ObservableCollection<EventViewModel> eventList)
        {
            _parent = parent;
            _workflowDto = workflowDto;
            Roles = roles;
            _eventConnection = eventConnection;
            _serverConnection = serverConnection;
            EventList = eventList;
        }

        #region Databindings

        public string Name
        {
            get { return _workflowDto.Name; }
            set
            {
                _workflowDto.Name = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<EventViewModel> EventList { get; set; }

        private EventViewModel _selectedEventViewModel;

        public EventViewModel SelectedEventViewModel
        {
            get { return _selectedEventViewModel; }
            set
            {
                _selectedEventViewModel = value;
                NotifyPropertyChanged();
            }
        }

        public string Status
        {
            get { return _parent.Status; }
            set
            {
                _parent.Status = value;
            }
        }

        public IEnumerable<string> Roles { get; }

        #endregion

        #region Actions

        public async void RefreshEvents()
        {
            var tasks = EventList.Select(async eventViewModel => await eventViewModel.GetState());
            await Task.WhenAll(tasks);
        }


        public async void GetEvents()
        {
            SelectedEventViewModel = null;
            EventList.Clear();

            var events = (await _serverConnection.GetEventsFromWorkflow(WorkflowId))
                .AsParallel()
                .Where(e => e.Roles.Intersect(Roles).Any()) //Only selects the events, the current user can execute
                .Select(eventAddressDto => new EventViewModel(eventAddressDto, this))
                .ToList();

            EventList = new ObservableCollection<EventViewModel>(events);

            SelectedEventViewModel = EventList.FirstOrDefault();

            NotifyPropertyChanged("");
        }

        /// <summary>
        /// Creates a new window with the log of the workflow
        /// </summary>
        public void GetHistory()
        {
            if (EventList != null && EventList.Count != 0)
            {
                var history = new HistorySelectView(new HistorySelectViewModel(SelectedEventViewModel, _serverConnection, _eventConnection));
                history.ShowDialog();
                //var historyView = new HistoryView(new HistoryListViewModel(WorkflowId));
                //historyView.Show();
            }
        }

        /// <summary>
        /// Creates a new window with the log of the workflow
        /// </summary>
        public void OpenMaliciousWindow()
        {
            if (EventList != null && EventList.Count != 0)
            {
                var maliciousWindow = new MaliciousView(new MaliciousViewModel(SelectedEventViewModel, _serverConnection, _eventConnection) );
                maliciousWindow.ShowDialog();
            }
        }

        public async Task DisableExecuteButtons()
        {
            await Task.WhenAll(EventList.Select(e => Task.Run(() => e.Executable = false)));
        }

        /// <summary>
        /// This method resets all the events on the workflow by deleting them and adding them again.
        /// This Method ONLY EXISTS FOR TESTING!
        /// This method is called when the button "Reset is called".
        /// </summary>
        public async void ResetWorkflow()
        {
            if (_resetEventRuns) return;
            _resetEventRuns = true;

            IEnumerable<EventAddressDto> adminEventList;
            try
            {
                adminEventList = await _serverConnection.GetEventsFromWorkflow(WorkflowId);
            }
            catch (NotFoundException)
            {
                Status = "The workflow wasn't found. Please refresh the list of workflows.";
                _resetEventRuns = false;
                return;
            }
            catch (HostNotFoundException)
            {
                Status = "The server is currently unavailable. Please try again later.";
                _resetEventRuns = false;
                return;
            }
            catch (Exception)
            {
                Status = "An unexpected error has occurred. Please refresh or try again later.";
                _resetEventRuns = false;
                return;
            }

            // Reset all the events.
            try
            {
                foreach (var eventViewModel in adminEventList)
                {
                    await _eventConnection.ResetEvent(eventViewModel.Uri, WorkflowId, eventViewModel.Id);
                }
                NotifyPropertyChanged("");
                GetEvents();
            }
            catch (NotFoundException)
            {
                Status = "One of the events wasn't found. Please refresh the list of workflows.";
            }
            catch (HostNotFoundException)
            {
                Status = "An event-server is currently unavailable. Please try again later.";
            }
            catch (Exception)
            {
                Status = "An unexpected error has occurred. Please refresh or try again later.";
            }
            _resetEventRuns = false;
        }
        #endregion
    }
}
