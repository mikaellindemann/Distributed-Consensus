namespace HistoryConsensus

open Action
open FailureTypes
open Graph

module HistoryValidation =
    val tagAllActionsWithFailureType : FailureType -> Graph -> Graph

    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType
        with
            member GetSuccess : 'SuccessType
            member GetFailure : 'FailureType

    type DCRRules = Set<EventId * EventId * ActionType> // From * To * Type

    ///Check a given Result and call the given function on it, if it is a Success.
    val bind : ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
    val (>>=) : Result<'a, 'b> -> ('a -> Result<'c, 'b>) -> Result<'c, 'b>

    val agreeOnAmtOfActions : Graph -> Graph -> Result<Graph * Graph, Graph * Graph>