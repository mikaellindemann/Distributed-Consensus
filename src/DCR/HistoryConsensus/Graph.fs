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

    let transitiveReduction beginningNodes graph =
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
                                       then endNodesFun endNodesRest newNeighbours fromNode newFromNodes (removeEdge fromNode.Id endNode.Id accGraph)
                                       else endNodesFun endNodesRest (unionListWithoutDuplicates [endNode] newNeighbours) fromNode newFromNodes accGraph
        fromNodesFun beginningNodes graph

    ///Determine whether there is a relation between two nodes by checking their individual Ids and Edges.
    let hasRelation (fromNode:Action) (toNode:Action) : bool =
        let checkID =
            fromNode.CounterpartId = toNode.Id && fst fromNode.Id = fst toNode.CounterpartId
        let checkRelation fromType toType =
            match fromType, toType with
            | ChecksConditon, CheckedConditon   -> true
            | Includes, IncludedBy              -> true
            | Excludes, ExcludedBy              -> true
            | SetsPending, SetPendingBy         -> true
            | Locks, LockedBy                   -> true
            | Unlocks, UnlockedBy               -> true
            | _                                 -> false
        checkID && checkRelation fromNode.Type toNode.Type

    let collapse (graph : Graph) =
        let createMapForSingleExecution actions newActionId map = 
            Set.fold (fun map actionId -> Map.add actionId newActionId map) map actions

        let findSuccessorOnEvent actionId currentExecution =
            let edges = (getNode graph actionId).Edges
            if Set.isEmpty edges
            then (currentExecution, None)
            else
                let immediateSuccessor = Seq.find (fun (neighborEventId,_) -> (fst actionId) = neighborEventId) edges
                let rest = Set.remove immediateSuccessor edges
                if Set.isEmpty rest
                then (currentExecution, Some <| immediateSuccessor)
                else (Set.add (Seq.find (fun t -> true) rest) currentExecution, Some immediateSuccessor)

        let rec findSingleExecution actionId result =
            let newResult = (Set.add actionId result)
            let action = getNode graph actionId
            if action.Type = ExecuteFinish
            then newResult
            else 
                match findSuccessorOnEvent actionId newResult with
                | (currentExecution, None) -> currentExecution
                | (currentExecution, Some successor) -> findSingleExecution successor currentExecution

        let startExecutions = 
            Map.fold (fun set actionId _ -> Set.add actionId set)
                Set.empty
                (Map.filter (fun _ action -> action.Type = ExecuteStart) graph.Nodes) 

        let mapOfCollapsedExecutions =
            let uncollapsed = Set.map (fun startActionId -> startActionId, findSingleExecution startActionId Set.empty) startExecutions
            Set.fold 
                (fun map (startActionId,execution) -> createMapForSingleExecution execution startActionId map)
                Map.empty
                uncollapsed

        let getEdgesThatIsntYourself actionId (newActionId : ActionId) map =
            let action = getNode graph actionId
            let newIds = Set.map (fun oldId -> Map.find oldId map) action.Edges
            Set.remove newActionId newIds

        Map.fold 
            (fun newGraph oldId newId ->
                let edgeSet = getEdgesThatIsntYourself oldId newId mapOfCollapsedExecutions // Todo, calculate this set of edges from oldId excluding events in the same event.
                let action = Action.create newId newId ActionType.ExecuteFinish edgeSet
                addNode action newGraph)
            empty
            mapOfCollapsedExecutions

    let simplify (graph:Graph) (actionType:ActionType) : Graph =
        let collapsedExecutions = collapse graph
        let transReduction = transitiveReduction (getBeginningNodes collapsedExecutions) collapsedExecutions
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

        Some <| List.fold (fun graph (fromNode, toNode) -> addEdge fromNode.Id toNode.Id graph) combinedGraph edgesList

    let cycleCheck node graph =
        let rec cycleThrough visitedNodes remaining =
            match remaining with
            | [] -> true
            | action::nodes -> if not <| List.contains action.Id visitedNodes
                               then cycleThrough <| action.Id::visitedNodes <| nodes @ (getNodes graph <| Set.toList node.Edges)
                               else false
        let neighbourNodes = getNodes graph <| Set.toList node.Edges
        cycleThrough [node.Id] neighbourNodes
