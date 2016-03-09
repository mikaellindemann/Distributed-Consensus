module GraphTests

open NUnit.Framework
open HistoryConsensus.Graph
open HistoryConsensus.Action
open NUnitRunner

[<TestFixture>]
type GraphTests() = 

    [<Test>]
    member g.testAddNode() = 
        let testGraph = {
            Nodes = Map.empty
        }

        let testAction = {
            Id = ("Test", 1);
            CounterpartEventId = "CounterpartTest";
            Type = ActionType.LockedBy;
            Edges = Set.empty
        }

        let result = addNode testAction testGraph
        Assert.IsTrue (Map.forall (fun a v -> a = testAction.Id && v = testAction) result.Nodes)

    [<Test>]
    member g.testAddNodeThatAlreadyExists() = 
        let testGraph = {
            Nodes = Map.ofList 
                [(("Test", 1), {
                    Id = ("Test", 1);
                    CounterpartEventId = "CounterpartTest";
                    Type = ActionType.LockedBy;
                    Edges = Set.ofList [("1", 1)]
                })]
        }

        let testAction = {
            Id = ("Test", 1);
            CounterpartEventId = "CounterpartTest";
            Type = ActionType.LockedBy;
            Edges = Set.ofList [("2", 2); ("3", 3); ("4", 4)]
        }

        let result = addNode testAction testGraph
        Assert.IsTrue (Map.forall (fun a v -> a = testAction.Id && v.Type = testAction.Type && v.CounterpartEventId = testAction.CounterpartEventId) result.Nodes)
        Assert.IsTrue ((Map.find ("Test", 1) result.Nodes).Edges = Set.ofList [("1", 1); ("2", 2); ("3", 3); ("4", 4)])
