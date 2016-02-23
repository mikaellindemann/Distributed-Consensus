using System;
using System.Threading.Tasks;
using Client.Connections;
using Client.Exceptions;
using Client.ViewModels;
using Common.DTO.Server;
using Moq;
using NUnit.Framework;

namespace Client.Tests.ViewModels
{
    [TestFixture]
    class LoginViewModelTests
    {
        private LoginViewModel _model;
        private Mock<IServerConnection> _serverConnectionMock;
        private RolesOnWorkflowsDto _rolesOnWorkflowsDto;
        private Action _mainWindowAction;

        [SetUp]
        public void SetUp()
        {
            _rolesOnWorkflowsDto = new RolesOnWorkflowsDto();

            _serverConnectionMock = new Mock<IServerConnection>(MockBehavior.Strict);
            _serverConnectionMock.Setup(sc => sc.Login(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(_rolesOnWorkflowsDto).Verifiable();

            _mainWindowAction = () => {};

            _model = new LoginViewModel(_serverConnectionMock.Object, new Uri("http://www.contoso.com/"),  _mainWindowAction);
        }

        #region Constructor

        [Test]
        public void Constructor_Ok()
        {
            // Act
            var model = new LoginViewModel();

            // Assert
            Assert.IsNotNull(model);
        }
        #endregion

        #region Databindings
        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Status")]
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

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Password")]
        public void Password_PropertyChanged(string password)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Password") changed = true; };

            // Act
            _model.Password = password;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(password, _model.Password);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Rubbish")]
        [TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Username")]
        public void Username_PropertyChanged(string username)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Username") changed = true; };

            // Act
            _model.Username = username;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(username, _model.Username);
        }
        #endregion

        #region Actions

        [Test]
        public void Login_Ok()
        {
            // Arrange
            var called = false;
            _mainWindowAction += () => called = true;

            // Create a new model to inject an action.
            var model = new LoginViewModel(_serverConnectionMock.Object, new Uri("http://www.contoso.com/"), _mainWindowAction);

            // Act
            model.Login();
            
            // Assert
            Assert.IsTrue(called);
            _serverConnectionMock.Verify(s => s.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Login_ThrowsLoginFailed()
        {
            // Arrange
            _serverConnectionMock.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new LoginFailedException(new Exception())).Verifiable();

            // Act
            _model.Login();

            // Assert
            Assert.AreEqual("The provided username and password does not correspond to a user in Flow", _model.Status);
            _serverConnectionMock.Verify(s => s.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Login_ThrowsHostNotFound()
        {
            // Arrange
            _serverConnectionMock.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new HostNotFoundException(new Exception())).Verifiable();

            // Act
            _model.Login();

            // Assert
            Assert.AreEqual("The server is not available, or the settings file is pointing to an invalid address", _model.Status);
            _serverConnectionMock.Verify(s => s.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Login_ThrowsUnknown()
        {
            // Arrange
            _serverConnectionMock.Setup(s => s.Login(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception()).Verifiable();

            // Act
            _model.Login();

            // Assert
            Assert.AreEqual("An unexpected error occured. Try again in a while.", _model.Status);
            _serverConnectionMock.Verify(s => s.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void Login_Only_One_caller()
        {
            // Arrange
            var called = false;
            _mainWindowAction += () => called = true;

            // Create a new model to inject an action.
            var model = new LoginViewModel(_serverConnectionMock.Object, new Uri("http://www.contoso.com/"), _mainWindowAction);

            // Act
            Task.Run(() => model.Login());
            model.Login();

            // Assert
            Assert.IsTrue(called);
            _serverConnectionMock.Verify(s => s.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        #endregion
    }
}
