using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Exceptions;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;
using Event.Logic;
using Event.Models;
using Moq;
using NUnit.Framework;

namespace Event.Tests.LogicTests
{
    [TestFixture]
    class StateLogicTests
    {
        private StateLogic _stateLogic;

        private Mock<IEventStorage> _eventStorageMock;
        private Mock<ILockingLogic> _lockingLogicMock;
        private Mock<IAuthLogic> _authLogicMock;
        private Mock<IEventFromEvent> _eventCommunicatorMock;
        private HashSet<RelationToOtherEventModel> _conditions, _responses, _inclusions, _exclusions;

        [SetUp]
        public void SetUp()
        {
            _conditions = new HashSet<RelationToOtherEventModel>();
            _responses = new HashSet<RelationToOtherEventModel>();
            _inclusions = new HashSet<RelationToOtherEventModel>();
            _exclusions = new HashSet<RelationToOtherEventModel>();

            _eventStorageMock = new Mock<IEventStorage>(MockBehavior.Strict);

            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetIncluded(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetConditions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_conditions);
            _eventStorageMock.Setup(s => s.GetResponses(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_responses);
            _eventStorageMock.Setup(s => s.GetInclusions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_inclusions);
            _eventStorageMock.Setup(s => s.GetExclusions(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_exclusions);
            _eventStorageMock.Setup(s => s.GetUri(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new Uri("http://www.contoso.com"));
            _eventStorageMock.Setup(s => s.Reload(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Delay(0));
            _eventStorageMock.Setup(s => s.SetExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.Delay(0)).Verifiable();
            _eventStorageMock.Setup(s => s.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.Delay(0)).Verifiable();

            _lockingLogicMock = new Mock<ILockingLogic>(MockBehavior.Strict);

            // Make the Event unlocked unless other is specified.
            _lockingLogicMock.Setup(l => l.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _lockingLogicMock.Setup(l => l.LockAllForExecute(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _lockingLogicMock.Setup(l => l.UnlockAllForExecute(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _authLogicMock = new Mock<IAuthLogic>(MockBehavior.Strict);

            // Make the caller authorized
            _authLogicMock.Setup(a => a.IsAuthorized(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(true);

            _eventCommunicatorMock = new Mock<IEventFromEvent>(MockBehavior.Strict);
            _eventCommunicatorMock.Setup(ec => ec.SendPending(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Delay(0));
            _eventCommunicatorMock.Setup(ec => ec.SendExcluded(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Delay(0));
            _eventCommunicatorMock.Setup(ec => ec.SendIncluded(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.Delay(0));
            _eventCommunicatorMock.Setup(ec => ec.IsExecuted(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _eventCommunicatorMock.Setup(ec => ec.IsIncluded(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _stateLogic = new StateLogic(_eventStorageMock.Object, _lockingLogicMock.Object, _authLogicMock.Object, _eventCommunicatorMock.Object);
        }

        #region Constructors and Dispose

        [Test]
        public void Constructor_NoArguments()
        {
            // Act
            var logic = new StateLogic();

            // Assert
            Assert.IsNotNull(logic);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArguments()
        {
            // Act
            var logic = new StateLogic(null, null, null, null);

            // Assert
            Assert.Fail("Should not be run: {0}", logic.GetType());
        }

        [Test]
        public void Constructor_ValidArguments()
        {
            // Act
            var logic = new StateLogic(_eventStorageMock.Object, _lockingLogicMock.Object, _authLogicMock.Object, _eventCommunicatorMock.Object);

            // Assert
            Assert.IsNotNull(logic);
        }

        [Test]
        public void Dispose_Ok()
        {
            // Arrange
            _lockingLogicMock.Setup(ll => ll.Dispose()).Verifiable();
            _authLogicMock.Setup(al => al.Dispose()).Verifiable();
            _eventStorageMock.Setup(es => es.Dispose()).Verifiable();
            _eventCommunicatorMock.Setup(ec => ec.Dispose()).Verifiable();

            // Act
            using (_stateLogic)
            {

            }

            // Assert
            _lockingLogicMock.Verify(ll => ll.Dispose(), Times.Once);
            _authLogicMock.Verify(al => al.Dispose(), Times.Once);
            _eventStorageMock.Verify(es => es.Dispose(), Times.Once);
            _eventCommunicatorMock.Verify(ec => ec.Dispose(), Times.Once);
        }
        #endregion

        #region IsExecuted
        [Test]
        public async Task IsExecuted_ReturnsTrue()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(true);

            // Assert
            Assert.IsTrue(await _stateLogic.IsExecuted("workflowId", "eventId", "senderId"));
        }

        [Test]
        public async Task IsExecuted_ReturnsFalse()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);

            // Assert
            Assert.IsFalse(await _stateLogic.IsExecuted("workflowId", "eventId", "senderId"));
        }

        [Test]
        public void IsExecuted_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.IsExecuted("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void IsExecuted_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.IsExecuted(workflowId, eventId, senderId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task IsExecuted_WaitForTurn()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetPending("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetName("workflowId", "eventId")).ReturnsAsync("Event Name");

            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _lockingLogicMock.Setup(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()))
                .Returns(Task.Delay(5)).Verifiable();

            // Act
            await _stateLogic.IsExecuted("workflowId", "eventId", "senderId");

            // Assert
            _lockingLogicMock.Verify(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()), Times.Once);
        }
        #endregion

        #region IsIncluded
        [Test]
        public async Task IsIncluded_ReturnsTrue()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(true);

            // Assert
            Assert.IsTrue(await _stateLogic.IsIncluded("workflowId", "eventId", "senderId"));
        }

        [Test]
        public async Task IsIncluded_ReturnsFalse()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(false);

            // Assert
            Assert.IsFalse(await _stateLogic.IsIncluded("workflowId", "eventId", "senderId"));
        }

        [Test]
        public void IsIncluded_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.IsIncluded("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void IsIncluded_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.IsIncluded(workflowId, eventId, senderId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task IsIncluded_WaitForTurn()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetPending("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetName("workflowId", "eventId")).ReturnsAsync("Event Name");

            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _lockingLogicMock.Setup(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()))
                .Returns(Task.Delay(5)).Verifiable();

            // Act
            await _stateLogic.IsIncluded("workflowId", "eventId", "senderId");

            // Assert
            _lockingLogicMock.Verify(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()), Times.Once);
        }
        #endregion

        #region GetStateDto
        [Test]
        public async Task GetStateDto_Returns_Executable_State()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetPending("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetName("workflowId", "eventId")).ReturnsAsync("Event Name");

            // Act
            var result = await _stateLogic.GetStateDto("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsFalse(result.Executed);
            Assert.IsTrue(result.Included);
            Assert.IsFalse(result.Pending);
            Assert.IsTrue(result.Executable);
            Assert.AreEqual("Event Name", result.Name);
            Assert.AreEqual("eventId", result.Id);
        }

        [Test]
        public async Task GetStateDto_Returns_NonExecutable_State()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetPending("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetName("workflowId", "eventId")).ReturnsAsync("Event Name");

            // Act
            var result = await _stateLogic.GetStateDto("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsFalse(result.Executed);
            Assert.IsFalse(result.Included);
            Assert.IsFalse(result.Pending);
            Assert.IsFalse(result.Executable);
            Assert.AreEqual("Event Name", result.Name);
            Assert.AreEqual("eventId", result.Id);
        }

        [Test]
        public void GetStateDto_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.GetStateDto("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void GetStateDto_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.GetStateDto(workflowId, eventId, senderId));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task GetStateDto_WaitForTurn()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.GetExecuted("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetIncluded("workflowId", "eventId")).ReturnsAsync(true);
            _eventStorageMock.Setup(s => s.GetPending("workflowId", "eventId")).ReturnsAsync(false);
            _eventStorageMock.Setup(s => s.GetName("workflowId", "eventId")).ReturnsAsync("Event Name");

            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _lockingLogicMock.Setup(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()))
                .Returns(Task.Delay(5)).Verifiable();

            // Act
            await _stateLogic.GetStateDto("workflowId", "eventId", "senderId");

            // Assert
            _lockingLogicMock.Verify(ll => ll.WaitForMyTurn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LockDto>()), Times.Once);
        }
        #endregion

        #region Execute
        [Test]
        public void Execute_Throws_NotAuthorizedException_When_Role_Is_Wrong()
        {
            // Arrange
            // Make the role wrong:
            _authLogicMock.Setup(a => a.IsAuthorized(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "WrongRole" } }));

            // Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_NotExecutableException_When_Not_Executable()
        {
            // Arrange
            // Make event not executable:
            _eventStorageMock.Setup(s => s.GetIncluded(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            // But allow the role:
            _authLogicMock.Setup(a => a.IsAuthorized(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>())).ReturnsAsync(true);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<NotExecutableException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToLockOtherEventsException_When_Other_Events_Cannot_Be_Locked()
        {
            // Arrange
            _lockingLogicMock.Setup(l => l.LockAllForExecute(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "Roles" } }));

            // Assert
            Assert.Throws<FailedToLockOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUnlockOtherEventsException_When_Other_Events_Cannot_Be_Locked()
        {
            // Arrange
            _lockingLogicMock.Setup(l => l.UnlockAllForExecute(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "Roles" } }));

            // Assert
            Assert.Throws<FailedToUnlockOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUpdateStateException_When_Executed_Cannot_Be_Set()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.SetExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<Exception>();

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<FailedToUpdateStateAtOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUpdateStateException_When_Pending_Cannot_Be_Set()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Throws<Exception>();

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<FailedToUpdateStateAtOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUpdateStateAtOtherEventException_When_Response_Cannot_Be_Found()
        {
            // Arrange
            _responses.Add(new RelationToOtherEventModel
                    {
                        WorkflowId = "NonExistentWorkflowId",
                        EventId = "NonExistentEventId",
                        Uri = new Uri("http://localhost:65443/")
                    });

            _eventCommunicatorMock.Setup(
                c => c.SendPending(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<HttpRequestException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () =>
                        await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<FailedToUpdateStateAtOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUpdateStateAtOtherEventException_When_Inclusion_Cannot_Be_Found()
        {
            // Arrange
            _inclusions.Add(new RelationToOtherEventModel
                    {
                        WorkflowId = "NonExistentWorkflowId",
                        EventId = "NonExistentEventId",
                        Uri = new Uri("http://localhost:65443/")
                    });

            _eventCommunicatorMock.Setup(
                c => c.SendIncluded(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<HttpRequestException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () =>
                        await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<FailedToUpdateStateAtOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_FailedToUpdateStateAtOtherEventException_When_Exclusion_Cannot_Be_Found()
        {
            // Arrange
            _exclusions.Add(new RelationToOtherEventModel
            {
                WorkflowId = "NonExistentWorkflowId",
                EventId = "NonExistentEventId",
                Uri = new Uri("http://localhost:65443/")
            });

            _eventCommunicatorMock.Setup(
                c => c.SendExcluded(It.IsAny<Uri>(), It.IsAny<EventAddressDto>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<HttpRequestException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () =>
                        await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<FailedToUpdateStateAtOtherEventException>(testDelegate);
        }

        [Test]
        public void Execute_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void Execute_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Arrange
            RoleDto roles = null;
            if (senderId != null)
            {
                roles = new RoleDto();
            }

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute(workflowId, eventId, roles));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task Execute_No_Relations()
        {
            // Act
            await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "Roles" } });

            // Assert
            _eventStorageMock.Verify(s => s.SetExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            _eventStorageMock.Verify(s => s.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public async Task Execute_AllRelations()
        {
            // Arrange
            var relationModel = new RelationToOtherEventModel
            {
                WorkflowId = "workflowId",
                EventId = "eventId",
                Uri = new Uri("http://www.contoso.com")
            };

            _conditions.Add(relationModel);
            _responses.Add(relationModel);
            _inclusions.Add(relationModel);
            _exclusions.Add(relationModel);

            // Act
            await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "Roles" } });

            // Assert
            _eventStorageMock.Verify(s => s.SetExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            _eventStorageMock.Verify(s => s.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void Execute_Throws_LockedException()
        {
            // Arrange
            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.Execute("workflowId", "eventId", new RoleDto { Roles = new List<string> { "RightRole" } }));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
        }
        #endregion

        #region SetPending

        [Test]
        public async Task SetPending_True()
        {
            // Arrange
            var pending = false;
            _eventStorageMock.Setup(es => es.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, bool b) => Task.Run(() => pending = b));

            // Act
            await _stateLogic.SetPending("workflowId", "eventId", "senderId", true);

            // Assert
            Assert.IsTrue(pending);
        }

        [Test]
        public async Task SetPending_False()
        {
            // Arrange
            var pending = true;
            _eventStorageMock.Setup(es => es.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, bool b) => Task.Run(() => pending = b));

            // Act
            await _stateLogic.SetPending("workflowId", "eventId", "senderId", false);

            // Assert
            Assert.IsFalse(pending);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void SetPending_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetPending(workflowId, eventId, senderId, true));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetPending_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetPending("workflowId", "eventId", "senderId", true));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [Test]
        public void SetPending_Throws_LockedException()
        {
            // Arrange
            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetPending("workflowId", "eventId", "senderId", true));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
        }
        #endregion

        #region SetIncluded
        [Test]
        public async Task SetIncluded_True()
        {
            // Arrange
            var included = false;
            _eventStorageMock.Setup(es => es.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, bool b) => Task.Run(() => included = b));

            // Act
            await _stateLogic.SetIncluded("workflowId", "eventId", "senderId", true);

            // Assert
            Assert.IsTrue(included);
        }

        [Test]
        public async Task SetIncluded_False()
        {
            // Arrange
            var included = true;
            _eventStorageMock.Setup(es => es.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, bool b) => Task.Run(() => included = b));

            // Act
            await _stateLogic.SetIncluded("workflowId", "eventId", "senderId", false);

            // Assert
            Assert.IsFalse(included);
        }

        [TestCase(null, null, null),
         TestCase("workflowId", null, null),
         TestCase("workflowId", "eventId", null),
         TestCase("workflowId", null, "senderId"),
         TestCase(null, "eventId", null),
         TestCase(null, "eventId", "senderId"),
         TestCase(null, null, "senderId")]
        public void SetIncluded_Throws_ArgumentNullException(string workflowId, string eventId, string senderId)
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetIncluded(workflowId, eventId, senderId, true));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SetIncluded_Throws_NotFoundException()
        {
            // Arrange
            _eventStorageMock.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetIncluded("workflowId", "eventId", "senderId", true));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [Test]
        public void SetIncluded_Throws_LockedException()
        {
            // Arrange
            _lockingLogicMock.Setup(
                ll => ll.IsAllowedToOperate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var testDelegate = new TestDelegate(async () => await _stateLogic.SetIncluded("workflowId", "eventId", "senderId", true));

            // Assert
            Assert.Throws<LockedException>(testDelegate);
        }
        #endregion
    }
}
