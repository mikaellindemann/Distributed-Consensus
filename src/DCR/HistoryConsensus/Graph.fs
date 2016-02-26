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
    let graphNodesToList graph = Map.toList (graph.Nodes)

    let empty : Graph = { Nodes = Map.empty }


    let getBeginningNodes graph : Action list = 
        let rec calcToNodes (nodes:(ActionId*Action) list) (accList:ActionId list) : ActionId list =
            match nodes with
            | [] -> accList
            | (x,y)::xs -> calcToNodes xs (List.foldBack (fun element list -> element::list) y.Edges accList)
        let toNodes = calcToNodes (graphNodesToList graph) []
        let beginningNodesIds = List.except (List.map (fun (x,y) -> x) (graphNodesToList graph)) toNodes
        getNodes graph beginningNodesIds

    let transitiveClousure beginningNodes graph actionType = 
        let rec transitiveClous (list:Action list) newGraph = 
            match list with
            | [] -> newGraph
            | x::xs -> let rec inner edgeList innerGraph newXs =
                        match edgeList with
                        | [] -> transitiveClous newXs innerGraph
                        | y::ys ->  let toNode = (getNode graph y)
                                    if(toNode.Type = actionType)
                                    then inner ys (addEdge (getNode graph x.Id) toNode innerGraph) (toNode::newXs)
                                    else inner ys innerGraph newXs
                       inner x.Edges newGraph xs
        transitiveClous beginningNodes graph

    let simplify (graph:Graph) (actionType:ActionType) : Graph =
        let beginningNodes = getBeginningNodes graph
        let graphWithTransClous = transitiveClousure beginningNodes graph actionType
        empty 