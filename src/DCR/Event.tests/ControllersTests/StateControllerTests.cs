using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Event;
using Common.DTO.Shared;
using Common.Exceptions;
using Event.Controllers;
using Event.Exceptions;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;
using Moq;
using NUnit.Framework;

namespace Event.Tests.ControllersTests
{
    [TestFixture]
    class StateControllerTests
    {
        private Mock<IStateLogic> _stateLogicMock;
        private Mock<IEventHistoryLogic> _historyLogicMock;
        private StateController _stateController;

        [SetUp]
        public void SetUp()
        {
            _stateLogicMock = new Mock<IStateLogic>(MockBehavior.Strict);
            _stateLogicMock.Setup(l => l.Dispose());

            _historyLogicMock = new Mock<IEventHistoryLogic>();

            _stateController = new StateController(_stateLogicMock.Object, _historyLogicMock.Object) { Request = new HttpRequestMessage() };
        }

        #region Constructor & Dispose

        [Test]
        public void StateControllerTests_Constructor_Runs_With_Argument()
        {
            new StateController(_stateLogicMock.Object, _historyLogicMock.Object);
        }

        [Test]
        public void Dispose_Test()
        {
            // Arrange
            _stateLogicMock.Setup(l => l.Dispose()).Verifiable();

            // Act
            using (_stateController)
            {
                // Do nothing
            }

            // Assert
            _stateLogicMock.Verify(l => l.Dispose(), Times.Once);
        }

        #endregion

