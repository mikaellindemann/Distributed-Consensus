namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module DCRSimulator =
    type EventState = bool * bool * bool // Included * Pending * Executed
    type DCRState = Map<EventId, EventState>
    type DCRRules = Set<EventId * EventId * ActionType> // From * To * Type

    val simulate : Graph -> DCRState -> DCRRules -> Result<Graph, FailureT list> // History -> Initial State -> DCR Graph -> Result
