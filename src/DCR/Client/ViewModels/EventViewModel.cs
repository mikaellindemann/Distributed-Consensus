using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Client.Connections;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;

namespace Client.ViewModels
{
    public class EventViewModel : ViewModelBase
    {
        private readonly EventAddressDto _eventAddressDto;
        private EventStateDto _eventStateDto;
        private readonly IWorkflowViewModel _parent;
        private static readonly Brush WhiteBrush, IncludedBrush, PendingBrush, ExecutedBrush;
        private readonly IEventConnection _eventConnection;

        static EventViewModel()
        {
            // Create the brushes, and Freeze them so the UI-thread can access them.
            // Pending
            var path = Path.Combine(Environment.CurrentDirectory, "Assets", "Pending.png");
            var uri = new Uri(path);
            PendingBrush = new ImageBrush(new BitmapImage(uri));
            PendingBrush.Freeze();

            // Executed
            path = Path.Combine(Environment.CurrentDirectory, "Assets", "Executed.png");
            uri = new Uri(path);
            ExecutedBrush = new ImageBrush(new BitmapImage(uri));
            ExecutedBrush.Freeze();

            // Included
            IncludedBrush = new SolidColorBrush(Colors.DeepSkyBlue);
            IncludedBrush.Freeze();

            // Empty
            WhiteBrush = new SolidColorBrush(Colors.White);
            WhiteBrush.Freeze();
        }

        public EventViewModel(EventAddressDto eventAddressDto, IWorkflowViewModel workflow)
        {
            if (eventAddressDto == null || workflow == null)
            {
                throw new ArgumentNullException();
            }
            _eventAddressDto = eventAddressDto;
            _parent = workflow;
            _eventStateDto = new EventStateDto();
            _eventConnection = new EventConnection();
            GetStateInternal();
        }

        public EventViewModel(IEventConnection eventConnection, EventAddressDto eventAddressDto, IWorkflowViewModel parent)
        {
            _parent = parent;
            _eventStateDto = new EventStateDto();
            _eventAddressDto = eventAddressDto;
            _eventConnection = eventConnection;
        }

        #region Databindings

        public string Id
        {
            get { return _eventAddressDto.Id; }
            set
            {
                _eventAddressDto.Id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public string Name
        {
            get
            {
                return _eventStateDto.Name;
            }
            set
            {
                _eventStateDto.Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public Uri Uri
        {
            get { return _eventAddressDto.Uri; }
            set
            {
                _eventAddressDto.Uri = value;
                NotifyPropertyChanged("Uri");
            }
        }

        public bool Pending
        {
            get { return _eventStateDto.Pending; }
            set
            {
                _eventStateDto.Pending = value;
                NotifyPropertyChanged("Pending");
                NotifyPropertyChanged("PendingColor");
            }
        }

        public Brush PendingColor
        {
            get { return Pending ? PendingBrush : WhiteBrush; }
        }

        public bool Executed
        {
            get { return _eventStateDto.Executed; }
            set
            {
                _eventStateDto.Executed = value;
                NotifyPropertyChanged("Executed");
                NotifyPropertyChanged("ExecutedColor");
            }
        }

        public Brush ExecutedColor
        {
            get { return Executed ? ExecutedBrush : WhiteBrush; }
        }
        public bool Included
        {
            get { return _eventStateDto.Included; }
            set
            {
                _eventStateDto.Included = value;
                NotifyPropertyChanged("Included");
                NotifyPropertyChanged("IncludedColor");
            }
        }

        public Brush IncludedColor
        {
            get { return Included ? IncludedBrush : WhiteBrush; }
        }

        public bool Executable
        {
            get { return _eventStateDto.Executable; }
            set
            {
                _eventStateDto.Executable = value;
                NotifyPropertyChanged("Executable");
            }
        }

        public string Status
        {
            get { return _parent.Status; }
            set { _parent.Status = value; }
        }

        #endregion

        #region Actions

        private async void GetStateInternal()
        {
            await GetState();
        }

        public async Task GetState()
        {
            Status = "";
            try
            {
                _eventStateDto = await _eventConnection.GetState(Uri, _parent.WorkflowId, Id);
                NotifyPropertyChanged("");
            }
            catch (NotFoundException)
            {
                Status = "The event could not be found. Please refresh the workflow";
            }
            catch (LockedException)
            {
                Status = "The event is currently locked. Please try again later.";
            }
            catch (HostNotFoundException)
            {
                Status =
                    "The host of the event was not found. Please refresh the workflow. If the problem persists, contact you Flow administrator";
            }
            catch (Exception e)
            {
                Status = e.Message;
            }
        }

        /// <summary>
        /// This method gets called by the Execute Button in the UI
        /// </summary>
        public async void Execute()
        {
            Status = "";
            await _parent.DisableExecuteButtons();
            try
            {
                await _eventConnection.Execute(_eventAddressDto.Uri, _parent.WorkflowId, _eventAddressDto.Id, _parent.Roles);
                _parent.RefreshEvents();
            }
            catch (NotFoundException)
            {
                Status = "The event could not be found. Please refresh the workflow";
            }
            catch (UnauthorizedException)
            {
                Status = "You do not have the rights to execute this event";
            }
            catch (LockedException)
            {
                Status = "The event is currently locked. Please try again later.";
            }
            catch (NotExecutableException)
            {
                Status = "The event is currently not executable. Please refresh the workflow";
            }
            catch (HostNotFoundException)
            {
                Status =
                    "The host of the event was not found. Please refresh the workflow. If the problem persists, contact you Flow administrator";
            }
            catch (Exception e)
            {
                Status = e.Message;
            }
        }
        #endregion
    }
}
