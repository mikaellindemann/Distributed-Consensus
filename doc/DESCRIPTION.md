# Problem Domain Description

## Relation Scenarios
`#number` is the local timestamp, note that because it is local, the *included* can have a lower timestamp than the *includer*.

### Inclusion
| Inclusion Relation         | Event 1: history                     | Event 2: history                     |
|----------------------------|--------------------------------------|--------------------------------------|
| `Event 1 includes Event 2` | `(#1, Event 1, includes, Event 2)`   | `(#1, Event 2, includedBy, Event 1)` |
| `Event 2 includes Event 1` | `(#1, Event 1, includedBy, Event 2)` | `(#3, Event 2, includes, Event 1)`   |

### Exclusion
| Exclusion Relation         | Event 1: history                     | Event 2: history                     |
|----------------------------|--------------------------------------|--------------------------------------|
| `Event 1 excludes Event 2` | `(#1, Event 1, excludes, Event 2)`   | `(#1, Event 2, excludedBy, Event 1)` |
| `Event 2 excludes Event 1` | `(#1, Event 1, excludedBy, Event 2)` | `(#3, Event 2, excludes, Event 1)`   |

### Pending
| Pending Relation               | Event 1: history                       | Event 2: history                       |
|--------------------------------|----------------------------------------|----------------------------------------|
| `Event 1 sets Pending Event 2` | `(#1, Event 1, setsPending, Event 2)`  | `(#1, Event 2, setPendingBy, Event 1)` |
| `Event 2 sets Pending Event 1` | `(#1, Event 1, setPendingBy, Event 2)` | `(#3, Event 2, setsPending, Event 1)`  |

### Conditions
| Condition Relation                               | Event 1: history                                 | Event 2: history                                 |
|--------------------------------------------------|--------------------------------------------------|--------------------------------------------------|
| `Event 1 checks Event 2 which is executable`     | `(#1, Event 1, ConditionChecks true, Event 2)`   | `(#1, Event 2, ConditionChecked true, Event 1)`  |
| `Event 1 checks Event 2 which is not executable` | `(#1, Event 1, ConditionChecks false, Event 2)`  | `(#1, Event 2, ConditionChecked false, Event 1)` |
| `Event 2 checks Event 1 which is executable`     | `(#1, Event 1, ConditionChecked true, Event 2)`  | `(#3, Event 2, ConditionChecks true, Event 1)`   |
| `Event 2 checks Event 1 which is not executable` | `(#1, Event 1, ConditionChecked false, Event 2)` | `(#3, Event 2, ConditionChecks false, Event 1)`  |

### Execution
| Execution                  | Event 1: history       |
|----------------------------|------------------------|
| `Event 1 Execution begins` | `(#1, Event 1)`        |
| `Event 1 Execution fails`  | `(#1, Event 1, false)` |
| `Event 1 Execution succes` | `(#1, Event 1, true)`  |

### Lock
| Lock                    | Event 1 History                | Event 2 History                    |
|-------------------------|--------------------------------|------------------------------------|
| `Event 1 Locks Event 2` | `#1, Event 1, Lock, Event 2`   | `(#1, Event 2, lockedBy, Event 1)` |
| `Event 2 Locks Event 1` | `#1, Event 1, LockBy, Event 2` | `(#1, Event 2, lock, Event 1)`     |

### Unlock
| Unlock                   | Event 1 History                  | Event 2 History                    |
|--------------------------|----------------------------------|------------------------------------|
| `Event 1 Unlock Event 2` | `#1, Event 1, Unlock, Event 2`   | `(#1, Event 2, UnlockBy, Event 1)` |
| `Event 2 Unlock Event 1` | `#1, Event 1, UnlockBy, Event 2` | `(#1, Event 2, Unlock, Event 1)`   |


### Happens before rules

    Y includedBy X          ->          X includes Y
    Y excludedBy X          ->          X excludes Y
    Y setPendingBy X        ->          X setsPending Y
    Y ConditionChecked X    ->          X ConditionChecks Y
    Y LockedBy X            ->          X Lock Y
    Y UnlockedBy X          ->          X Unlock Y
    X Execution begins      ->          X Execution fails / Success
    X Execute Start         ->          X Locks Y
    X Locks Y               ->          X Include Y
    X Locks Y               ->          X Exclude Y
    X Locks Y               ->          X setPending Y
    X Locks Y               ->          X ConditionChecks Y
    X Locks Y               ->          X Unlock Y
    X Locks Y               ->          X Unlock Y              ->           X Execute Fail / Success
    Y Lockby X              ->          Y UnlockBy X            ->           Y LockBy "z"


