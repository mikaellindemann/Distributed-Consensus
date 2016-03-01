namespace HistoryConsensus
open Action
open Graph

module History =
    val produce : string list -> Graph -> Graph
    val stitch : Graph -> Graph list -> Graph option
