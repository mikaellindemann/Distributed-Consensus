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
            { Nodes = Map.add node.Id { inGraph with Edges = Set.union inGraph.Edges node.Edges; FailureTypes = Set.union inGraph.FailureTypes node.FailureTypes } graph.Nodes }
        else
            { Nodes = Map.add node.Id node graph.Nodes }

    let removeNode node (graph : Graph) : Graph =
        let graph' = Map.remove node.Id graph.Nodes
        {
            Nodes = Map.map
                        (fun id action -> { action with Edges = Set.filter (fun id -> id <> node.Id) action.Edges })
                        graph'
        }

    //Retrive a single node from an actionId.
    let getNode graph actionId = Map.find actionId graph.Nodes

    let addEdge fromNodeId toNodeId (graph : Graph) : Graph =
        let fromNode = getNode graph fromNodeId
        {
            Nodes = Map.add
                        fromNodeId
                        { fromNode with Edges = Set.add toNodeId fromNode.Edges}
                        graph.Nodes
        }

    let removeEdge fromNodeId toNodeId (graph : Graph) : Graph =
        let fromNode = getNode graph fromNodeId
        {
            
            Nodes = Map.add
                        fromNode.Id
                        { fromNode with Edges = Set.filter (fun actionId -> actionId <> toNodeId) fromNode.Edges }
                        graph.Nodes
        }
    
    let hasEdge (fromAction:Action) (toActionId:ActionId) = Set.exists (fun id -> id=toActionId) fromAction.Edges
    
    let hasPath sourceNode toNodeId graph = 
        let rec checkNodes nodeId =    
            let node = getNode graph nodeId     
            if hasEdge node toNodeId then true
            else Set.fold (fun acc i -> acc || checkNodes i) false node.Edges    
        checkNodes sourceNode.Id
    
    //Retrieve a collection of nodes from given ActionIds.
    let getNodes graph actionIdList = List.map (getNode graph) actionIdList
    let getNodesS graph actionIdSet = Set.map (getNode graph) actionIdSet
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

    let transitiveReduction (graph: Graph) : Graph =
        let ifTransitiveClosureThenReduce sourceNodeId sourceNodeAction neighbourNodeAction destinationNodeId graphDestination =
            if (hasEdge neighbourNodeAction destinationNodeId && hasEdge sourceNodeAction destinationNodeId)
            then removeEdge sourceNodeId destinationNodeId graphDestination
            else graphDestination

        let ifEdgeThenCheckFurther sourceNodeId sourceAction neighbourNodeId neighbourAction graphNeighbour = 
            if (hasPath sourceAction neighbourNodeId graphNeighbour)
            then 
                Map.fold
                    ( fun graphDestination destinationNodeId destinationNodeAction -> 
                        ifTransitiveClosureThenReduce sourceNodeId sourceAction neighbourAction destinationNodeId graphDestination
                    ) graphNeighbour graphNeighbour.Nodes
            else graphNeighbour

        Map.fold 
            (fun graphSource sourceNodeId sourceAction -> 
                Map.fold 
                    (fun graphNeighbour neighbourNodeId neighbourAction -> 
                        ifEdgeThenCheckFurther sourceNodeId sourceAction neighbourNodeId neighbourAction graphNeighbour
                    ) graphSource graphSource.Nodes
            ) graph graph.Nodes

    ///Determine whether there is a relation between two nodes by checking their individual Ids and Edges.
    let hasRelation (fromNode:Action) (toNode:Action) : bool =
        let checkID =
            fromNode.CounterpartId = toNode.Id && fromNode.Id = toNode.CounterpartId
        let checkRelation fromType toType =
            match fromType, toType with
            | ChecksCondition, CheckedConditionBy -> true
            | Includes, IncludedBy                -> true
            | Excludes, ExcludedBy                -> true
            | SetsPending, SetPendingBy           -> true
            | _                                   -> false
        checkID && checkRelation fromNode.Type toNode.Type

    let merge localGraph otherGraph =
        let addToUsedActions action corner = Set.add action.Id corner

        let hasRelation first second corner =
            (first.Id <> second.Id)
                && hasRelation first second
                && not <| Set.contains second.Id corner

        let combinedGraph = Map.fold (fun graph key value -> addNode value graph) localGraph otherGraph.Nodes
        let combinedGraphAsSeq = Seq.map (fun (id,action) -> action) <| Map.toSeq combinedGraph.Nodes

        let findFirstRelation action corner = Seq.tryFind (fun action' -> hasRelation action action' corner) combinedGraphAsSeq

        let edgesSet,_ =
            Seq.fold (fun (newEdgesSet,corner) action ->
                    match findFirstRelation action corner with
                    | Some action' -> (Set.add (action, action') newEdgesSet, addToUsedActions action' corner)
                    | None -> newEdgesSet,corner
                ) (Set.empty, Set.empty) combinedGraphAsSeq

        Some <| Set.fold (fun graph (fromNode, toNode) -> addEdge fromNode.Id toNode.Id graph) combinedGraph edgesSet

    let cycleCheck node graph =
        let rec cycleThrough visitedNodes remaining =
            match remaining with
            | [] -> true
            | action::nodes -> if not <| List.contains action.Id visitedNodes
                               then cycleThrough <| action.Id::visitedNodes <| nodes @ (getNodes graph <| Set.toList node.Edges)
                               else false
        let neighbourNodes = getNodes graph <| Set.toList node.Edges
        cycleThrough [node.Id] neighbourNodes

    let fold folder state graph = Map.fold (fun state _ node -> folder state node) state graph.Nodes

    let forall predicate graph = Map.forall (fun _ node -> predicate node) graph.Nodes

    let exists predicate graph = Map.exists (fun _ node -> predicate node) graph.Nodes