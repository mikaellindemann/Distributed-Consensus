namespace HistoryConsensus
open Action

module Graph =
    type Graph =
        {
            Nodes: Map<ActionId, Action>
        }

    let addNode node (graph : Graph) : Graph = { Nodes = Map.add node.Id node graph.Nodes }
    let removeNode node (graph : Graph) : Graph =
        let graph' = Map.remove node.Id graph.Nodes
        { Nodes = Map.map (fun id action -> 
            {
                Id = id; 
                CounterpartEventId = action.CounterpartEventId; 
                Type = action.Type;
                Edges = List.ofSeq (Seq.where (fun id -> id = node.Id) (List.toSeq action.Edges))
            }) graph' }

    let addEdge fromNode toNode (graph : Graph) : Graph =
        { Nodes = Map.add fromNode.Id 
            ({
                Id = fromNode.Id; 
                CounterpartEventId = fromNode.CounterpartEventId; 
                Type = fromNode.Type;
                Edges = toNode.Id :: fromNode.Edges}) graph.Nodes }

    let removeEdge fromNode toNode (graph : Graph) : Graph =
        { Nodes = Map.add fromNode.Id 
            ({
                    Id = fromNode.Id;
                    CounterpartEventId = fromNode.CounterpartEventId;
                    Type = fromNode.Type;
                    Edges = List.ofSeq (Seq.where (fun actionId -> actionId <> toNode.Id) (Seq.ofList fromNode.Edges))
            }) graph.Nodes }

    let getNode graph actionId : Action = Map.find actionId graph.Nodes
    let getNodes graph actionIdList = List.map (getNode graph) actionIdList
    let getActionsFromGraph graph = List.map (fun (actionId, action) -> action) (Map.toList graph.Nodes)

    let empty : Graph = { Nodes = Map.empty }


    let getBeginningNodes graph : Action list = 

        // Calculate all the action ids that are referenced by other nodes in the graph (which is turned into a list).
        let rec calcToNodes (nodes:(ActionId*Action) list) (accList:ActionId list) : ActionId list =
            match nodes with
            | [] -> accList
            | (_,action)::xs -> calcToNodes xs (List.foldBack (fun element list -> element::list) action.Edges accList)
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
                                else inner ys innerGraph newXs
                inner x.Edges newGraph xs
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

    let hasRelation (fromNode:Action) (toNode:Action) : bool = 
        let checkID = fromNode.CounterpartEventId = (fst toNode.Id)  && (fst fromNode.Id) = toNode.CounterpartEventId
        let checkRelation fromType toType = 
            match (fromType, toType) with
            | (CheckedConditon, ChecksConditon) -> true
            | (IncludedBy, Includes) -> true
            | (ExcludedBy, Excludes) -> true
            | (SetPendingBy, SetsPending) -> true
            | (LockedBy, Locks) -> true
            | (UnlockedBy, Unlocks) -> true
            | _ -> false
        checkID && (checkRelation fromNode.Type toNode.Type)

    let simplify (graph:Graph) (actionType:ActionType) : Graph =
        let beginningNodes = getBeginningNodes graph
        let graphWithTransClos = transitiveClosure beginningNodes graph actionType
        let filteredGraph = { Nodes = Map.filter (fun id action -> action.Type = actionType ) graphWithTransClos.Nodes }
        let transReduction = transitiveReduction (getBeginningNodes filteredGraph) filteredGraph
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
                              mergeInnerInner xs graph
        Some (mergeInner (Map.toList combinedGraph.Nodes)  combinedGraph)

    

    let merge2 (localGraph : Graph) (otherGraph : Graph) = 
        // Add all nodes from the otherGraph to the localGraph
        let combinedGraph = { Nodes = Map.fold (fun acc key value -> Map.add key value acc) localGraph.Nodes otherGraph.Nodes }

        // Retrieve the Actions of the Graph for later use.
        let combinedGraphList = getActionsFromGraph combinedGraph

        // For every pair of actions in the Graph
        List.fold2 (fun mergedGraph node1 node2 ->
                        // If the actions are related by type and Id's (an action knows its counterpart) and they
                        // have no edge between them already
                        if (hasRelation node1 node2 && not (List.exists (fun id -> id = node2.Id) node1.Edges)) 
                        // Add the edge and look at the rest of the pairs of actions.
                        then (addEdge node1 node2 mergedGraph)
                        // Otherwise, just look at the rest of the pairs of actions.
                        else mergedGraph) 
                   combinedGraph combinedGraphList combinedGraphList
        
        
        // TODO: reimplement addNode node graph to update edges if node already exists.