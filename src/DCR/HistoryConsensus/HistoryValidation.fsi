﻿namespace HistoryConsensus

module HistoryValidation =
    ///A validation result type, either a Success or a Failure.
    type Result<'SuccessType, 'FailureType> = 
        | Success of 'SuccessType
        | Failure of 'FailureType

    ///Check a given Result and call the given function on it, if it is a Success.
    val bind : ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
    val (>>=) : Result<'a, 'b> -> ('a -> Result<'c, 'b>) -> Result<'c, 'b>