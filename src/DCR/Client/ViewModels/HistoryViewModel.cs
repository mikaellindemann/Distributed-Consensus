using System;
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
                NotifyPropertyChanged();
            }
        }

        public string EventId
        {
            get { return _actionDto.EventId; }
            set
            {
                _actionDto.EventId = value;
                NotifyPropertyChanged();
            }
        }

        public string CounterpartId
        {
            get { return _actionDto.CounterpartId; }
            set
            {
                _actionDto.CounterpartId = value;
                NotifyPropertyChanged();
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
                NotifyPropertyChanged();
            }
        }

        public int TimeStamp
        {
            get { return _actionDto.TimeStamp; }
            set
            {
                _actionDto.TimeStamp = value;
                NotifyPropertyChanged(); }
        }
        
        #endregion
    }
}
