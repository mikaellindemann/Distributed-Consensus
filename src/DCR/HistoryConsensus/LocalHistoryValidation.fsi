namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module LocalHistoryValidation =
    val giantLocalCheck : Graph -> EventId -> (unit -> Map<EventId, ActionType list>) -> (unit -> (EventId * ActionType) seq) -> Result<Graph, Graph>
    val smallerLocalCheck : Graph -> EventId -> Result<Graph, Graph>