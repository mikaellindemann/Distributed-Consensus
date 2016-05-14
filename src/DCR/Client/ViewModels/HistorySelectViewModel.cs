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
using Common.DTO.Event;
using Common.DTO.Shared;
using GraphOptionToSvg;
using HistoryConsensus;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using Action = HistoryConsensus.Action;
using Common.DTO.History;

namespace Client.ViewModels
{
    public class HistorySelectViewModel : ViewModelBase, IDisposable
    {
        private IEventConnection _eventConnection;
        private readonly IEventConnection _realEventConnection;

        private IServerConnection _serverConnection;
        private readonly IServerConnection _realServerConnection;

        private readonly DummyConnection _dummyConnection; // can be used as either eventconnection or Serverconnection

        private bool _canPressButtons;
        private bool _useDummyConnection;

        private bool
            _shouldValidate = true,
            _shouldFilter = true,
            _shouldMerge = true,
            _shouldCollapse = true,
            _shouldReduce = true,
            _shouldSimulate = true;
            

        private string _executionTime;
        private string _status;
        private Uri _svgPath;

        public HistorySelectViewModel(WorkflowViewModel workflowViewModel, IServerConnection serverConnection, IEventConnection eventConnection) : this(serverConnection, eventConnection)
        {
            _realWorkflows = workflowViewModel.Parent.WorkflowList.Select(model => model.WorkflowDto);
            Workflows = new ObservableCollection<WorkflowDto>(_realWorkflows);
            SelectedWorkflow = Workflows.FirstOrDefault(model => model.Id == workflowViewModel.WorkflowId);

            TypeDescriptor.AddAttributes(
                typeof(Tuple<string, int>),
                new TypeConverterAttribute(typeof(TupleConverter)));
        }

        public HistorySelectViewModel(IServerConnection serverConnection, IEventConnection eventConnection)
        {
            CanPressButtons = true;
            _serverConnection = serverConnection;
            _realServerConnection = serverConnection;

            _eventConnection = eventConnection;
            _realEventConnection = eventConnection;
            _dummyConnection = new DummyConnection();
            Failures = new ObservableSet<string>();
            Workflows = new ObservableCollection<WorkflowDto>();
        }

        #region Databindings

        private readonly IEnumerable<WorkflowDto> _realWorkflows;
        public ObservableCollection<WorkflowDto> Workflows { get; set; }
        public WorkflowDto SelectedWorkflow { get; set; } 

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

        public bool UseDummyConnection
        {
            get { return _useDummyConnection; }
            set
            {
                if (_useDummyConnection == value) return;
                _useDummyConnection = value;
                if (_useDummyConnection)
                {
                    _eventConnection = (IEventConnection)_dummyConnection;
                    _serverConnection = (IServerConnection)_dummyConnection;
                    Workflows = new ObservableCollection<WorkflowDto>(_dummyConnection.WorkflowDtos);
                }
                else
                {
                    _eventConnection = _realEventConnection;
                    _serverConnection = _realServerConnection;
                    Workflows = new ObservableCollection<WorkflowDto>(_realWorkflows);
                }
                
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Workflows));
                SelectedWorkflow = Workflows.FirstOrDefault();
                NotifyPropertyChanged(nameof(SelectedWorkflow));
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

