using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.History;
using Common.Exceptions;
using Moq;
using NUnit.Framework;
using Server.Controllers;
using Server.Interfaces;

namespace Server.Tests.ControllerTests
{
    [TestFixture]
    class HistoryControllerTests
    {
        private HistoryController _controller;
        private Mock<IWorkflowHistoryLogic> _historyLogicMock;
        private List<ActionDto> _historyDtos;

        [SetUp]
        public void SetUp()
        {
            _historyDtos = new List<ActionDto>();

            _historyLogicMock = new Mock<IWorkflowHistoryLogic>(MockBehavior.Strict);
            _historyLogicMock.Setup(hl => hl.SaveHistory(It.IsAny<ActionModel>()))
                .Returns(Task.Delay(0)).Verifiable();
            _historyLogicMock.Setup(hl => hl.Dispose()).Verifiable();

            _historyLogicMock.Setup(hl => hl.GetHistoryForWorkflow(It.IsAny<string>()))
                .ReturnsAsync(_historyDtos);

            _controller = new HistoryController(_historyLogicMock.Object) {Request = new HttpRequestMessage()};
        }

        #region Constructor and Dispose
        [Test]
        public void Constructor_No_Parameters()
        {
            // Act
            var controller = new HistoryController(_historyLogicMock.Object);

            // Assert
            Assert.IsNotNull(controller);
        }

        [Test]
        public void Dispose_Ok()
        {
            // Act
            using (_controller)
            {
                
            }

            // Assert
            _historyLogicMock.Verify(hl => hl.Dispose(), Times.Once);
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
                    CounterpartId = "counterpartId",
                    TimeStamp = 1
                });
            }

            // Act
            var result = await _controller.GetHistory("workflowId");

            // Assert
            Assert.AreSame(_historyDtos, result);
            _historyLogicMock.Verify(hl => hl.GetHistoryForWorkflow("workflowId"), Times.Once);
        }

        [Test]
        public void GetHistory_ArgumentNull()
        {
            // Arrange
            _historyLogicMock.Setup(hl => hl.GetHistoryForWorkflow(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentNullException());

            // Act
            var testDelegate = new TestDelegate(async () => await _controller.GetHistory("workflowId"));

            // Assert
            var responseException = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.BadRequest, responseException.Response.StatusCode);
        }

        [Test]
        public void GetHistory_NotFound()
        {
            // Arrange
            _historyLogicMock.Setup(hl => hl.GetHistoryForWorkflow(It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _controller.GetHistory("workflowId"));

            // Assert
            var responseException = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.NotFound, responseException.Response.StatusCode);
        }

        [TestCase(typeof(Exception)),
         TestCase(typeof(ArgumentException)),
         TestCase(typeof(DivideByZeroException))]
        public void GetHistory_UnknownError(Type exceptionType)
        {
            // Arrange
            _historyLogicMock.Setup(hl => hl.GetHistoryForWorkflow(It.IsAny<string>()))
                .ThrowsAsync((Exception) exceptionType.GetConstructors().First().Invoke(null));

            // Act
            var testDelegate = new TestDelegate(async () => await _controller.GetHistory("workflowId"));

            // Assert
            var responseException = Assert.Throws<HttpResponseException>(testDelegate);
            Assert.AreEqual(HttpStatusCode.InternalServerError, responseException.Response.StatusCode);
        }
        #endregion
    }
}
