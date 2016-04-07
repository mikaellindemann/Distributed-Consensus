using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

        public string ExecutionTime
        {
            get { return _executionTime; }
            set
            {
                if (value == _executionTime) return;
                _executionTime = value;
                NotifyPropertyChanged();
            }
        }

        private readonly IEventConnection _connection;
        private bool _canPressButtons;
        private string _executionTime;

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
            Status = "Attempting to produce history - pdf result will open when done";
            try
            {
                var tokenSource = new CancellationTokenSource();

                DoAsyncTimerUpdate(tokenSource.Token, DateTime.Now, TimeSpan.FromMilliseconds(20));
                var json =
                    await
                        _connection.Produce(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId,
                            EventViewModel.Id);
                tokenSource.Cancel();

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                var process = Program.DoTheStuff(new[] { file });
                process.Exited += (sender, args) =>
                {
                    File.Delete(file);
                    CanPressButtons = true;
                };

                Status = "";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
                CanPressButtons = true;
            }
        }

        public async void Collapse()
        {
            CanPressButtons = false;
            Status = "Attempting to produce+collapse history - pdf result will open when done";
            try
            {
                var tokenSource = new CancellationTokenSource();

                DoAsyncTimerUpdate(tokenSource.Token, DateTime.Now, TimeSpan.FromMilliseconds(20));
                var json = await _connection.Collapse(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId, EventViewModel.Id);
                tokenSource.Cancel();

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                var process = Program.DoTheStuff(new[] { file });
                process.Exited += (sender, args) =>
                {
                    File.Delete(file);
                    CanPressButtons = true;
                };
                Status = "";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
                CanPressButtons = true;
            }
        }

        public async void Create()
        {
            CanPressButtons = false;
            Status = "Attempting to create history - pdf result will open when done";
            

            try
            {
                var tokenSource = new CancellationTokenSource();

                DoAsyncTimerUpdate(tokenSource.Token, DateTime.Now, TimeSpan.FromMilliseconds(20));
                var json = await _connection.Create(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId, EventViewModel.Id);
                tokenSource.Cancel();
                
                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                var process = Program.DoTheStuff(new[] { file });
                process.Exited += (sender, args) =>
                {
                    File.Delete(file);
                    CanPressButtons = true;
                };
                
                Status = "";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
                CanPressButtons = true;
            }
        }

        public async void DoAsyncTimerUpdate(CancellationToken token, DateTime start, TimeSpan timeout)
        {
            while (!token.IsCancellationRequested)
            {
                if (timeout > TimeSpan.Zero)
                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(timeout);

                ExecutionTime = DateTime.Now.Subtract(start).ToString(@"h\:mm\:ss\.ff", new DateTimeFormatInfo());
            }
        }
    }
}