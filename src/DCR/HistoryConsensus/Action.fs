﻿namespace HistoryConsensus

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
            CounterpartEventId: EventId;
            Type: ActionType;
            Edges: ActionId Set;
        }

    let create id counterpartId actionType edges = 
        {   Id = id; 
            CounterpartEventId = counterpartId; 
            Type = actionType; 
            Edges = edges}