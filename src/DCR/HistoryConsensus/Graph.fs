namespace HistoryConsensus
open Action

module Graph =
    type Graph = Map<ActionId, Action>
