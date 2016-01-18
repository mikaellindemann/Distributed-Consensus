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
\#{number} is the local timestamp, note that because it is local, the *included* can have a lower timestamp than the *includer*.

### Inclusion
| Inclusion Relation       | Event 1: history                   | Event 2: history                   |
|--------------------------|------------------------------------|------------------------------------|
| Event 1 includes Event 2 | (#1, Event 1, includes, Event 2)   | (#1, Event 2, includedBy, Event 1) |
| Event 2 includes Event 1 | (#1, Event 1, includedBy, Event 2) | (#3, Event 2, includes, Event 1)   |

### Exclusio
| Exclusion Relation       | Event 1: history                   | Event 2: history                   |
|--------------------------|------------------------------------|------------------------------------|
| Event 1 excludes Event 2 | (#1, Event 1, excludes, Event 2)   | (#1, Event 2, excludedBy, Event 1) |
| Event 2 excludes Event 1 | (#1, Event 1, excludedBy, Event 2) | (#3, Event 2, excludes, Event 1)   |

### Pending
| Pending Relation             | Event 1: history                     | Event 2: history                     |
|------------------------------|--------------------------------------|--------------------------------------|
| Event 1 sets Pending Event 2 | (#1, Event 1, setsPending, Event 2)  | (#1, Event 2, setPendingBy, Event 1) |
| Event 2 sets Pending Event 1 | (#1, Event 1, setPendingBy, Event 2) | (#3, Event 2, setsPending, Event 1)  |

### Conditions
| Condition Relation                         | Event 1: history                               | Event 2: history                               |
|--------------------------------------------|------------------------------------------------|------------------------------------------------|
| Event 1 checks Event 2 which is executable | (#1, Event 1, ConditionChecks true, Event 2)   | (#1, Event 2, ConditionChecked true, Event 1)  |
| Event 1 checks Event 2 which is executable | (#1, Event 1, ConditionChecks false, Event 2)  | (#1, Event 2, ConditionChecked false, Event 1) |
| Event 2 checks Event 1 which is executable | (#1, Event 1, ConditionChecked true, Event 2)  | (#3, Event 2, ConditionChecks true, Event 1)   |
| Event 2 checks Event 1 which is executable | (#1, Event 1, ConditionChecked false, Event 2) | (#3, Event 2, ConditionChecks false, Event 1)  |

### Execution
| Execution                | Event 1: history     |
|--------------------------|----------------------|
| Event 1 Execution begins | (#1, Event 1)        |
| Event 1 Execution fails  | (#1, Event 1, false) |
| Event 1 Execution succes | (#1, Event 1, true)  |

