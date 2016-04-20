namespace HistoryConsensus

open Action
open Graph
open HistoryValidation

module LocalHistoryValidation =
    // When an events history is returned, the eventID (not counterpart) should ALWAYS be the event itself.
    let checkLocalHistoryForLocalInformationAboutOthers eventId history : Result<Graph, FailureT list> =
        if Graph.forall (fun action -> (fst action.Id) = eventId) history
        then Success history
        else Failure [([eventId], HistoryAboutOthers)]

    // Checks a local history against allowed incoming actions.
    // This means, that if an incoming action occurs, then every other incoming action from the same counterpart must
    // also occur.
    // The function furthermore requires a map of allowed incoming relations for the local history, which tells whether or not
    // a given relation are allowed to contact this event. If not, this event must be emitting malicious information.
    let checkLocalHistoryAgainstIncomingRelations allowedIngoingRelations history : Result<Graph, FailureT list> =
        let ingoing = allowedIngoingRelations()
        let historyAsList = Map.toList history.Nodes

        let rec hasRequired actions counterpartId remainingRelations : (ActionId * Action) list option =
            match remainingRelations with
            | [] -> Some actions
            | _ -> 
                match actions with
                | [] -> None
                | (_,action) :: rest ->
                    if List.contains action.Type remainingRelations
                    then hasRequired rest counterpartId (List.filter (fun actionType -> actionType <> action.Type) remainingRelations)
                    else None

        let rec checker nodes =
            match nodes with
            | [] -> Success history
            | (_, node) :: rest ->
                match node.Type with
                | IncludedBy | ExcludedBy | SetPendingBy | CheckedCondition ->
                    let counterpart = fst node.CounterpartId
                    if not <| Map.containsKey counterpart ingoing
                    then Failure [([fst node.Id], FakeRelationsIn)]
                    else 
                        match hasRequired nodes counterpart (Map.find counterpart ingoing) with
                        | None -> Failure [([fst node.Id], FakeRelationsIn)]
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
                    | None -> Failure [([fst action.Id], FakeRelationsOut)]
                    | Some rest' -> checker rest'
                // If one of these occurs, then they have happened without the correct relations following from an ExecuteStart
                | Includes | Excludes | SetsPending | ChecksCondition | ExecuteFinish -> Failure [([fst action.Id], FakeRelationsOut)]
                // We don't care about ingoing relations in this check.
                | _ -> checker rest

        checker historyAsList

    // Check that the correct order is established for single execution
    let checkLocalHistoryForCorrectOrder history : Result<Graph, FailureT list> =
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
            else Failure [([fst actionId], LocalTimestampOutOfOrder)]

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
                           else Failure [([eventId], IncomingChangesWhileExecuting)]
            | _         -> Success history 

        checkChunks executionChunks

    let checkLocalHistoryForCorrectCounterpartOrder history : Result<Graph, FailureT list> =
        // Find beginning node (there can be 0 in an empty local history, and 1 otherwise)
        let (_, firstAction)= Seq.minBy (fun (id,_) -> snd id) (Map.toSeq history.Nodes)

        let rec checkCounterpartOrder action lastTimestamps =
            if action.Type = ExecuteStart || action.Type = ExecuteFinish
            then
                if not <| Set.isEmpty action.Edges
                then checkCounterpartOrder (getNode history <| Seq.head action.Edges) lastTimestamps
                else Success history
            else
                let counterpartId = action.CounterpartId
                if Map.containsKey (fst counterpartId) lastTimestamps
                // A counterpart which has been in the history before.
                then
                    let storedTimestamp = Map.find (fst counterpartId) lastTimestamps
                    let newTimestamp = snd counterpartId
                    if fst action.Id = fst counterpartId
                    then 
                        // Special case where the local event is also the counterpart.
                        // This means that the actions can be interchangeable, because we also want the local timestamps to be in correct order.
                        if abs (storedTimestamp - newTimestamp) >= 1
                        then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (max storedTimestamp newTimestamp) lastTimestamps)
                        else Failure [([fst action.Id], CounterpartTimestampOutOfOrder)] 
                    else
                        if (Map.find (fst counterpartId) lastTimestamps) < snd counterpartId
                        // Correct order
                        then 
                            if not <| Set.isEmpty action.Edges
                            then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (snd counterpartId) lastTimestamps)
                            else Success history
                        // Wrong order.
                        else Failure [([fst action.Id], CounterpartTimestampOutOfOrder)] 
                // A counterpart which hasn't been seen before in this history
                else
                    if not <| Set.isEmpty action.Edges
                    then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (snd counterpartId) lastTimestamps)
                    else Success history

        checkCounterpartOrder firstAction Map.empty

    // Check that no ingoing actions can appear when executing.

        

    // Validating
    let giantLocalCheck input eventId allowedIngoingRelations allowedOutgoingRelations =
        input |> hasBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId 
            >>= checkLocalHistoryForCorrectOrder
            >>= checkLocalHistoryForCorrectCounterpartOrder
            >>= checkLocalHistoryAgainstIncomingRelations allowedIngoingRelations
            >>= checkLocalHistoryAgainstOutgoingRelations allowedOutgoingRelations


    let smallerLocalCheck input eventId =
        input |> hasBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId
            >>= checkLocalHistoryForCorrectOrder
            >>= checkLocalHistoryForCorrectCounterpartOrder