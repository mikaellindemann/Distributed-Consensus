# Algorithm Description

The algorithm is implemented using the following F# data types:

	///A unique identifier type of the local timestamp and ID of a given node.
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

Using the `LocalTimestamp` and `Id` of `Age`s we can build a _happens-before_ relation of the system up to the current point in time and determine what/if any nodes are faulty. 

The algorithm fetches the initial and current `State` of every node to further assist in creating the history.



## Relation Scenarios
\#{number} is the local timestamp, note that because it is local, the *included* can have a lower timestamp than the *includer*.

### Inclusion
| Inclusion Relation       | Event 1: history                   | Event 2: history                   |
|--------------------------|------------------------------------|------------------------------------|
| Event 1 includes Event 2 | (#1, Event 1, includes, Event 2)   | (#1, Event 2, includedBy, Event 1) |
| Event 2 includes Event 1 | (#1, Event 1, includedBy, Event 2) | (#3, Event 2, includes, Event 1)   |

### Exclusion
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


## Algorithms

### Fetching Algorithm

A request for a history has an ID, initially set to `None`.
The first node that recieves a history request sets the ID of that history to its `Age: (local timestamp, event ID)`.
All subsequent requestsfor generating the history include this ID. 
Whenever a Node in the graph generate a history, it persists it with the corrosponding history ID.
Whenever a Node is asked for a history, it checks if any history matches the ID, and returns it if it exists.

#### Overview -> with little redudancy

Each node has a boolean CreatingHistory

 - History is requested by `X` with history ID: `HID`
    - If (Lookup history for `HID`) is not empty
        - Return lookup history for `HID`
 - If `CreatingHistory` of node is `false` 
    - Set `CreatingHistory` of node to `true`
    - Ask all relations for their history
        - If all neighbours return None
            - Create history of self
            - Return history to `X`
        - If any history is returned from relations
            - Create history of self
            - Stich all histories together
            - Return "new" history to `X`
    - Set `CreatingHistory` to `false`
 - If `CreatingHistory` is `true`
    - Return `None`

#### Walkthrough 

Consider the following graph:

![](http://g.gravizo.com/g?
 digraph G {
   | "Event 5" -> "Event 3"
   | "Event 5" -> "Event 2"
   | "Event 3" -> "Event 5"
   | "Event 3" -> "Event 1"
   | "Event 2" -> "Event 1"
   | "Event 2" -> "Event 3"
   | "Event 1" -> "Event 4"
   | "Event 1" -> "Event 6"
   | "Event 6" -> "Event 1"
 })

- Event 5 gets asked by *Client* for its history. 
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (Event 2 and Event 5) for their history.
- Event 2 gets asked by Event 5 of their history
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (Event 3 and Event 1) for their history.
- Event 3 gets asked by Event 5 of their history
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (Event 5 and Event 1) for their history.
- Event 3 gets asked by Event 2 of their history
    - return is already creating history
- Event 1 gets asked by Event 2 of their history
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (Event 4) for their history.
- Event 4 gets asked by Event 1 of their history
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (None) for their history. (has none)
    - Since No neighbors return a history to Caller (Event 1)
- Event 1 gets asked by Event 3 of their history
    - return is already creating history
- Event 6 gets asked by Event 1 of their history
    - It sets its CreatingHistory boolean to true. 
    - Asks all its neighbors (Event 1) for their history.
    - Since No neighbors return a history (6 is already working) to Caller (Event 1)
- Event 1 gets asked by Event 6 of their history
    - return is already creating history
- Event 1 gets history from Event 4 and Event 6
    - Get own history
    - Stitch together history of neighbors and self
    - return history to Caller (Event 2)
- Event 2 gets history from 6 and empty from 3
    - Get own history
    - Stitch together history of neighbors and self
    - return history to Caller (Event 5)
- Event 3 gets no history from neighbors
    - Get own history
    - return history to Caller (Event 5)


#### Overview -> with redudancy for safety

Each node has two lists: `request trace` and `wait for` as well as a queue: `requesters`

 - History is requested by `X` with `request trace` `T` and history ID: `HID`
    - If (Lookup history for `HID`) is not empty
        - Return lookup history for `HID`
    - `X` gets added to `requesters`
    - Add all relations to `wait for`
    - If `wait for` is empty
        - Create history
        - Return 
    - For each node `n` in `T`
        - if `n` is in `wait for`
            - remove `n` from `wait for`
    - Create `T'` by appending own ID to `T`
    - If `wait for` is empty
        Deadlock case: Return empty set 
    - Ask all nodes in `wait for` for their history with `T'`
    - Create relations' from `wait for` answers
    - Stitch own history with answers
    - Return "new" history to all nodes in `requesters`
  
#### Walkthrough   
    
| Execution          	| Trace        	| Wait for     	| Trace'          	| Action                                        	|
|--------------------	|--------------	|--------------	|-----------------	|--------------------------------------------------	|
| C -> Event 5       	| []           	| [3; 2]       	| [5]             	| LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN 	|
| Event 5 -> Event 3 	| [5]          	| [6; 1]       	| [5;3]           	| LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN 	|
| Event 5 -> Event 2 	| [5]          	| [3;1]        	| [5; 2]          	| LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN 	|
| Event 2 -> Event 1 	| [5; 2]       	| [6; 4]       	| [5; 2; 1]       	| LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN 	|
| Event 1 -> Event 6 	| [5; 2; 1]    	| [1;3] => [3] 	| [5; 2; 1; 6]    	| LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN 	|
| Event 6 -> Event 3 	| [5; 2; 1; 6] 	| [1; 6] => [] 	| [5; 2; 1; 6; 3] 	| Deadlock: RETURN EMPTY                           	|
| Event 3 -> Event 6 	| [5; 3]       	| [1; 6]       	| [5; 3; 6]       	| LOOKUP, RETURN                                   	|
| Event 1 -> Event 4 	| [5; 2; 1]    	| []           	| [5; 2; 1; 4]    	| LOOKUP, CREATE, PERSIST, RETURN                 	|
    
### Stitch History Algorithm
#### Overview

#### Walkthrough


### Check-Consistency Algorithm
#### Overview

#### Walkthrough

