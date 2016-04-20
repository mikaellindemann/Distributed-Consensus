using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Connections;
using Common;

namespace Client.ViewModels
{
    public class MaliciousViewModel : ViewModelBase
    {
        private readonly IMaliciousConnection _maliciousConnection;
        private string _status = "";
        private bool _canPressButtons;
        public MaliciousViewModel(EventViewModel eventViewModel, IMaliciousConnection maliciousConnection)
        {
            CanPressButtons = true;
            EventViewModel = eventViewModel;
            _maliciousConnection = maliciousConnection;

            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
        }

        public MaliciousViewModel(IMaliciousConnection maliciousConnection)
        {
            CanPressButtons = true;
            _maliciousConnection = maliciousConnection;
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

        public async void HistoryAboutOthers()
        {
            try
            {
                await _maliciousConnection.ApplyCheatingType(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId,
                    EventViewModel.Id, CheatingTypeEnum.HistoryAboutOthers);
                EventViewModel.IsEvil = true;
                Status = "Now the event is evil";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
            }
            
        }

        public async void MixUpLocalTimestamp()
        {
            try
            {
                await _maliciousConnection.ApplyCheatingType(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId,
                    EventViewModel.Id, CheatingTypeEnum.LocalTimestampOutOfOrder);
                EventViewModel.IsEvil = true;
                Status = "Now the event is evil";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
            }
            
        }
        public void Test()
        {
            Status = "Test button";
            EventViewModel.IsEvil = true; // todo remove
        }
        #endregion

    }
}
