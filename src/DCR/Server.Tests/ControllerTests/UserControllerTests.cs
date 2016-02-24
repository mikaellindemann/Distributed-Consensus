using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Server;
using Common.Exceptions;
using Moq;
using NUnit.Framework;
using Server.Controllers;
using Server.Exceptions;
using Server.Interfaces;

namespace Server.Tests.ControllerTests
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<IServerLogic> _logicMock;
        private UsersController _usersController;
        private Mock<IWorkflowHistoryLogic> _historyLogic;

        [SetUp]
        public void SetUp()
        {
            _logicMock = new Mock<IServerLogic>();
            _historyLogic = new Mock<IWorkflowHistoryLogic>();
            
            _usersController = new UsersController(_logicMock.Object, _historyLogic.Object) {Request = new HttpRequestMessage()};
        }

        private IEnumerable<WorkflowRole> GetSomeRoles()
        {
            var roles = new List<WorkflowRole>
            {
                new WorkflowRole() {Role = "Ambassador", Workflow = "Healthcare"},
                new WorkflowRole() {Role = "Governor", Workflow = "Healthcare"},
                new WorkflowRole() {Role = "President", Workflow = "Healtchcare"},
                new WorkflowRole() {Role = "Nurse", Workflow = "Healthcare"}
            };

            return roles;
        }

        private UserDto GetValidUserDto()
        {
            UserDto returnDto = new UserDto()
            {
                Name = "Hans",
                Password = "abcdef123",
                Roles = GetSomeRoles()
            };

            return returnDto;
        }

        #region Login
        [Test]
        public async Task LoginReturnsRolesOnExistingUser()
        {
            // Arrange
            var loginDto = new LoginDto() {Username = "Hans", Password = "1234"};
            var rolesDictionary = new Dictionary<string, ICollection<string>>();
            var roles = new List<string> {"Inspector", "Administrator", "Receptionist"};
            rolesDictionary.Add("Hans",roles);

            var returnDto = new RolesOnWorkflowsDto { RolesOnWorkflows = rolesDictionary };

            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).ReturnsAsync(returnDto);

            // Act
            var result = await _usersController.Login(loginDto);

            // Assert
            Assert.AreEqual(returnDto,result);
        }

        [Test]
        public async Task Login_LogsWhenRolesAreSuccesfullyReturned()
        {
            // Arrange
            var logMethodWasCalled = false;
            var loginDto = new LoginDto() { Username = "Hans", Password = "1234" };
            var rolesDictionary = new Dictionary<string, ICollection<string>>();

            var returnDto = new RolesOnWorkflowsDto {RolesOnWorkflows = rolesDictionary};

            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).ReturnsAsync(returnDto);

            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Returns((ActionModel history) => Task.Run(() => logMethodWasCalled = true));

            // Act
            await _usersController.Login(loginDto);

            // Assert
            Assert.IsTrue(logMethodWasCalled);
        }

        [Test]
        public async Task Login_HandsOverLoginDtoUnAffectedToLogicLayer()
        {
            // Arrange
            var inputlist = new List<LoginDto>();
            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).Returns((LoginDto dto) => Task.Run(() =>
            {
                inputlist.Add(dto);
                return (RolesOnWorkflowsDto) null;
            }));
            var inputDto = new LoginDto() {Username = "Hans", Password = "snah123"};

            // Act
            await _usersController.Login(inputDto);

            // Assert
            Assert.AreEqual(inputDto,inputlist.First());
        }

        [TestCase(typeof(ArgumentNullException))]
        [TestCase(typeof(UnauthorizedException))]
        [TestCase(typeof(Exception))]
        public void Login_WillThrowHttpResponseExceptionWhenCatchingExceptionFromLogic3(Type exceptionType)
        {
            // Arrange
            var loginDto = new LoginDto();
            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).ThrowsAsync((Exception) exceptionType.GetConstructors().First().Invoke(null));

            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.Login(loginDto));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [TestCase(typeof(UnauthorizedException))]
        [TestCase(typeof(Exception))]
        public async Task Login_WhenExceptionIsThrownHistoryIsCalled1(Type exceptionType)
        {
            // Arrange
            var logMethodWasCalled = false;
            var loginDto = new LoginDto() { Username = "Hans", Password = "1234" };

            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).ThrowsAsync((Exception)exceptionType.GetConstructors().First().Invoke(null));

            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Callback((ActionModel history) => logMethodWasCalled = true);

            // Act
            await _usersController.Login(loginDto);

            // Assert
            Assert.IsTrue(logMethodWasCalled);
        }

        [Test]
        public async Task Login_WhenExceptionIsThrownHistoryIsCalled2()
        {
            // Arrange
            var logMethodWasCalled = false;
            var loginDto = new LoginDto() { Username = "Hans", Password = "1234" };

            _logicMock.Setup(m => m.Login(It.IsAny<LoginDto>())).ThrowsAsync(new ArgumentNullException());

            _historyLogic.Setup(m => m.SaveHistory(It.IsAny<ActionModel>()))
                .Callback((ActionModel history) => logMethodWasCalled = true);

            // Act
            await _usersController.Login(loginDto);

            // Assert
            Assert.IsTrue(logMethodWasCalled);
        }

        [Test]
        public void Login_ThrowsExceptionWhenProvidedNullArgument()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.Login(null));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public void Login_CallsHistoryWhenProvidedNullArgument()
        {
            // Arrange
            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Callback((ActionModel model) => {});

            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.Login(null));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        #endregion

        #region CreateUser

        [Test]
        public void CreateUser_RaisesExceptionWhenCalledWithNullArgument()
        {
            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.CreateUser(null));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public async Task CreateUser_WillLogIfCalledWithNullArgument()
        {
            // Arrange
            var logWasCalled = false;
            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Callback((ActionModel model) => logWasCalled = true);

            // Act
            await _usersController.CreateUser(null);

            // Assert
            Assert.IsTrue(logWasCalled);
        }

        [Test]
        public void CreateUser_WillForwardUserDtoUnAffectedToLogicLayer()
        {
            // Arrange
            var catchArgumentList = new List<UserDto>();
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>()))
                .Callback((UserDto providedDto) => catchArgumentList.Add(providedDto));
            var rolesList = new List<WorkflowRole> {new WorkflowRole {Role = "Ambassador", Workflow = "Healthcare"}};
            var argumentToProvide = new UserDto
            {
                Name = "Otto",
                Password = "MargaretThatcher",
                Roles = rolesList
            };

            // Act
            var testDelegate = _usersController.CreateUser(argumentToProvide);

            // Assert
            var actualElementThatWasPassedOn = catchArgumentList.First();
            Assert.AreEqual(argumentToProvide,actualElementThatWasPassedOn);
        }

        [TestCase(typeof(ArgumentNullException))]
        [TestCase(typeof(NotFoundException))]
        [TestCase(typeof(UserExistsException))]
        [TestCase(typeof(InvalidOperationException))]
        [TestCase(typeof(ArgumentException))]
        [TestCase(typeof(Exception))]
        public void CreateUser_WillCatchAndConvertException(Type exceptionType)
        {
            // Arrange
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>())).Throws((Exception)exceptionType.GetConstructors().First().Invoke(null));
            var rolesList = new List<WorkflowRole> {new WorkflowRole {Role = "Ambassador", Workflow = "Healthcare"}};
            var argumentToProvide = new UserDto
            {
                Name = "Otto",
                Password = "MargaretThatcher",
                Roles = rolesList
            };

            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.CreateUser(argumentToProvide));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [TestCase(typeof(ArgumentNullException))]
        [TestCase(typeof(NotFoundException))]
        [TestCase(typeof(UserExistsException))]
        public async Task CreateUser_WillLogWhenAnExceptionWasThrown_1(Type exceptionType)
        {
            // Arrange
            var logWasCalled = false;
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>())).Throws((Exception)exceptionType.GetConstructors().First().Invoke(null));
            _historyLogic.Setup(m => m.SaveHistory(It.IsAny<ActionModel>())).Callback((ActionModel x) => logWasCalled = true);

            var argumentToProvide = new UserDto();

            // Act
            await _usersController.CreateUser(argumentToProvide);

            // Assert
            Assert.IsTrue(logWasCalled);
        }


        [TestCase(typeof(InvalidOperationException))]
        [TestCase(typeof(ArgumentException))]
        [TestCase(typeof(Exception))]
        public async Task CreateUser_WillLogWhenAnExceptionWasThrown_2(Type exceptionType)
        {
            // Arrange
            var logWasCalled = false;
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>())).Throws((Exception)exceptionType.GetConstructors().First().Invoke(null));
            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>())).Callback((ActionModel x) => logWasCalled = true);

            var argumentToProvide = new UserDto();

            // Act
            await _usersController.CreateUser(argumentToProvide);

            // Assert
            Assert.IsTrue(logWasCalled);
        }

        [Test]
        public void CreateUser_WillHandleArgumentExceptionCorrectly_1()
        {
            // Arrange
            var exceptionToBeThrown = new ArgumentException("Conflicting name", "user");
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>())).Throws(exceptionToBeThrown);
            var provideDto = new UserDto();

            // Act
            var task = _usersController.CreateUser(provideDto);

            // Assert
            var exception = task.Exception.InnerException as HttpResponseException;
            if (exception == null)
            {
                Assert.Fail();
            }

            Assert.AreEqual(HttpStatusCode.Conflict,exception.Response.StatusCode);
        }

        [Test]
        public void CreateUser_WillHandleArgumentExceptionCorrectly_2()
        {
            // Arrange
            var exceptionToBeThrown = new ArgumentException();
            _logicMock.Setup(m => m.AddUser(It.IsAny<UserDto>())).Throws(exceptionToBeThrown);
            UserDto provideDto = GetValidUserDto();

            // Act
            var task = _usersController.CreateUser(provideDto);

            // Assert
            var exception = task.Exception.InnerException as HttpResponseException;
            if (exception == null)
            {
                Assert.Fail();
            }

            Assert.AreEqual(HttpStatusCode.BadRequest, exception.Response.StatusCode);
        }


        [Test]
        public void CreateUser_ThrowsExceptionWhenProvidedNonMappableInput()
        {
            // Arrange
            _usersController.ModelState.AddModelError("Name",new ArgumentNullException());
            var userDto = new UserDto();

            // Act
            var task = _usersController.CreateUser(userDto);

            var exception = task.Exception.InnerException as HttpResponseException;
            if (exception == null)
            {
                Assert.Fail();
            }

            // Assert
            Assert.IsInstanceOf<HttpResponseException>(exception);
            Assert.AreEqual(HttpStatusCode.BadRequest,exception.Response.StatusCode);
        }
        #endregion

        #region AddRolesToUser

        [Test]
        public void AddRolesToUser_ThrowsExceptionWhenProvidedNonMappableInput()
        {
            // Arrange
            _usersController.ModelState.AddModelError("Role",new ArgumentNullException());
            var rolesList = GetSomeRoles();
            // Act
            var testDelegate = new TestDelegate(async() => await _usersController.AddRolesToUser("Hanne", GetSomeRoles()));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }

        [Test]
        public async Task AddRolesToUser_WillLogWhenProvidedNonMappableInput()
        {
            // Arrange
            var logWasCalled = false;
            _usersController.ModelState.AddModelError("Name",new ArgumentNullException());
            _historyLogic.Setup(m => m.SaveNoneWorkflowSpecificHistory(It.IsAny<ActionModel>()))
                .Callback((ActionModel model) => logWasCalled = true);
            var rolesList = GetSomeRoles();


            // Act
            await _usersController.AddRolesToUser("Hanne", rolesList);

            // Assert
            Assert.IsTrue(logWasCalled);
        }


        [Test]
        public async Task AddRolesToUser_HandsOverInputUnaffectedToLogicLayer()
        {
            // Arrange
            const string user = "Hanne";
            string receivedUserOnLogicSide = null;
            var rolesList = GetSomeRoles();
            IEnumerable<WorkflowRole> rolesListReceivedOnLogicSide = null;
            _logicMock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<WorkflowRole>>())).
                Callback((string u, IEnumerable<WorkflowRole> roles) =>
                {
                    receivedUserOnLogicSide = u;
                    rolesListReceivedOnLogicSide = roles;
                });

            // Act
            await _usersController.AddRolesToUser(user, rolesList);

            // Assert
            Assert.AreEqual(user,receivedUserOnLogicSide);
            Assert.AreEqual(rolesList,rolesListReceivedOnLogicSide);
        }

        [TestCase(typeof(ArgumentNullException))]
        [TestCase(typeof(NotFoundException))]
        [TestCase(typeof(Exception))]
        public void AddRolesToUser_HandlesExceptionCorrectly_1(Type exceptionType)
        {
            // Arrange
            _logicMock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<WorkflowRole>>()))
                .Throws((Exception)exceptionType.GetConstructors().First().Invoke(null));
            var rolesList = GetSomeRoles();
            const string user = "Hanne";

            // Act
            var testDelegate = new TestDelegate(async () => await _usersController.AddRolesToUser(user, rolesList));

            // Assert
            Assert.Throws<HttpResponseException>(testDelegate);
        }


        [TestCase(typeof (ArgumentNullException), HttpStatusCode.BadRequest)]
        [TestCase(typeof (NotFoundException), HttpStatusCode.NotFound)]
        [TestCase(typeof (Exception), HttpStatusCode.BadRequest)]
        public void AddRolesToUser_HandlesExceptionCorrectly_2(Type exceptionType, HttpStatusCode statuscode)
        {
            // Arrange
            _logicMock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<WorkflowRole>>()))
                .Throws((Exception)exceptionType.GetConstructors().First().Invoke(null));
            var rolesList = GetSomeRoles();
            var user = "Hanne";

            // Act
            var task = _usersController.AddRolesToUser(user, rolesList);

            // Assert
            var exception = task.Exception.InnerException as HttpResponseException;
            if (exception == null)
            {
                Assert.Fail();
            }

            Assert.AreEqual(statuscode,exception.Response.StatusCode);
        }
        #endregion
    }
}
