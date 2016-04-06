using System.IO;
using Client.Connections;
using GraphOptionToGravizo;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase
    {
        public EventViewModel EventViewModel { get; set; }
        private string _status = "";
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyPropertyChanged("Status");
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

        private readonly IEventConnection _connection;
        private bool _canPressButtons;

        public HistorySelectViewModel(EventViewModel eventViewModel)
        {
            CanPressButtons = true;
            EventViewModel = eventViewModel;
            _connection = new EventConnection();
        }

        public HistorySelectViewModel()
        {
            CanPressButtons = true;
            _connection = new EventConnection();
        }

        public async void Produce()
        {
            CanPressButtons = false;
            Status = "Attempting to produce history - your pdf reader will open when done";
            try
            {
                var json =
                    await
                        _connection.Produce(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId,
                            EventViewModel.Id);

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                Program.Main(new[] { file });
                File.Delete(file);
                Status = "";
            }
            catch
            {
                Status = "Something went wrong";
            }
            finally
            {
                CanPressButtons = true;
            }
        }

        public async void Collapse()
        {
            CanPressButtons = false;
            Status = "Attempting to produce and collapse history - your pdf reader will open when done";
            try
            {
                var json = await _connection.Collapse(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId, EventViewModel.Id);

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                Program.Main(new[] { file });
                File.Delete(file);
                Status = "";
            }
            catch
            {
                Status = "Something went wrong";
            }
            finally
            {
                CanPressButtons = true;
            }
        }

        public async void Create()
        {
            CanPressButtons = false;
            Status = "Attempting to produc, collapse and reduce history - your pdf reader will open when done";
            try
            {

                var json = await _connection.Create(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId, EventViewModel.Id);

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                Program.Main(new[] { file });
                File.Delete(file);
                Status = "";
            }
            catch
            {
                Status = "Something went wrong";
            }
            finally
            {
                CanPressButtons = true;
            }
        }
    }
}