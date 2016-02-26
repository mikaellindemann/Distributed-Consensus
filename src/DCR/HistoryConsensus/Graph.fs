namespace HistoryConsensus
open Action

module Graph =
    type Graph = Map<ActionId, Action>

    let addNode node (graph : Graph) : Graph = Map.add node.Id node graph
    let removeNode node (graph : Graph) : Graph =
        let graph' = Map.remove node.Id graph
        Map.map (fun id action -> 
                    {
                        Id = id; 
                        CounterpartEventId = action.CounterpartEventId; 
                        Type = action.Type;
                        Edges = List.ofSeq (Seq.where (fun id -> id = node.Id) (List.toSeq action.Edges))
                    }) graph'

    let addEdge fromNode toNode (graph : Graph) : Graph =
        Map.add fromNode.Id ({
                                Id = fromNode.Id; 
                                CounterpartEventId = fromNode.CounterpartEventId; 
                                Type = fromNode.Type;
                                Edges = toNode.Id :: fromNode.Edges}) graph

    let removeEdge fromNode toNode (graph : Graph) : Graph =
        Map.add fromNode.Id ({
                                Id = fromNode.Id;
                                CounterpartEventId = fromNode.CounterpartEventId;
                                Type = fromNode.Type;
                                Edges = List.ofSeq (Seq.where (fun actionId -> actionId <> toNode.Id) (Seq.ofList fromNode.Edges))
                                }) graph