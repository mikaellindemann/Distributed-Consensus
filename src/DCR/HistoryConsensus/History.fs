namespace HistoryConsensus
open FSharp.Data
open Action
open Graph

module History =
    let produce (uris : string list) (localHistory : Graph) : Graph =
        
        Graph.empty // Todo: Download and produce/stich results.
        
    let rec stitch (localHistory : Graph) (histories : Graph list) : Graph option =
        match histories with
        | [] -> Some localHistory
        | history::histories' -> 
            match merge localHistory history with
            | None -> None
            | Some graph -> stitch graph histories'