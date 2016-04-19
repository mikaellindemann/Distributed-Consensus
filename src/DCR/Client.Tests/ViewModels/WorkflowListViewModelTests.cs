using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Client.ViewModels;
using Common.DTO.Event;
using Common.DTO.Shared;
using Moq;
using NUnit.Framework;

namespace Client.Tests.ViewModels
{
    [TestFixture]
    class WorkflowListViewModelTests
    {
        private WorkflowListViewModel _model;
        private Mock<IServerConnection> _serverConnectionMock;
        private Mock<IEventConnection> _eventConnectionMock;
        private Dictionary<string, ICollection<string>> _rolesForWorkflows;
        private ObservableCollection<WorkflowViewModel> _workflowViewModels;
        private ObservableCollection<EventViewModel> _eventViewModels;
        private List<WorkflowDto> _workflowDtos;

        [SetUp]
        public void SetUp()
        {
            _workflowDtos = new List<WorkflowDto> { new WorkflowDto{ Id = "WorkflowId"} };

            _serverConnectionMock = new Mock<IServerConnection>(MockBehavior.Strict);
            _serverConnectionMock.Setup(s => s.GetWorkflows())
                .ReturnsAsync(_workflowDtos);

            _rolesForWorkflows = new Dictionary<string, ICollection<string>>
            {
                {"WorkflowId", new List<string>{ "Admin" }}
            };

            _workflowViewModels = new ObservableCollection<WorkflowViewModel>();

            _model = new WorkflowListViewModel(_serverConnectionMock.Object, _rolesForWorkflows, _workflowViewModels);

            _eventConnectionMock = new Mock<IEventConnection>(MockBehavior.Strict);

            _eventViewModels = new ObservableCollection<EventViewModel>();
        }

        #region Constructors

        [Test]
        public void WorkflowListViewModel_WithArguments_Ok()
        {
            // Act
            // This test does not work properly, because it uses a default connection to server.
            // The exception is catched in the code, and therefore the test does not fail.
            var model = new WorkflowListViewModel(_rolesForWorkflows);

            // Assert
            Assert.IsNotNull(model);
        }

        [Test]
        public void WorkflowListViewModel_WithArguments_NullArgumentException()
        {
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => new WorkflowListViewModel(null));
        }

        [Test]
        public void WorkflowListViewModel_NoArguments()
        {
            // Act
            var model = new WorkflowListViewModel();

            // Assert
            Assert.IsNotNull(model);
        }
        #endregion

        #region Databindings
        [TestCase(""),
         TestCase(null),
         TestCase("Rubbish"),
         TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Status")]
        public void Status_PropertyChanged(string status)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Status") changed = true; };

            // Act
            _model.Status = status;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(status, _model.Status);
        }

        [Test]
        public void RolesForWorkflows_Get()
        {
            // Act
            var result = _model.RolesForWorkflows;

            // Assert
            Assert.AreSame(_rolesForWorkflows, result);
        }
        #endregion

        #region Actions

        [Test]
        public void GetEventsOnWorkflow_NoWorkflows()
        {
            // Arrange
            _model.SelectedWorkflowViewModel = null;

            _eventConnectionMock.Setup(e => e.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new EventStateDto())).Verifiable();

            // Act
            _model.GetEventsOnWorkflow();

            // Assert
            _eventConnectionMock.Verify(e => e.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GetEventsOnWorkflow_WithWorkflow()
        {
            // Arrange
            _workflowViewModels.Add(new WorkflowViewModel(_model, new WorkflowDto(), new List<string> { "Admin" }, _eventConnectionMock.Object, _serverConnectionMock.Object, _eventViewModels));

            _serverConnectionMock.Setup(s => s.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(new List<ServerEventDto> { new ServerEventDto { Roles = new List<string> { "Admin" } } }).Verifiable();

            _model.SelectedWorkflowViewModel = _workflowViewModels.First();

            _eventViewModels.Add(new EventViewModel(_eventConnectionMock.Object, new ServerEventDto(), _model.SelectedWorkflowViewModel));

            // Act
            _model.GetEventsOnWorkflow();

            // Assert
            _serverConnectionMock.Verify(s => s.GetEventsFromWorkflow(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void GetWorkflows_Ok()
        {
            // Arrange

            // Act
            _model.GetWorkflows();

            // Assert
            Assert.AreEqual(1, _model.WorkflowList.Count);
            Assert.IsNotNull(_model.SelectedWorkflowViewModel);
        }

        [Test]
        public void GetWorkflows_Miss()
        {
            // Arrange
            _workflowDtos.Clear();
            _workflowDtos.Add(new WorkflowDto { Id = "WrongId" });

            // Act
            _model.GetWorkflows();

            // Assert
            Assert.AreEqual(0, _model.WorkflowList.Count);
            Assert.IsNull(_model.SelectedWorkflowViewModel);
        }

        [Test]
        public void GetWorkflows_HostNotFound()
        {
            // Arrange
            _serverConnectionMock.Setup(s => s.GetWorkflows()).ThrowsAsync(new HostNotFoundException(new Exception()));

            // Act
            _model.GetWorkflows();

            // Assert
            Assert.AreEqual("The host of the server was not found. If the problem persists, contact you Flow administrator", _model.Status);
        }

        [Test]
        public void GetWorkflows_UnknownError()
        {
            // Arrange
            _serverConnectionMock.Setup(s => s.GetWorkflows()).ThrowsAsync(new Exception("Messagesss"));

            // Act
            _model.GetWorkflows();

            // Assert
            Assert.AreEqual("Messagesss", _model.Status);
        }
        #endregion
    }
}
