using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Common.DTO.Event;
using Common.DTO.History;
using Common.Exceptions;
using Common.Tools;
using Moq;
using NUnit.Framework;

namespace Client.Tests.Connections
{
    [TestFixture]
    class EventConnectionTests
    {
        private EventConnection _connection;
        private Mock<HttpClientToolbox> _toolboxMock;
        private List<ActionDto> _historyDtos;

        [SetUp]
        public void SetUp()
        {
            _historyDtos = new List<ActionDto>();

            _toolboxMock = new Mock<HttpClientToolbox>(MockBehavior.Strict);
            _toolboxMock.Setup(t => t.ReadList<ActionDto>(It.IsAny<string>()))
                .ReturnsAsync(_historyDtos);

            _connection = new EventConnection(_toolboxMock.Object);
        }

        #region Constructor and dispose

        [Test]
        public void EventConnection_No_Arguments()
        {
            // Act
            var conn = new EventConnection();

            // Assert
            Assert.IsNotNull(conn);
        }

        [Test]
        public void Dispose_ok()
        {
            using (var conn = new EventConnection())
            {
                // Do nothing.
            }

            // If no errors happened, all is good.
        }
        #endregion

        #region GetState
        [Test]
        public async Task GetState_Ok()
        {
            // Arrange
            var dto = new EventStateDto();

            _toolboxMock.Setup(t => t.Read<EventStateDto>(It.IsAny<string>()))
                .ReturnsAsync(dto).Verifiable();

            // Act
            var result = await _connection.GetState(new Uri("http://uri.uri"), "workflow", "event");

            // Assert
            Assert.AreSame(dto, result);
            _toolboxMock.Verify(t => t.Read<EventStateDto>(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void GetState_HostNotFound()
        {
            // Arrange
            _toolboxMock.Setup(t => t.Read<EventStateDto>(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.GetState(new Uri("http://uri.uri"), "workflow", "event"));

            // Assert
            Assert.Throws<HostNotFoundException>(testDelegate);
            _toolboxMock.Verify(t => t.Read<EventStateDto>(It.IsAny<string>()), Times.Once);
        }

        [TestCase(typeof(NotFoundException)),
         TestCase(typeof(LockedException)),
         TestCase(typeof(NotExecutableException)),
         TestCase(typeof(UnauthorizedException))]
        public void GetState_ExceptionPassthrough(Type exceptionType)
        {
            // Arrange
            var exception = (Exception) exceptionType.GetConstructors().First().Invoke(null);

            _toolboxMock.Setup(t => t.Read<EventStateDto>(It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()));

            // Assert
            var thrown = Assert.Throws(exceptionType, testDelegate);
            Assert.AreSame(exception, thrown);
        }
        #endregion

        #region GetHistory
        [TestCase(0),
                 TestCase(1),
                 TestCase(500)]
        public async Task GetHistory_Ok(int amount)
        {
            // Arrange
            for (var i = 0; i < amount; i++)
            {
                _historyDtos.Add(new ActionDto
                {
                    WorkflowId = "workflowId",
                    EventId = "eventId",
                    TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    HttpRequestType = "GET"
                });
            }

            // Act
            var result = await _connection.GetHistory(new Uri("http://uri.uri"), "workflowId", "eventId");

            // Assert
            Assert.AreSame(_historyDtos, result);
        }

        [Test]
        public void GetHistory_HostNotFound()
        {
            // Arrange
            _toolboxMock.Setup(t => t.ReadList<ActionDto>(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.GetHistory(new Uri("http://uri.uri"), "workflow", "event"));

            // Assert
            Assert.Throws<HostNotFoundException>(testDelegate);
            _toolboxMock.Verify(t => t.ReadList<ActionDto>(It.IsAny<string>()), Times.Once);
        }

        [TestCase(typeof(NotFoundException)),
         TestCase(typeof(LockedException)),
         TestCase(typeof(NotExecutableException)),
         TestCase(typeof(UnauthorizedException))]
        public void GetHistory_ExceptionPassthrough(Type exceptionType)
        {
            // Arrange
            var exception = (Exception)exceptionType.GetConstructors().First().Invoke(null);

            _toolboxMock.Setup(t => t.ReadList<ActionDto>(It.IsAny<string>()))
                .ThrowsAsync(exception);

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.GetHistory(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()));

            // Assert
            var thrown = Assert.Throws(exceptionType, testDelegate);
            Assert.AreSame(exception, thrown);
        }
        #endregion

        #region ResetEvent
        [Test]
        public async Task ResetEvent_Ok()
        {
            // Arrange
            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            await _connection.ResetEvent(new Uri("http://uri.uri"), "workflowId", "eventId");

            // Assert
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public void ResetEvent_HostNotFound()
        {
            // Arrange
            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<object>()))
                .Throws(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.ResetEvent(new Uri("http://uri.uri"), "workflow", "event"));

            // Assert
            Assert.Throws<HostNotFoundException>(testDelegate);
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }

        [TestCase(typeof(NotFoundException)),
         TestCase(typeof(LockedException)),
         TestCase(typeof(NotExecutableException)),
         TestCase(typeof(UnauthorizedException))]
        public void ResetEvent_ExceptionPassthrough(Type exceptionType)
        {
            // Arrange
            var exception = (Exception)exceptionType.GetConstructors().First().Invoke(null);

            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<object>()))
                .Throws(exception);

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()));

            // Assert
            var thrown = Assert.Throws(exceptionType, testDelegate);
            Assert.AreSame(exception, thrown);
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
        #endregion

        #region Execute
        [Test]
        public async Task Execute_Ok()
        {
            // Arrange


            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            await _connection.Execute(new Uri("http://uri.uri"), "workflowId", "eventId", new List<string>());

            // Assert
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()), Times.Once);
        }

        [Test]
        public void Execute_HostNotFound()
        {
            // Arrange
            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(new HttpRequestException());

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.Execute(new Uri("http://uri.uri"), "workflow", "event", new List<string>()));

            // Assert
            Assert.Throws<HostNotFoundException>(testDelegate);
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()), Times.Once);
        }

        [TestCase(typeof(NotFoundException)),
         TestCase(typeof(LockedException)),
         TestCase(typeof(NotExecutableException)),
         TestCase(typeof(UnauthorizedException))]
        public void Execute_ExceptionPassthrough(Type exceptionType)
        {
            // Arrange
            var exception = (Exception)exceptionType.GetConstructors().First().Invoke(null);

            _toolboxMock.Setup(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()))
                .Throws(exception);

            // Act
            var testDelegate = new TestDelegate(async () => await _connection.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), new List<string>()));

            // Assert
            var thrown = Assert.Throws(exceptionType, testDelegate);
            Assert.AreSame(exception, thrown);
            _toolboxMock.Verify(t => t.Update(It.IsAny<string>(), It.IsAny<RoleDto>()), Times.Once);
        }
        #endregion
    }
}
