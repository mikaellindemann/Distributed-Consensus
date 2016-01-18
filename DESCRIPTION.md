# Algorithm Description

The algorithm is implemented using the following F# data types:

	///A unique ifdentifier type of the local timestamp and ID of a given node.
    type Age = {
        LocalTimestamp: int;
        Id: string
    }
    
    ///A state of a node.
    type State = {
        Pending: bool;
        Excluded: bool
    }
    
    ///The beginning and end of an execution of a Node. The bool signifies whether the execution was succesful. The Age is an identifier for when the Execution started and when it ended.
    type Execution = 
        | Begin of Age
        | Finish of bool * Age
    
    ///A type for the different kinds of edges between nodes in the graph.
    type Relation = {
        | Includes
        | IncludedBy
        | Excludes
        | ExcludedBy
        | SetsPending
        | SetPendingBy
        | CheckedConditon of bool
        | ChecksConditon of bool
    }
    
    ///The "log entry" in the built history. Every node stores one StateChange for every event that occurs related involving a Relation. The final history is then built from all StateChanges of all nodes.
    type StateChange = {
        Author: Age;
        Relation: Relation;
        Counterpart: string option
    }

The algorithm works by collecting every StateChange from all nodes in the system and determining what the history of execution was in the system. 
Using the `LocalTimestamp` and `Id` of `Age`s we can build a _happens-before_ relations of the system up to the current point in time, and determine what/if any nodes are faulty. 
 

## Relation Scenarios
