using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Connections;
using Common.DTO.Shared;
using GraphOptionToGravizo;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase
    {
        private readonly IEventConnection _eventConnection;
        private readonly IServerConnection _serverConnection;
        private bool _canPressButtons;

        private bool 
            _shouldValidate = true,
            _shouldFilter = true,
            _shouldCollapse = true,
            _shouldReduce = true,
            _shouldSimulate = true;

        private string _executionTime;
        private string _status;
        private Uri _svgPath;

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

        #region Databindings

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

        public bool ShouldValidate
        {
            get { return _shouldValidate; }
            set
            {
                if (_shouldValidate == value) return;
                _shouldValidate = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShouldFilter));
            }
        }

        public bool ShouldFilter
        {
            get { return _shouldFilter && ShouldValidate; }
            set
            {
                if (_shouldFilter == value) return;
                _shouldFilter = value;
                NotifyPropertyChanged();
            }
        }

        public bool ShouldCollapse
        {
            get { return _shouldCollapse; }
            set
            {
                if (_shouldCollapse == value) return;
                _shouldCollapse = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ShouldSimulate));
                NotifyPropertyChanged(nameof(ShouldReduce));
            }
        }

        public bool ShouldReduce
        {
            get { return _shouldReduce && ShouldCollapse; }
            set
            {
                if (_shouldReduce == value) return;
                _shouldReduce = value;
                NotifyPropertyChanged();
            }
        }

        public bool ShouldSimulate
        {
            get { return _shouldSimulate && ShouldCollapse; }
            set
            {
                if (_shouldSimulate == value) return;
                _shouldSimulate = value;
                NotifyPropertyChanged();
            }
        }

        public Uri SvgPath
        {
            get { return _svgPath; }
            set
            {
                if (value == _svgPath) return;
                _svgPath = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Databindings


        #region Actions
        public async void GenerateHistory()
        {
            Status = "Attempting to generate history with the given parameters";
            var tokenSource = new CancellationTokenSource();
            CanPressButtons = false;

            try
            {
                DoAsyncTimerUpdate(tokenSource.Token, DateTime.Now, TimeSpan.FromMilliseconds(20));

                var events = await _serverConnection.GetEventsFromWorkflow(EventViewModel.EventAddressDto.WorkflowId);
                var localHistories = new List<Tuple<string, Graph.Graph>>();
                var wrongHistories = new List<string>();
                var serverEventDtos = events as IList<ServerEventDto> ?? events.ToList();

                await
                    Task.WhenAll(
                        serverEventDtos.OrderBy(@event => @event.EventId).Select(
                            async @event => await _eventConnection.Lock(@event.Uri, @event.WorkflowId, @event.EventId)));

                foreach (var @event in serverEventDtos)
                {
                    var localHistory =
                        JsonConvert.DeserializeObject<Graph.Graph>(
                            await _eventConnection.GetLocalHistory(@event.Uri, @event.WorkflowId, @event.EventId));
                    localHistories.Add(new Tuple<string, Graph.Graph>(@event.EventId, localHistory));
                }

                await
                    Task.WhenAll(
                        serverEventDtos.Select(
                            async @event => await _eventConnection.Unlock(@event.Uri, @event.WorkflowId, @event.EventId)));

                if (ShouldValidate)
                {
                    foreach (var history in localHistories)
                    {
                        var validationResult = await Task.Run(()=>LocalHistoryValidation.smallerLocalCheck(history.Item2, history.Item1));
                        if (!validationResult.IsSuccess)
                        {
                            wrongHistories.Add(history.Item1);
                        }
                    }
                    // todo validation on pairs and all
                }
                if (ShouldFilter)
                {
                    localHistories = localHistories.Where(tuple => !wrongHistories.Contains(tuple.Item1)).ToList();
                }
                Graph.Graph mergedGraph;
                {
                    // Merging
                    var first = localHistories.First().Item2;
                    var rest =
                        ToFSharpList(
                            localHistories.Where(elem => !ReferenceEquals(elem.Item2, first))
                                .Select(tuple => tuple.Item2));

                    var result = await Task.Run(()=>History.stitch(first, rest));
                    mergedGraph = FSharpOption<Graph.Graph>.get_IsSome(result) ? result.Value : null;
                }
                if (ShouldCollapse)
                {
                    mergedGraph = await Task.Run(()=>History.collapse(mergedGraph));
                }
                if (ShouldReduce)
                {
                    mergedGraph = await Task.Run(()=>History.reduce(mergedGraph));
                }
                if (ShouldSimulate)
                {
                    var initialStates = serverEventDtos.Select(dto => new Tuple<string, Tuple<bool, bool, bool>>(dto.EventId, new Tuple<bool, bool, bool>(dto.Included, dto.Pending, dto.Executed)));
                    var rules = new List<Tuple<string, string, Action.ActionType>>();
                    foreach (var serverEventDto in serverEventDtos)
                    {
                        //conditions
                        rules.AddRange(serverEventDtos.SelectMany(dto => dto.Conditions).Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.ChecksCondition)));
                        //inclusions
                        rules.AddRange(serverEventDtos.SelectMany(dto => dto.Inclusions).Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.Includes)));
                        //exclusions
                        rules.AddRange(serverEventDtos.SelectMany(dto => dto.Exclusions).Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.Excludes)));
                        //responses
                        rules.AddRange(serverEventDtos.SelectMany(dto => dto.Responses).Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.SetsPending)));
                    }

                    var result = HistoryConsensus.DCRSimulator.simulate(mergedGraph, new FSharpMap<string, Tuple<bool, bool, bool>>(initialStates), new FSharpSet<Tuple<string, string, Action.ActionType>>(rules));
                    // todo use this result
                }

                //new GraphToSvgConverter().ConvertAndShow(mergedGraph);
                var tempFile = Path.GetTempFileName() + ".svg";
                new GraphToSvgConverter().ConvertGraphToSvg(mergedGraph, tempFile);

                SvgPath = new Uri(tempFile);

                Status = "";
            }
            catch (Exception)
            {
                Status = "Something went wrong";
            }
            finally
            {
                tokenSource.Cancel();
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
        #endregion Actions
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