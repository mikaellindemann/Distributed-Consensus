namespace HistoryConsensus
open FSharp.Data
open Action
open Graph

module History =
    let produce (uris : string list) (localHistory : Graph) : Graph =
        Graph.empty // Todo: Download and produce/stich results.
