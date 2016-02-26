namespace HistoryConsensus
open Action

module Graph =
    type Graph =
        {
            Nodes: Map<ActionId, Action>
        }

    val addNode : Action -> Graph -> Graph
    val removeNode : Action -> Graph -> Graph
    val addEdge : Action -> Action -> Graph -> Graph
    val removeEdge : Action -> Action -> Graph -> Graph
    val empty : Graph