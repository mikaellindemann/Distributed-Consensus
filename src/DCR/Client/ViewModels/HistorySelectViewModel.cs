using System.IO;
using Client.Connections;
using GraphOptionToGravizo;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase
    {
        public EventViewModel EventViewModel { get; set; }

        private readonly IEventConnection _connection;

        public HistorySelectViewModel(EventViewModel eventViewModel)
        {
            EventViewModel = eventViewModel;
            _connection = new EventConnection();
        }

        public HistorySelectViewModel()
        {
            _connection = new EventConnection();
        }

        public async void Produce()
        {
            var json = await _connection.Produce(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId, EventViewModel.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new [] {file});
            File.Delete(file);
        }

        public async void Collapse()
        {
            var json = await _connection.Collapse(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId, EventViewModel.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new[] { file });
            File.Delete(file);
        }

        public async void Create()
        {
            var json = await _connection.Create(EventViewModel.Uri, EventViewModel._eventAddressDto.WorkflowId, EventViewModel.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new[] { file });
            File.Delete(file);
        }
    }
}