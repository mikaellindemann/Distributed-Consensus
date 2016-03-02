namespace HistoryConsensus
open FSharp.Data
open Action
open Graph
open HttpClient
open Newtonsoft.Json

type WorkflowId = string

module History =

    /// Utility function. Makes an options list to a list option.
    /// If any element of the source list is None, the result will become None.
    let rec optionUnwrapper optionList = 
        List.foldBack 
            (fun elem state ->
                match elem,state with
                | None,_             -> None
                | _,None             -> None
                | Some el, Some list -> Some (el :: list))
            optionList (Some [])

    /// The stitch algorithm takes a local graph, and a set of other graphs, 
    /// and tries to merge these graphs together. If this can be done without
    /// consistency issues, the resulting graph is returned as an option. Otherwise None is the result.
    let rec stitch localHistory histories =
        match histories with
        | [] -> Some localHistory
        | history::histories' -> 
            match merge localHistory history with
            | None -> None
            | Some graph -> stitch graph histories'

    /// Externally calls the produce request on neighboring nodes, and returns the result as
    /// an option type.
    let callProduce workflowId eventId trace uri =
        let response =
            createRequest Post (sprintf "%s/history/%s/%s/produce" uri workflowId eventId)
            |> withBody (JsonConvert.SerializeObject (eventId :: trace))
            |> withHeader (ContentType "application/json") 
            |> getResponseBody
        JsonConvert.DeserializeObject<Graph option> response

    /// The produce algorithm checks whether or not this event is already part of this history call.
    /// If it is, it just returns its local history. If not, it gathers information from its neighbors,
    /// Stitches it (if possible) and returns this as a Graph option.
    let produce workflowId eventId trace uris localHistory =
        if List.contains eventId trace
        then Some localHistory
        else
            let otherHistories = List.map (fun uri -> callProduce workflowId eventId trace uri) uris
            match optionUnwrapper otherHistories with
            | None -> None
            | Some histories -> stitch localHistory histories
        
    