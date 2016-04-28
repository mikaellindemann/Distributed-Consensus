﻿namespace HistoryConsensus

open Action
open FailureTypes
open Graph
open HistoryValidation

module DCRSimulator =
    type EventState = bool * bool * bool // Included * Pending * Executed
    type DCRState = Map<EventId, EventState>

    let buildCounterMap history : Map<ActionId, int> =
        Graph.fold 
            (fun counterMap action ->
                Set.fold 
                    (fun counterMap' edge ->
                        let newValue = (Map.find edge counterMap') + 1
                        Map.add edge newValue counterMap')
                    counterMap 
                    action.Edges)
            (Map.map (fun _ _ -> 0) history.Nodes) // This maps every actionId to 0
            history

    let updateStateForRelation state eventId relationType =
        let (included, pending, executed) = Map.find eventId state

        match relationType with
        | SetsPending -> Map.add eventId (included, true, executed) state
        | Includes    -> Map.add eventId (true, pending, executed)  state
        | Excludes    -> Map.add eventId (false, pending, executed) state
        | _ -> failwith "Wrong relation type in model!"

    let isExecutable conditions milestones state =
        if Set.fold 
            (fun exec (_,conditionEventId,_) ->
                let (included, _, executed) = Map.find conditionEventId state
                exec && (not included || executed)) // If included and not executed -> Not executable.
            true // Assume executable
            conditions

        then
            Set.fold
                (fun exec (_,milestoneEventId,_) ->
                    let (included, pending, _) = Map.find milestoneEventId state
                    exec && (not included || not pending)) // If included and pending -> Not executable
                true // Assume executable
                milestones
        else
            false

    let execute (state : DCRState) (rules : DCRRules) eventId : DCRState option =
        let eventRules = Set.filter (fun (id,_,_) -> id = eventId) rules

        
        let conditions = Set.filter (fun (_,from,relationType) -> relationType = ActionType.ChecksCondition && from = eventId) eventRules
        let milestones = Set.filter (fun (_,from,relationType) -> relationType = ActionType.ChecksMilestone && from = eventId) eventRules

        if not <| isExecutable conditions milestones state
        then None // Not executable, this execution is illegal
        else
            // Executable -> Apply state update.
            match Map.tryFind eventId state with
            | None -> None
            | Some (included, pending, _) ->
                Some <| Set.fold
                    (fun state' (_,toEventId,relationType) ->
                        updateStateForRelation state' toEventId relationType)
                    (Map.add eventId (included, pending, true) state) // Set executed state of current event.
                    (Set.filter (fun (_,_,relationType) -> relationType <> ActionType.ChecksCondition && relationType <> ActionType.ChecksMilestone) eventRules) // Only look at rules that are not conditions and milestones.

    let updateCounterMap counterMap history actionId : Map<ActionId, int> =
        let counterMap' = Map.remove actionId counterMap // Remove the action that was just executed.

        Set.fold // Decrement all the actions that are pointed at by the executed action.
            (fun map toActionId -> 
                let newValue = (Map.find toActionId map) - 1
                Map.add toActionId newValue map)
            counterMap'
            (Graph.getNode history actionId).Edges


    let simulate (history : Graph) (initialState : DCRState) (rules : DCRRules) : Result<Graph, Graph> =
        let initialCounterMap = buildCounterMap history
        
        let rec simulateExecution state counterMap failureList : ActionId list =
            if Map.isEmpty counterMap
            then failureList
            else
                // Take the first action that has a counter of 0
                let ((eventId, _) as actionId,_) = 
                    Map.filter (fun _ count -> count = 0) counterMap |> Map.toSeq |> Seq.head

                match execute state rules eventId with // Execute if possible
                | None -> 
                    let counterMap' = updateCounterMap counterMap history actionId // Update counters after execution
                    simulateExecution state counterMap' (actionId :: failureList)
                | Some state' -> 
                    let counterMap' = updateCounterMap counterMap history actionId // Update counters after execution
                    simulateExecution state' counterMap' failureList // Recursive step

        let failures = simulateExecution initialState initialCounterMap []

        if List.isEmpty failures
        then Success history
        else
            Failure <| List.fold 
                (fun hist fail -> 
                    let node = Graph.getNode hist fail
                    Graph.addNode { node with FailureTypes = Set.add ExecutedWithoutProperState node.FailureTypes } hist)
                history
                failures