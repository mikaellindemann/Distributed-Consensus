namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module LocalHistoryValidation =
    val giantLocalCheck : Graph -> EventId -> (unit -> Map<EventId, ActionType list>) -> (unit -> (EventId * ActionType) list) -> Result<Graph, FailureT list>
    val smallerLocalCheck : Graph -> EventId -> Result<Graph, FailureT list>