namespace HistoryConsensus
open Action

module Graph =
    type Graph =
        {
            Nodes: Map<ActionId, Action>
        }

    let addNode node graph =
        if Map.containsKey node.Id graph.Nodes
        then
            let inGraph = Map.find node.Id graph.Nodes
            { Nodes = Map.add node.Id { inGraph with Edges = Set.union inGraph.Edges node.Edges} graph.Nodes }
        else
            { Nodes = Map.add node.Id node graph.Nodes }

    (*///Add a node to the Action graph.
    ///If the node already exists, add the edges of the node to the existing node in the graph.
    let addNode node graph =
        let existsInGraph n = Map.containsKey n graph.Nodes

        //Convert lists to Sets, union sets and convert back in order to only add not present elements.
        let addToListIfNotPresent toAdd existing =
            Set.toList (Set.union <| Set.ofList toAdd <| Set.ofList existing)

        //Retrieve an existing node Id from the graph and add the edges from the given node to the one
        //in the graph.
        let findAndAdd n =
            let inGraph = Map.find n.Id graph.Nodes
            let added = addToListIfNotPresent n.Edges inGraph.Edges
            let inGraph' = { inGraph with Edges = added }
            Map.add inGraph'.Id inGraph' graph.Nodes

        //If the node does not exist already, add it to the graph.
        //If it does exist, add edges from the given node to the existing node.
        let edges' =
            if not <| existsInGraph node.Id
            then Map.add node.Id node graph.Nodes
            else findAndAdd node
        { Nodes = edges' }*)

    let removeNode node (graph : Graph) : Graph =
        let graph' = Map.remove node.Id graph.Nodes
        {
            Nodes = Map.map
                        (fun id action -> { Id = id;
                                            CounterpartEventId = action.CounterpartEventId;
                                            Type = action.Type
                                            Edges = Set.filter (fun id -> id <> node.Id) action.Edges })
                        graph'
        }

    let addEdge fromNode toNode (graph : Graph) : Graph =
        {
            Nodes = Map.add
                        fromNode.Id
                        { fromNode with Edges = Set.add toNode.Id fromNode.Edges}
                        graph.Nodes
        }

    let removeEdge fromNode toNode (graph : Graph) : Graph =
        {
            Nodes = Map.add
                        fromNode.Id
                        { fromNode with Edges = Set.filter (fun actionId -> actionId <> toNode.Id) fromNode.Edges }
                        graph.Nodes
        }

    //Retrive a single node from an actionId.
    let getNode graph actionId = Map.find actionId graph.Nodes
    //Retrieve a collection of nodes from given ActionIds.
    let getNodes graph actionIdList = List.map (getNode graph) actionIdList
    //Retrieve every Action in the graph.
    let getActionsFromGraph graph = List.map snd (Map.toList graph.Nodes)

    let empty : Graph = { Nodes = Map.empty }




    let getBeginningNodes graph : Action list =
        // Calculate all the action ids that are referenced by other nodes in the graph (which is turned into a list).
        let calcToNodeIds allNodes =
            List.filter (fun action ->
                List.exists
                    (fun otherAction ->
                        Set.contains action.Id otherAction.Edges)
                    allNodes)
                allNodes

        let allNodes = List.map (fun (_,action) -> action) <| Map.toList graph.Nodes

        let toNodes = calcToNodeIds allNodes

        // Find all actions, that are not referenced in the graph.
        List.except toNodes allNodes

    (*// Calculate all edges between nodes?
    let transitiveClosure beginningNodes graph actionType =
        let rec transitiveClos (list:Action list) newGraph =
            match list with
            | [] -> newGraph
            | x::xs ->
                let rec inner edgeList innerGraph newXs =
                    match edgeList with
                    | [] -> transitiveClos newXs innerGraph
                    | y::ys ->  let toNode = (getNode graph y)
                                if(toNode.Type = actionType)
                                then inner ys (addEdge (getNode graph x.Id) toNode innerGraph) (toNode::newXs)
                                else inner (List.ofSeq <| Set.union (Set.ofList ys) (getNode innerGraph y).Edges)  innerGraph newXs
                inner (Set.toList x.Edges) newGraph xs
        transitiveClos beginningNodes graph*)

    let transitiveClosureBetter beginningNodes graph actionType =
        // Go over every node in the graph
        let rec transitiveClos (list:Action list) (accGraph:Graph) =
            match list with
            | [] -> accGraph
            | node::fromNodes -> innerFun (getNodes graph (Set.toList node.Edges)) fromNodes node accGraph
        // With a fromNode - find add edge to all other nodes of type ActionType
        and innerFun edgeList newFromNodes fromNode (innerAccGraph:Graph) =
            match edgeList with
            | [] -> transitiveClos newFromNodes innerAccGraph // when it has been iterated over call the first method with a new list and new graph
            | toNode::toNodes ->
                if toNode.Type = actionType // we only need these edges
                then innerFun toNodes                                                               (Set.toList <| (Set.add toNode (Set.ofList newFromNodes)))  fromNode    (addEdge fromNode toNode innerAccGraph) // update xs to now have the toNode - this is done to reduce unneccesary transitive clousures
                else innerFun (Set.toList <| Set.union (Set.ofList toNodes)  (Set.ofList (getNodes innerAccGraph (Set.toList toNode.Edges))))          newFromNodes            fromNode    innerAccGraph // since no match was found add the edges of the toNode to the nodes which needs to be examined. By adding to the end of the list we achieve breadth first.

        transitiveClos beginningNodes graph

    (*let transitiveReduction beginningNodes graph =
        let rec transitiveRed (list:Action list) newGraph =
            match list with
            | [] -> newGraph
            | x::xs ->
                let rec inner newList newNewGraph =
                    match xs with
                    | [] -> transitiveRed xs newNewGraph
                    | y::ys -> if (Set.exists (fun id -> id = y.Id) x.Edges)
                               then
                                   let rec innerInner newNewList newNewNewGraph =
                                       match ys with
                                       | [] -> inner ys newNewNewGraph
                                       | z::zs -> if (Set.exists (fun id -> id = z.Id) y.Edges && Set.exists (fun id -> id = z.Id) x.Edges)
                                                  then innerInner zs (removeEdge x z newNewNewGraph)
                                                  else innerInner zs newNewNewGraph
                                   innerInner ys newNewGraph
                               else
                                   inner ys newNewGraph
                inner xs newGraph
        transitiveRed beginningNodes graph*)

    let transitiveReduction beginningNodes graph =
        let removeEdgeIfRedundant x y z graph' =
            if (Set.exists (fun id -> id = z.Id) y.Edges && Set.exists (fun id -> id = z.Id) x.Edges)
            then removeEdge x z graph'
            else graph'

        let rec transitiveRed (list:Action list) newGraph =
            List.fold (fun graph' action' ->
                List.fold (fun graph'' action'' ->
                    if Set.contains action''.Id action'.Edges
                    then List.fold (fun graph''' action''' -> removeEdgeIfRedundant action' action'' action''' graph''') graph'' list
                    else graph'') graph' list) newGraph list

        transitiveRed beginningNodes graph


    let transitiveReductionBetter beginningNodes graph =
        let unionListWithoutDuplicates firstList secondList = Set.union (Set.ofList firstList) (Set.ofList secondList) |> Set.toList
        // the beginning nodes - goes over all of them -> probably egts updated from the next function.
        let rec fromNodesFun (fromNodes:Action list) accGraph =
            match fromNodes with
            | [] -> accGraph
            | fromNode::fromNodesRest -> neighboursFun (getNodes accGraph (Set.toList fromNode.Edges)) fromNode fromNodesRest accGraph
        // Goes over all the neighbours of the fromNode -> neighbours can get updated from the next function. Update fromNodes list with each neighbour
        and neighboursFun (neighbours:Action list) (fromNode:Action) (newFromNodes:Action list) (accGraph:Graph) =
            match neighbours with
            | [] -> fromNodesFun newFromNodes accGraph
            | neighbour::neighboursRest ->
                endNodesFun (getNodes accGraph (Set.toList neighbour.Edges)) neighboursRest fromNode (unionListWithoutDuplicates [neighbour] newFromNodes) accGraph
        // Goes over all the neighbours of the neighbour in the previous function. -> If there is an edge from fromNode to endNode remove it. Otherwise add the endNode to neighbours in the previous function.
        and endNodesFun (endNodes:Action list) (newNeighbours:Action list) (fromNode:Action) (newFromNodes:Action list) (accGraph:Graph) : Graph =
            match endNodes with
            | [] -> neighboursFun newNeighbours fromNode newFromNodes accGraph
            | endNode::endNodesRest -> if (Set.exists (fun id -> id = endNode.Id) fromNode.Edges)
                                       then endNodesFun endNodesRest newNeighbours fromNode newFromNodes (removeEdge fromNode endNode accGraph)
                                       else endNodesFun endNodesRest (unionListWithoutDuplicates [endNode] newNeighbours) fromNode newFromNodes accGraph
        fromNodesFun beginningNodes graph

    let transitiveReductionExperimental beginningNodes graph = 
        let unionListWithoutDuplicates firstList secondList = Set.union (Set.ofList firstList) (Set.ofList secondList) |> Set.toList
        let neighboursNeighbour node: ActionId Set =
            let rec inner (list:ActionId list) (set:ActionId Set) = 
                match list with
                | [] -> set
                | newNode::nodes -> let newSet = Set.foldBack (fun id acc -> Set.union (getNode graph id).Edges acc) node.Edges set
                                    inner (Set.toList <| Set.union (Set.ofList nodes) newSet) newSet
            inner (Set.toList node.Edges) Set.empty
        // the beginning nodes - goes over all of them -> probably egts updated from the next function.
        let rec fromNodesFun (fromNodes:Action list) accGraph = 
            match fromNodes with
            | [] -> accGraph
            | fromNode::fromNodesRest -> let neighboursNeighours = neighboursNeighbour fromNode
                                         let transitiveEdges = Set.filter (fun edge -> neighboursNeighours.Contains edge ) fromNode.Edges
                                         let newGraph = Set.foldBack (fun toNode acc-> removeEdge fromNode (getNode accGraph toNode) acc ) transitiveEdges graph
                                         fromNodesFun (unionListWithoutDuplicates (getNodes newGraph (Set.toList fromNode.Edges)) fromNodesRest) newGraph
        fromNodesFun beginningNodes graph

    ///Determine whether there is a relation between two nodes by checking their individual Ids and Edges.
    let hasRelation (fromNode:Action) (toNode:Action) : bool =
        let checkID =
            fromNode.CounterpartEventId = fst toNode.Id && fst fromNode.Id = toNode.CounterpartEventId
        let checkRelation fromType toType =
            match fromType, toType with
            | CheckedConditon, ChecksConditon   -> true
            | IncludedBy, Includes              -> true
            | ExcludedBy, Excludes              -> true
            | SetPendingBy, SetsPending         -> true
            | LockedBy, Locks                   -> true
            | UnlockedBy, Unlocks               -> true
            | _                                 -> false
        checkID && checkRelation fromNode.Type toNode.Type


    let simplify (graph:Graph) (actionType:ActionType) : Graph =
        let beginningNodes = getBeginningNodes graph
        let graphWithTransClos = transitiveClosureBetter beginningNodes graph actionType
        let filteredGraph = Map.foldBack (fun actionId action graph -> if action.Type = actionType then graph else removeNode action graph) graphWithTransClos.Nodes graphWithTransClos
        let transReduction = transitiveReduction (getBeginningNodes filteredGraph) filteredGraph
        transReduction

    let merge localGraph otherGraph =
        let addToUsedActions action corner = Set.add action.Id corner

        let hasRelation first second corner =
            (fst first.Id <> fst second.Id)
                && hasRelation first second
                && not <| Set.contains second.Id corner

        let combinedGraph = Map.fold (fun graph key value -> addNode value graph) localGraph otherGraph.Nodes
        let combinedGraphAsSeq = Seq.map (fun (id,action) -> action) <| Map.toSeq combinedGraph.Nodes

        let findFirstRelation action corner = Seq.tryFind (fun action' -> hasRelation action action' corner) combinedGraphAsSeq

        let edgesList,_ =
            Seq.fold (fun (newEdgesList,corner) action ->
                    match findFirstRelation action corner with
                    | Some action' -> ((action, action') :: newEdgesList, addToUsedActions action' corner)
                    | None -> newEdgesList,corner
                ) ([], Set.empty) combinedGraphAsSeq

        Some <| List.fold (fun graph (fromNode, toNode) -> addEdge fromNode toNode graph) combinedGraph edgesList

    let cycleCheck node graph =
        let rec cycleThrough visitedNodes remaining =
            match remaining with
            | [] -> true
            | action::nodes -> if not <| List.contains action.Id visitedNodes
                               then cycleThrough <| action.Id::visitedNodes <| nodes @ (getNodes graph <| Set.toList node.Edges)
                               else false
        let neighbourNodes = getNodes graph <| Set.toList node.Edges
        cycleThrough [node.Id] neighbourNodes
