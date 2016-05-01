using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Connections;
using Client.Helpers.ViewModelHelpers;
using Common.DTO.Shared;
using GraphOptionToSvg;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase, IDisposable
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

        public HistorySelectViewModel(WorkflowViewModel workflowViewModel, IServerConnection serverConnection, IEventConnection eventConnection) : this(serverConnection, eventConnection)
        {
            Workflows = workflowViewModel.Parent.WorkflowList;
            SelectedWorkflow = Workflows.FirstOrDefault(model => model.WorkflowId == workflowViewModel.WorkflowId);

            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
        }

        public HistorySelectViewModel(IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            _serverConnection = serverConnection;
            _eventConnection = eventConnection;
            Failures = new ObservableSet<string>();
            Workflows = new ObservableCollection<WorkflowViewModel>();
        }

        #region Databindings

        public ObservableCollection<WorkflowViewModel> Workflows { get; set; }
        public WorkflowViewModel SelectedWorkflow { get; set; } 

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
                var oldValue = _svgPath;
                _svgPath = value;
                NotifyPropertyChanged();
                if (oldValue != null && File.Exists(oldValue.LocalPath))
                    File.Delete(oldValue.LocalPath);
            }
        }

        public ObservableSet<string> Failures { get; set; }
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

                var events = await _serverConnection.GetEventsFromWorkflow(SelectedWorkflow.WorkflowId);
                var localHistories = new List<Tuple<string, Graph.Graph>>();
                var wrongHistories = new HashSet<Tuple<string, FailureTypes.FailureType>>();
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

                // Extract DCR rules
                var rules = GetRules(serverEventDtos);
                var dcrRules = new FSharpSet<Tuple<string, string, Action.ActionType>>(rules);

                if (ShouldValidate)
                {
                    for (int index = 0; index < localHistories.Count; index++)
                    {
                        var history = localHistories[index];

                        var validationResult =
                            await Task.Run(() => LocalHistoryValidation.giantLocalCheck(history.Item2, history.Item1, dcrRules));
                        if (validationResult.IsFailure)
                        {
                            var failureHistory = validationResult.GetFailure;

                            var failures = failureHistory.Nodes.Where(node => !node.Value.FailureTypes.IsEmpty).SelectMany(actionTuple => actionTuple.Value.FailureTypes).Distinct();

                            foreach (var failureType in failures)
                            {
                                wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(history.Item1, failureType));
                            }

                            localHistories[index] = new Tuple<string, Graph.Graph>(history.Item1, failureHistory);
                        }
                    }
                    // pair validations
                    for (var index1 = 0; index1 < localHistories.Count; index1++)
                    {
                        var history1 = localHistories[index1];
                        for (var index2 = index1 + 1; index2 < localHistories.Count; index2++)
                        {
                            var history2 = localHistories[index2];
                            if (rules.Any(tuple => tuple.Item1 == history1.Item1 && tuple.Item2 == history2.Item1))
                            {
                                var validationResult =
                                    await Task.Run(() => HistoryValidation.pairValidationCheck(history1.Item2, history2.Item2));
                                if (validationResult.IsFailure)
                                {
                                    var failureHistory = validationResult.GetFailure;

                                    wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(history1.Item1, FailureTypes.FailureType.Maybe));
                                    wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(history2.Item1, FailureTypes.FailureType.Maybe));

                                    localHistories[index1] = new Tuple<string, Graph.Graph>(history1.Item1, failureHistory.Item1);
                                    localHistories[index2] = new Tuple<string, Graph.Graph>(history2.Item1, failureHistory.Item2);
                                }
                            }
                        }
                    }
                }
                if (ShouldFilter)
                {
                    localHistories = localHistories.Where(tuple => wrongHistories.All(badTuple => badTuple.Item1 != tuple.Item1)).ToList();
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
                    var result = DCRSimulator.simulate(mergedGraph, new FSharpMap<string, Tuple<bool, bool, bool>>(initialStates), dcrRules);

                    if (result.IsFailure)
                    {
                        var failureResult = result.GetFailure;

                        foreach (var keyValuePair in failureResult.Nodes.Where(actionTuple => actionTuple.Value.FailureTypes.Contains(FailureTypes.FailureType.ExecutedWithoutProperState)))
                        {
                            wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(keyValuePair.Key.Item1,
                                FailureTypes.FailureType.ExecutedWithoutProperState));
                        }

                        mergedGraph = failureResult;
                    }
                }

                //new GraphToSvgConverter().ConvertAndShow(mergedGraph);
                var tempFile = Path.GetTempFileName() + ".svg";
                File.Delete(tempFile.Substring(0, tempFile.Length - 4));

                new GraphToSvgConverter().ConvertGraphToSvg(mergedGraph, tempFile);

                // Update the failure list on the right:
                Failures.Clear();
                foreach (var wrongHistory in wrongHistories)
                {
                    Failures.Add($"{wrongHistory.Item1} {FailureTypeToString(wrongHistory.Item2)}");
                }

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

        private static string FailureTypeToString(FailureTypes.FailureType type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (type.IsCounterpartTimestampOutOfOrder) return "had counterpart timestamps out of order";
            if (type.IsExecutedWithoutProperState) return "executed without proper state";
            if (type.IsFakeRelationsIn) return "had fake ingoing relations";
            if (type.IsFakeRelationsOut) return "had fake outgoing relations";
            if (type.IsHistoryAboutOthers) return "contained history about others";
            if (type.IsIncomingChangesWhileExecuting) return "had incoming changes while executing";
            if (type.IsLocalTimestampOutOfOrder) return "had local timestamps out of order";
            if (type.IsMalicious) return "is somehow malicious";
            if (type.IsMaybe) return "might be malicious";
            if (type.IsPartOfCycle) return "is part of cycle";
            if (type.IsPartialOutgoingWhenExecuting) return "only used some of the outgoing relations when executing";

            throw new ArgumentException("Unknown type", nameof(type));
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

        private IList<Tuple<string, string, Action.ActionType>> GetRules(IList<ServerEventDto> serverEventDtos)
        {
            var rules = new List<Tuple<string, string, Action.ActionType>>();
            foreach (var serverEventDto in serverEventDtos)
            {
                //conditions
                rules.AddRange(serverEventDto.Conditions.Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.ChecksCondition)));
                //inclusions
                rules.AddRange(serverEventDto.Inclusions.Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.Includes)));
                //exclusions
                rules.AddRange(serverEventDto.Exclusions.Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.Excludes)));
                //responses
                rules.AddRange(serverEventDto.Responses.Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.SetsPending)));
                //milestones
                rules.AddRange(serverEventDto.Milestones.Select(dto => new Tuple<string, string, Action.ActionType>(serverEventDto.EventId, dto.Id, Action.ActionType.ChecksMilestone)));
            }
            return rules;
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

        public void Dispose()
        {
            if (SvgPath != null && File.Exists(SvgPath.LocalPath))
                File.Delete(SvgPath.LocalPath);
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