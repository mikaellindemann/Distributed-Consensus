namespace HistoryConsensus.Tests

open NUnit.Framework
open FsUnit

open HistoryConsensus
open LocalHistoryValidation

module LocalHistoryValidationTests = 
    [<TestFixture>]
    type LocalHistoryValidationTests() =

        [<Test>]
        member lh.EnTest() = 
            let badHistory = ()

            true |> should equal true