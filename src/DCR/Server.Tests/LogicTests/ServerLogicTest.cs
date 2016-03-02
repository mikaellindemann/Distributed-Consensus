using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.Event;
using Common.DTO.Server;
using Common.DTO.Shared;
using Common.Exceptions;
using Moq;
using NUnit.Framework;
using Server.Interfaces;
using Server.Logic;
using Server.Models;

namespace Server.Tests.LogicTests
{
    [TestFixture]
    public class ServerLogicTest
    {
        private List<ServerWorkflowModel> _list;
        private Mock _mock;
        private ServerLogic _toTest;

        [TestFixtureSetUp]
        public void Init()
        {
            Setup();

            //Create dummy objects.
            var toSetup = new Mock<IServerStorage>();

            //Set up method for adding events to workflows. The callback adds the input parameters to the list.
            toSetup.Setup(m => m.AddEventToWorkflow(It.IsAny<ServerEventModel>()))
                .Returns((ServerEventModel eventToAdd) => Task.Run(() =>
                {
                    var eventModel = _list.Find(workflow => workflow.Id == eventToAdd.ServerWorkflowModelId).ServerEventModels;
                    eventModel.Add(eventToAdd);
                }));

            //Set up method for adding a new workflow. The callback adds the input parameter to the list.
            toSetup.Setup(m => m.AddNewWorkflow(It.IsAny<ServerWorkflowModel>()))
                .Returns((ServerWorkflowModel toAdd) => Task.Run(() => _list.Add(toAdd)));

            //Set up method for getting all workflows. Simply returns the dummy list.
            toSetup.Setup(m => m.GetAllWorkflows())
                .ReturnsAsync(_list);

            //Set up method for getting all events in a workflow. Gets the list of events on the given workflow.
            toSetup.Setup(m => m.GetEventsFromWorkflow(It.IsAny<string>()))
                .Returns((string toGet) => Task.FromResult(_list.Single(w => w.Id == toGet).ServerEventModels.AsEnumerable()));

            //Set up method for getting a specific workflow. Finds the given workflow in the list.
            toSetup.Setup(m => m.GetWorkflow(It.IsAny<string>()))
                .Returns((string workflowId) => Task.FromResult(_list.Find(x => x.Id == workflowId)));

            //Set up method for removing an event from a workflow. 
            //Finds the given workflow in the list, finds the event in the workflow and removes it.
            toSetup.Setup(m => m.RemoveEventFromWorkflow(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string workflowId, string eventId) => Task.Run(() =>
                {
                    var events = _list.Find(x => x.Id == workflowId).ServerEventModels;
                    var toRemove = events.First(x => x.Id == eventId);
                    events.Remove(toRemove);
                }));

            //Set up method for removing workflow. Removes the input workflow from the list. 
            toSetup.Setup(m => m.RemoveWorkflow(It.IsAny<string>()))
                .Returns((string workflowId) => Task.Run(() =>
                {
                    var toRemove = _list.Find(x => x.Id == workflowId);
                    _list.Remove(toRemove);
                }));

            //Set up method for updating a workflow.
            //Finds the workflow to update in the list, then replaces it with the new workflow.
            toSetup.Setup(m => m.UpdateWorkflow(It.IsAny<ServerWorkflowModel>()))
                .Returns((ServerWorkflowModel toUpdate) => Task.Run(() =>
                {
                    var oldWorkflow = _list.Find(x => x.Id == toUpdate.Id);
                    var index = _list.IndexOf(oldWorkflow);
                    _list.Insert(index, toUpdate);
                }));

            //Assigns the mock to the global variable. 
            //Mock.Setup() is not supported if the variable is already global.
            _mock = toSetup;
            _toTest = new ServerLogic((IServerStorage)_mock.Object);
        }

        /// <summary>
        /// Set up a Mock IServerStorage for validating logic.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            //Setup roles.
            var role = new ServerRoleModel { Id = "test", ServerWorkflowModelId = "1"};
            var roles = new List<ServerRoleModel> { role };

            //Create empty workflows.
            var w1 = new ServerWorkflowModel { Name = "w1", Id = "1" };
            var w2 = new ServerWorkflowModel { Name = "w2", Id = "2" };

            //Ensure that it's REALLY set up this time...
            role.ServerWorkflowModel = w1;
            w1.ServerRolesModels = roles;

            //Create an event with a role.
            var eventToAdd = new ServerEventModel
            {
                Id = "1",
                ServerWorkflowModelId = "1",
                ServerRolesModels = roles,
                ServerWorkflowModel = w1,
                Uri = "http://2.2.2.2/"
            };

