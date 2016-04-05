module GraphTests

open NUnit.Framework
open FsUnit

open HistoryConsensus.Graph
open HistoryConsensus.Action

[<TestFixture>]
type GraphTests() = 
    let testAction = {
            Id = ("Test", 1);
            CounterpartId = ("CounterpartTest", 1);
            Type = ActionType.IncludedBy;
            Edges = Set.ofList [("2", 2); ("3", 3); ("4", 4)]
        }

    [<Test>]
    member g.``Add node to an empty map`` () = 
        let testGraph = {
            Nodes = Map.empty
        }

        let result = addNode testAction testGraph
        let actual =  Map.find ("Test", 1) result.Nodes
        actual |> should equal testAction

    [<Test>]
    member g.``Add node that already exists, and verify that edges are added to existing`` () = 
        let testGraph = {
            Nodes = Map.ofList 
                [(("Test", 1), {
                    Id = ("Test", 1);
                    CounterpartId = ("CounterpartTest", 1);
                    Type = ActionType.IncludedBy;
                    Edges = Set.ofList [("1", 1)]
                })]
        }

        let result = addNode testAction testGraph
        let expected = { testAction with Edges = Set.ofList [("1", 1); ("2", 2); ("3", 3); ("4", 4)] }

        let actual =  Map.find ("Test", 1) result.Nodes
        actual |> should equal expected