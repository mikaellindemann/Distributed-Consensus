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
using Common.Exceptions;
using Moq;
using NUnit.Framework;

namespace Client.Tests.ViewModels
{
    [TestFixture]
    class WorkflowViewModelTests
    {
        private WorkflowViewModel _model;
        private Mock<IWorkflowListViewModel> _workflowListViewModelMock;
        private Mock<IEventConnection> _eventConnectionMock;
        private Mock<IServerConnection> _serverConnectionMock;
        private WorkflowDto _dto;
        private IList<string> _rolesList;
        private ObservableCollection<EventViewModel> _eventList;
        
        [SetUp]
        public void SetUp()
        {
            _workflowListViewModelMock = new Mock<IWorkflowListViewModel>(MockBehavior.Strict);
            _workflowListViewModelMock.SetupAllProperties();

            _eventConnectionMock = new Mock<IEventConnection>(MockBehavior.Strict);

            _serverConnectionMock = new Mock<IServerConnection>(MockBehavior.Strict);

            _eventList = new ObservableCollection<EventViewModel>();

            _rolesList = new List<string>();

            _dto = new WorkflowDto {Id = "WorkflowId", Name = "WorkflowName"};

            _model = new WorkflowViewModel(_workflowListViewModelMock.Object, _dto, _rolesList, _eventConnectionMock.Object, _serverConnectionMock.Object, _eventList);
        }

        #region Constructors

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void Test_ConstructorWithNullArguments()
        {
            //Act
            var workflowViewModel = new WorkflowViewModel(null, null, null);

            //Assert
            Assert.IsNull(workflowViewModel);
        }

        [Test]
        public void Test_ConstructorWithLegalArguments()
        {
            //Arrange
            var workflowListViewModel = new WorkflowListViewModel();
            var workflowDto = new WorkflowDto();

            //Act
            var workflowViewModel = new WorkflowViewModel(workflowListViewModel, workflowDto, new List<string>());

            //Assert
            Assert.IsNotNull(workflowViewModel);

        }
        #endregion

        #region Databindings

        [Test]
        public void Test_SetNamePropertyChanged()
        {
            //Arrange
            var b = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Name") b = true; };

            //Act
            _model.Name = "Hola";
               
            //Assert
            Assert.IsTrue(b);
        }
        [Test]
        public void Test_SetNamePropertyChangesWorkflowName()
        {
            //Arrange
            

            //Act
            _model.Name = "Hola";

            //Assert
            Assert.AreEqual("Hola", _model.Name);
            Assert.AreEqual("Hola", _dto.Name);
        }

        [Test]
        public void Test_SetStatusPropertyChangesWorkflowName()
        {
            //Arrange


            //Act
            _model.Status = "Hola";

            //Assert
            Assert.AreEqual("Hola", _model.Status);
        }

        [TestCase(null)]
        [TestCase("WorkflowId")]
        [TestCase("Random")]
        [TestCase("A very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very long id")]
        public void WorkflowId_matches_Dto(string workflowId)
        {
            // Arrange
            _dto.Id = workflowId;

            // Act
            var result = _model.WorkflowId;

            // Assert
            Assert.AreEqual(workflowId, result);
        }

        [Test]
        public void Test_SetSelectedEventPropertyChanged()
        {
            //Arrange
            var b = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "SelectedEventViewModel") b = true; };

            //Act
            _model.SelectedEventViewModel = new EventViewModel(null, null, null);

