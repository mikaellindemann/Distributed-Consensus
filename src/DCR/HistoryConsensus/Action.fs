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
        | CheckedMilestoneBy
        | ChecksMilestone
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

    let create id counterpartId actionType edges = 
        {   Id = id; 
            CounterpartId = counterpartId; 
            Type = actionType; 
            Edges = edges;
            FailureTypes = Set.empty}