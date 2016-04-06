using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Moq;
using NUnit.Framework;
using Server.Interfaces;
using Server.Logic;

namespace Server.Tests.LogicTests
{
    [TestFixture]
    class WorkflowsHistoryLogicTests
    {
        private Mock<IServerHistoryStorage> _storageMock;
        private List<ActionModel> _testModelList;
        private IWorkflowHistoryLogic _toTest;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var mock = new Mock<IServerHistoryStorage>();

            mock.Setup(m => m.GetHistoryForWorkflow(It.IsAny<string>()))
                .Returns((string workflowId) => 
                            Task.Run( () => _testModelList.Where(w => w.WorkflowId == workflowId).AsQueryable() )).Verifiable();

            mock.Setup(m => m.SaveHistory(It.IsAny<ActionModel>()))
                .Returns((ActionModel model) =>
                            Task.Run( () => _testModelList.Add(model))).Verifiable();

            mock.Setup(m => m.SaveNonWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Returns((ActionModel model) => 
                            Task.Run(() => _testModelList.Add(model))).Verifiable();

            mock.Setup(m => m.Dispose()).Verifiable();

            _storageMock = mock;
            _toTest = new WorkflowHistoryLogic(mock.Object);
        }

        [SetUp]
        public void ResetList()
        {
            _testModelList = new List<ActionModel>();
        }

        [Test]
        public async Task GetHistoryForWorkflowTest()
        {
            //Setup.
            var testHistory = CreateTestHistory();

            _testModelList.Add(testHistory);

            //Execute.
            var collection = await _toTest.GetHistoryForWorkflow(@"&%¤#æøå*¨^´`?");
            var result = collection.First();

            //Assert.
            _storageMock.Verify(m => m.GetHistoryForWorkflow(It.IsAny<string>()), Times.Once);
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _toTest.GetHistoryForWorkflow(null));
            Assert.DoesNotThrowAsync(async () => await _toTest.GetHistoryForWorkflow(@"&%¤#æøå*¨^´`?"));
            Assert.IsTrue(_testModelList.Any());
            Assert.AreEqual(testHistory.WorkflowId, result.WorkflowId);
            Assert.AreEqual(testHistory.EventId, result.EventId);
            Assert.AreEqual(testHistory.CounterpartId, result.CounterpartId);
            Assert.AreEqual(1, result.TimeStamp);
        }

        [Test]
        public void SaveHistoryTest()
        {
            //Setup.
            var testHistory = CreateTestHistory();

            //Execute.
            Assert.DoesNotThrowAsync(async () => await _toTest.SaveHistory(testHistory));

            //Assert.
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _toTest.SaveHistory(null));
            _storageMock.Verify(m => m.SaveHistory(It.IsAny<ActionModel>()), Times.Once);
            Assert.IsTrue(_testModelList.Any());
        }

        public void SaveNoneWorkflowSpecificHistoryTest()
        {
            //Setup.
            var testHistory = CreateTestHistory();

            //Execute.
            Assert.DoesNotThrowAsync(async () => await _toTest.SaveNoneWorkflowSpecificHistory(testHistory));

            //Assert.
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _toTest.SaveNoneWorkflowSpecificHistory(null));
            _storageMock.Verify(m => m.SaveNonWorkflowSpecificHistory(It.IsAny<ActionModel>()), Times.Once);
            Assert.IsTrue(_testModelList.Any());
        }

        [Test]
        public void DisposeTest()
        {
            using (_toTest)
            {
                Assert.IsTrue(true);
            }

            _storageMock.Verify(m => m.Dispose(), Times.Once);
        }

        private static ActionModel CreateTestHistory()
        {
            return new ActionModel
            {
                EventId = @"&%¤#æøå*¨^´`?",
                WorkflowId = @"&%¤#æøå*¨^´`?",
                Id = 1,
                CounterpartId = @"&%¤#æøå*¨^´`?",
                Type = ActionType.Excludes
            };
        }
    }
}