        public bool ShouldMerge
        {
            get { return _shouldMerge; }
            set
            {
                if (_shouldMerge == value) return;
                _shouldMerge = value;
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

                var events = await _serverConnection.GetEventsFromWorkflow(SelectedWorkflow.Id);
                var serverEventDtos = events as IList<ServerEventDto> ?? events.ToList();

                var localHistories = await GenerateLocalGraphs(serverEventDtos);
                var wrongHistories = new HashSet<Tuple<string, FailureTypes.FailureType>>();
                
                // Extract DCR rules
                var dcrRules = new FSharpSet<Tuple<string, string, Action.ActionType>>(GetRules(serverEventDtos));

                if (ShouldValidate)
                {
                    await Validation(localHistories, dcrRules, wrongHistories);
                }
                if (ShouldFilter)
                {
                    localHistories = localHistories.Where(id => wrongHistories.All(badTuple => badTuple.Item1 != id.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);                    
                }
                Graph.Graph mergedGraph = Graph.empty;
                if (localHistories.Count != 0) // can only merge if there is something to merge
                {
                    if (ShouldMerge) 
                    {
                        mergedGraph = await Merging(localHistories);
                    }
                    else
                    {
                        mergedGraph = await Union(localHistories);
                    }
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
                    mergedGraph = Simulation(serverEventDtos, mergedGraph, dcrRules, wrongHistories);
                }

                CreateSVG(mergedGraph, wrongHistories);

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

        private void CreateSVG(Graph.Graph mergedGraph, HashSet<Tuple<string, FailureTypes.FailureType>> wrongHistories)
        {
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
        }

        private static async Task<Graph.Graph> Merging(Dictionary<string, Graph.Graph> localHistories)
        {
            var first = localHistories.First().Value;
            var rest =
                ToFSharpList(
                    localHistories.Where(elem => !ReferenceEquals(elem.Value, first))
                        .Select(tuple => tuple.Value));

            var result = await Task.Run(() => History.stitch(first, rest));
            return FSharpOption<Graph.Graph>.get_IsSome(result) ? result.Value : null;
        }

        private static async Task<Graph.Graph> Union(Dictionary<string, Graph.Graph> localHistories)
        {
            var first = localHistories.First().Value;
            var rest =
                ToFSharpList(
                    localHistories.Where(elem => !ReferenceEquals(elem.Value, first))
                        .Select(tuple => tuple.Value));

            var result = await Task.Run(() => History.union(first, rest));
            return FSharpOption<Graph.Graph>.get_IsSome(result) ? result.Value : null;
        }

        private static Graph.Graph Simulation(IList<ServerEventDto> serverEventDtos, Graph.Graph mergedGraph, FSharpSet<Tuple<string, string, Action.ActionType>> dcrRules, HashSet<Tuple<string, FailureTypes.FailureType>> wrongHistories)
        {
            var initialStates =
                serverEventDtos.Select(
                    dto =>
                        new Tuple<string, Tuple<bool, bool, bool>>(dto.EventId,
                            new Tuple<bool, bool, bool>(dto.Included, dto.Pending, dto.Executed)));
            var result = DCRSimulator.simulate(mergedGraph, new FSharpMap<string, Tuple<bool, bool, bool>>(initialStates),
                dcrRules);

            if (result.IsFailure)
            {
                var failureResult = result.GetFailure;

                foreach (
                    var keyValuePair in
                        failureResult.Nodes.Where(
                            actionTuple =>
                                actionTuple.Value.FailureTypes.Contains(FailureTypes.FailureType.ExecutedWithoutProperState)))
                {
                    wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(keyValuePair.Key.Item1,
                        FailureTypes.FailureType.ExecutedWithoutProperState));
                }

                mergedGraph = failureResult;
            }
            return mergedGraph;
        }

        private static async Task Validation(Dictionary<string, Graph.Graph> localHistories, FSharpSet<Tuple<string, string, Action.ActionType>> dcrRules, HashSet<Tuple<string, FailureTypes.FailureType>> wrongHistories)
        {
            foreach (var historyId in localHistories.Keys.ToList())
            {
                var historyGraph = localHistories[historyId];
                var validationResult =
                    await Task.Run(() => LocalHistoryValidation.giantLocalCheck(historyGraph, historyId, dcrRules));
                if (validationResult.IsFailure)
                {
                    var failureHistory = validationResult.GetFailure;

                    var failures =
                        failureHistory.Nodes.Where(node => !node.Value.FailureTypes.IsEmpty)
                            .SelectMany(actionTuple => actionTuple.Value.FailureTypes)
                            .Distinct();

                    foreach (var failureType in failures)
                    {
                        wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(historyId, failureType));
                    }

                    localHistories[historyId] = failureHistory;
                }
            }
            // pair validations
            foreach (var historyId1 in localHistories.Keys.ToList())
            {
                var history1 = localHistories[historyId1];
                foreach (var historyId2 in localHistories.Keys.ToList())
                {
                    var history2 = localHistories[historyId2];
                    if (dcrRules.Any(tuple => tuple.Item1 == historyId1 && tuple.Item2 == historyId2))
                    {
                        var validationResult =
                            await Task.Run(() => HistoryValidation.pairValidationCheck(history1, history2));
                        if (validationResult.IsFailure)
                        {
                            var failureHistory = validationResult.GetFailure;

                            wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(historyId1,
                                FailureTypes.FailureType.Maybe));
                            wrongHistories.Add(new Tuple<string, FailureTypes.FailureType>(historyId2,
                                FailureTypes.FailureType.Maybe));

                            localHistories[historyId1] = failureHistory.Item1;
                            localHistories[historyId2] = failureHistory.Item2;
                        }
                    }
                }
            }
        }

        private async Task<Dictionary<string, Graph.Graph>> GenerateLocalGraphs(IList<ServerEventDto> serverEventDtos)
        {
            
            var localHistories = new Dictionary<string, Graph.Graph>();

            await
                Task.WhenAll(
                    serverEventDtos.OrderBy(@event => @event.EventId).Select(
                        async @event => await _eventConnection.Lock(@event.Uri, @event.WorkflowId, @event.EventId)));

            foreach (var @event in serverEventDtos)
            {
                var localHistory = (await _eventConnection.GetLocalHistory(@event.Uri, @event.WorkflowId, @event.EventId));

                localHistories.Add(@event.EventId, localHistory);
            }

            await
                Task.WhenAll(
                    serverEventDtos.Select(
                        async @event => await _eventConnection.Unlock(@event.Uri, @event.WorkflowId, @event.EventId)));

            return localHistories;
        }

        private string FailureTypeToString(FailureTypes.FailureType type)
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