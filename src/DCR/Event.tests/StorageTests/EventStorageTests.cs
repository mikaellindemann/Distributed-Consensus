using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Common.Exceptions;
using Event.Interfaces;
using Event.Models;
using Event.Models.UriClasses;
using Event.Storage;
using Moq;
using NUnit.Framework;

namespace Event.Tests.StorageTests
{
    [TestFixture]
    class EventStorageTests
    {
        private Mock<IEventContext> _contextMock;
        private EventStorage _eventStorage;
        private List<EventModel> _eventModels;
        private List<ActionModel> _historyModels;
        private List<ExclusionUri> _exclusionUris;
        private List<InclusionUri> _inclusionUris;
        private List<ConditionUri> _conditionUris;
        private List<ResponseUri> _responseUris;
        private FakeDbSet<EventModel> _eventModelMock;
        private FakeDbSet<ActionModel> _historyMock; 
        private FakeDbSet<ResponseUri> _responseMock;
        private FakeDbSet<ConditionUri> _conditionMock;
        private FakeDbSet<InclusionUri> _inclusionMock;
        private FakeDbSet<ExclusionUri> _exclusionMock;
        
        
        [SetUp]
        public void SetUp()
        {
            _contextMock = new Mock<IEventContext>();
            _contextMock.Setup(c => c.Dispose()).Verifiable();
            _contextMock.SetupAllProperties();

            _eventModels = new List<EventModel>
            { 
                new EventModel 
                {
                    Id = "eventId",
                    Name = "Event",
                    WorkflowId = "workflowId",
                    OwnUri = "http://www.contoso.com/",
                    Roles = new List<EventRoleModel>
                    {
                        new EventRoleModel
                        {
                            WorkflowId = "workflowId",
                            EventId = "eventId",
                            Role = "Student"
                        }
                    },
                    Executed = false,
                    Included = true,
                    Pending = false,
                    LockOwner = "Flow"
                }
            };
            _eventModelMock = new FakeDbSet<EventModel>(_eventModels.AsQueryable());
            _eventModelMock.EventStateMockSet.Setup(c => c.Remove(It.IsAny<EventModel>())).Verifiable();

            _historyModels = new List<ActionModel>();
            _historyMock = new FakeDbSet<ActionModel>(_historyModels.AsQueryable());

            _responseUris = new List<ResponseUri>();
            _responseMock = new FakeDbSet<ResponseUri>(_responseUris.AsQueryable());
            _responseMock.EventStateMockSet.Setup(c => c.RemoveRange(It.IsAny<IEnumerable<ResponseUri>>())).Verifiable();

            _conditionUris = new List<ConditionUri>();
            _conditionMock = new FakeDbSet<ConditionUri>(_conditionUris.AsQueryable());
            _conditionMock.EventStateMockSet.Setup(c => c.RemoveRange(It.IsAny<IEnumerable<ConditionUri>>())).Verifiable();

            _exclusionUris = new List<ExclusionUri>();
            _exclusionMock = new FakeDbSet<ExclusionUri>(_exclusionUris.AsQueryable());
            _exclusionMock.EventStateMockSet.Setup(c => c.RemoveRange(It.IsAny<IEnumerable<ExclusionUri>>())).Verifiable();

            _inclusionUris = new List<InclusionUri>();
            _inclusionMock = new FakeDbSet<InclusionUri>(_inclusionUris.AsQueryable());
            _inclusionMock.EventStateMockSet.Setup(c => c.RemoveRange(It.IsAny<IEnumerable<InclusionUri>>())).Verifiable();

            _contextMock.Setup(c => c.Events).Returns(_eventModelMock.Object);
            _contextMock.Setup(c => c.History).Returns(_historyMock.Object);
            _contextMock.Setup(c => c.Responses).Returns(_responseMock.Object);
            _contextMock.Setup(c => c.Conditions).Returns(_conditionMock.Object);
            _contextMock.Setup(c => c.Exclusions).Returns(_exclusionMock.Object);
            _contextMock.Setup(c => c.Inclusions).Returns(_inclusionMock.Object);

            _eventStorage = new EventStorage(_contextMock.Object);
        }

        #region Constructor and Dispose

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArgument()
        {
            // Act
            var storage = new EventStorage(null);

            // Assert
            Assert.Fail("This should not happen: {0}", storage.GetType());
        }

        [Test]
        public void Constructor_Ok()
        {
            // Act
            var storage = new EventStorage(_contextMock.Object);

            // Assert
            Assert.IsNotNull(storage);
        }

