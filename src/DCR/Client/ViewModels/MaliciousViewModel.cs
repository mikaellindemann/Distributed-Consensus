using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Connections;

namespace Client.ViewModels
{
    public class MaliciousViewModel : ViewModelBase
    {
        private readonly IEventConnection _eventConnection;
        private readonly IServerConnection _serverConnection;
        private string _status = "";
        private bool _canPressButtons;
        public MaliciousViewModel(EventViewModel eventViewModel, IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            EventViewModel = eventViewModel;
            _serverConnection = serverConnection;
            _eventConnection = eventConnection;

            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
        }

        public MaliciousViewModel(IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            _serverConnection = serverConnection;
            _eventConnection = eventConnection;
        }

        public MaliciousViewModel()
        {
        }

        public EventViewModel EventViewModel { get; set; }

        #region databindings
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged();
            }
        }
        public bool CanPressButtons
        {
            get { return _canPressButtons; }
            set
            {
                if (_canPressButtons == value) return;
                _canPressButtons = value;
                NotifyPropertyChanged();
            }
        }
        #endregion

        #region Actions

        public void Test()
        {
            Status = "Button Pressed";
        }
        #endregion

    }
}
