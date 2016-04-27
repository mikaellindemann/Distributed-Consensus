namespace HistoryConsensus

open FailureTypes

module Action =
    type EventId = string
    type LocalTimeStamp = int
    
    type ActionId = (EventId * LocalTimeStamp)
    
    type ActionType =
        | Includes
        | IncludedBy
        | Excludes
        | ExcludedBy
        | SetsPending
        | SetPendingBy
        | CheckedConditionBy
        | ChecksCondition
        | ExecuteStart
        | ExecuteFinish

    type Action =
        {
            Id: ActionId;
            CounterpartId: ActionId;
            Type: ActionType;
            Edges: ActionId Set;
            FailureTypes: FailureType Set;
        }

    val create : ActionId -> ActionId -> ActionType -> ActionId Set -> Action
