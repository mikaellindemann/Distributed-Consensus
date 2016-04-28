namespace HistoryConsensus

open Action;
open FailureTypes
open Graph;

module HistoryValidation =
    let tagAllActionsWithFailureType failureType history =
        { Nodes = (Map.map (fun _ action -> { action with FailureTypes = Set.add failureType action.FailureTypes}) history.Nodes)}

    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType
        with
            member x.GetSuccess =
                match x with
                | Success s -> s
                | _ -> failwith "Check the result first!"
            member x.GetFailure =
                match x with
                | Failure f -> f
                | _ -> failwith "Check the result first!"

    type DCRRules = Set<EventId * EventId * ActionType> // From * To * Type

    ///Check a given Result and call the given function on it, if it is a Success.
    let bind func resultInput = 
        match resultInput with 
        | Success s -> func s
        | Failure f -> Failure f

    let (>>=) resultInput func = bind func resultInput

    let agreeOnAmtOfActions  ((history1 : Graph), (history2 : Graph))  : Result<Graph * Graph, Graph * Graph> =
        let eventId1 = fst (Map.pick (fun x y -> Some x) history1.Nodes)
        let eventId2 = fst (Map.pick (fun x y -> Some x) history2.Nodes)
        if ((Map.filter (fun actionId action -> fst action.CounterpartId = eventId2) history1.Nodes).Count = (Map.filter (fun actionId action -> fst action.CounterpartId = eventId1) history2.Nodes).Count)
        then
            Success (history1, history2)
        else 
            Failure <| (tagAllActionsWithFailureType Maybe history1, tagAllActionsWithFailureType Maybe history2)


    let agreeOnActions  ((history1 : Graph), (history2 : Graph))  : Result<Graph * Graph, Graph * Graph> =
        let eventId1 = fst (Map.pick (fun x y -> Some x) history1.Nodes)
        let eventId2 = fst (Map.pick (fun x y -> Some x) history2.Nodes)
        let actionsAbout2From1 = Map.filter (fun actionId action -> fst action.CounterpartId = eventId2) history1.Nodes |> Map.toSeq
        let actionsAbout1From2 = Map.filter (fun actionId action -> fst action.CounterpartId = eventId1) history2.Nodes |> Map.toSeq
        
        let zipped = Seq.zip actionsAbout2From1 actionsAbout1From2
        if (Seq.exists (fun ((actionId1,action1), (actionId2,action2)) -> snd action2.Id = snd action1.CounterpartId && snd action1.Id = snd action2.CounterpartId ) zipped)
        then
            Success (history1, history2)
        else 
            Failure <| (tagAllActionsWithFailureType Maybe history1, tagAllActionsWithFailureType Maybe history2)

    let pairValidationCheck (history1 : Graph) (history2 : Graph)  : Result<Graph * Graph, Graph * Graph> =
        (history1, history2) |> agreeOnAmtOfActions >>= agreeOnActions