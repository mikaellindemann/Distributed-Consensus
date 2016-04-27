namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module DCRSimulator =
    type EventState = bool * bool * bool // Included * Pending * Executed
    type DCRState = Map<EventId, EventState>
    type DCRRules = Set<EventId * EventId * ActionType> // From * To * Type


    /// <summary>
    /// Simulates the execution of a collapsed graph, using the given initial state and the rules for the graph.
    /// </summary>
    /// Finds a valid total order of execution given the collapsed input history. This is done by reference counting.
    /// When this order has been found, the state is checked against whether the next event to execute is executable.
    /// If it is, the state is updated according to the given rules, and the steps are repeated until all events in
    /// the order of execution has been executed.
    /// If, for some reason an event is not executable in the given total order, this means that the history has
    /// invalid information. Therefore a fail is returned.
    /// If everything is alright, the initial collapsed history is returned.
    val simulate : Graph -> DCRState -> DCRRules -> Result<Graph, FailureT list> // History -> Initial State -> DCR Graph -> Result
