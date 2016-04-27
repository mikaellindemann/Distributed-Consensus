namespace HistoryConsensus.Tests
//
//open NUnit.Framework
//open FsUnit
//
//open HistoryConsensus
//open FailureTypes
//open HistoryValidation
//
//module HistoryValidationTests = 
//    [<TestFixture>]
//    type HistoryValidationTests() =
//        [<Test>]
//        member h.``Test that empty history is alright when looking at beginning nodes``() =
//            let g = Graph.empty
//            let result = hasBeginningNodesValidation g
//            let expected : Result<Graph.Graph, FailureT list> = Success g
//
//            result |> should equal expected
//
//        [<Test>]
//        member h.``Test that non-empty history with two actions that points to each other is bad``() =
//            let n1 = Action.create ("MyId", 1) ("CounterpartId", 2) Action.ActionType.Includes (Set.singleton ("CounterpartId", 2))
//            let n2 = Action.create ("CounterpartId", 2) ("MyId", 1) Action.ActionType.IncludedBy (Set.singleton ("MyId", 1))
//
//            let g = Graph.addNode n2 (Graph.addNode n1 Graph.empty)
//
//            let result = hasBeginningNodesValidation g
//            let expected : Result<Graph.Graph, FailureT list> = Failure [(["Find cycles!"], Malicious)]
//
//            result |> should equal expected
//
//        [<Test>]
//        member h.``Test that a local history with a beginning node is alright``() =
//            let n1 = Action.create ("MyId", 1) ("CounterpartId", 2) Action.ActionType.Includes (Set.singleton ("CounterpartId", 2))
//            let n2 = Action.create ("CounterpartId", 2) ("MyId", 1) Action.ActionType.IncludedBy Set.empty
//
//            let g = Graph.addNode n2 (Graph.addNode n1 Graph.empty)
//
//            let result = hasBeginningNodesValidation g
//            let expected : Result<Graph.Graph, FailureT list> = Success g
//
//            result |> should equal expected
//
//        [<Test>]
//        member h.x() =
//            let n0 = Action.create ("AnotherId", 0) ("", -1) Action.ActionType.ExecuteStart (Set.singleton ("MyId", 1))
//            let n1 = Action.create ("MyId", 1) ("CounterpartId", 2) Action.ActionType.Includes (Set.singleton ("CounterpartId", 2))
//            let n2 = Action.create ("CounterpartId", 2) ("MyId", 1) Action.ActionType.IncludedBy (Set.singleton ("MyId", 1))
//
//            let g = Graph.addNode n2 (Graph.addNode n1 (Graph.addNode n0 Graph.empty))
//
//            let result = noCycleValidation g
//            let expected : Result<Graph.Graph, FailureT list> = Failure [(["MyId"; "CounterpartId"], Malicious)]
//
//            result |> should equal expected