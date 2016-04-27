namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module LocalHistoryValidation =
    val giantLocalCheck : Graph -> EventId -> DCRRules -> Result<Graph, Graph>
    val smallerLocalCheck : Graph -> EventId -> Result<Graph, Graph>