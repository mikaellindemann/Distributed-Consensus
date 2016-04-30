namespace HistoryConsensus

open Action
open FailureTypes
open Graph
open HistoryValidation

module LocalHistoryValidation =
    // When an events history is returned, the eventID (not counterpart) should ALWAYS be the event itself.
    let checkLocalHistoryForLocalInformationAboutOthers eventId history : Result<Graph, Graph> =
        if Graph.forall (fun action -> (fst action.Id) = eventId) history
        then Success history
        else Failure <| tagAllActionsWithFailureType HistoryAboutOthers history

    let localBeginningNodesValidation (history : Graph) : Result<Graph, Graph> = 
        if Map.isEmpty history.Nodes
        then
            Success history // History is empty, therefore no beginning nodes exist.
        else 
            let beginningNodes = Graph.getBeginningNodes history
            match beginningNodes |> List.isEmpty |> not with
            | true -> // has beginning nodes
                if (Graph.getBeginningNodes history).Length = 1
                then Success history // There must only be one
                else Failure <| tagAllActionsWithFailureType Malicious history // History has cycles. TODO: Find them
            | false -> // no beginning nodes
                Failure <| tagAllActionsWithFailureType Malicious history // History has cycles. TODO: Find them

    // Checks a local history against allowed incoming actions.
    // This means, that if an incoming action occurs, then every other incoming action from the same counterpart must
    // also occur.
    // The function furthermore requires a map of allowed incoming relations for the local history, which tells whether or not
    // a given relation are allowed to contact this event. If not, this event must be emitting malicious information.
    let checkLocalHistoryAgainstIncomingRelations allowedIngoingRelations history : Result<Graph, Graph> =
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
                | IncludedBy | ExcludedBy | SetPendingBy | CheckedConditionBy | CheckedMilestoneBy ->
                    let counterpart = fst node.CounterpartId
                    if not <| Map.containsKey counterpart ingoing
                    then Failure <| tagAllActionsWithFailureType FakeRelationsIn history
                    else 
                        match hasRequired nodes counterpart (Map.find counterpart ingoing) with
                        | None -> Failure <| tagAllActionsWithFailureType FakeRelationsIn history
                        | Some rest -> checker rest // All required relations were found
                | _ -> checker rest // This is not of our interest in this check

        checker historyAsList        

    let isIngoing = function
        | CheckedConditionBy | IncludedBy | ExcludedBy | SetPendingBy | CheckedMilestoneBy -> true
        | _ -> false

    // Checks that whenever an
    let checkLocalHistoryHasAllIngoingRelationsPerExecution allowedInGoingRelations (ownId:EventId) (history:Graph) : Result<Graph, Graph> =
        // Lazy initialization of this calculation.
        let ingoing = allowedInGoingRelations()
        let historyAsList = Map.toList history.Nodes 

        let rec iterateOverHistoryUntilIngoing historyList = 
            match historyList with 
            | [] -> Success history
            | ((eventId,_),action)::rest -> 
                if (isIngoing action.Type)
                then
                    let counterPart = fst action.CounterpartId
                    let (incomingOnEvent:ActionType list) = Map.find counterPart ingoing
                    matchRestOfIncoming incomingOnEvent historyList counterPart
                else 
                    iterateOverHistoryUntilIngoing rest
        and matchRestOfIncoming relationsLeft actions counterPartId =
            if relationsLeft.IsEmpty 
            then iterateOverHistoryUntilIngoing actions
            else 
                match actions with
                | [] -> Failure <| tagAllActionsWithFailureType Malicious history
                | (actionId,action)::rest -> if fst action.CounterpartId = counterPartId && List.exists (fun actionType -> action.Type = actionType) relationsLeft
                                             then 
                                                let elementToRemove = List.find (fun actionType -> action.Type = actionType) relationsLeft
                                                let newList = List.except [elementToRemove] relationsLeft
                                                matchRestOfIncoming newList rest counterPartId
                                             else Failure <| tagAllActionsWithFailureType Malicious history
        iterateOverHistoryUntilIngoing historyAsList

    let checkLocalHistoryAgainstOutgoingRelations (allowedOutgoingRelations : unit -> (EventId * ActionType) seq) history : Result<Graph, Graph> =
        // Lazy initialization of this calculation.
        let outgoing = Seq.toList <| allowedOutgoingRelations()
        let historyAsList = Map.toList history.Nodes

        let rec hasRequired actions (remainingRelations : (EventId * ActionType) list) : (ActionId * Action) list option =
            match remainingRelations with
            // If we have no more required relations
            | [] ->
                match actions with
                // Then the current action must be an execute finish, because no more outgoing relations should be in the history.
                | (_, action) :: rest when action.Type = ExecuteFinish -> Some rest
                // Locally, ingoing actions are alright, if the counterpart are yourself.
                | (_, action) :: rest when isIngoing action.Type -> hasRequired rest remainingRelations
                | _ -> None // There should be no more relations. Is this correct?

            // If there are more required relations
            | _ ->
                match actions with
                // And we have no more history, then there is a problem with the local history.
                | [] -> None
                // If we have more history
                // But this history is ingoing, ignore it
                | (_, action) :: rest when isIngoing action.Type -> hasRequired rest remainingRelations
                // Otherwise check action and rest.
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
                    | None -> Failure <| tagAllActionsWithFailureType FakeRelationsOut history
                    | Some rest' -> checker rest'
                // If one of these occurs, then they have happened without the correct relations following from an ExecuteStart
                | Includes | Excludes | SetsPending | ChecksCondition | ChecksMilestone | ExecuteFinish -> 
                    Failure <| tagAllActionsWithFailureType FakeRelationsOut history
                // We don't care about ingoing relations in this check.
                | _ -> checker rest

        checker historyAsList

    // Check that the correct order is established for single execution
    let checkLocalHistoryForCorrectOrder history : Result<Graph, Graph> =
        // Find beginning node (there can be 0 in an empty local history, and 1 otherwise) there exists a check for this
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
            else Failure <| tagAllActionsWithFailureType LocalTimestampOutOfOrder history

        // Implementation specific first value. The timestamps of our database should never be less than either 0 or 1.
        checkOrder firstActionId -1

    // Check that no incoming actions appear when executing.
    let checkLocalIncomingRelationsWhenExecuting (history:Graph) : Result<Graph, Graph> = 
        
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
            | IncludedBy | ExcludedBy | SetPendingBy | CheckedConditionBy | CheckedMilestoneBy -> false
            | _                                                            -> true

        // Check a list of execution chunks for validity.
        let rec checkChunks chunks = 
            // Check Action types in a chunk for validity.
            let checkChunk chunk = 
                                                             // The event accepts incoming requests from itself.
                List.forall (fun a -> hasValidType a.Type || (fst a.Id) = (fst a.CounterpartId)) chunk
            
            // Check every chunk for validity. If an illegal chunk is discovered, add a Failure to the list of failures.
            match chunks with 
            | c::cs     -> let chunkIsValid = checkChunk c
                           if chunkIsValid then checkChunks cs 
                           else Failure <| tagAllActionsWithFailureType IncomingChangesWhileExecuting history
            | _         -> Success history 

        checkChunks executionChunks

    let checkLocalHistoryForCorrectCounterpartOrder history : Result<Graph, Graph> =
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
                    if fst action.Id = fst counterpartId && (action.Type = ActionType.CheckedConditionBy || action.Type = ActionType.CheckedMilestoneBy || action.Type = ActionType.ExcludedBy || action.Type = ActionType.IncludedBy || action.Type = ActionType.SetPendingBy)
                    then 
                        // Special case where the local event is also the counterpart.
                        // This means that the actions can be interchangeable, because we also want the local timestamps to be in correct order.
                        if storedTimestamp - 1 = newTimestamp
                        then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (max storedTimestamp newTimestamp) lastTimestamps)
                        else Failure <| tagAllActionsWithFailureType CounterpartTimestampOutOfOrder history
                    else
                        if (Map.find (fst counterpartId) lastTimestamps) < snd counterpartId
                        // Correct order
                        then 
                            if not <| Set.isEmpty action.Edges
                            then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (snd counterpartId) lastTimestamps)
                            else Success history
                        // Wrong order.
                        else Failure <| tagAllActionsWithFailureType CounterpartTimestampOutOfOrder history
                // A counterpart which hasn't been seen before in this history
                else
                    if not <| Set.isEmpty action.Edges
                    then checkCounterpartOrder (getNode history <| Seq.head action.Edges) (Map.add (fst counterpartId) (snd counterpartId) lastTimestamps)
                    else Success history

        checkCounterpartOrder firstAction Map.empty        


    let outgoingRelationsMustHaveHigherTimestampsValidation history =
        if Graph.forall 
            (fun action -> 
                match action.Type with
                | Includes | Excludes | SetsPending | ChecksCondition | ChecksMilestone ->
                    (snd action.Id) < (snd action.CounterpartId)
                | _ -> true)
            history
        then Success history
        else Failure <| tagAllActionsWithFailureType PartOfCycle history


    let mapOutgoingToIngoing = function
        | SetsPending -> SetPendingBy
        | ChecksCondition -> CheckedConditionBy
        | ChecksMilestone -> CheckedMilestoneBy
        | Includes -> IncludedBy
        | Excludes -> ExcludedBy
        | _ -> failwith "Not an outgoing relation!"

    let allowedIngoingRelationsMapMaker eventId (rules : DCRRules) (unit : unit) =
        let ingoing = Set.filter (fun (_, toId, _) -> toId = eventId) rules
        let mafds = Set.map (fun (fromId, _, actionType) -> (fromId, mapOutgoingToIngoing actionType)) ingoing

        let groups = Seq.groupBy (fun (fromId, _) -> fromId) mafds

        Map.ofSeq (Seq.map (fun (fromId, actionTypes) -> (fromId, List.ofSeq (Seq.map (fun (_,types) -> types) actionTypes))) groups)

    let allowedOutgoingRelationsMaker eventId (rules : DCRRules) (unit : unit) =
        let outgoing = Set.filter (fun (fromId, _, _) -> fromId = eventId) rules
        Seq.map (fun (_,toId, actionType) -> (toId, actionType)) outgoing

    // Validating
    let giantLocalCheck input eventId (rules : DCRRules) =
        input |> localBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId 
            >>= checkLocalHistoryForCorrectOrder
            >>= checkLocalHistoryForCorrectCounterpartOrder
            >>= outgoingRelationsMustHaveHigherTimestampsValidation
            >>= checkLocalIncomingRelationsWhenExecuting
            >>= checkLocalHistoryAgainstIncomingRelations (allowedIngoingRelationsMapMaker eventId rules)
            >>= checkLocalHistoryAgainstOutgoingRelations (allowedOutgoingRelationsMaker eventId rules)
            >>= checkLocalHistoryHasAllIngoingRelationsPerExecution (allowedIngoingRelationsMapMaker eventId rules) eventId


    let smallerLocalCheck input eventId =
        input |> localBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId 
            >>= checkLocalHistoryForCorrectOrder
            >>= checkLocalHistoryForCorrectCounterpartOrder
            >>= checkLocalIncomingRelationsWhenExecuting
            >>= outgoingRelationsMustHaveHigherTimestampsValidation