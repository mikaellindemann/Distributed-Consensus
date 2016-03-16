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
        | CheckedConditon// of bool
        | ChecksConditon// of bool
        | Locks
        | LockedBy
        | Unlocks
        | UnlockedBy
        | ExecuteStart
        | ExecuteFinish
    
    type Action =
        {
            Id: ActionId;
            CounterpartId: ActionId;
            Type: ActionType;
            Edges: ActionId Set;
        }

    let create id counterpartId actionType edges = 
        {   Id = id; 
            CounterpartId = counterpartId; 
            Type = actionType; 
            Edges = edges}