using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Connections;
using GraphOptionToGravizo;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase
    {
        private readonly IEventConnection _eventConnection;
        private readonly IServerConnection _serverConnection;
        private bool _canPressButtons;
        private string _executionTime;
        private string _status;

        public HistorySelectViewModel(EventViewModel eventViewModel, IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            EventViewModel = eventViewModel;
            _serverConnection = serverConnection;
            _eventConnection = eventConnection;

            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
        }

        public HistorySelectViewModel(IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            _serverConnection = serverConnection;
            _eventConnection = eventConnection;
        }

        public EventViewModel EventViewModel { get; set; }

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status == value) return;
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

        private async Task<Graph.Graph> FetchAndMerge()
        {
            var events = await _serverConnection.GetEventsFromWorkflow(EventViewModel.EventAddressDto.WorkflowId);

            var localHistories = new List<Graph.Graph>();

            foreach (var @event in events)
            {
                localHistories.Add(JsonConvert.DeserializeObject<Graph.Graph>(await _eventConnection.GetLocalHistory(@event.Uri, @event.WorkflowId, @event.EventId)));
            }

            var first = localHistories.First();
            var rest = ToFSharpList(localHistories.Where(elem => !ReferenceEquals(elem, first)));

            var finalGraph = History.stitch(first, rest);

            return FSharpOption<Graph.Graph>.get_IsSome(finalGraph) ? finalGraph.Value : null;
        }

        public async Task MakeHistoryLocal(Func<Task<Graph.Graph>> makeHistory)
        {
            var tokenSource = new CancellationTokenSource();
            CanPressButtons = false;
            
            try
            {
                DoAsyncTimerUpdate(tokenSource.Token, DateTime.Now, TimeSpan.FromMilliseconds(20));
                var graph = await makeHistory();
                tokenSource.Cancel();

                new GraphToPdfConverter().ConvertAndShow(graph);
                
                CanPressButtons = true;
                Status = "";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
                CanPressButtons = true;
            }
        }

        public async void ProduceLocal()
        {
            Status = "Attempting to produce history - pdf result will open when done";
            await MakeHistoryLocal(FetchAndMerge);
        }

        public async void CollapseLocal()
        {
            Status = "Attempting to produce+collapse history - pdf result will open when done";
            await MakeHistoryLocal(async () => History.collapse(await FetchAndMerge()));
        }

        public async void CreateLocal()
        {
            Status = "Attempting to create history - pdf result will open when done";
            await MakeHistoryLocal(async () => History.simplify(await FetchAndMerge()));
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
                        _eventConnection.Produce(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId,
                            EventViewModel.Id);
                tokenSource.Cancel();

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                new GraphToPdfConverter().ConvertAndShow(file);

                CanPressButtons = true;
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
                var json = await _eventConnection.Collapse(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId, EventViewModel.Id);
                tokenSource.Cancel();

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                new GraphToPdfConverter().ConvertAndShow(file);

                CanPressButtons = true;
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
                var json = await _eventConnection.Create(EventViewModel.Uri, EventViewModel.EventAddressDto.WorkflowId, EventViewModel.Id);
                tokenSource.Cancel();

                var file = Path.GetTempFileName();
                File.WriteAllText(file, json);

                new GraphToPdfConverter().ConvertAndShow(file);

                CanPressButtons = true;
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

        /// <summary>
        /// Turns a typed IEnumerable into the corresponding FSharpList.
        /// 
        /// The list gets reversed up front because it is cons'ed together backwards.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        private static FSharpList<T> ToFSharpList<T>(IEnumerable<T> elements)
        {
            return elements.Reverse().Aggregate(FSharpList<T>.Empty, (current, element) => FSharpList<T>.Cons(element, current));
        }
    }

    public class TupleConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var s = value as string;
            if (s != null)
            {
                s = s.Substring(1, s.Length - 2);
                var v = s.Split(',');
                return new Tuple<string, int>(v[0], int.Parse(v[1]));
            }
            return base.ConvertFrom(context, culture, value);
        }

        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var v = value as Tuple<string, int>;
                if (v != null)
                    return $"({v.Item1}, {v.Item2})";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}