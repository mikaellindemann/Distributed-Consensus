namespace HistoryConsensus
open Action
open Newtonsoft.Json

module Graph =
    type Graph =
        {
            Nodes: Map<ActionId, Action>
        }

    val addNode : Action -> Graph -> Graph
    val removeNode : Action -> Graph -> Graph
    val addEdge : Action -> Action -> Graph -> Graph
    val removeEdge : Action -> Action -> Graph -> Graph
    val merge : Graph -> Graph -> Graph option
    val empty : Graph
    val simplify : Graph -> ActionType -> Graph