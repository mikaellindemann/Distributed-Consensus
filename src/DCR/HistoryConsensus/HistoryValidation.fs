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