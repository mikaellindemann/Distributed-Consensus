namespace HistoryConsensus

open Action;
open Graph;

module HistoryValidation =
    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType

    ///Check a given Result and call the given function on it, if it is a Success.
    let bind func resultInput = 
        match resultInput with 
        | Success s -> func s
        | Failure f -> Failure f

    let (>>=) resultInput func = 
        bind func resultInput
    
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
    let hasBeginningNodesValidation (history : Graph) = 
        match getBeginningNodes history |> List.isEmpty |> not with
        | true -> Success history // Is this always correct?
        | false -> 
            if Map.isEmpty history.Nodes
            then Success history // History is empty, therefore no beginning nodes exist.
            else Failure history // History has cycles.

    let noCycleValidation (history : Graph) =
        let beginningNodes = getBeginningNodes history

        let rec cycleDfsCheck node trace =
            if List.contains node.Id trace
            then Failure history
            else 
                Set.fold 
                    (fun status edge -> 
                        match status with
                        | Failure g -> Failure g
                        | Success g -> cycleDfsCheck (getNode history edge) (node.Id :: trace))
                    (Success history)
                    node.Edges

        List.fold 
            (fun status node ->
                match status with
                | Failure g -> Failure g
                | Success g -> cycleDfsCheck node [])
            (Success history)
            beginningNodes