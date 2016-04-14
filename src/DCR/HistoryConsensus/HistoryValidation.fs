namespace HistoryConsensus

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
        