        #region GetExecuted
        [Test]
        public async Task GetExecuted_Returns_true()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await _stateController.GetExecuted("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetExecuted_Returns_false()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _stateController.GetExecuted("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetExecuted_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetExecuted("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetExecuted_NotFound_Throws_404_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetExecuted("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void GetExecuted_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetExecuted("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetExecuted_Locked_Throws_409_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsExecuted(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetExecuted("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }
        #endregion

        #region GetIncluded
        [Test]
        public async Task GetIncluded_Returns_true()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await _stateController.GetIncluded("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetIncluded_Returns_false()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _stateController.GetIncluded("workflowId", "eventId", "senderId");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetIncluded_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetIncluded("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetIncluded_NotFound_Throws_404_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetIncluded("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void GetIncluded_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetIncluded("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetIncluded_Locked_Throws_409_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.IsIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetIncluded("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }
        #endregion

        #region GetState
        // Not all cases can occur in our code, but for the sake of testing:
        [TestCase(true, true, true, true)]
        [TestCase(true, true, true, false)]
        [TestCase(true, true, false, true)] // Executable cannot be true when included is false.
        [TestCase(true, true, false, false)] // Executable cannot be true when included is false.
        [TestCase(true, false, true, true)]
        [TestCase(true, false, true, false)]
        [TestCase(true, false, false, true)] // Executable cannot be true when included is false.
        [TestCase(true, false, false, false)] // Executable cannot be true when included is false.
        [TestCase(false, true, true, true)]
        [TestCase(false, true, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, true, true)]
        [TestCase(false, false, true, false)]
        [TestCase(false, false, false, true)]
        [TestCase(false, false, false, false)]
        public async Task GetState_Returns_Case(bool executable, bool executed, bool included, bool pending)
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.GetStateDto(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string workflowId, string eventId, string senderId) => Task.Run(() => new EventStateDto
                {
                    Id = eventId,
                    Name = eventId.ToUpper(),
                    Executable = executable,
                    Executed = executed,
                    Included = included,
                    Pending = pending
                }));

            // Act
            var result = await _stateController.GetState("workflowId", "eventId", "senderId");

            // Assert
            Assert.AreEqual("eventId", result.Id);
            Assert.AreEqual("EVENTID", result.Name);
            Assert.AreEqual(executable, result.Executable);
            Assert.AreEqual(executed, result.Executed);
            Assert.AreEqual(included, result.Included);
            Assert.AreEqual(pending, result.Pending);
        }

        [Test]
        public void GetState_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.GetStateDto(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetState("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetState_NotFound_Throws_404_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.GetStateDto(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetState("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void GetState_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.GetStateDto(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetState("workflowId", "eventId", "senderId"));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void GetState_Locked_Throws_409_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.GetStateDto(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.GetState("workflowId", "eventId", "senderId"));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);

            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }
        #endregion

        #region UpdateIncluded
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task UpdateIncluded_Was_x_SetTo_y(bool x, bool y)
        {
            // Arrange
            var logicIncluded = x;

            _stateLogicMock.Setup(sl => sl.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, string senderId, bool newIncluded) => Task.Run(() => logicIncluded = newIncluded));

            // Act
            // Update included:
            await _stateController.UpdateIncluded("workflowId", "eventId", y, new EventAddressDto { Id = "senderId" });

            // Assert
            Assert.AreEqual(y, logicIncluded);
        }

        [Test]
        public void UpdateIncluded_ModelState_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("eventAddressDto",
                "Could not be deserialised into an EventAddressDto");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, null));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdateIncluded_ModelState_BadRequest_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("eventAddressDto",
                "Could not be deserialised into an EventAddressDto");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, null));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
        }

        [Test]
        public void UpdateIncluded_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<NotFoundException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdateIncluded_NotFound_Throws_404_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<NotFoundException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void UpdateIncluded_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<LockedException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdateIncluded_Locked_Throws_409_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetIncluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<LockedException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdateIncluded("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }
        #endregion

        #region UpdatePending
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task UpdatePending_Was_x_SetTo_y(bool x, bool y)
        {
            // Arrange
            var logicPending = x;

            _stateLogicMock.Setup(sl => sl.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns((string workflowId, string eventId, string senderId, bool newPending) => Task.Run(() => logicPending = newPending));

            // Act
            // Update pending:
            await _stateController.UpdatePending("workflowId", "eventId", y, new EventAddressDto { Id = "senderId" });

            // Assert
            Assert.AreEqual(y, logicPending);
        }

        [Test]
        public void UpdatePending_ModelState_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("eventAddressDto",
                "Could not be deserialised into an EventAddressDto");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.UpdatePending("workflowId", "eventId", true, null));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdatePending_ModelState_BadRequest_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("eventAddressDto",
                "Could not be deserialised into an EventAddressDto");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.UpdatePending("workflowId", "eventId", true, null));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
        }

        [Test]
        public void UpdatePending_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<NotFoundException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdatePending("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdatePending_NotFound_Throws_404_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<NotFoundException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdatePending("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void UpdatePending_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<LockedException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdatePending("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void UpdatePending_Locked_Throws_409_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.SetPending(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Throws<LockedException>();

            // Act
            var testDelegate =
                new TestDelegate(
                    async () => await _stateController.UpdatePending("workflowId", "eventId", true, new EventAddressDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }
        #endregion

        #region Execute

        [Test]
        public void Execute_Invalid_ModelState_Throws_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("roleDto", "RoleDto is empty");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_Invalid_ModelState_Throws_BadRequest_HttpResponseException()
        {
            // Arrange
            _stateController.ModelState.AddModelError("roleDto", "RoleDto is empty");

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_Not_Authorized_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new UnauthorizedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_Not_Authorized_Throws_Unauthorized_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new UnauthorizedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.Unauthorized, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_Locked_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_Locked_Throws_Conflict_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new LockedException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_NotFound_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_NotFound_Throws_NotFound_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.NotFound, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_NotExecutable_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new NotExecutableException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_NotExecutable_Throws_PreconditionFailed_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new NotExecutableException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.PreconditionFailed, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_FailedToLockOtherEvent_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToLockOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_FailedToLockOtherEvent_Throws_Conflict_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToLockOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.Conflict, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_FailedToUnlockOtherEvent_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUnlockOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_FailedToUnlockOtherEvent_Throws_InternalServerError_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUnlockOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.InternalServerError, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_FailedToUpdateState_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUpdateStateException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_FailedToUpdateState_Throws_InternalServerError_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUpdateStateException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.InternalServerError, exception.Response.StatusCode);
        }

        [Test]
        public void Execute_FailedToUpdateStateAtOtherEvent_Throws_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUpdateStateAtOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Execute_FailedToUpdateStateAtOtherEvent_Throws_InternalServerError_HttpResponseException()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new FailedToUpdateStateAtOtherEventException());

            // Act
            var testDelegate = new TestDelegate(async () => await _stateController.Execute("workflowId", "eventId", new RoleDto()));

            // Assert
            var exception = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.InternalServerError, exception.Response.StatusCode);
        }

        [Test]
        public async Task Execute_Calls_Logic_Execute()
        {
            // Arrange
            _stateLogicMock.Setup(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            await _stateController.Execute("workflowId", "eventId", new RoleDto());

            // Assert
            _stateLogicMock.Verify(sl => sl.Execute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RoleDto>()), Times.Once());
        }
        #endregion
    }
}
