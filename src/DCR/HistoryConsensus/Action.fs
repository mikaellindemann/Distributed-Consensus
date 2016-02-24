namespace HistoryConsensus

module Action =
    type EventId = string
    type LocalTimeStamp = string
    
    type ActionId = (EventId * LocalTimeStamp)
    
    type ActionType =
        | Includes
        | IncludedBy
        | Excludes
        | ExcludedBy
        | SetsPending
        | SetPendingBy
        | CheckedConditon of bool
        | ChecksConditon of bool
        | Locks
        | LockedBy
        | Unlocks
        | UnlockedBy
        | ExecuteStart
        | ExecuteFinish
    
    type Action =
        {
            Id: ActionId;
            CounterpartEventId: EventId;
            Type: ActionType;
            Edges: ActionId list;
        }

