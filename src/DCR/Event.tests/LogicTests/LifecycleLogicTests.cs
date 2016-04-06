using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Interfaces;
using Event.Logic;
using Event.Models;
using Moq;
using NUnit.Framework;

namespace Event.Tests.LogicTests
{
    [TestFixture]
    class LifecycleLogicTests
    {
        private Mock<IEventStorage> _storageMock;
        private Mock<IEventStorageForReset> _resetStorageMock;
        private Mock<ILockingLogic> _lockingLogicMock;
        private LifecycleLogic _lifecycleLogic;

        #region Setup

        [OneTimeSetUp]
        public void Setup()
        {
            _storageMock = new Mock<IEventStorage>(MockBehavior.Strict);

            _resetStorageMock = new Mock<IEventStorageForReset>(MockBehavior.Strict);

            _lockingLogicMock = new Mock<ILockingLogic>(MockBehavior.Strict);

            _lifecycleLogic = new LifecycleLogic(_storageMock.Object, _resetStorageMock.Object, _lockingLogicMock.Object);
        }

        #endregion


        #region CreateEvent tests

        [Test]
        public void CreateEvent_CalledWithNullEventDto()
        {
            // Arrange
            var uri = new Uri("http://www.dr.dk");

            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.CreateEvent(null, uri);

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void CreateEvent_CalledWithNullUri()
        {
            // Arrange
            var eventDto = new EventDto
            {
                Conditions = new List<EventAddressDto>(),
                EventId = "Check in",
                Exclusions = new List<EventAddressDto>(),
                Executed = false,
                Included = true,
                Inclusions = new List<EventAddressDto>(),
                Name = "Check in at hospital",
                Pending = false,
                Responses = new List<EventAddressDto>(),
                Roles = new List<string>(),
                WorkflowId = "Cancer surgery"
            };

            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.CreateEvent(eventDto, null);

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task CreateEvent_WithIdAlreadyInDatabase()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.Exists(It.IsAny<string>(), It.IsAny<string>())).Returns(() => Task.Run(() => true));

            var mockResetStorage = new Mock<IEventStorageForReset>();

            var mockLockingLogic = new Mock<ILockingLogic>();

            ILifecycleLogic logic = new LifecycleLogic(
                mockStorage.Object,
                mockResetStorage.Object,
                mockLockingLogic.Object);

            var eventDto = new EventDto
            {
                Conditions = new List<EventAddressDto>(),
                EventId = "theAwesomeEventId",
                Exclusions = new List<EventAddressDto>(),
                Executed = false,
                Included = true,
                Inclusions = new List<EventAddressDto>(),
                Name = "Check in at hospital",
                Pending = false,
                Responses = new List<EventAddressDto>(),
                Roles = new List<string>(),
                WorkflowId = "Cancer surgery"
            };

            var uri = new Uri("http://www.dr.dk");

            // Act
            try
            {
                await logic.CreateEvent(eventDto, uri);
            }
            catch (Exception e)
            {
                // Assert
                Assert.IsInstanceOf<EventExistsException>(e);
            }
        }
        #endregion

        #region DeleteEvent tests

        [Test]
        public void DeleteEvent_WorkflowIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.DeleteEvent(null, "eventId");

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void DeleteEvent_EvendIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.DeleteEvent("workflowid", null);

            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void DeleteEvent_DeleteNonExistingIdDoesNotThrowException()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            var mockResetStorage = new Mock<IEventStorageForReset>();
            var mockLockingLogic = new Mock<ILockingLogic>();
            ILifecycleLogic logic = new LifecycleLogic(mockStorage.Object, mockResetStorage.Object, mockLockingLogic.Object);

            // If this method should throw an exception, the unit test will fail, hence no need to assert
            logic.DeleteEvent("notWorkflowId", "nonexistingId");
        }

        [Test]
        public void DeleteEvent_WillFailIfEventIsLockedByAnotherEvent()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.GetLockDto(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Run(() => new LockDto
            {
                LockOwner = "AnotherEventWhoLockedMeId",
                WorkflowId = "workflowId",
                EventId = "AnotherEventWhoLockedMeId"
            }));

