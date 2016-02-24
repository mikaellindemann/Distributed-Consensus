using System;
using System.Globalization;
using Common.DTO.History;

namespace Client.ViewModels
{
    public class HistoryViewModel : ViewModelBase
    {
        private readonly ActionDto _actionDto;
        public HistoryViewModel()
        {
            _actionDto = new ActionDto();
        }
        public HistoryViewModel(ActionDto actionDto)
        {
            if (actionDto == null)
            {
                throw new ArgumentNullException(nameof(actionDto));
            }
            _actionDto = actionDto;
        }

        #region DataBindings
        public string WorkflowId
        {
            get { return _actionDto.WorkflowId; }
            set
            {
                _actionDto.WorkflowId = value;
                NotifyPropertyChanged("WorkflowId");
            }
        }

        public string EventId
        {
            get { return _actionDto.EventId; }
            set
            {
                _actionDto.EventId = value;
                NotifyPropertyChanged("EventId");
            }
        }

        private string _title = "";
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public string Message
        {
            get { return _actionDto.Message; }
            set
            {
                _actionDto.Message = value;
                NotifyPropertyChanged("Message");
            }
        }

        public DateTime TimeStamp
        {
            get { return DateTime.Parse(_actionDto.TimeStamp, new DateTimeFormatInfo()); }
            set
            {
                _actionDto.TimeStamp = value.ToString(CultureInfo.InvariantCulture);
                NotifyPropertyChanged("TimeStamp"); }
        }
        
        #endregion
    }
}