        [Test]
        public void Dispose_Ok()
        {
            // Act
            using (_eventStorage)
            {
                
            }

            // Assert
            _contextMock.Verify(c => c.Dispose(), Times.Once);
        }
        #endregion

        #region GetExecuted
        [Test]
        public async Task GetExecuted_Returns_True()
        {
            // Arrange
            _eventModels.First().Executed = true;

            // Act
            var result = await _eventStorage.GetExecuted("workflowId", "eventId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetExecuted_Returns_False()
        {
            // Act
            var result = await _eventStorage.GetExecuted("workflowId", "eventId");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetExecuted_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetExecuted("notWorkflowId", "notEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetExecuted_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetExecuted(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetPending
        [Test]
        public async Task GetPending_Returns_True()
        {
            // Arrange
            _eventModels.First().Pending = true;

            // Act
            var result = await _eventStorage.GetPending("workflowId", "eventId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetPending_Returns_False()
        {
            // Arrange

            // Act
            var result = await _eventStorage.GetPending("workflowId", "eventId");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetPending_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetPending("notWorkflowId", "notEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetPending_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetPending(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetIncluded
        [Test]
        public async Task GetIncluded_Returns_True()
        {
            // Arrange

            // Act
            var result = await _eventStorage.GetIncluded("workflowId", "eventId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetIncluded_Returns_False()
        {
            // Arrange
            _eventModels.First().Included = false;

            // Act
            var result = await _eventStorage.GetIncluded("workflowId", "eventId");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetIncluded_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetIncluded("notWorkflowId", "notEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetIncluded_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetIncluded(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region Exists

        [Test]
        public async Task Exists_Returns_True()
        {
            // Act
            var result = await _eventStorage.Exists("workflowId", "eventId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task Exists_Returns_False()
        {
            // Act
            var result = await _eventStorage.Exists("notWorkflowId", "notEventId");

            // Assert
            Assert.IsFalse(result);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void Exists_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.Exists(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetName
        [Test]
        public async Task GetName_Returns_Event()
        {
            // Act
            var result = await _eventStorage.GetName("workflowId", "eventId");

            // Assert
            Assert.AreEqual("Event", result);
        }

        [Test]
        public void GetName_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetName("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetName_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetName(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetRoles
        [Test]
        public async Task GetRoles_Returns_List()
        {
            // Act
            var result = (await _eventStorage.GetRoles("workflowId", "eventId")).ToList();

            // Assert
            Assert.IsNotEmpty(result);
            Assert.IsTrue(result.Contains("Student"));
        }

        [Test]
        public void GetRoles_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetRoles("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetRoles_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetRoles(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetHistoryForEvent
        [Test]
        public async Task GetHistoryForEvent_Returns_Histories()
        {
            // Arrange
            _historyModels.Add(new ActionModel
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });

            // Act
            var result = await _eventStorage.GetHistoryForEvent("workflowId", "eventId");

            // Assert
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void GetHistoryForEvent_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetHistoryForEvent("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetHistoryForEvent_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetHistoryForEvent(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetUri
        [Test]
        public async Task GetUri_Ok()
        {
            // Act
            var result = await _eventStorage.GetUri("workflowId", "eventId");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(new Uri("http://www.contoso.com"), result);
        }

        [Test]
        public void GetUri_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetUri("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetUri_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetUri(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region Reload
        [Test]
        public void Reload_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.Reload("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void Reload_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.Reload(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region DeleteEvent

        [Test]
        public async Task DeleteEvent_Ok()
        {
            // Arrange
            _conditionUris.Add(new ConditionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });
            _responseUris.Add(new ResponseUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });
            _inclusionUris.Add(new InclusionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });
            _exclusionUris.Add(new ExclusionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });

            // Act
            await _eventStorage.DeleteEvent("workflowId", "eventId");

            // Assert
            _responseMock.EventStateMockSet.Verify(t => t.RemoveRange(It.IsAny<IEnumerable<ResponseUri>>()), Times.Once);
            _conditionMock.EventStateMockSet.Verify(t => t.RemoveRange(It.IsAny<IEnumerable<ConditionUri>>()), Times.Once);
            _exclusionMock.EventStateMockSet.Verify(t => t.RemoveRange(It.IsAny<IEnumerable<ExclusionUri>>()), Times.Once);
            _inclusionMock.EventStateMockSet.Verify(t => t.RemoveRange(It.IsAny<IEnumerable<InclusionUri>>()), Times.Once);
            _eventModelMock.EventStateMockSet.Verify(t => t.Remove(It.IsAny<EventModel>()), Times.Once);
        }

        [Test]
        public void DeleteEvent_NotFound()
        {
            // Act
            var testDelegate =
                new TestDelegate(async () => await _eventStorage.DeleteEvent("notWorkflowId", "notEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, "eventId"),
         TestCase(null, null),
         TestCase("workflowId", null)]
        public void DeleteEvent_NullArgument(string workflowId, string eventId)
        {
            // Act
            var testDelegate =
                new TestDelegate(async () => await _eventStorage.DeleteEvent(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region SaveHistory

        [Test]
        public async Task SaveHistory_Ok()
        {
            // Act
            await _eventStorage.SaveHistory(new ActionModel
            {
                WorkflowId = "workflowId",
                EventId = "eventId"
            });

            // Assert
            _historyMock.EventStateMockSet.Verify(c => c.Add(It.IsAny<ActionModel>()), Times.Once);
        }

        [Test]
        public void SaveHistory_NullArgument()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SaveHistory(null));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [TestCase(null, "eventId"),
         TestCase(null, null),
         TestCase("workflowId", null)]
        public void SaveHistory_NullIds(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SaveHistory(new ActionModel
            {
                WorkflowId = workflowId,
                EventId = eventId
            }));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SaveHistory_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SaveHistory(new ActionModel
            {
                WorkflowId = "notWorkflowId",
                EventId = "notEventId"
            }));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
        #endregion

        #region GetLockDto
        [Test]
        public async Task GetLockDto_Locked()
        {
            // Act
            var result = await _eventStorage.GetLockDto("workflowId", "eventId");

            // Assert
            Assert.AreEqual("Flow", result.LockOwner);
            Assert.AreEqual("workflowId", result.WorkflowId);
            Assert.AreEqual("eventId", result.EventId);
        }

        [Test]
        public async Task GetLockDto_NotLocked()
        {
            // Arrange
            _eventModels.First().LockOwner = null;

            // Act
            var result = await _eventStorage.GetLockDto("workflowId", "eventId");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetLockDto_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetLockDto("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetLockDto_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetLockDto(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetRelations
        #region GetConditions
        [Test]
        public async Task GetConditions_Ok()
        {
            // Arrange
            _conditionUris.Add(new ConditionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId",
                UriString = "http://contoso.com/",
                ForeignEventId = "anotherEventId"
            });

            // Act
            var result = await _eventStorage.GetConditions("workflowId", "eventId");

            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(_conditionUris.Count, result.Count);
        }

        [Test]
        public void GetConditions_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetConditions("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetConditions_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetConditions(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetResponses
        [Test]
        public async Task GetResponses_Ok()
        {
            // Arrange
            _responseUris.Add(new ResponseUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId",
                UriString = "http://contoso.com/",
                ForeignEventId = "anotherEventId"
            });

            // Act
            var result = await _eventStorage.GetResponses("workflowId", "eventId");

            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(_responseUris.Count, result.Count);
        }

        [Test]
        public void GetResponses_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetResponses("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetResponses_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetResponses(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetInclusions
        [Test]
        public async Task GetInclusions_Ok()
        {
            // Arrange
            _inclusionUris.Add(new InclusionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId",
                UriString = "http://contoso.com/",
                ForeignEventId = "anotherEventId"
            });

            // Act
            var result = await _eventStorage.GetInclusions("workflowId", "eventId");

            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(_inclusionUris.Count, result.Count);
        }

        [Test]
        public void GetInclusions_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetInclusions("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetInclusions_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetInclusions(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region GetExclusions
        [Test]
        public async Task GetExclusions_Ok()
        {
            // Arrange
            _exclusionUris.Add(new ExclusionUri
            {
                WorkflowId = "workflowId",
                EventId = "eventId",
                UriString = "http://contoso.com/",
                ForeignEventId = "anotherEventId"
            });

            // Act
            var result = await _eventStorage.GetExclusions("workflowId", "eventId");

            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(_exclusionUris.Count, result.Count);
        }

        [Test]
        public void GetExclusions_Throws_NotFoundException()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetExclusions("wrongWorkflowId", "wrongEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void GetExclusions_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.GetExclusions(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion
        #endregion

        #region SetState
        #region SetExecuted
        [Test]
        public async Task SetExecuted_False()
        {
            // Arrange
            _eventModels.First().Executed = true;

            // Act
            await _eventStorage.SetExecuted("workflowId", "eventId", false);

            // Assert
            Assert.IsFalse(_eventModels.First().Executed);
        }

        [Test]
        public async Task SetExecuted_True()
        {
            // Arrange
            _eventModels.First().Executed = false;

            // Act
            await _eventStorage.SetExecuted("workflowId", "eventId", true);

            // Assert
            Assert.IsTrue(_eventModels.First().Executed);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void SetExecuted_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetExecuted(workflowId, eventId, true));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetExecuted_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetExecuted("notWorkflowId", "notEventId", true));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
        #endregion

        #region SetPending
        [Test]
        public async Task SetPending_False()
        {
            // Arrange
            _eventModels.First().Pending = true;

            // Act
            await _eventStorage.SetPending("workflowId", "eventId", false);

            // Assert
            Assert.IsFalse(_eventModels.First().Pending);
        }

        [Test]
        public async Task SetPending_True()
        {
            // Arrange
            _eventModels.First().Pending = false;

            // Act
            await _eventStorage.SetPending("workflowId", "eventId", true);

            // Assert
            Assert.IsTrue(_eventModels.First().Pending);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void SetPending_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetPending(workflowId, eventId, true));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetPending_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetPending("notWorkflowId", "notEventId", true));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
        #endregion

        #region SetIncluded
        [Test]
        public async Task SetIncluded_False()
        {
            // Arrange
            _eventModels.First().Included = true;

            // Act
            await _eventStorage.SetIncluded("workflowId", "eventId", false);

            // Assert
            Assert.IsFalse(_eventModels.First().Included);
        }

        [Test]
        public async Task SetIncluded_True()
        {
            // Arrange
            _eventModels.First().Included = false;

            // Act
            await _eventStorage.SetIncluded("workflowId", "eventId", true);

            // Assert
            Assert.IsTrue(_eventModels.First().Included);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void SetIncluded_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetIncluded(workflowId, eventId, true));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetIncluded_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetIncluded("notWorkflowId", "notEventId", true));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
        #endregion
        #endregion

        #region InitializeNewEvent

        [Test]
        public async Task InitializeNewEvent_Ok()
        {
            // Act
            await _eventStorage.InitializeNewEvent(new EventModel
            {
                WorkflowId = "WorkflowId",
                Id = "EventId"
            });

            // Assert
            _eventModelMock.EventStateMockSet.Verify(e => e.Add(It.IsAny<EventModel>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void InitializeNewEvent_NullArgument()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.InitializeNewEvent(null));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [TestCase(null, null),
         TestCase(null, "eventId"),
         TestCase("workflowId", null)]
        public void InitializeNewEvent_NullArguments(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.InitializeNewEvent(new EventModel
            {
                WorkflowId = workflowId,
                Id = eventId
            }));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void InitializeNewEvent_EventExists()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.InitializeNewEvent(new EventModel
            {
                WorkflowId = "workflowId",
                Id = "eventId"
            }));

            // Assert
            Assert.Throws<EventExistsException>(testDelegate);
        }
        #endregion

        #region ClearLock
        [TestCase(null, null)]
        [TestCase("workflowId", null)]
        [TestCase(null, "eventId")]
        public void ClearLock_NullArgument(string workflowId, string eventId)
        {
            // Arrange
            _eventModels.First().LockOwner = null;

            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.ClearLock(workflowId, eventId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void ClearLock_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.ClearLock("notWorkflowId", "notEventId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
        #endregion

        #region SetLock
        [Test]
        public async Task SetLock_Ok()
        {
            // Arrange
            _eventModels.First().LockOwner = null;

            // Act
            await _eventStorage.SetLock("workflowId", "eventId", "Flow");

            // Assert
            Assert.AreEqual("Flow", _eventModels.First().LockOwner);
        }

        [TestCase(null, null, null)]
        [TestCase("workflowId", null, null)]
        [TestCase("workflowId", "eventId", null)]
        [TestCase("workflowId", null, "flow")]
        [TestCase(null, "eventId", null)]
        [TestCase(null, "eventId", "flow")]
        [TestCase(null, null, "flow")]
        public void SetLock_NullArgument(string workflowId, string eventId, string lockOwner)
        {
            // Arrange
            _eventModels.First().LockOwner = null;

            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetLock(workflowId, eventId, lockOwner));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetLock_NotFound()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetLock("notWorkflowId", "notEventId", "flow"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [Test]
        public void SetLock_Locked()
        {
            // Arrange
            _eventModels.First().LockOwner = "notFlow";

            // Act
            var testDelegate = new TestDelegate(async () => await _eventStorage.SetLock("workflowId", "eventId", "flow"));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
        }
        #endregion
    }
}