            var mockResetStorage = new Mock<IEventStorageForReset>();
            var mockLockingLogic = new Mock<ILockingLogic>();

            ILifecycleLogic logic = new LifecycleLogic(mockStorage.Object, mockResetStorage.Object, mockLockingLogic.Object);

            // Act
            AsyncTestDelegate testDelegate = async () => await logic.DeleteEvent("workflowId", "Check patient");


            // Aseert
            Assert.ThrowsAsync<LockedException>(testDelegate);
        }
        #endregion

        #region ResetEvent tests

        [Test]
        public void ResetEvent_WorkflowIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.ResetEvent(null, "eventId");

            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void ResetEvent_EvendIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.ResetEvent("workflowid", null);

            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        #endregion

        #region GetEvent tests
        [Test]
        public void GetEvent_Will_Throw_NotFoundException_If_Ids_Does_Not_Exist()
        {
            // Arrange
            var mockStorage = new Mock<IEventStorage>();
            mockStorage.Setup(m => m.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            var mockResetStorage = new Mock<IEventStorageForReset>();
            var mockLockLogic = new Mock<ILockingLogic>();

            ILifecycleLogic logic = new LifecycleLogic(mockStorage.Object, mockResetStorage.Object, mockLockLogic.Object);

            // Act
            AsyncTestDelegate testdelegate = async () => await logic.GetEventDto("workflowId", "someEvent");

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testdelegate);
        }

        [Test]
        public void GetEvent_WorkflowIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.GetEventDto(null, "eventId");

            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void GetEvent_EvendIdIsNullWillThrowException()
        {
            // Act
            AsyncTestDelegate testDelegate = async () => await _lifecycleLogic.GetEventDto("workflowid", null);

            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task GetEvent_ReturnsANewEventDtoWithCorrectInformation()
        {
            //Assign
            var mockStorage = new Mock<IEventStorage>();
            var mockResetStorage = new Mock<IEventStorageForReset>();
            var mockLockLogic = new Mock<ILockingLogic>();

            var roles = new List<string>() { "Role1", "Role2", "Role3" };
            var conditions = new HashSet<RelationToOtherEventModel>()
            {
                new RelationToOtherEventModel() {EventId = "EventId1", Uri = new Uri("http://uri1.dk"), WorkflowId = "WorkflowId1"},
                new RelationToOtherEventModel() {EventId = "EventId2", Uri = new Uri("http://uri2.dk"), WorkflowId = "WorkflowId2"},
                new RelationToOtherEventModel() {EventId = "EventId3", Uri = new Uri("http://uri3.dk"), WorkflowId = "WorkflowId3"}
            };
            var responses = new HashSet<RelationToOtherEventModel>()
            {
                new RelationToOtherEventModel() {EventId = "EventId4", Uri = new Uri("http://uri4.dk"), WorkflowId = "WorkflowId4"},
                new RelationToOtherEventModel() {EventId = "EventId5", Uri = new Uri("http://uri5.dk"), WorkflowId = "WorkflowId5"},
            };
            var exclusions = new HashSet<RelationToOtherEventModel>()
            {
                new RelationToOtherEventModel() {EventId = "EventId6", Uri = new Uri("http://uri6.dk"), WorkflowId = "WorkflowId6"},
                new RelationToOtherEventModel() {EventId = "EventId7", Uri = new Uri("http://uri7.dk"), WorkflowId = "WorkflowId7"},
            };
            var inclusions = new HashSet<RelationToOtherEventModel>()
            {
                new RelationToOtherEventModel() {EventId = "EventId8", Uri = new Uri("http://uri8.dk"), WorkflowId = "WorkflowId8"},
                new RelationToOtherEventModel() {EventId = "EventId9", Uri = new Uri("http://uri9.dk"), WorkflowId = "WorkflowId9"},
            };

            const string name = "Name";
            const bool executed = false;
            const bool included = false;
            const bool pending = false;

            const string workflowId = "workflowId";
            const string eventId = "eventId";


            mockStorage.Setup(m => m.GetName(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(name);
            mockStorage.Setup(m => m.GetExecuted(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(executed);
            mockStorage.Setup(m => m.GetIncluded(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(included);
            mockStorage.Setup(m => m.GetPending(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(pending);
            mockStorage.Setup(m => m.GetConditions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(conditions);
            mockStorage.Setup(m => m.GetResponses(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(responses);
            mockStorage.Setup(m => m.GetExclusions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(exclusions);
            mockStorage.Setup(m => m.GetInclusions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(inclusions);
            mockStorage.Setup(m => m.GetRoles(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(roles);

            mockStorage.Setup(m => m.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            ILifecycleLogic lifecycleLogic = new LifecycleLogic(mockStorage.Object, mockResetStorage.Object, mockLockLogic.Object);


            //Act
            var eventDto = await lifecycleLogic.GetEventDto(workflowId, eventId);

            var con = conditions.Select(model => new EventAddressDto {WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri}).ToList();
            var res = responses.Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }).ToList();
            var exc = exclusions.Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }).ToList();
            var inc = inclusions.Select(model => new EventAddressDto { WorkflowId = model.WorkflowId, Id = model.EventId, Uri = model.Uri }).ToList();

            //Forcing the linq to load
            var conAct = eventDto.Conditions.ToList();
            var resAct = eventDto.Responses.ToList();
            var excAct = eventDto.Exclusions.ToList();
            var incAct = eventDto.Inclusions.ToList();


            //Assert
            Assert.AreEqual(name, eventDto.Name);
            Assert.AreEqual(executed, eventDto.Executed);
            Assert.AreEqual(included, eventDto.Included);
            Assert.AreEqual(pending, eventDto.Pending);

            Assert.AreEqual(con.Count, conAct.Count);
            for (var i = 0; i < con.Count; i++)
            {
                var expDto = con[i];
                var actDto = conAct[i];

                Assert.AreEqual(expDto.WorkflowId, actDto.WorkflowId);
                Assert.AreEqual(expDto.Id, actDto.Id);
                Assert.AreEqual(expDto.Roles, actDto.Roles);
                Assert.AreEqual(expDto.Uri, actDto.Uri);
            }

            Assert.AreEqual(res.Count, resAct.Count);
            for (var i = 0; i < res.Count; i++)
            {
                var expDto = res[i];
                var actDto = resAct[i];

                Assert.AreEqual(expDto.WorkflowId, actDto.WorkflowId);
                Assert.AreEqual(expDto.Id, actDto.Id);
                Assert.AreEqual(expDto.Roles, actDto.Roles);
                Assert.AreEqual(expDto.Uri, actDto.Uri);
            }

            Assert.AreEqual(exc.Count, excAct.Count);
            for (var i = 0; i < exc.Count; i++)
            {
                var expDto = exc[i];
                var actDto = excAct[i];

                Assert.AreEqual(expDto.WorkflowId, actDto.WorkflowId);
                Assert.AreEqual(expDto.Id, actDto.Id);
                Assert.AreEqual(expDto.Roles, actDto.Roles);
                Assert.AreEqual(expDto.Uri, actDto.Uri);
            }

            Assert.AreEqual(inc.Count, incAct.Count);
            for (var i = 0; i < inc.Count; i++)
            {
                var expDto = inc[i];
                var actDto = incAct[i];

                Assert.AreEqual(expDto.WorkflowId, actDto.WorkflowId);
                Assert.AreEqual(expDto.Id, actDto.Id);
                Assert.AreEqual(expDto.Roles, actDto.Roles);
                Assert.AreEqual(expDto.Uri, actDto.Uri);
            }
            
            CollectionAssert.AreEquivalent(roles, eventDto.Roles);

            Assert.AreEqual(workflowId, eventDto.WorkflowId);
            Assert.AreEqual(eventId, eventDto.EventId);
        }
        #endregion


    }
}
