namespace HistoryConsensus
open Action

module Graph =
    type Graph =
        {
            Nodes: Map<ActionId, Action>
        }

    ///Add a node to the Action graph. 
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
        { Nodes = edges' }

    let removeNode node (graph : Graph) : Graph =
        let graph' = Map.remove node.Id graph.Nodes
        { 
            Nodes = Map.map 
                        (fun id action -> { Id = id;
                                            CounterpartEventId = action.CounterpartEventId;
                                            Type = action.Type
                                            Edges = List.where (fun id -> id = node.Id) action.Edges }) 
                        graph' 
        }

    let addEdge fromNode toNode (graph : Graph) : Graph =
        { 
            Nodes = Map.add 
                        fromNode.Id 
                        { fromNode with Edges = toNode.Id :: fromNode.Edges} 
                        graph.Nodes 
        }

    let removeEdge fromNode toNode (graph : Graph) : Graph =
        { 
            Nodes = Map.add 
                        fromNode.Id 
                        { fromNode with Edges = List.where (fun actionId -> actionId <> toNode.Id) fromNode.Edges } 
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
        let rec calcToNodes nodes accList =
            match nodes with
            | [] -> accList
            | (_, action)::rest -> calcToNodes rest (List.foldBack (fun element list -> element::list) action.Edges accList)
        let toNodes = calcToNodes (Map.toList graph.Nodes) []        

        // Find all action ids, that are not referenced in the graph.
        let beginningNodesIds = List.except toNodes (List.map (fun (x,y) -> x) (Map.toList graph.Nodes))

        // Return the Actions of the non-referenced action ids.
        getNodes graph beginningNodesIds


    // Calculate all edges between nodes?
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
                                else inner (ys@(getNode innerGraph y).Edges)  innerGraph newXs
                inner x.Edges newGraph xs
        transitiveClos beginningNodes graph


    let transitiveClosureBetter beginningNodes graph actionType = 
        // Go over every node in the graph
        let rec transitiveClos (list:Action list) (accGraph:Graph) = 
            match list with
            | [] -> accGraph
            | node::fromNodes -> innerFun node.Edges fromNodes node accGraph
        // With a fromNode - find add edge to all other nodes of type ActionType
        and innerFun edgeList newFromNodes fromNode (innerAccGraph:Graph) = 
            match edgeList with
            | [] -> transitiveClos newFromNodes innerAccGraph // when it has been iterated over call the first method with a new list and new graph
            | node2::toNodes ->
                let toNode = (getNode graph node2) // find the toNode
                if toNode.Type = actionType // we only need these edges
                then innerFun toNodes                (toNode::newFromNodes) fromNode (addEdge fromNode toNode innerAccGraph) // update xs to now have the toNode - this is done to reduce unneccesary transitive clousures
                else innerFun (toNodes@toNode.Edges) newFromNodes           fromNode innerAccGraph // since no match was found add the edges of the toNode to the nodes which needs to be examined. By adding to the end of the list we achieve breadth first.
        
        transitiveClos beginningNodes graph


    let transitiveReduction beginningNodes graph = 
        let rec transitiveRed (list:Action list) newGraph = 
            match list with
            | [] -> newGraph
            | x::xs -> 
                let rec inner newList newNewGraph = 
                    match xs with
                    | [] -> transitiveRed xs newNewGraph
                    | y::ys -> if (List.exists (fun id -> id = y.Id) x.Edges) 
                               then 
                                   let rec innerInner newNewList newNewNewGraph = 
                                       match ys with 
                                       | [] -> inner ys newNewNewGraph
                                       | z::zs -> if (List.exists (fun id -> id = z.Id) y.Edges && List.exists (fun id -> id = z.Id) x.Edges) 
                                                  then innerInner zs (removeEdge x z newNewNewGraph) 
                                                  else innerInner zs newNewNewGraph
                                   innerInner ys newNewGraph
                               else 
                                   inner ys newNewGraph
                inner xs newGraph
        transitiveRed beginningNodes graph


    let transitiveReductionBetter beginningNodes graph = 
        // the beginning nodes - goes over all of them -> probably egts updated from the next function.
        let rec fromNodesFun (fromNodes:Action list) accGraph = 
            match fromNodes with
            | [] -> accGraph
            | fromNode::fromNodesRest -> neighboursFun (getNodes accGraph fromNode.Edges) fromNode fromNodesRest accGraph
        // Goes over all the neighbours of the fromNode -> neighbours can get updated from the next function. Update fromNodes list with each neighbour
        and neighboursFun (neighbours:Action list) (fromNode:Action) (newFromNodes:Action list) (accGraph:Graph) =
            match neighbours with
            | [] -> fromNodesFun newFromNodes accGraph
            | neighbour::neighboursRest -> 
                endNodesFun (getNodes accGraph neighbour.Edges) neighboursRest fromNode (neighbour::newFromNodes) accGraph
        // Goes over all the neighbours of the neighbour in the previous function. -> If there is an edge from fromNode to endNode remove it. Otherwise add the endNode to neighbours in the previous function.
        and endNodesFun (endNodes:Action list) (newNeighbours:Action list) (fromNode:Action) (newFromNodes:Action list) (accGraph:Graph) : Graph=
            match endNodes with 
            | [] -> neighboursFun newNeighbours fromNode newFromNodes accGraph
            | endNode::endNodesRest -> if (List.exists (fun id -> id = endNode.Id) fromNode.Edges)
                                       then endNodesFun endNodesRest newNeighbours fromNode newFromNodes (removeEdge fromNode endNode accGraph) 
                                       else endNodesFun endNodesRest (endNode::newNeighbours) fromNode newFromNodes accGraph
        
        fromNodesFun beginningNodes graph


    ///Determine whether there is a relation between two nodes by checking their individual Ids and Edges.
    let hasRelation (fromNode:Action) (toNode:Action) : bool = 
        let checkID = 
            let eventId = fst toNode.Id
            fromNode.CounterpartEventId = eventId && eventId = toNode.CounterpartEventId
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
        let transReduction = transitiveReductionBetter (getBeginningNodes filteredGraph) filteredGraph
        transReduction


    let merge (localGraph : Graph) (otherGraph : Graph) = 
        let combinedGraph = { Nodes = Map.fold (fun acc key value -> Map.add key value acc) localGraph.Nodes otherGraph.Nodes }
        let rec mergeInner (list:(ActionId*Action) list) graph = 
            match list with
            | [] -> graph
            | (xId,xa)::xs -> let rec mergeInnerInner innerList innerGraph = 
                                match innerList with 
                                | [] -> mergeInner xs innerGraph
                                | (yId,ya)::ys -> if (hasRelation xa ya && not (List.exists (fun id -> id = yId) xa.Edges)) 
                                                  then mergeInnerInner ys (addEdge xa ya innerGraph)
                                                  else mergeInnerInner ys innerGraph
                              mergeInnerInner list graph
        Some <| mergeInner (Map.toList combinedGraph.Nodes) combinedGraph


    // a more functional graph
    let mergeBetter (localGraph : Graph) (otherGraph : Graph) = 
        // Combine the graphs by putting all the nodes in the local graph
        let combinedGraph = { Nodes = Map.fold (fun acc key value -> Map.add key value acc) localGraph.Nodes otherGraph.Nodes }
        
        // go over every node in the graph
        let rec mergeInner (list:(ActionId*Action) list) (accGraph:Graph) : Graph = 
            match list with
            | [] -> accGraph
            | (node1Id,node1Action)::xs -> 
                // This calls the mergeInner recoursively with a new folded graph
                mergeInner xs (List.foldBack (fun (node2Id, node2Action) foldGraph -> 
                    // the foldback goes over all nodes and checks if relations exist from node1 to node2
                    // Check if a relation exist from node1 to node2 and check that there is not already an edge from node1 to node2.
                    if (hasRelation node1Action node2Action && not (List.exists (fun id -> id = node2Id) node1Action.Edges))
                    // add the edge to the foldGraph
                    then (addEdge node1Action node2Action foldGraph)
                    // dont change foldGraph
                    else foldGraph ) list accGraph)           
                                             
        Some (mergeInner (Map.toList combinedGraph.Nodes)  combinedGraph)    