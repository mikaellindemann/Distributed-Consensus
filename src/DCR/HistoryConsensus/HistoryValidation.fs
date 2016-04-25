namespace HistoryConsensus

open Action;
open Graph;

module HistoryValidation =

    type FailureType =
        | HistoryAboutOthers
        | FakeRelationsOut
        | FakeRelationsIn
        | LocalTimestampOutOfOrder
        | IncomingChangesWhileExecuting
        | PartialOutgoingWhenExecuting
        | CounterpartTimestampOutOfOrder
        | Malicious // TODO: Remove

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

        let rec cycleDfsCheck node trace failures =
            Set.fold
                (fun acc edge ->
                    if List.contains node.Id trace
                    then
                        // Todo: Is it correct that it is always the last two that created the cycle?
                        Set.add ([fst node.Id; fst <| List.head trace], Malicious) acc
                    else
                        cycleDfsCheck (Graph.getNode history edge) (node.Id :: trace) acc
                )
                failures
                node.Edges

        let failureSet = 
            List.fold 
                (fun failures node ->
                    cycleDfsCheck node [] failures
                )
                Set.empty
                beginningNodes

        match Set.toList failureSet with
        | [] -> Success history
        | x  -> Failure x


    let agreeOnAmtOfActions  (history1 : Graph)   (history2 : Graph)  : Result<(Graph*Graph), FailureT list> =
        let eventId1 = (Map.pick (fun x y -> Some x) history1.Nodes)
        let eventId2 = (Map.pick (fun x y -> Some x) history2.Nodes)
        if ((Map.filter (fun actionId action -> action.CounterpartId = eventId2) history1.Nodes).Count = (Map.filter (fun actionId action -> action.CounterpartId = eventId1) history2.Nodes).Count)
        then
            Success (history1, history2)
        else 
            Failure [([string eventId1; string eventId2], Malicious)];