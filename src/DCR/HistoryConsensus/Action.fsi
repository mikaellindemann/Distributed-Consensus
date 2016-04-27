namespace HistoryConsensus

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
        | CheckedCondition// of bool
        | ChecksCondition// of bool
        | ExecuteStart
        | ExecuteFinish

    type Action =
        {
            Id: ActionId;
            CounterpartId: ActionId;
            Type: ActionType;
            Edges: ActionId Set;
        }

    val create : ActionId -> ActionId -> ActionType -> ActionId Set -> Action
