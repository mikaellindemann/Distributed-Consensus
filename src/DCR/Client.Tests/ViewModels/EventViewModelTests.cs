using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
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
    class EventViewModelTests
    {
        private EventAddressDto _eventAddressDto;
        private EventViewModel _model;
        private Mock<IWorkflowViewModel> _workflowViewModelMock;
        private Mock<IEventConnection> _eventConnectionMock;

        [SetUp]
        public void SetUp()
        {
            _eventAddressDto = new EventAddressDto();

            _workflowViewModelMock = new Mock<IWorkflowViewModel>(MockBehavior.Strict);
            _workflowViewModelMock.SetupAllProperties();
            _workflowViewModelMock.Setup(wvm => wvm.RefreshEvents()).Verifiable();

            _eventConnectionMock = new Mock<IEventConnection>(MockBehavior.Strict);
            _eventConnectionMock.Setup(
                connection => connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(
                    (Uri uri, string workflowId, string eventId) => Task.FromResult(new EventStateDto {Id = eventId}));

            _model = new EventViewModel(_eventConnectionMock.Object, _eventAddressDto, _workflowViewModelMock.Object);
        }

        #region Constructor
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullArguments()
        {
            // Act
            var eventViewModel = new EventViewModel(null, null);

            // Assert
            Assert.IsNull(eventViewModel);
        }

        [Test]
        public void Constructor_Ok()
        {
            // Arrange

            // Act
            var eventViewModel = new EventViewModel(_eventAddressDto, _workflowViewModelMock.Object);

            // Assert
            Assert.IsNotNull(eventViewModel);
        }
        #endregion

        #region DataBindings

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Id")]
        public void Id_PropertyChanged(string id)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Id") changed = true; };

            // Act
            _model.Id = id;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(id, _model.Id);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Name")]
        public void Name_PropertyChanged(string name)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Name") changed = true; };

            // Act
            _model.Name = name;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(name, _model.Name);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Name")]
        public void Status_PropertyChanged(string status)
        {
            // Arrange

            // Act
            _model.Status = status;

            // Assert
            Assert.AreEqual(status, _model.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Pending_PropertyChanged(bool pending)
        {
            // Arrange
            var changed = false;
            var colorchanged = false;
            _model.PropertyChanged += (o, s) =>
            {
                if (s.PropertyName == "Pending") changed = true;
                if (s.PropertyName == "PendingColor") colorchanged = true;
            };

            // Act
            _model.Pending = pending;

            // Assert
            Assert.IsTrue(changed);
            Assert.IsTrue(colorchanged);
            Assert.AreEqual(pending, _model.Pending);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Executed_PropertyChanged(bool executed)
        {
            // Arrange
            var changed = false;
            var colorchanged = false;
            _model.PropertyChanged += (o, s) =>
            {
                if (s.PropertyName == "Executed") changed = true;
                if (s.PropertyName == "ExecutedColor") colorchanged = true;
            };

            // Act
            _model.Executed = executed;

            // Assert
            Assert.IsTrue(changed);
            Assert.IsTrue(colorchanged);
            Assert.AreEqual(executed, _model.Executed);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Included_PropertyChanged(bool included)
        {
            // Arrange
            var changed = false;
            var colorchanged = false;
            _model.PropertyChanged += (o, s) =>
            {
                if (s.PropertyName == "Included") changed = true;
                if (s.PropertyName == "IncludedColor") colorchanged = true;
            };

            // Act
            _model.Included = included;

            // Assert
            Assert.IsTrue(changed);
            Assert.IsTrue(colorchanged);
            Assert.AreEqual(included, _model.Included);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Executable_PropertyChanged(bool executable)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) =>
            {
                if (s.PropertyName == "Executable") changed = true;
            };

            // Act
            _model.Executable = executable;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(executable, _model.Executable);
        }

        [Test]
        public void ExecutedColor_WhiteWhenFalse()
        {
            // Arrange
            _model.Executed = false;

            // Act
            var brush = (SolidColorBrush)_model.ExecutedColor;

            // Assert
            Assert.AreEqual(Colors.White, brush.Color);
        }

        [Test]
        public void ExecutedColor_BitmapWhenTrue()
        {
            // Arrange
            _model.Executed = true;

            // Act
            var brush = _model.ExecutedColor as ImageBrush;

            // Assert
            Assert.NotNull(brush);
        }

        [Test]
        public void PendingColor_WhiteWhenFalse()
        {
            // Arrange
            _model.Pending = false;

            // Act
            var brush = (SolidColorBrush)_model.PendingColor;

            // Assert
            Assert.AreEqual(Colors.White, brush.Color);
        }

        [Test]
        public void PendingColor_BitmapWhenTrue()
        {
            // Arrange
            _model.Pending = true;

            // Act
            var brush = _model.PendingColor as ImageBrush;

            // Assert
            Assert.NotNull(brush);
        }

        [Test]
        public void IncludedColor_WhiteWhenFalse()
        {
            // Arrange
            _model.Included = false;

            // Act
            var brush = (SolidColorBrush)_model.IncludedColor;

            // Assert
            Assert.AreEqual(Colors.White, brush.Color);
        }

        [Test]
        public void IncludedColor_BlueWhenTrue()
        {
            // Arrange
            _model.Included = true;

            // Act
            var brush = _model.IncludedColor as SolidColorBrush;

            // Assert
            Assert.NotNull(brush);
            Assert.AreEqual(Colors.DeepSkyBlue, brush.Color);
        }

        [TestCase(null)]
        public void Uri_PropertyChanged(Uri uri)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Uri") changed = true; };

            // Act
            _model.Uri = uri;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(uri, _model.Uri);
        }

        [TestCase("http://www.contoso.com")]
        [TestCase("http://flowit.azurewebsites.net/workflows/healthcare")]
        public void Uri_PropertyChanged(string uri)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Uri") changed = true; };

            // Act
            _model.Uri = new Uri(uri);

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(new Uri(uri), _model.Uri);
        }
        #endregion

        #region Actions

        [Test]
        public async Task GetState_EventNotFound()
        {
            // Arrange
            _eventConnectionMock.Setup(
                connection => connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new NotFoundException());

            // Act
            await _model.GetState();

            // Assert
            Assert.AreEqual("The event could not be found. Please refresh the workflow", _model.Status);
        }

        [Test]
        public async Task GetState_EventHostNotFound()
        {
            // Arrange
            _eventConnectionMock.Setup(
                connection => connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HostNotFoundException(new Exception()));

            // Act
            await _model.GetState();

            // Assert
            Assert.AreEqual("The host of the event was not found. Please refresh the workflow. If the problem persists, contact you Flow administrator", _model.Status);
        }

        [Test]
        public async Task GetState_EventLocked()
        {
            // Arrange
            _eventConnectionMock.Setup(
                connection => connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LockedException());

            // Act
            await _model.GetState();

            // Assert
            Assert.AreEqual("The event is currently locked. Please try again later.", _model.Status);
        }

        [Test]
        public async Task GetState_UnknownError()
        {
            // Arrange
            _eventConnectionMock.Setup(
                connection => connection.GetState(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SomeMessage"));

            // Act
            await _model.GetState();

            // Assert
            Assert.AreEqual("SomeMessage", _model.Status);
        }

        [Test]
        public async Task GetState_Ok()
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "") changed = true; };

            // Act
            await _model.GetState();

            // Assert
            Assert.IsTrue(changed);
        }

        [Test]
        public void Execute_DisablesExecuteButtons()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0)).Verifiable();

            // Act
            _model.Execute();

            // Assert
            _workflowViewModelMock.Verify(wvm => wvm.DisableExecuteButtons(), Times.Once);
        }

        [Test]
        public void Execute_Ok()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Returns(Task.Delay(0));

            // Act
            _model.Execute();

            // Assert
            _workflowViewModelMock.Verify(wvm => wvm.RefreshEvents(), Times.Once);
        }

        [Test]
        public void Execute_EventNotFound()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws<NotFoundException>();

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("The event could not be found. Please refresh the workflow", _model.Status);
        }

        [Test]
        public void Execute_UserUnauthorized()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws<UnauthorizedException>();

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("You do not have the rights to execute this event", _model.Status);
        }

        [Test]
        public void Execute_EventLocked()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws<LockedException>();

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("The event is currently locked. Please try again later.", _model.Status);
        }

        [Test]
        public void Execute_EventNotExecutable()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws<NotExecutableException>();

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("The event is currently not executable. Please refresh the workflow", _model.Status);
        }

        [Test]
        public void Execute_EventHostNotFound()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws(new HostNotFoundException(new Exception()));

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("The host of the event was not found. Please refresh the workflow. If the problem persists, contact you Flow administrator", _model.Status);
        }

        [Test]
        public void Execute_EventUnknownError()
        {
            // Arrange
            _workflowViewModelMock.Setup(wvm => wvm.DisableExecuteButtons()).Returns(Task.Delay(0));
            _eventConnectionMock.Setup(conn => conn.Execute(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .Throws(new Exception("Message"));

            // Act
            _model.Execute();

            // Assert
            Assert.AreEqual("Message", _model.Status);
        }
        #endregion
    }
}
