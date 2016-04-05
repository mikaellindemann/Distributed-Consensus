using System.CodeDom.Compiler;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Client.Connections;
using Common.DTO.Shared;
using GraphOptionToGravizo;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase
    {
        public EventAddressDto EventAddressDto { get; set; }

        private readonly IEventConnection _connection;

        public HistorySelectViewModel(EventAddressDto eventAddressDto)
        {
            EventAddressDto = eventAddressDto;
            _connection = new EventConnection();
        }

        public HistorySelectViewModel()
        {
            _connection = new EventConnection();
        }

        public async void Produce()
        {
            var json = await _connection.Produce(EventAddressDto.Uri, EventAddressDto.WorkflowId, EventAddressDto.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new [] {file});
            File.Delete(file);
        }

        public async void Collapse()
        {
            var json = await _connection.Collapse(EventAddressDto.Uri, EventAddressDto.WorkflowId, EventAddressDto.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new[] { file });
            File.Delete(file);
        }

        public async void Create()
        {
            var json = await _connection.Create(EventAddressDto.Uri, EventAddressDto.WorkflowId, EventAddressDto.Id);

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);

            Program.Main(new[] { file });
            File.Delete(file);
        }
    }
}