            //Assert
            Assert.IsTrue(b);
        }

        [Test]
        public void Test_SetSelectedEventPropertyChangesWorkflowName()
        {
            //Arrange
            var viewmodel = new EventViewModel(null, null, null);

            //Act
            _model.SelectedEventViewModel = viewmodel;

            //Assert
            Assert.AreSame(viewmodel, _model.SelectedEventViewModel);
        }
        #endregion

        #region Actions

        [Test]
        public void RefreshEvents_Calls_Events_in_List()
        {
            // Arrange
            var addressDto = new EventAddressDto();
            _eventList.Add(new EventViewModel(_eventConnectionMock.Object, addressDto, _model));

            _eventConnectionMock.Setup(ec => ec.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new EventStateDto())).Verifiable();

            // Act
            _model.RefreshEvents();

            // Assert
            _eventConnectionMock.Verify(ec => ec.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task DisableExecuteButtons_AreCalled()
        {
            // Arrange
            for (var i = 0; i < 10; i++)
            {
                _eventList.Add(new EventViewModel(_eventConnectionMock.Object, new EventAddressDto(), _model) { Executable = true});
            }

            // Act
            await _model.DisableExecuteButtons();

            // Assert
            Assert.IsTrue(_eventList.All(e => e.Executable == false));
        }

        [Test]
        public void ResetWorkflow_Ok()
        {
            // Arrange
            var eventAddressList = new List<EventAddressDto> {new EventAddressDto()};

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.AtLeastOnce);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(eventAddressList.Count));
        }

        [Test]
        public void ResetWorkflow_SecondCallReturnsImmediately()
        {
            // Arrange
            var eventAddressList = new List<EventAddressDto> { new EventAddressDto() };

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(200)).Verifiable();

            
            // Act
            // One of these calls will hit the boolean-switch which tells the call not to continue.
            Task.Run(() => _model.ResetWorkflow());
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.AtMost(2));
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(eventAddressList.Count));
        }

        [Test]
        public void ResetWorkflow_ServerThrowsNotFound()
        {
            // Arrange
            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException()).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.AtLeastOnce);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.AreEqual("The workflow wasn't found. Please refresh the list of workflows.", _model.Status);
        }

        [Test]
        public void ResetWorkflow_ServerThrowsHostNotFound()
        {
            // Arrange
            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ThrowsAsync(new HostNotFoundException(new Exception())).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.AtLeastOnce);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.AreEqual("The server is currently unavailable. Please try again later.", _model.Status);
        }

        [Test]
        public void ResetWorkflow_ServerThrowsUnknownException()
        {
            // Arrange
            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ThrowsAsync(new Exception()).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.Delay(0)).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.AtLeastOnce);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.AreEqual("An unexpected error has occurred. Please refresh or try again later.", _model.Status);
        }

        [Test]
        public void ResetWorkflow_EventThrowsNotFoundException()
        {
            // Arrange
            var eventAddressList = new List<EventAddressDto> { new EventAddressDto() };

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new NotFoundException()).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.Once);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual("One of the events wasn't found. Please refresh the list of workflows.", _model.Status);
        }

        [Test]
        public void ResetWorkflow_EventThrowsHostNotFoundException()
        {
            // Arrange
            var eventAddressList = new List<EventAddressDto> { new EventAddressDto() };

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new HostNotFoundException(new Exception())).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.Once);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual("An event-server is currently unavailable. Please try again later.", _model.Status);
        }

        [Test]
        public void ResetWorkflow_EventThrowsUnknownException()
        {
            // Arrange
            var eventAddressList = new List<EventAddressDto> {new EventAddressDto()};

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            _eventConnectionMock.Setup(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception()).Verifiable();

            // Act
            _model.ResetWorkflow();

            // Assert
            _serverConnectionMock.Verify(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()), Times.Once);
            _eventConnectionMock.Verify(conn => conn.ResetEvent(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual("An unexpected error has occurred. Please refresh or try again later.", _model.Status);
        }

        [Test]
        public void GetEvents_Ok()
        {
            // Arrange
            _rolesList.Add("Admin");

            var eventAddressList = new List<EventAddressDto>
            {
                new EventAddressDto
                {
                    Id = "eventId",
                    Roles = new List<string>
                    {
                        "Admin"
                    }
                }
            };

            _serverConnectionMock.Setup(conn => conn.GetEventsFromWorkflow(It.IsAny<string>()))
                .ReturnsAsync(eventAddressList).Verifiable();

            // Act
            _model.GetEvents();
            
            // Assert
            Assert.AreEqual(1, _model.EventList.Count);
            Assert.AreEqual("eventId", _model.SelectedEventViewModel.Id);
        }
        #endregion
    }
}