            //Add the event to one of the events.
            w1.ServerEventModels.Add(eventToAdd);

            _list = new List<ServerWorkflowModel> { w1, w2 };
        }


        [Test]
        public async void TestAddEventToWorkflow()
        {
            var toAdd = new EventAddressDto
            {
                Id = "3",
                Roles = new List<string> {"lol"},
                Uri = new Uri("http://1.1.1.1/")
            };

            await _toTest.AddEventToWorkflow("1", toAdd);

            var workflow = _list.First(x => x.Id == "1");
            var expectedEvent = workflow.ServerEventModels.First(x => x.Id == "3");

            Assert.AreEqual(expectedEvent.Id, "3");
            Assert.AreEqual(expectedEvent.Uri, "http://1.1.1.1/");
        }

        [Test]
        public async void TestAddNewWorkflow()
        {
            await _toTest.AddNewWorkflow(new WorkflowDto { Id = "3", Name = "w3"});

            var expectedWorkflow = _list.Find(x => x.Id == "3");
            Assert.IsNotNull(expectedWorkflow);
            Assert.AreEqual(expectedWorkflow.Name, "w3");
            Assert.AreEqual(expectedWorkflow.Id, "3");
        }

        [TestCase(null)]
        [TestCase("")]
        public void TestAddEventToWorkflow_Throws_ArgumentNull(string workflowId)
        {
            TestDelegate testDelegate1 = async () => await _toTest.AddEventToWorkflow(workflowId, null);
            TestDelegate testDelegate2 = async () => await _toTest.AddEventToWorkflow(null, new EventAddressDto());

            Assert.Throws<ArgumentNullException>(testDelegate1);
            Assert.Throws<ArgumentNullException>(testDelegate2);
        }

