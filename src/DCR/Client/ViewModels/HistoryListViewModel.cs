using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Common.DTO.Shared;
using Common.Exceptions;

namespace Client.ViewModels
{
    public class HistoryListViewModel : ViewModelBase
    {
        private readonly IServerConnection _serverConnection;
        private readonly IEventConnection _eventConnection;

        public HistoryListViewModel()
        {
            HistoryViewModelList = new ObservableCollection<HistoryViewModel>();
            var settings = Settings.LoadSettings();
            var serverAddress = new Uri(settings.ServerAddress);
            _serverConnection = new ServerConnection(serverAddress);
            _eventConnection = new EventConnection();
        }

        public HistoryListViewModel(string workflowId)
        {
            HistoryViewModelList = new ObservableCollection<HistoryViewModel>();
            WorkflowId = workflowId;

            var settings = Settings.LoadSettings();
            var serverAddress = new Uri(settings.ServerAddress);

            _serverConnection = new ServerConnection(serverAddress);
            _eventConnection = new EventConnection();

            GetHistory();
        }

        public HistoryListViewModel(string workflowId, IServerConnection serverConnection, IEventConnection eventConnection)
        {
            HistoryViewModelList = new ObservableCollection<HistoryViewModel>();
            WorkflowId = workflowId;

            _serverConnection = serverConnection;
            _eventConnection = eventConnection;

            GetHistory();
        }

        #region DataBindings

        private string _workflowId;
        private string _status;

        public string WorkflowId
        {
            get { return _workflowId; }
            set
            {
                _workflowId = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        public ObservableCollection<HistoryViewModel> HistoryViewModelList { get; set; }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }

        #region Actions

        /// <summary>
        /// Gets the history of the workflow and the events on it. 
        /// orders the list by timestamp
        /// </summary>
        /// <returns></returns>
        public async void GetHistory()
        {
            HistoryViewModelList.Clear();
            NotifyPropertyChanged("");

            IEnumerable<ServerEventDto> eventAddresses;
            ConcurrentBag<HistoryViewModel> history;

            try
            {
                // create a server connection
                // get all addresses of events. This is neccesary since events might not be present if Adam removes events due to roles.
                eventAddresses = await _serverConnection.GetEventsFromWorkflow(WorkflowId);

                // add the history of the server
                history =
                    new ConcurrentBag<HistoryViewModel>(
                        (await _serverConnection.GetHistory(WorkflowId)).Select(
                            dto => new HistoryViewModel(dto) { Title = WorkflowId }));
            }
            catch (NotFoundException)
            {
                Status = "Workflow wasn't found on server. Please refresh the workflow and try again.";
                return;
            }
            catch (HostNotFoundException)
            {
                Status = "The server could not be found. Please try again later or contact your Flow administrator";
                return;
            }
            catch (Exception)
            {
                Status = "An unexpected error has occured. Please try again later.";
                return;
            }

            var tasks = eventAddresses.Select(async dto =>
            {
                var list =
                    (await _eventConnection.GetHistory(dto.Uri, WorkflowId, dto.EventId)).Select(
                        historyDto => new HistoryViewModel(historyDto) { Title = dto.EventId });
                list.ToList().ForEach(history.Add);
            });

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (NotFoundException)
            {
                Status = "An event wasn't found. Please refresh the workflow and try again.";
                return;
            }
            catch (HostNotFoundException)
            {
                Status = "An event-server could not be found. Please try again later or contact your Flow administrator";
                return;
            }
            catch (Exception)
            {
                Status = "An unexpected error has occured. Please try again later.";
                return;
            }

            // order them by timestamp
            var orderedHistory = history.ToList().OrderByDescending(model => model.TimeStamp);

            // move the list into the observable collection.
            HistoryViewModelList = new ObservableCollection<HistoryViewModel>(orderedHistory);
            NotifyPropertyChanged("");
        }
        #endregion
    }
}
