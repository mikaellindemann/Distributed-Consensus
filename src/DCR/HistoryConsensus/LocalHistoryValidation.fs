namespace HistoryConsensus
open Action
open Graph
open HistoryValidation

module LocalHistoryValidation =
    // When an events history is returned, the eventID (not counterpart) should ALWAYS be the event itself.
    let checkLocalHistoryForLocalInformationAboutOthers eventId history : Result<Graph, FailureT list> =
        if Graph.forall (fun action -> (fst action.Id) = eventId) history
        then Success history
        else Failure [([eventId], Malicious)]

    // Checks a local history against allowed incoming actions.
    // This means, that if an incoming action occurs, then every other incoming action from the same counterpart must
    // also occur.
    // The function furthermore requires a map of allowed incoming relations for the local history, which tells whether or not
    // a given relation are allowed to contact this event. If not, this event must be emitting malicious information.
    let checkLocalHistoryAgainstIncomingRelations allowedIngoingRelations history : Result<Graph, FailureT list> =
        let ingoing = allowedIngoingRelations()
        let historyAsList = Map.toList history.Nodes

        let rec hasRequired actions counterpartId remaindingRelations : (ActionId * Action) list option =
            match remaindingRelations with
            | [] -> Some actions
            | _ -> 
                match actions with
                | [] -> None
                | (_,action) :: rest ->
                    if List.contains action.Type remaindingRelations
                    then hasRequired rest counterpartId (List.filter (fun actionType -> actionType <> action.Type) remaindingRelations)
                    else None

        let rec checker nodes =
            match nodes with
            | [] -> Success history
            | (_, node) :: rest ->
                match node.Type with
                | IncludedBy | ExcludedBy | SetPendingBy | CheckedCondition ->
                    let counterpart = fst node.CounterpartId
                    if not <| Map.containsKey counterpart ingoing
                    then Failure [([fst node.Id], Malicious)]
                    else 
                        match hasRequired nodes counterpart (Map.find counterpart ingoing) with
                        | None -> Failure [([fst node.Id], Malicious)]
                        | Some rest -> checker rest // All required relations were found
                | _ -> checker rest // This is not of our interest in this check

        checker historyAsList        

    let checkLocalHistoryAgainstOutgoingRelations allowedOutgoingRelations history : Result<Graph, FailureT list> =
        // Lazy initialization of this calculation.
        let outgoing = allowedOutgoingRelations()
        let historyAsList = Map.toList history.Nodes

        let rec hasRequired actions (remainingRelations : (EventId * ActionType) list) : (ActionId * Action) list option =
            match remainingRelations with
            // If we have no more required relations
            | [] ->
                match actions with
                // Then the current action must be an execute finish, because no more outgoing relations should be in the history.
                | (_, action) :: rest when action.Type = ExecuteFinish -> Some rest
                | _ -> None // There should be no more relations. Is this correct?

            // If there are more required relations
            | _ ->
                match actions with
                // And we have no more history, then there is a problem with the local history.
                | [] -> None
                // If we have more history
                | (_, action) :: rest ->
                    // The type of the action (together with the counterpart who initiated it) should exist in the remaining relations.
                    if List.contains (fst action.CounterpartId, action.Type) remainingRelations
                    // If this is the case we continue to scan for more required relations.
                    then hasRequired rest (List.filter (fun actionType -> actionType <> (fst action.CounterpartId, action.Type)) remainingRelations)
                    // Otherwise we have a problem.
                    else None

        let rec checker nodes =
            match nodes with
            // If we have an empty history or rest, this means that all rules so far has been fulfilled.
            // Therefore we have a success case.
            | [] -> Success history
            | (_,action) :: rest -> 
                match action.Type with
                // If we find an ExecuteStart, then we should call the helper function which checks that all outgoing relations
                // required by the DCR-Graph are located in this history.
                | ExecuteStart -> 
                    match hasRequired rest outgoing with
                    | None -> Failure [([fst action.Id], Malicious)]
                    | Some rest' -> checker rest'
                // If one of these occurs, then they have happened without the correct relations following from an ExecuteStart
                | Includes | Excludes | SetsPending | ChecksCondition | ExecuteFinish -> Failure [([fst action.Id], Malicious)]
                // We don't care about ingoing relations in this check.
                | _ -> checker rest

        checker historyAsList
            

    // Check a single local history if it contacts or are contacted by wrong events.
    let checkLocalHistoryAgainstRelations allowedIngoingRelations allowedOutgoingRelations history : Result<Graph, FailureT list> =
        let ingoing = allowedIngoingRelations()
        let outgoing = allowedOutgoingRelations()
        Graph.fold 
            (fun status action -> 
                match status with
                | Success i -> 
                    match action.Type with
                    // TODO: Make it stricter, denoting which kind of actions can come from which events,
                    // and also check that if one relation comes from another event, then all of its outgoing relations
                    // must be in the execution as well.
                    | ExecuteStart | ExecuteFinish -> Success i
                    | Includes | Excludes | SetsPending | ChecksCondition ->
                        if Set.contains (fst action.CounterpartId) ingoing
                        then Success i
                        else Failure [([fst action.Id], HasWrongOutgoingAction)]
                    | IncludedBy | ExcludedBy | SetPendingBy | CheckedCondition ->
                        if Set.contains (fst action.CounterpartId) outgoing
                        then Success i
                        else Failure [([fst action.Id], HasWrongIngoingAction)]
                | Failure i -> Failure i
            )
            (Success history)
            history

    // Check that the correct order is established for single execution
    let checkLocalForCorrectOrder history : Result<Graph, FailureT list> =
        // Find beginning node (there can be 0 in an empty local history, and 1 otherwise)
        let (firstActionId, _)= Seq.minBy (fun (id,_) -> snd id) (Map.toSeq history.Nodes)

        let rec checkOrder actionId lastTimestamp =
            let timestamp = snd actionId
            if timestamp > lastTimestamp
            then 
                let node = getNode history actionId
                if Set.isEmpty node.Edges
                then Success history
                // In local history, there can be at most one successor on each action.
                else checkOrder (Seq.head node.Edges) timestamp
            else Failure [([fst actionId], Malicious)]

        // Implementation specific first value. The timestamps of our database should never be less than either 0 or 1.
        checkOrder firstActionId -1

    // Check that no incoming actions appear when executing.
    let checkLocalIncomingRelationsWhenExecuting eventId (history:Graph) : Result<Graph, FailureT list> = 
        
        // Get execution chunks, as a list of [ExecutionStart Action, random Action, random Action, ExecutionFinish Action])
        let executionChunks = 
            // From a given ExecuteStart Action take following Actions in the history until an ExecutionFinish is found or the history is empty.
            let rec takeNodesUntilExecuteFinish action acc = 
                if (action.Type = ExecuteFinish || action.Edges.IsEmpty) then action::acc
                else let appendedAcc = action::acc
                     let next = (Graph.getNode history action.Edges.MinimumElement)
                     takeNodesUntilExecuteFinish next appendedAcc

            // When an ExecuteStart is encountered, generate a chunk of Actions until the next ExectuteFinish is encountered.
            let generateChunkIfExecuteStart action acc : Map<Action, Action list> = 
                if action.Type = ExecuteStart 
                then Map.add action <| takeNodesUntilExecuteFinish action [] <| acc
                else acc

            // Generate a list of execution chunks from the given history.
            Map.fold (fun acc actionId _ -> generateChunkIfExecuteStart <| Graph.getNode history actionId <| acc) Map.empty history.Nodes
                |> Map.toList
                |> List.map snd
        
        // Check whether a given Action type is valid/allowed.
        let hasValidType actionType = 
            match actionType with 
            | IncludedBy | ExcludedBy | SetPendingBy | CheckedCondition  -> false
            | _                                                          -> true

        // Check a list of execution chunks for validity.
        let rec checkChunks chunks = 
            // Check Action types in a chunk for validity.
            let checkChunk chunk = 
                List.forall (fun a -> hasValidType a.Type) chunk
            
            // Check every chunk for validity. If an illegal chunk is discovered, add a Failure to the list of failures.
            match chunks with 
            | c::cs     -> let chunkIsValid = checkChunk c
                           if chunkIsValid then checkChunks cs 
                           else Failure [([eventId], HasWrongIngoingAction)]
            | _         -> Success history 

        checkChunks executionChunks

    // Validating
    let giantLocalCheck input eventId allowedIngoingRelations allowedOutgoingRelations =
        input |> hasBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId 
            >>= checkLocalForCorrectOrder
            >>= checkLocalHistoryAgainstRelations allowedIngoingRelations allowedOutgoingRelations
            >>= checkLocalIncomingRelationsWhenExecuting eventId 