        [Test]
        public void TestAddNewWorkflow_Throws_ArgumentNull()
        {
            TestDelegate testDelegate = async () => await _toTest.AddNewWorkflow(null);

            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void TestRemoveWorkflow_Throws_ArgumentNull()
        {
            TestDelegate testDelegate = async () => await _toTest.RemoveWorkflow(null);

            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [TestCase(null,null)]
        [TestCase("", null)]
        [TestCase(null, "")]
        public void TestRemoveEventFromWorkflow_Throws_ArgumentNull(string workflowId, string eventId)
        {
            TestDelegate testDelegate = async () => await _toTest.RemoveEventFromWorkflow(workflowId, eventId);

            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task TestGetAllWorkflows()
        {
            var expected = (await _toTest.GetAllWorkflows()).ToList();

            var w1 = new WorkflowDto {Id = "1", Name = "w1"};
            var w2 = new WorkflowDto {Id = "2", Name = "w2"};

            var exp1 = expected.First(x => x.Id == "1");
            var exp2 = expected.First(x => x.Id == "2");

            Assert.IsNotNull(exp1);
            Assert.IsNotNull(exp2);
            Assert.AreEqual(w1.Id, exp1.Id);
            Assert.AreEqual(w2.Name, exp2.Name);
        }

        [Test]
        public async Task TestGetEventsOnWorkflow()
        {
            var result = (await _toTest.GetEventsOnWorkflow("1")).ToList();
            var expectedEvent = result.First(x => x.Id == "1");

            Assert.IsNotNull(expectedEvent);
            Assert.AreEqual(expectedEvent.Id, "1");
            Assert.AreEqual(expectedEvent.Uri.AbsoluteUri, "http://2.2.2.2/");

        }
        [Test]
        public void TestGetEventsOnWorkflow_Throws_NullArgument()
        {
            TestDelegate testDelegate = async () => await _toTest.GetEventsOnWorkflow(null);

            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task TestGetWorkflow()
        {
            var result = await _toTest.GetWorkflow("1");
            var actual = _list.First(x => x.Id == "1");

            Assert.AreEqual(actual.Id, result.Id);
            Assert.AreEqual(actual.Name, result.Name);
        }
        [Test]
        public void TestGetWorkflow_NullArgument()
        {
            TestDelegate testDelegate = async () => await _toTest.GetWorkflow(null);

            Assert.Throws<ArgumentNullException>(testDelegate);
        }


        [Test]
        public async Task TestRemoveEventFromWorkflow()
        {
            await _toTest.RemoveEventFromWorkflow("1", "1");

            Assert.IsNotNull(_list.Find(x => x.Id == "1"));
            Assert.IsEmpty(_list.Find(x => x.Id == "1").ServerEventModels);
        }

        [Test]
        public async void TestRemoveWorkflow()
        {
            Assert.AreEqual(1, _list.Count(x => x.Id == "1"));
            
            await _toTest.RemoveWorkflow("1");

            Assert.AreEqual(0, _list.Count(x => x.Id == "1"));
        }

        #region Login

        [Test]
        public async void Login_Succes_UserHasNoRolesReturnEmptyList()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.Login(It.IsAny<ServerUserModel>())).ReturnsAsync(new List<ServerRoleModel>());
            mock.Setup(m => m.GetUser(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ServerUserModel());
            IServerLogic logic = new ServerLogic(mock.Object);
            var loginDto = new LoginDto(){Username = "uName", Password="pWord"};
            //Act
            var result = await logic.Login(loginDto);
            //Assert
            Assert.IsEmpty(result.RolesOnWorkflows);
        }

        [Test]
        public async void Login_Succes_UserHasARoleReturnEmptyList()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            var userModel = new ServerUserModel{Name = "Name", Password="password"};
            var serverRoleModel = new ServerRoleModel {Id = "ServerRoleModelId", ServerWorkflowModelId = "Wid"};

            mock.Setup(m => m.Login(It.Is<ServerUserModel>(model => userModel == model))).ReturnsAsync(new List<ServerRoleModel> { serverRoleModel });
            mock.Setup(m => m.GetUser("Name", "password")).ReturnsAsync(userModel);

            IServerLogic logic = new ServerLogic(mock.Object);
            var loginDto = new LoginDto() { Username = "Name", Password = "password" };
            //Act
            var result = await logic.Login(loginDto);
            //Assert
            Assert.AreEqual(1, result.RolesOnWorkflows.Count);
            Assert.AreEqual(1, result.RolesOnWorkflows["Wid"].Count);
            Assert.Contains(serverRoleModel.Id, result.RolesOnWorkflows["Wid"].ToList());
        }

        [Test]
        public async void Login_Succes_UserHas2EqualRoleReturnEmptyList()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            var userModel = new ServerUserModel { Name = "Name", Password = "password" };
            var serverRoleModel1 = new ServerRoleModel { Id = "ServerRoleModelId", ServerWorkflowModelId = "Wid" };
            var serverRoleModel2 = new ServerRoleModel { Id = "ServerRoleModelId2", ServerWorkflowModelId = "Wid" };

            mock.Setup(m => m.Login(It.Is<ServerUserModel>(model => userModel == model))).ReturnsAsync(new List<ServerRoleModel> { serverRoleModel1, serverRoleModel2 });
            mock.Setup(m => m.GetUser(It.Is<string>(name => name == "Name"), It.Is<string>(pass => pass == "password"))).ReturnsAsync(userModel);

            IServerLogic logic = new ServerLogic(mock.Object);
            var loginDto = new LoginDto() { Username = "Name", Password = "password" };
            //Act
            var result = await logic.Login(loginDto);
            //Assert
            Assert.AreEqual(1,result.RolesOnWorkflows.Count);
            Assert.AreEqual(2, result.RolesOnWorkflows["Wid"].Count);
            Assert.Contains(serverRoleModel1.Id, result.RolesOnWorkflows["Wid"].ToList());
            Assert.Contains(serverRoleModel2.Id, result.RolesOnWorkflows["Wid"].ToList());
        }

        [Test]
        public void Login_Throws_GetUserReturnsNull()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.GetUser(It.Is<string>(name => name == "Name"), It.Is<string>(pass => pass == "password"))).ReturnsAsync(null);

