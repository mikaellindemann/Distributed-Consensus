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
        | Locks
        | LockedBy
        | Unlocks
        | UnlockedBy
    }
    
    ///The "log entry" in the built history. Every node stores one StateChange for every event that occurs related involving a Relation. The final history is then built from all StateChanges of all nodes.
    type StateChange = {
        Author: Age;
        Relation: Relation;
        Counterpart: string option
    }
    
    ///A node in the graph. A Node contains WaitFor, a list of neightbours it is waiting for a response from, and a RequestTrace, which is an incoming trace of every request that occured before reaching this node appended by the ID of the Node. 
    type Node = {
        Id: string;
        WaitFor : string list; 
        RequestTrace : string list;
        Current
    }

The algorithm works by collecting every StateChange from all nodes in the system and determining what the history of execution was in the system. 

Using the `LocalTimestamp` and `Id` of `Age`s we can build a _happens-before_ relation of the system up to the current point in time and determine what/if any nodes are faulty. 

The algorithm fetches the initial and current `State` of every node to further assist in creating the history.



## Relation Scenarios
`#number` is the local timestamp, note that because it is local, the *included* can have a lower timestamp than the *includer*.

### Inclusion
| Inclusion Relation       | Event 1: history                   | Event 2: history                   |
|--------------------------|------------------------------------|------------------------------------|
| Event 1 includes Event 2 | (#1, Event 1, includes, Event 2)   | (#1, Event 2, includedBy, Event 1) |
| Event 2 includes Event 1 | (#1, Event 1, includedBy, Event 2) | (#3, Event 2, includes, Event 1)   |

Happens Before Rule: includedBy -> includes

### Exclusion
| Exclusion Relation       | Event 1: history                   | Event 2: history                   |
|--------------------------|------------------------------------|------------------------------------|
| Event 1 excludes Event 2 | (#1, Event 1, excludes, Event 2)   | (#1, Event 2, excludedBy, Event 1) |
| Event 2 excludes Event 1 | (#1, Event 1, excludedBy, Event 2) | (#3, Event 2, excludes, Event 1)   |

Happens Before Rule: excludedBy -> excludes

### Pending
| Pending Relation             | Event 1: history                     | Event 2: history                     |
|------------------------------|--------------------------------------|--------------------------------------|
| Event 1 sets Pending Event 2 | (#1, Event 1, setsPending, Event 2)  | (#1, Event 2, setPendingBy, Event 1) |
| Event 2 sets Pending Event 1 | (#1, Event 1, setPendingBy, Event 2) | (#3, Event 2, setsPending, Event 1)  |

Happens Before Rule: setPendingBy -> setsPending

### Conditions
| Condition Relation                         | Event 1: history                               | Event 2: history                               |
|--------------------------------------------|------------------------------------------------|------------------------------------------------|
| Event 1 checks Event 2 which is executable | (#1, Event 1, ConditionChecks true, Event 2)   | (#1, Event 2, ConditionChecked true, Event 1)  |
| Event 1 checks Event 2 which is executable | (#1, Event 1, ConditionChecks false, Event 2)  | (#1, Event 2, ConditionChecked false, Event 1) |
| Event 2 checks Event 1 which is executable | (#1, Event 1, ConditionChecked true, Event 2)  | (#3, Event 2, ConditionChecks true, Event 1)   |
| Event 2 checks Event 1 which is executable | (#1, Event 1, ConditionChecked false, Event 2) | (#3, Event 2, ConditionChecks false, Event 1)  |

Happens Before Rule: ConditionChecked -> ConditionChecks

### Execution
| Execution                | Event 1: history     |
|--------------------------|----------------------|
| Event 1 Execution begins | (#1, Event 1)        |
| Event 1 Execution fails  | (#1, Event 1, false) |
| Event 1 Execution succes | (#1, Event 1, true)  |

Happens Before Rule: Execution begins -> Execution fails / Execution succes

### Lock
| Lock                  | Event 1 History              | Event 2 History                  |
|-----------------------|------------------------------|----------------------------------|
| Event 1 Locks Event 2 | #1, Event 1, Lock, Event 2   | (#1, Event 2, lockedBy, Event 1) |
| Event 2 Locks Event 1 | #1, Event 1, LockBy, Event 2 | (#1, Event 2, lock, Event 1)     |

Happens Before Rule: LockedBy -> Unlock

### Unlock
| Unlock                 | Event 1 History                | Event 2 History                  |
|------------------------|--------------------------------|----------------------------------|
| Event 1 Unlock Event 2 | #1, Event 1, Unlock, Event 2   | (#1, Event 2, UnlockBy, Event 1) |
| Event 2 Unlock Event 1 | #1, Event 1, UnlockBy, Event 2 | (#1, Event 2, Unlock, Event 1)   |

Happens Before Rule: UnlockedBy -> Unlock


### Rules across scenarios

Execute Start -> Lock
Lock -> Include
Lock -> Exclude
Lock -> setPending
Lock -> ConditionChecks
Lock -> Unlock
Unlock -> Execute Fail / Execute Succes

Lockby x -> UnlockBy x -> LockBy y

## Algorithms

### Fetching Algorithm

#### Overview
 - History is requested by `X`
 - If `CreatingHistory` of node is `false` 
    - Set `CreatingHistory` of node to `true`
    - Ask all relations for their history
        - If all relations return None
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



#### Overview - with redundancy for safety

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
The stiching algorithm should determine in what order Events have been executed, given a partial or full history. 

#### Overview


#### Walkthrough


### Check-Consistency Algorithm
#### Overview

#### Walkthrough

