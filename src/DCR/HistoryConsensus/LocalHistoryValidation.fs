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
        

    // Check a single local history if it contacts or are contacted by wrong events.
    let checkLocalHistoryAgainstRelations allowedIngoingRelations allowedOutgoingRelations history : Result<Graph, FailureT list> =
        Graph.fold 
            (fun status action -> 
                match status with
                | Success i -> 
                    match action.Type with
                    // TODO: Make it stricter, denoting which kind of actions can come from which events,
                    // and also check that if one relation comes from another event, then all of its outgoing relations
                    // must be in the execution as well.
                    | ExecuteStart | ExecuteFinish -> Success i
                    | Includes | Excludes | SetsPending | ChecksConditon ->
                        if Set.contains (fst action.CounterpartId) allowedOutgoingRelations
                        then Success i
                        else Failure [([fst action.Id], HasWrongOutgoingAction)]
                    | IncludedBy | ExcludedBy | SetPendingBy | CheckedConditon ->
                        if Set.contains (fst action.CounterpartId) allowedIngoingRelations
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

    // Check that no ingoing actions can appear when executing.

    // Check that all relations appears inside execution

    let giantLocalCheck input eventId allowedIngoingRelations allowedOutgoingRelations =
        input |> hasBeginningNodesValidation
            >>= checkLocalHistoryForLocalInformationAboutOthers eventId 
            >>= checkLocalForCorrectOrder
            >>= checkLocalHistoryAgainstRelations allowedIngoingRelations allowedOutgoingRelations