# Description of Algorithms

## Data Types

The algorithms use the following F# data types:

```fsharp
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
```

History will be created by fetching every StateChange from all nodes in the system and determining what the history of execution was in the system.

Using the `Age` and `StateChange`s of the nodes in the system we can build a _happens-before_ relation up to the current point in time and determine what/if any nodes are faulty.

The algorithm fetches the initial and current `State` of every node to further assist in creating the history.


## Algorithms
History is created by using Fetch-and-Stitch with validation in the stitching phase.

The receiver of the first create history call should

- create history ID
- fetch history from neighbours
    - each of these should:
        - fetch
        - stitch
        - return
- stitch
- Call for a vote
- Clousure of event graph to an execution graph.

### Fetching Algorithm - Deadlock (and Attack) Secure

#### Corectness / Goal of the algorithm
The goal of the algorithm is to fetch the history of all the events in the workflow.
Correcness should be based on:
- How many nodes' history gets fetched in the workflow (higher is better).
- How rendundant the data is (higher redundancy is better).
- How well it handles cycles in the graph
- That it finishes.


#### Overview

Each node has two lists: `request trace` and `wait for`.

 1. History is requested by `X` with `request trace` `T` and history ID: `HID`
    1. If (Lookup history for `HID`) is not empty
        - Return lookup history for `HID`
    - Add all relations to `wait for`
    - If `wait for` is empty
        - Create history
        - Return
    - For each node `n` in `T`
        - if `n` is in `wait for`
            - remove `n` from `wait for`
    - Create `T'` by appending own ID to `T`
    - If `wait for` is empty
        - Cyclic case: Return empty set -> maybe return local set
    - Ask all nodes in `wait for` for their history with `T'`
    - Stitch own history with answers
    - Return "new" history

In case of cyclic it could be smart to return the local set of history since it can be used for checking with the rest of the trace.

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

| Execution          	| Trace         	| Wait for      	| Trace'            	| Action                                            	|
|--------------------	|---------------	|---------------	|-------------------	|---------------------------------------------------	|
| `C -> Event 5`       	| `[]`           	| `[3; 2]`       	| `[5]`             	| `LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN`   	|
| `Event 5 -> Event 3` 	| `[5]`          	| `[6; 1]`       	| `[5;3]`           	| `LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN`   	|
| `Event 5 -> Event 2` 	| `[5]`          	| `[3;1]`        	| `[5; 2]`          	| `LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN`   	|
| `Event 2 -> Event 1` 	| `[5; 2]`       	| `[6; 4]`       	| `[5; 2; 1]`       	| `LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN`   	|
| `Event 1 -> Event 6` 	| `[5; 2; 1]`    	| `[1;3] => [3]` 	| `[5; 2; 1; 6]`    	| `LOOKUP, WAIT, CREATE, STITCH, PERSIST, RETURN`   	|
| `Event 6 -> Event 3` 	| `[5; 2; 1; 6]` 	| `[1; 6] => []` 	| `[5; 2; 1; 6; 3]` 	| `Deadlock: RETURN EMPTY`                           	|
| `Event 3 -> Event 6` 	| `[5; 3]`       	| `[1; 6]`       	| `[5; 3; 6]`       	| `LOOKUP, RETURN`                                   	|
| `Event 1 -> Event 4` 	| `[5; 2; 1]`    	| `[]`           	| `[5; 2; 1; 4]`    	| `LOOKUP, CREATE, PERSIST, RETURN`                 	|


#### Limitations
There can be cases with conditional relations where we wont have information about nodes.

![](http://g.gravizo.com/g?
 digraph G {
   | "Event 1" -> "Event 2"
   | "Event 2" -> "Event 1"
   | "Event 3" -> "Event 1"
 })
 
 In this case if `event 1` gets asked about its history `event 3`'s history will never be known if the relation from `event 3` to `even 1` is a conditional relation.

### Fetch Algorithm - Simple (No Redundancy)

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
    - Asks all its neighbors (Event 2 and Event 3) for their history.
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


### Stitch Algorithm
The stiching algorithm should determine in what order Events have been executed, given a partial or full history based on DCR Graph-specific and common happens-before relations.

Maybe this algorithm should just create an order of the logs according to the happen before relation rules. And send back the ordered list.
While creating the order the Validate-History algorithm should be used.

#### Overview


#### Walkthrough


### Validate-History Algorithm
#### Overview
Check for:
Inconsistent data


#### Walkthrough

### Create Execution Tree
#### Overview
Create a tree with the known execution order.

         (Event 1)
    /        |         \
Event 2    Event 3    Event 4
