# Algorithm Description

The algorithm is implemented using the following F# data types:

    type Age = {
        LocalTimestamp: int;
        Id: string
        }
        
    type State = {
        Pending: bool
        Excluded: bool
        }
    
    type Execution
    
    
    
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