            IServerLogic logic = new ServerLogic(mock.Object);
            var loginDto = new LoginDto() { Username = "Name", Password = "password" };
            //Act
            TestDelegate testDelegate = async() => await logic.Login(loginDto);
            //Assert
            Assert.Throws<UnauthorizedException>(testDelegate);
        }
        #endregion

        #region AddUser
        [Test]
        public void AddUser_Success_NormalUserWithoutRoles()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.AddUser(It.IsAny<ServerUserModel>())).Returns(Task.Delay(0));
            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddUser(new UserDto{Name = "Name", Password = "Password", Roles = new List<WorkflowRole>()});
            //Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void AddUser_Success_NormalUserWithRoleThatAlreadyExists()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.AddUser(It.IsAny<ServerUserModel>())).Returns(Task.Delay(0));
            mock.Setup(m => m.RoleExists(It.IsAny<ServerRoleModel>())).ReturnsAsync(true);
            mock.Setup(m => m.GetRole(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new ServerRoleModel{Id = "role",ServerWorkflowModelId = "Wid"});
            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddUser(new UserDto { Name = "Name", Password = "Password", Roles = new List<WorkflowRole>{new WorkflowRole{Role = "Role",Workflow = "Wid"}} });
            //Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void AddUser_Throws_NormalUserWithRoleThatDoesNotAlreadyExists()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.AddUser(It.IsAny<ServerUserModel>())).Returns(Task.Delay(0));
            mock.Setup(m => m.RoleExists(It.IsAny<ServerRoleModel>())).ReturnsAsync(false);
            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddUser(new UserDto { Name = "Name", Password = "Password", Roles = new List<WorkflowRole> { new WorkflowRole { Role = "Role", Workflow = "Wid" } } });
            //Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [Test]
        public void AddUser_Throws_NullArgument()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddUser(null);
            //Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region AddRolesToUser

        [TestCase(null)]
        [TestCase("")]
        public void AddRolesToUser_Throws_NullArgument(string username)
        {
            TestDelegate testDelegate1 = async () => await _toTest.AddRolesToUser(username, null);
            TestDelegate testDelegate2 = async () => await _toTest.AddRolesToUser(null, new List<WorkflowRole>());

            Assert.Throws<ArgumentNullException>(testDelegate1);
            Assert.Throws<ArgumentNullException>(testDelegate2);
        }

        [Test]
        public void AddRolesToUser_Succes_UserExists()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.UserExists(It.IsAny<string>())).ReturnsAsync(true);
            mock.Setup(m => m.RoleExists(It.IsAny<ServerRoleModel>())).ReturnsAsync(true);
            mock.Setup(m => m.AddRolesToUser(It.IsAny<string>(),It.IsAny<IEnumerable<ServerRoleModel>>())).Returns(Task.Delay(0));

            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddRolesToUser("Wid", new List<WorkflowRole>());
            //Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void AddRolesToUser_Succes_UserExistsMoreRoles()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.UserExists(It.IsAny<string>())).ReturnsAsync(true);
            mock.Setup(m => m.RoleExists(It.IsAny<ServerRoleModel>())).ReturnsAsync(true);
            mock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<ServerRoleModel>>())).Returns(Task.Delay(0));

            var roles = new List<WorkflowRole> { 
                new WorkflowRole { Role = "role1", Workflow = "Wid" },
                new WorkflowRole { Role = "role2", Workflow = "Wid" },
                new WorkflowRole { Role = "role3", Workflow = "Wid2" } 
            };

            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddRolesToUser("Wid", roles);
            //Assert
            Assert.DoesNotThrow(testDelegate);
        }

        [Test]
        public void AddRolesToUser_Throws_UserDoesNotExistsMoreRoles()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.UserExists(It.IsAny<string>())).ReturnsAsync(true);
            mock.Setup(m => m.RoleExists(It.IsAny<ServerRoleModel>())).ReturnsAsync(false);
            mock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<ServerRoleModel>>())).Returns(Task.Delay(0));

            var roles = new List<WorkflowRole> { 
                new WorkflowRole { Role = "role1", Workflow = "Wid" },
                new WorkflowRole { Role = "role2", Workflow = "Wid" },
                new WorkflowRole { Role = "role3", Workflow = "Wid2" } 
            };

            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddRolesToUser("Wid", roles);
            //Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        [Test]
        public void AddRolesToUser_Throws_UserDoesNotExist()
        {
            //Arrange
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.UserExists(It.IsAny<string>())).ReturnsAsync(false);
            mock.Setup(m => m.AddRolesToUser(It.IsAny<string>(), It.IsAny<IEnumerable<ServerRoleModel>>())).Returns(Task.Delay(0));

            IServerLogic logic = new ServerLogic(mock.Object);
            //Act
            TestDelegate testDelegate = async () => await logic.AddRolesToUser("Wid", new List<WorkflowRole>());
            //Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }

        #endregion

        [Test]
        public void Dispose_Test()
        {
            var mock = new Mock<IServerStorage>();
            mock.Setup(m => m.Dispose()).Verifiable("");
            IServerLogic logic = new ServerLogic(mock.Object);
            using (logic)
            {
                
            }
            mock.Verify(storage => storage.Dispose(),Times.Once);
        }
    }
}
