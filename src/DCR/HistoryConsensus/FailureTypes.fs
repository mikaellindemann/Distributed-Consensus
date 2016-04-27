namespace HistoryConsensus

module FailureTypes =
    type FailureType =
        | HistoryAboutOthers
        | FakeRelationsOut
        | FakeRelationsIn
        | LocalTimestampOutOfOrder
        | IncomingChangesWhileExecuting
        | PartialOutgoingWhenExecuting
        | CounterpartTimestampOutOfOrder
        | PartOfCycle
        | Maybe
        | ExecutedWithoutProperState
        | Malicious // TODO: Remove