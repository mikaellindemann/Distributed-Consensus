using System;
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
        internal readonly ServerEventDto EventAddressDto;
        private EventStateDto _eventStateDto;
        private readonly IWorkflowViewModel _parent;
        private static readonly Brush WhiteBrush, IncludedBrush, PendingBrush, ExecutedBrush, IsEvilBrush;
        private readonly IEventConnection _eventConnection;

        static EventViewModel()
        {
            // Create the brushes, and Freeze them so the UI-thread can access them.
            // Pending
            PendingBrush = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Client;component/Assets/Pending.png", UriKind.Absolute)));
            PendingBrush.Freeze();

            // Executed
            ExecutedBrush = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Client;component/Assets/Executed.png", UriKind.Absolute)));
            ExecutedBrush.Freeze();

            IsEvilBrush = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Client;component/Assets/IsEvil.png", UriKind.Absolute)));
            IsEvilBrush.Freeze();

            // Included
            IncludedBrush = new SolidColorBrush(Colors.DeepSkyBlue);
            IncludedBrush.Freeze();

            // Empty
            WhiteBrush = new SolidColorBrush(Colors.White);
            WhiteBrush.Freeze();
        }

        public EventViewModel(ServerEventDto eventAddressDto, IWorkflowViewModel workflow)
        {
            if (eventAddressDto == null || workflow == null)
            {
                throw new ArgumentNullException();
            }
            EventAddressDto = eventAddressDto;
            _parent = workflow;
            _eventStateDto = new EventStateDto();
            _eventConnection = new EventConnection();
            GetStateInternal();
        }

        public EventViewModel(IEventConnection eventConnection, ServerEventDto eventAddressDto, IWorkflowViewModel parent)
        {
            _parent = parent;
            _eventStateDto = new EventStateDto();
            EventAddressDto = eventAddressDto;
            _eventConnection = eventConnection;
        }

        #region Databindings

        public string Id
        {
            get { return EventAddressDto.EventId; }
            set
            {
                EventAddressDto.EventId = value;
                NotifyPropertyChanged();
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
                NotifyPropertyChanged();
            }
        }

        public Uri Uri
        {
            get { return EventAddressDto.Uri; }
            set
            {
                EventAddressDto.Uri = value;
                NotifyPropertyChanged();
            }
        }

        public bool Pending
        {
            get { return _eventStateDto.Pending; }
            set
            {
                _eventStateDto.Pending = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PendingColor));
            }
        }

        public Brush PendingColor => Pending ? PendingBrush : WhiteBrush;

        public bool Executed
        {
            get { return _eventStateDto.Executed; }
            set
            {
                _eventStateDto.Executed = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ExecutedColor));
            }
        }

        public Brush ExecutedColor => Executed ? ExecutedBrush : WhiteBrush;

        public bool Included
        {
            get { return _eventStateDto.Included; }
            set
            {
                _eventStateDto.Included = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IncludedColor));
            }
        }

        public Brush IncludedColor => Included ? IncludedBrush : WhiteBrush;

        public bool Executable
        {
            get { return _eventStateDto.Executable; }
            set
            {
                _eventStateDto.Executable = value;
                NotifyPropertyChanged();
            }
        }


        public bool IsEvil
        {
            get { return _eventStateDto.IsEvil; }
            set
            {
                _eventStateDto.IsEvil = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsEvilColor));
            }
        }

        public Brush IsEvilColor => IsEvil ? IsEvilBrush : WhiteBrush;

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
                await _eventConnection.Execute(EventAddressDto.Uri, _parent.WorkflowId, EventAddressDto.EventId, _parent.Roles);
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
