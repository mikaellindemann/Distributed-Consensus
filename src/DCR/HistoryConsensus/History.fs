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

        let response = 
            Http.RequestString((sprintf "%s/produce" uri), 
                body = HttpRequestBody.TextRequest body, 
                customizeHttpRequest = 
                    (fun request -> 
                        request.Timeout <- 3600000; 
                        request.ContentType <- "application/json"; 
                        request.Accept <- "application/json"; 
                        request.Method <- "POST";
                        request
                    )
            )
        let result = JsonConvert.DeserializeObject<Graph option> response
        result

    /// The produce algorithm checks whether or not this event is already part of this history call.
    /// If it is, it just returns its local history. If not, it gathers information from its neighbors,
    /// Stitches it (if possible) and returns this as a Graph option.
    let produce (workflowId : WorkflowId) (eventId : EventId) trace uris localHistory =
        if List.contains eventId trace
        then Some localHistory
        else
            let otherHistories = List.map (fun uri -> callProduce eventId trace uri) uris
            match optionUnwrapper otherHistories with
            | None -> None
            | Some histories -> stitch localHistory histories

    let collapse graph =
        let createMapForSingleExecution actions newActionId map = 
            Set.fold (fun map actionId -> Map.add actionId newActionId map) map actions

        let findSuccessorOnEvent actionId currentExecution =
            let edges = (getNode graph actionId).Edges
            if Set.isEmpty edges
            then (currentExecution, None)
            else
                let immediateSuccessor = Seq.find (fun (neighborEventId,_) -> (fst actionId) = neighborEventId) edges
                let rest = Set.remove immediateSuccessor edges
                if Set.isEmpty rest
                then (currentExecution, Some <| immediateSuccessor)
                else (Set.add (Seq.head rest) currentExecution, Some immediateSuccessor)

        let rec findSingleExecution actionId result =
            let newResult = (Set.add actionId result)
            let action = getNode graph actionId
            if action.Type = ExecuteFinish
            then newResult
            else 
                match findSuccessorOnEvent actionId newResult with
                | (currentExecution, None) -> currentExecution
                | (currentExecution, Some successor) -> findSingleExecution successor currentExecution

        let startExecutions = 
            Map.fold (fun set actionId _ -> Set.add actionId set)
                Set.empty
                (Map.filter (fun _ action -> action.Type = ExecuteStart) graph.Nodes) 


        let mapOfCollapsedExecutions =
            let uncollapsed = Set.map (fun startActionId -> startActionId, findSingleExecution startActionId Set.empty) startExecutions
            
            Set.fold 
                (fun map (startActionId,execution) -> createMapForSingleExecution execution startActionId map)
                Map.empty
                uncollapsed

        let rec findStartExecution actionId =
            let node = getNode graph actionId
            if (node.Type = ExecuteStart) then Some actionId
            elif (Set.isEmpty node.Edges)
            then None
            else findStartExecution (Seq.head node.Edges)

        let getEdgesThatAreNotYourself actionId (newActionId : ActionId) map =
            let action = getNode graph actionId
            let newIds = Set.ofSeq <| 
                            Seq.choose (fun oldId ->
                                            match Map.tryFind oldId map with
                                            | Some id -> Some id
                                            | None -> findStartExecution oldId) action.Edges
            Set.remove newActionId newIds
            
        Map.fold 
            (fun newGraph oldId newId ->
                let edgeSet = getEdgesThatAreNotYourself oldId newId mapOfCollapsedExecutions
                let action = Action.create newId newId ActionType.ExecuteFinish edgeSet
                addNode action newGraph)
            empty
            mapOfCollapsedExecutions

    let simplify (graph:Graph) : Graph =
        let collapsedExecutions = collapse graph
        let transReduction = transitiveReduction collapsedExecutions
        transReduction