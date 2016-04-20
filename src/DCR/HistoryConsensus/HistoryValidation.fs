namespace HistoryConsensus

open Action;
open Graph;

module HistoryValidation =

    type FailureType =
        | Maybe
        | HasWrongOutgoingAction
        | HasWrongIngoingAction
        | Malicious

    type FailureT = string list * FailureType

    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType

    ///Check a given Result and call the given function on it, if it is a Success.
    let bind func resultInput = 
        match resultInput with 
        | Success s -> func s
        | Failure f -> Failure f

    let (>>=) resultInput func = bind func resultInput
    
    let validationExample = 
        let failureInput = "2 A"
        let successInput = "okaystring"

        let noSpacesValidation input = 
            if Seq.exists (fun c -> System.Char.IsWhiteSpace(c)) input
            then Failure "Input contained spaces."
            else Success input

        let noNumberValidation input =
            if Seq.exists (fun c -> System.Char.IsDigit(c)) input
            then Failure "Input contains a number."
            else Success input

        let noUpperCaseValidation input =
            if Seq.exists (fun c -> System.Char.IsUpper(c)) input
            then Failure "Input contains an uppercase character."
            else Success input

        let failureResult = failureInput |> noSpacesValidation >>= noNumberValidation >>= noUpperCaseValidation
        let successResult = successInput |> noSpacesValidation >>= noNumberValidation >>= noUpperCaseValidation
        printf "Failure result: %O" failureResult
        printf "Success result: %O" successResult
        

    // If no beginning nodes exist, we have either an empty history, or a history with cycles
    let hasBeginningNodesValidation (history : Graph) : Result<Graph, FailureT list> = 
        match Graph.getBeginningNodes history |> List.isEmpty |> not with
        | true -> Success history // Is this always correct?
        | false -> 
            if Map.isEmpty history.Nodes
            then Success history // History is empty, therefore no beginning nodes exist.
            else Failure [(["Find cycles!"],Malicious)] // History has cycles. TODO: Find them

    let noCycleValidation (history : Graph) : Result<Graph, FailureT list> =
        let beginningNodes = Graph.getBeginningNodes history

        let rec cycleDfsCheck node trace : Result<Graph, FailureT list> =
            if List.contains node.Id trace
            then Failure [([(fst node.Id)], Malicious)]
            else 
                Set.fold 
                    (fun status edge -> 
                        match status with
                        | Failure g -> Failure g
                        | Success g -> cycleDfsCheck (Graph.getNode history edge) (node.Id :: trace))
                    (Success history)
                    node.Edges

        List.fold 
            (fun status node ->
                match status with
                | Failure g -> Failure g
                | Success g -> cycleDfsCheck node [])
            (Success history)
            beginningNodes


    

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