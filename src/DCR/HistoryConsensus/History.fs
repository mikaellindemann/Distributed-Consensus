namespace HistoryConsensus
open FSharp.Data
open Array.Parallel
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
    let callProduce eventId trace uri =
        let body = JsonConvert.SerializeObject (eventId :: trace)

        let response = Http.RequestString((sprintf "%s/produce" uri), body = HttpRequestBody.TextRequest body, headers = [("ContentType", "application/json"); ("Accept", "application/json")], httpMethod = "POST", customizeHttpRequest = (fun request -> request.Timeout <- 3600000; request.ContentType <- "application/json"; request))
        (*let response =
            createRequest Post (sprintf "%s/produce" uri)
            |> withBody body
            |> withHeader (ContentType "application/json") 
            |> withHeader (Accept "application/json")
            |> withKeepAlive true
            |> getResponseBody*)
        let result = JsonConvert.DeserializeObject<Graph option> response
        result

    let mapParallel func list = List.ofArray <| Array.Parallel.map func (List.toArray list)

    /// The produce algorithm checks whether or not this event is already part of this history call.
    /// If it is, it just returns its local history. If not, it gathers information from its neighbors,
    /// Stitches it (if possible) and returns this as a Graph option.
    let produce (workflowId : WorkflowId) (eventId : EventId) trace uris localHistory =
        if List.contains eventId trace
        then Some localHistory
        else
            let otherHistories = mapParallel (fun uri -> callProduce eventId trace uri) uris
            match optionUnwrapper otherHistories with
            | None -> None
            | Some histories -> stitch localHistory histories
        
    
    