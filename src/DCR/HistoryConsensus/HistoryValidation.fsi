namespace HistoryConsensus

open Action
open Graph

module HistoryValidation =
    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType

    type FailureType =
        | Maybe
        | HasWrongOutgoingAction
        | HasWrongIngoingAction
        | Malicious

    type FailureT = EventId list * FailureType

    ///Check a given Result and call the given function on it, if it is a Success.
    val bind : ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
    val (>>=) : Result<'a, 'b> -> ('a -> Result<'c, 'b>) -> Result<'c, 'b>

    val hasBeginningNodesValidation : Graph -> Result<Graph, FailureT list>