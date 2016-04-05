using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Client.ViewModels;
using Common.DTO.History;
using Common.DTO.Shared;
using Common.Exceptions;
using Moq;
using NUnit.Framework;

namespace Client.Tests.ViewModels
{
    [TestFixture]
    class HistoryListViewModelTests
    {
        private HistoryListViewModel _model;
        private Mock<IServerConnection> _serverConnectionMock;
        private Mock<IEventConnection> _eventConnectionMock;
        private List<ActionDto> _serverHistoryDtos, _eventHistoryDtos;
        private List<EventAddressDto> _eventAddressDtos;
        private readonly string[] _events = { "eventId", "elj", "hrioargn", "vifhd", "ragæoj", "grnjalgr" };

        [SetUp]
        public void SetUp()
        {
            _serverHistoryDtos = new List<ActionDto>();
            _eventHistoryDtos = new List<ActionDto>();
            _eventAddressDtos = new List<EventAddressDto>();

            _serverConnectionMock = new Mock<IServerConnection>(MockBehavior.Strict);
            _serverConnectionMock.Setup(connection => connection.GetEventsFromWorkflow(It.IsAny<string>()))
                .Returns((string workflowId) => Task.FromResult(_eventAddressDtos.Where(dto => dto.WorkflowId == workflowId)));

            _serverConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<string>()))
                .Returns((string workflowId) => Task.FromResult(_serverHistoryDtos.Where(dto => dto.WorkflowId == workflowId)));

            _eventConnectionMock = new Mock<IEventConnection>(MockBehavior.Strict);
            _eventConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((Uri uri, string workflowId, string eventId) => Task.FromResult(_eventHistoryDtos.Where(dto => dto.WorkflowId == workflowId && dto.EventId == eventId)));

            _model = new HistoryListViewModel("workflowId", _serverConnectionMock.Object, _eventConnectionMock.Object);
        }

        #region Constructors

        [Test]
        public void Constructor_NoArguments_Ok()
        {
            // Act
            var historyListViewModel = new HistoryListViewModel();

            // Assert
            Assert.IsNotNull(historyListViewModel);
        }

        [Test]
        public void Constructor_SingleArgument()
        {
            // Act
            // This call contacts a server (typically the Azure-one)
            var historyListViewModel = new HistoryListViewModel("workflowId");

            // Assert
            Assert.IsNotNull(historyListViewModel);
        }
        #endregion
        #region Databindings
        [TestCase("Great Success")]
        [TestCase("Too bad!")]
        [TestCase("Hrello")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("A really, really, really, really, really, really, really, really, really, really, really, really, really long workflow-id")]
        public void WorkflowId_PropertyChanged(string workflowId)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (sender, e) => { if (e.PropertyName == "WorkflowId") changed = true; };

            // Act
            _model.WorkflowId = workflowId;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(workflowId, _model.WorkflowId);
        }

        [TestCase("Great Success")]
        [TestCase("Too bad!")]
        [TestCase("Hrello")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("A really, really, really, really, really, really, really, really, really, really, really, really, really long status")]
        public void Status_PropertyChanged(string status)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (sender, e) => { if (e.PropertyName == "Status") changed = true; };

            // Act
            _model.Status = status;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(status, _model.Status);
        }
        #endregion

        #region Actions
        [Test]
        public void GetHistory_NoEventsOrHistory()
        {
            // Arrange

            // Act
            _model.GetHistory();

            // Assert
            Assert.IsEmpty(_model.HistoryViewModelList);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(200)]
        public void GetHistory_HistoriesFromServer(int amount)
        {
            // Arrange
            for (var i = 0; i < amount; i++)
            {
                _serverHistoryDtos.Add(new ActionDto
                {
                    WorkflowId = "workflowId",
                    EventId = "eventId",
                    CounterpartId = "counterpartId",
                    TimeStamp = 1
                });
            }

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual(amount, _model.HistoryViewModelList.Count);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(6, 200)]
        public void GetHistory_HistoriesFromEvents(int eventAmount, int historyAmount)
        {
            // Arrange
            for (var i = 0; i < eventAmount; i++)
            {
                var eventId = _events[i];
                _eventAddressDtos.Add(new EventAddressDto { WorkflowId = "workflowId", Id = eventId });

                for (var j = 0; j < historyAmount; j++)
                {
                    _eventHistoryDtos.Add(new ActionDto
                    {
                        WorkflowId = "workflowId",
                        EventId = "eventId",
                        CounterpartId = "counterpartId",
                        TimeStamp = 1
                    });
                }
            }


            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual(eventAmount * historyAmount, _model.HistoryViewModelList.Count);
        }

        [Test]
        public void GetHistory_ServerThrowsNotFoundException()
        {
            // Arrange
            _serverConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("Workflow wasn't found on server. Please refresh the workflow and try again.",
                _model.Status);
        }

        [Test]
        public void GetHistory_ServerThrowsHostNotFoundException()
        {
            // Arrange
            _serverConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<string>()))
                .ThrowsAsync(new HostNotFoundException(new HttpRequestException()));

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("The server could not be found. Please try again later or contact your Flow administrator", 
                _model.Status);
        }

        [Test]
        public void GetHistory_ServerThrowsException()
        {
            // Arrange
            _serverConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("An unexpected error has occured. Please try again later.",
                _model.Status);
        }

        [Test]
        public void GetHistory_EventThrowsNotFoundException()
        {
            // Arrange
            _eventAddressDtos.Add(new EventAddressDto { WorkflowId = "workflowId" });

            _eventConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("An event wasn't found. Please refresh the workflow and try again.",
                _model.Status);
        }

        [Test]
        public void GetHistory_EventThrowsHostNotFoundException()
        {
            // Arrange
            _eventAddressDtos.Add(new EventAddressDto{ WorkflowId = "workflowId"});

            _eventConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HostNotFoundException(new HttpRequestException()));

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("An event-server could not be found. Please try again later or contact your Flow administrator",
                _model.Status);
        }

        [Test]
        public void GetHistory_EventThrowsException()
        {
            // Arrange
            _eventAddressDtos.Add(new EventAddressDto { WorkflowId = "workflowId" });

            _eventConnectionMock.Setup(connection => connection.GetHistory(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            // Act
            _model.GetHistory();

            // Assert
            Assert.AreEqual("An unexpected error has occured. Please try again later.",
                _model.Status);
        }
        #endregion
    }
}
