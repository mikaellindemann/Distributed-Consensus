using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Interfaces;
using Event.Logic;
using Event.Models;
using Moq;
using NUnit.Framework;

namespace Event.Tests.LogicTests
{
    [TestFixture]
    class LockLogicTests
    {

        #region Setup

        /// <summary>
        /// This method returns a ILockingLogic instance, that was initialized using dependency-injection.
        /// The injected (mocked) modules are not configured; this method should not be used, if you intend on testing
        /// some interaction with either EventCommunicator or EventStorage. 
        /// </summary>
        /// <returns></returns>
        public ILockingLogic SetupDefaultLockingLogic()
        {
            var mockStorage = new Mock<IEventStorage>();
            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);
            
            return logic;
        }
        #endregion

        #region Constructor and dispose

        [Test]
        public void DisposeStorage_Test()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.Dispose()).Verifiable();

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            using (lockingLogic)
            {
                // Do nothing.
            }

            // Assert
            mockStorage.Verify(t => t.Dispose(), Times.Once);
        }

        [Test]
        public void DisposeCommunicator_Test()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Dispose()).Verifiable();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            using (lockingLogic)
            {
                // Do nothing.
            }

            // Assert
            mockEventCommunicator.Verify(t => t.Dispose(), Times.Once);
        }

        #endregion

        #region WaitForMyTurn Tests

        [TestCase("AnotherWid","Eid")]
        [TestCase("Wid", "AnotherEid")]
        [TestCase("AnotherWid", "AnotherEid")]
        public async void WaitForMyTurn_Succes_OtherLocksOnOtherWorkflowsExist(string alreadyWid, string alreadyEid)
        {
            //Arrange
            ILockingLogic lockingLogic = SetupDefaultLockingLogic();

            string eventId = "Eid";
            string workflowId = "Wid";

            var eventDictionary = LockingLogic.LockQueue.GetOrAdd(alreadyWid, new ConcurrentDictionary<string, ConcurrentQueue<LockDto>>());
            var queue = eventDictionary.GetOrAdd(alreadyEid, new ConcurrentQueue<LockDto>());
            queue.Enqueue(new LockDto { WorkflowId = alreadyWid, EventId = alreadyEid, LockOwner = "AlreadyThereOwner" });

            LockDto lockDto = new LockDto { EventId = eventId, LockOwner = "LockOwner", WorkflowId = workflowId };
            //Act
            await lockingLogic.WaitForMyTurn(workflowId, eventId, lockDto);
            //Assert
            Assert.IsEmpty(LockingLogic.LockQueue[workflowId][eventId]);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }
        
        [Test]
        public async void WaitForMyTurn_Succes_EmptyQueueLockEntersAndLeaves()
        {
            //Arrange
            ILockingLogic lockingLogic = SetupDefaultLockingLogic();
            LockDto lockDto = new LockDto { EventId = "Eid", LockOwner = "LockOwner", WorkflowId = "Wid" };
            //Act
            await lockingLogic.WaitForMyTurn("Wid", "Eid", lockDto);
            //Assert
            Assert.IsEmpty(LockingLogic.LockQueue["Wid"]["Eid"]);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        [Test]
        public async void WaitForMyTurn_Succes_QueueHasAnElementWhichGetsRemovedAfter5Seconds()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();
            var lockDtoToReturnFromStorage = new LockDto
            {
                WorkflowId = "Wid",
                EventId = "Eid",
                LockOwner = "AlreadyThereOwner"          // Notice, AlreadyThereOwner will be the lockOwner according to Storage!
            };

            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(lockDtoToReturnFromStorage);

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            string eventId = "Eid";
            string workflowId = "Wid";

            var eventDictionary = LockingLogic.LockQueue.GetOrAdd(workflowId, new ConcurrentDictionary<string, ConcurrentQueue<LockDto>>());
            var queue = eventDictionary.GetOrAdd(eventId, new ConcurrentQueue<LockDto>());
            queue.Enqueue(new LockDto { WorkflowId = workflowId, EventId = eventId, LockOwner = "AlreadyThereOwner" });

            LockDto lockDto = new LockDto { EventId = eventId, LockOwner = "LockOwner", WorkflowId = workflowId };
            //Act
            //DO NOT AWAIT
            var task = Task.Run(async () =>
            {
                await Task.Delay(5000);
                LockDto dequeuedDto;
                queue.TryDequeue(out dequeuedDto);
            });
            // To begin with we want the task to still be running which removes the queued object
            if (!task.IsCompleted)
            {
                await lockingLogic.WaitForMyTurn("Wid", "Eid", lockDto);
                // after waiting for my turn, the task must have been completed.
                if(!task.IsCompleted) Assert.Fail();
            }
            else
            {
                Assert.Fail();
            }
            //Assert
            Assert.IsEmpty(LockingLogic.LockQueue["Wid"]["Eid"]);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        [Test]
        public async void WaitForMyTurn_Succes_AlreadyLockedBySelf()
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";
            string lockOwner = "lockOwner";

            var mockStorage = new Mock<IEventStorage>();
            var lockDtoToReturnFromStorage = new LockDto
            {
                WorkflowId = workflowId,
                EventId = eventId,
                LockOwner = lockOwner
            };

            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(lockDtoToReturnFromStorage);

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            LockDto lockDto = new LockDto { EventId = eventId, LockOwner = lockOwner, WorkflowId = workflowId };
            //Act
            await lockingLogic.WaitForMyTurn(workflowId, eventId, lockDto);
            //Assert
            Assert.IsEmpty(LockingLogic.LockQueue[workflowId][eventId]);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        [TestCase(null,"")]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void WaitForMyTurn_ParameterIsNull(string workflowId, string eventId)
        {
            //Arrange
            ILockingLogic lockingLogic = SetupDefaultLockingLogic();
            LockDto lockDto = new LockDto{ EventId = eventId, LockOwner = "LockOwner", WorkflowId = workflowId};
            //Act
            var testDelegate = new TestDelegate(async () => await lockingLogic.WaitForMyTurn(workflowId, eventId, lockDto));
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }
        [Test]
        public void WaitForMyTurn_LockDtoIsNull()
        {
            //Arrange
            var lockingLogic = SetupDefaultLockingLogic();
            //Act
            var testDelegate = new TestDelegate(async () => await lockingLogic.WaitForMyTurn("Wid", "Eid", null));
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\n")]
        [TestCase("\t")]
        [TestCase(null)]
        public void WaitForMyTurn_LockDtoOwnerIsNull(string lockOwner)
        {
            //Arrange
            ILockingLogic lockingLogic = SetupDefaultLockingLogic();
            LockDto lockDto = new LockDto { EventId = "Eid", LockOwner = lockOwner, WorkflowId = "Wid" };
            //Act
            var testDelegate = new TestDelegate(async () => await lockingLogic.WaitForMyTurn("Wid", "Eid", lockDto));
            //Assert
            Assert.Throws<ArgumentException>(testDelegate);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        [Test]
        public void WaitForMyTurn_QueueHasAnElementWhichDoesNotGetRemoved()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();
            var lockDtoToReturnFromStorage = new LockDto
            {
                WorkflowId = "Wid",
                EventId = "Eid",
                LockOwner = "AlreadyThereOwner"          // Notice, AlreadyThereOwner will be the lockOwner according to Storage!
            };

            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(lockDtoToReturnFromStorage);

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            string eventId = "Eid";
            string workflowId = "Wid";

            var eventDictionary = LockingLogic.LockQueue.GetOrAdd(workflowId, new ConcurrentDictionary<string, ConcurrentQueue<LockDto>>());
            var queue = eventDictionary.GetOrAdd(eventId, new ConcurrentQueue<LockDto>());
            queue.Enqueue(new LockDto { WorkflowId = workflowId, EventId = eventId, LockOwner = "AlreadyThereOwner"});

            LockDto lockDto = new LockDto { EventId = eventId, LockOwner = "LockOwner", WorkflowId = workflowId };
            //Act
            var testDelegate = new TestDelegate(async () => await lockingLogic.WaitForMyTurn(workflowId, eventId, lockDto));
            //Assert
            Assert.Throws<LockedException>(testDelegate);
            //Cleanup
            LockingLogic.LockQueue.Clear();
        }

        #endregion

        #region IsAllowedToOperate tests

        [Test]
        public void IsAllowedToOperate_ReturnsTrueWhenNoLockIsSet()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>())).Returns(() => Task.Run(() => (LockDto)null));
            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            var result = logic.IsAllowedToOperate("workflowId", "testA", "testB").Result;

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsAllowedToOperate_ReturnsTrueWhenEventWasPreviouslyLockedWithCallersId()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            var lockDto = new LockDto
            {
                LockOwner = "EventA",
                WorkflowId = "workflowId",
                EventId = "DatabaseRelevantId"
            };
            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Run(() => lockDto));

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            var result = logic.IsAllowedToOperate("workflowId", "irrelevantToTestId", "EventA").Result;

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsAllowedToOperate_ReturnsFalseWhenEventWasPreviouslyLockedWithAnotherId()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            var lockDto = new LockDto
            {
                LockOwner = "EventA",       // Notice, EventA is locking!
                WorkflowId = "workflowId", 
                EventId = "DatabaseRelevantId"
            };
            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Run(() => lockDto));

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            var result = logic.IsAllowedToOperate("workflowId", "irrelevantToTestId", "EventB").Result; // Notice EventB is used here

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsAllowedToOperate_RaisesExceptionIfProvidedNullEventId()
        {
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var testDelegate = new TestDelegate(async () => await logic.IsAllowedToOperate(null, null, "EventA"));
            
            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void IsAllowedToOperate_RaisesExceptionIfProvidedNullCallerId()
        {
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var testDelegate = new TestDelegate(async () => await logic.IsAllowedToOperate("workflowId", "someEvent", null));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        #endregion 

        #region LockAllForExecute tests

        [TestCase(null,null)]
        [TestCase("", null)]
        [TestCase(null, "")]
        [TestCase("text", null)]
        [TestCase(null, "text")]
        public void LockAllForExecute_WillRaiseExceptionIfEventIdIsNull(string workflowId, string eventId)
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var lockAllTask = logic.LockAllForExecute(workflowId, eventId);

            // Assert
            if (lockAllTask.Exception == null)
            {
                Assert.Fail("lockAllTask was expected to contain a non-null Exception-property");
            }

            var innerException = lockAllTask.Exception.InnerException;

            Assert.IsInstanceOf<ArgumentNullException>(innerException);
        }

        [Test]
        public async void LockAllForExecute_Success_EmptyRelationLists()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockAllForExecute("Wid","Eid");
            
            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true,false,false,false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void LockAllForExecute_Success_1OtherElementInRelations(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void LockAllForExecute_Success_TheSameEventInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void LockAllForExecute_Success_ManyOtherElementsInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        [Test]
        public async void LockAllForExecute_Success_ManySameElementsInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }
            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }
        #endregion

        #region LockList

        [Test]
        public async void LockAll_Success_EmptyRelationList()
        {
            //Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();
            //Act
            var returnValue = await logic.LockList(new SortedDictionary<string, RelationToOtherEventModel>(), "Eid");
            //Assert
            Assert.IsTrue(returnValue);
        }

        [Test]
        public async void LockAll_Success_1ElementRelationList()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);
            //Act
            var returnValue = await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel> { { "testId", new RelationToOtherEventModel() } }, "Eid");
            //Assert
            Assert.IsTrue(returnValue);
        }

        [Test] 
        public void LockAll_Fails_1ElementRelationListNullEventId()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);
            //Act
            TestDelegate testDelegate = async()=>await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel> { { "testId", new RelationToOtherEventModel() } }, null);
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [TestCase(null)]
        [TestCase("Eid")]
        public void LockAll_Fails_WhenParametersAreNull(string eventId)
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);
            //Act
            TestDelegate testDelegate = async () => await lockingLogic.LockList(null, eventId);
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void LockAll_Fail_NullEventIdAndConnectionFails_UnlockSomeThrows()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            TestDelegate testDelegate = async () => await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel>{{"testId", new RelationToOtherEventModel()}}, null);
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async void LockAll_Succes_FailsToLockAllEventsReturnsFalse()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel>() { { "testId", new RelationToOtherEventModel() } }, "Eid");
            //Assert
            Assert.IsFalse(returnValue);
        }

        [Test]
        public async void LockAll_Succes_TwoElementsSecondFails()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), "fails"))
                .Throws(new Exception());
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel>() { { "AtestId", new RelationToOtherEventModel() }, { "fails", new RelationToOtherEventModel { EventId = "fails" } } }, "Eid");
            //Assert
            Assert.IsFalse(returnValue);
        }

        [Test] 
        public async void LockAll_Succes_TwoElementsSecondFailsAndUnlockFails()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));
            mockEventCommunicator.Setup(m => m.Lock(It.IsAny<Uri>(), It.IsAny<LockDto>(), It.IsAny<string>(), "fails"))
                .Throws(new Exception());
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.LockList(new SortedDictionary<string, RelationToOtherEventModel>() { { "AtestId", new RelationToOtherEventModel() }, { "fails", new RelationToOtherEventModel { EventId = "fails" } } }, "Eid");
            //Assert
            Assert.IsFalse(returnValue);
        }

        #endregion

        #region LockSelf tests
        [Test]
        public void LockSelf_Success()
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            TestDelegate testDelegate = async () => await logic.LockSelf("Wid", "Eid", new LockDto{LockOwner="LockOwner"});

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void LockSelf_WillRaiseExceptionIfLockDtoIsNull()
        {
            // Arrange
            var logic = SetupDefaultLockingLogic();

            // Act 
            var testDelegate = new TestDelegate(async () => await logic.LockSelf("workflowId", "testA", null));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void LockSelf_WillRaiseExceptionIfEventIdIsNull()
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            var lockDto = new LockDto
            {
                WorkflowId = "workflowId", 
                EventId = "DatabaseId",
                LockOwner = "whatever"
            };

            // Act 
            var testDelegate = new TestDelegate(async () => await logic.LockSelf(null, null,lockDto));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }



        #endregion

        #region UnlockAllForExecute tests

        [Test]
        public void UnlockAll_WillRaiseExceptionIfEventIdWasNull()
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var unlockAllTask = logic.UnlockAllForExecute(null, null);

            // Assert
            if (unlockAllTask.Exception == null)
            {
                Assert.Fail("Task should have thrown an exception");
            }
            Assert.IsInstanceOf<ArgumentNullException>(unlockAllTask.Exception.InnerException);
        }

        [Test]
        public void UnlockAll_WillRaiseExceptionIfStorageReturnsNullRelationsSets()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Run(() => (HashSet<RelationToOtherEventModel>) null));

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            var unlockAllTask = logic.UnlockAllForExecute("workflowId", "someEvent");

            // Assert
            if (unlockAllTask.Exception == null)
            {
                Assert.Fail("Task should have thrown an exception");
            }
            Assert.IsInstanceOf<NullReferenceException>(unlockAllTask.Exception.InnerException);
        }

        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase(null, "")]
        [TestCase("text", null)]
        [TestCase(null, "text")]
        public void UnlockAllForExecute_WillRaiseExceptionIfEventIdIsNull(string workflowId, string eventId)
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var lockAllTask = logic.UnlockAllForExecute(workflowId, eventId);

            // Assert
            if (lockAllTask.Exception == null)
            {
                Assert.Fail("lockAllTask was expected to contain a non-null Exception-property");
            }

            var innerException = lockAllTask.Exception.InnerException;

            Assert.IsInstanceOf<ArgumentNullException>(innerException);
        }

        [Test]
        public async void UnlockAllForExecute_Success_EmptyRelationLists()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute("Wid", "Eid");

            //Assert
            Assert.IsTrue(returnValue);
        }

        [Test]
        public async void UnlockAllForExecute_Success_ReturnFalseWhenSecondElementFailsTo()
        {
            //Arrange
            var mockStorage = new Mock<IEventStorage>();

            mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>{new RelationToOtherEventModel{EventId="Fails"}});
            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(
                m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute("Wid", "Eid");

            //Assert
            Assert.IsFalse(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void UnlockAllForExecute_Success_1OtherElementInRelations(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void UnlockAllForExecute_Success_TheSameEventInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel> { new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId } });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        public async void UnlockAllForExecute_Success_ManyOtherElementsInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = "Eid2", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid3", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }

        [TestCase(true, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, true, false)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, false, true)]
        [TestCase(true, true, true, true)]
        [Test]
        public async void UnlockAllForExecute_Success_ManySameElementsInResponses(bool conditions, bool exclusions, bool responses, bool inclusions)
        {
            //Arrange
            string eventId = "Eid";
            string workflowId = "Wid";

            var mockStorage = new Mock<IEventStorage>();

            if (!conditions)
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!exclusions)
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!responses)
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }

            if (!inclusions)
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new HashSet<RelationToOtherEventModel>());
            }
            else
            {
                mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HashSet<RelationToOtherEventModel>
                {
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = eventId, Uri = new Uri("http://www.google.com"), WorkflowId = workflowId },
                    new RelationToOtherEventModel { EventId = "Eid4", Uri = new Uri("http://www.google.com"), WorkflowId = workflowId }
                });
            }
            mockStorage.Setup(m => m.GetUri(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Uri("http://www.google.com"));

            var mockEventCommunicator = new Mock<IEventFromEvent>();
            mockEventCommunicator.Setup(m => m.Unlock(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0));

            ILockingLogic lockingLogic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            //Act
            var returnValue = await lockingLogic.UnlockAllForExecute(workflowId, eventId);

            //Assert
            Assert.IsTrue(returnValue);
        }
        
        #endregion

        #region UnlockSelf tests

        [Test]
        public void UnlockSelf_Success()
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            TestDelegate testDelegate = async ()=> await logic.UnlockSelf("Wid", "Eid", "someEvent");

            // Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void UnlockSelf_WillRaiseExceptionIfCalledWithNullEventId()
        {
            // Arrange
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var unlockSelfTask = logic.UnlockSelf(null, null, "someEvent");

            // Assert
            if (unlockSelfTask.Exception == null)
            {
                Assert.Fail("Task should have thrown an exception");
            }
            Assert.IsInstanceOf<ArgumentNullException>(unlockSelfTask.Exception.InnerException);
        }

        [Test]
        public void UnlockSelf_WillRaiseExceptionIfCalledWithNullCallerId()
        {
            ILockingLogic logic = SetupDefaultLockingLogic();

            // Act
            var unlockSelfTask = logic.UnlockSelf("workflowId", "someEvent", null);

            // Assert
            if (unlockSelfTask.Exception == null)
            {
                Assert.Fail("Task should have thrown an exception");
            }
            Assert.IsInstanceOf<ArgumentNullException>(unlockSelfTask.Exception.InnerException);
        }

        [Test]
        public void UnlockSelf_WillRaiseExceptionIfEventIsLockedBySomeoneElse()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            var lockDtoToReturnFromStorage = new LockDto
            {
                WorkflowId = "workflowId", 
                EventId = "databaseRelevantId",
                LockOwner = "Johannes"          // Notice, Johannes will be the lockOwner according to Storage!
            };

            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(lockDtoToReturnFromStorage);

            var mockEventCommunicator = new Mock<IEventFromEvent>();

            ILockingLogic logic = new LockingLogic(
                mockStorage.Object,
                mockEventCommunicator.Object);

            // Act
            var testDelegate = new TestDelegate(async () => await logic.UnlockSelf("workflowId", "irrelevantId", "Per")); // Notice, we're trying to let Per unlock

            // Assert
            Assert.Throws<LockedException>(testDelegate);
        }

        #endregion

    }
}
