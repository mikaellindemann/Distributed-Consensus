namespace HistoryConsensus
open Action
open Graph

type WorkflowId = string

module History =
    val produce : WorkflowId -> EventId -> EventId list -> string list -> Graph -> Graph option
    val stitch : Graph -> Graph list -> Graph option
    val collapse : Graph -> Graph
    val simplify : Graph -> Graph
    val reduce : Graph -> Graph