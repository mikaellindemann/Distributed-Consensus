using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DTO.History;
using Common.Exceptions;
using Moq;
using NUnit.Framework;
using Server.Exceptions;
using Server.Interfaces;
using Server.Models;
using Server.Storage;

namespace Server.Tests.StorageTests
{
    [TestFixture]
    class StorageTests 
    {
        private IServerContext _context;
        private List<ServerUserModel> _users;
        private List<ServerWorkflowModel> _workflows;
        private List<ServerEventModel> _events;
        private List<ServerRoleModel> _roles;
        private List<ActionModel> _history;
            
        [SetUp]
        public void SetUp()
        {
            var context = new Mock<IServerContext>(MockBehavior.Strict);
            context.SetupAllProperties();

            //USERS:
            _users = new List<ServerUserModel> { new ServerUserModel { Name = "TestingName", Password = PasswordHasher.HashPassword("TestingPassword") } };
            var fakeUserSet = new FakeDbSet<ServerUserModel>(_users.AsQueryable()).Object;

            //WORKFLOWS:
            _workflows = new List<ServerWorkflowModel> { new ServerWorkflowModel { Id = "1", Name = "TestingName" } };
            var fakeWorkflowsSet = new FakeDbSet<ServerWorkflowModel>(_workflows.AsQueryable()).Object;

            //EVENTS:
            _events = new List<ServerEventModel> { new ServerEventModel { Id = "1", ServerWorkflowModelId = "1", Uri = "http://testing.com", ServerWorkflowModel = new ServerWorkflowModel() { Id = "1", Name = "TestingName" } } };
            var fakeEventsSet = new FakeDbSet<ServerEventModel>(_events.AsQueryable()).Object;

            //ROLES:
            _roles = new List<ServerRoleModel> { new ServerRoleModel { Id = "1", ServerWorkflowModelId = "1" } };
            var fakeRolesSet = new FakeDbSet<ServerRoleModel>(_roles.AsQueryable()).Object;

            //HISTORY:
            _history = new List<ActionModel>();
            var fakeHistorySet = new FakeDbSet<ActionModel>(_history.AsQueryable()).Object;

            // Final prep
            context.Setup(m => m.Users).Returns(fakeUserSet);
            context.Setup(m => m.Workflows).Returns(fakeWorkflowsSet);  
            context.Setup(m => m.Events).Returns(fakeEventsSet);
            context.Setup(m => m.Roles).Returns(fakeRolesSet);
            context.Setup(m => m.History).Returns(fakeHistorySet);

            context.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

            //Assign the mocked StorageContext for use in tests.
            _context = context.Object;
        }

        #region AddEventToWorkflow

        [Test]
        public void AddEventToWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddEventToWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void AddEventToWorkflow_WhenWorkflowDoesNotExistNotFoundExceptionIsRaised()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            var eventModel = new ServerEventModel()
            {
                ServerWorkflowModelId = "DailyCleaning",
            };
            
            // Act
            var testDelegate = new AsyncTestDelegate(async() => await storage.AddEventToWorkflow(eventModel));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public void AddEventToWorkflow_WhenEventAlreadyExistsAnEventExistsExceptionIsRaised()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            var eventModel = new ServerEventModel()
            {
                ServerWorkflowModelId = "1",
                Id = "1"
            };

            // Act
            var testDelegate = new AsyncTestDelegate(async() => await storage.AddEventToWorkflow(eventModel));

            // Assert
            Assert.ThrowsAsync<EventExistsException>(testDelegate);
        }

        [Test]
        public void AddEventToWorkflow_HandlesIllegalStorageStateCorrectlyByThrowingException()
        {
            // Arrange
            // Copies Setup() method - only difference is in WORKLOWS, where an extra entry is inserted with identical ID as first entry
            var context = new Mock<IServerContext>();
            context.SetupAllProperties();

            //USERS:
            var users = new List<ServerUserModel> { new ServerUserModel { Name = "TestingName", Password = PasswordHasher.HashPassword("TestingPassword") } };
            var fakeUsersSet = new FakeDbSet<ServerUserModel>(users.AsQueryable()).Object;

            //WORKFLOWS:
            var workflows = new List<ServerWorkflowModel> { new ServerWorkflowModel { Id = "1", Name = "TestingName" }, new ServerWorkflowModel() { Id = "1", Name = "ConflictingWorkflowDueToIdenticalId" } };
            context.Object.Workflows = new FakeDbSet<ServerWorkflowModel>(workflows.AsQueryable()).Object;

            //EVENTS:
            var events = new List<ServerEventModel> { new ServerEventModel { Id = "1", ServerWorkflowModelId = "1", Uri = "http://testing.com", ServerWorkflowModel = new ServerWorkflowModel() { Id = "1", Name = "TestingName" } } };
            context.Object.Events = new FakeDbSet<ServerEventModel>(events.AsQueryable()).Object;

            //ROLES:
            var roles = new List<ServerRoleModel> { new ServerRoleModel { Id = "1", ServerWorkflowModelId = "TestingName" } };
            context.Object.Roles = new FakeDbSet<ServerRoleModel>(roles.AsQueryable()).Object;

            //Assign the mocked StorageContext for use in tests.
            _context = context.Object;

            IServerStorage storage = new ServerStorage(_context);
            var eventModel = new ServerEventModel()
            {
                ServerWorkflowModelId = "1",
                Id = "32"
            };

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddEventToWorkflow(eventModel));

            // Assert
            Assert.ThrowsAsync<IllegalStorageStateException>(testDelegate);
        }

        #endregion

        #region AddNewWorkflow

        [Test]
        public void AddNewWorkflow_HandlesNullArgumentCorrectly()
        {
            // Arrange 
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddNewWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void AddNewWorkflow_WhenWorkflowExistsExceptionIsRaised()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            var workflowModel = new ServerWorkflowModel {Id = "1", Name = "TestingName"};

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddNewWorkflow(workflowModel));

            // Assert
            Assert.ThrowsAsync<WorkflowAlreadyExistsException>(testDelegate);
        }
        #endregion

        #region AddRolesToUser
        [TestCase(null)]
        [TestCase("Per")]
        public void AddRolesToUser_HandlesNullArguments(string username)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            
            IEnumerable<ServerRoleModel> roles = null;
            if (username == null)   // If one argument is null, the other should not be. 
            {
                roles = new List<ServerRoleModel>
                {
                    new ServerRoleModel {Id = "Ambassador"},
                    new ServerRoleModel {Id = "Professor"}
                };
            }
            

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddRolesToUser(username, roles));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void AddRolesToUser_WhenUserDoesNotExistNotFoundExceptionIsThrown()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            IEnumerable<ServerRoleModel> roles = new List<ServerRoleModel>
                {
                    new ServerRoleModel {Id = "Ambassador"},
                    new ServerRoleModel {Id = "Professor"}
                };
            var username = "NonExistingUser";

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddRolesToUser(username, roles));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion

        #region AddRolesToWorkflow

        [Test]
        public void AddRolesToWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddRolesToWorkflow(null));
            
            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region AddUser

        [Test]
        public void AddUser_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddUser(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void AddUser_WhenUserAlreadyExistsUserExistsExceptionIsThrown()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            var userToAdd = new ServerUserModel {Name = "TestingName"};

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.AddUser(userToAdd));

            // Assert
            Assert.ThrowsAsync<UserExistsException>(testDelegate);
        }

        #endregion

        #region EventExists

        [TestCase(null,null)]
        [TestCase("GenericWorkflow",null)]
        [TestCase(null,"Register patient")]
        public void EventExists_HandlessNullArguments(string workflowId, string eventId)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async() => await storage.EventExists(workflowId, eventId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [TestCase("1","1",true)]
        [TestCase("1","2",false)]
        [TestCase("2","1",false)]
        [TestCase("","",false)]
        public async Task EventExists_Test(string workflowId,string eventId, bool expectedResult)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var actualResult = await storage.EventExists(workflowId,eventId);

            // Assert
            Assert.AreEqual(expectedResult,actualResult);
        }
        #endregion

        #region GetAllWorkflows

        [Test]
        public async Task GetAllWorkflows_CanHandleThatNoWorkflowExists()
        {
            // Arrange
            var context = new Mock<IServerContext>(MockBehavior.Strict);
            context.SetupAllProperties();

            //USERS:
            _users = new List<ServerUserModel> { new ServerUserModel { Name = "TestingName", Password = PasswordHasher.HashPassword("TestingPassword") } };
            var mockSetUsers = new FakeDbSet<ServerUserModel>(_users.AsQueryable());

            //WORKFLOWS:
            _workflows = new List<ServerWorkflowModel> ();       // This is the difference from the Setup() method
            var mockSetWorkflows = new FakeDbSet<ServerWorkflowModel>(_workflows.AsQueryable());

            //EVENTS:
            _events = new List<ServerEventModel> { new ServerEventModel { Id = "1", ServerWorkflowModelId = "1", Uri = "http://testing.com", ServerWorkflowModel = new ServerWorkflowModel() { Id = "1", Name = "TestingName" } } };
            var mockSetEvents = new FakeDbSet<ServerEventModel>(_events.AsQueryable());

            //ROLES:
            _roles = new List<ServerRoleModel> { new ServerRoleModel { Id = "1", ServerWorkflowModelId = "TestingName" } };
            var mockSetRoles = new FakeDbSet<ServerRoleModel>(_roles.AsQueryable());

            // Final prep
            context.Setup(m => m.Users).Returns(mockSetUsers.Object);
            context.Setup(m => m.Workflows).Returns(mockSetWorkflows.Object);
            context.Setup(m => m.Events).Returns(mockSetEvents.Object);
            context.Setup(m => m.Roles).Returns(mockSetRoles.Object);

            context.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

            //Assign the mocked StorageContext for use in tests.
            _context = context.Object;

            IServerStorage storage = new ServerStorage(_context);

            // Act
            var workflowList = await storage.GetAllWorkflows();

            // Assert
            Assert.IsEmpty(workflowList);
        }
        #endregion

        #region GetEventsFromWorkflow

        [Test]
        public void GetEventsFromWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetEventsFromWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void GetEventsFromWorkflow_WhenWorkflowDoesNotExistExceptionIsRaised()
        {
            // Arrange
            var storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetEventsFromWorkflow("NonExistingWorkflowId"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public async Task GetEventsFromWorkflow_WhenWorkflowExistChildEventsAreReturned()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var eventsList = await storage.GetEventsFromWorkflow("1");
            var singleEventBelongingToWorkflow1 = eventsList.First();

            // Assert
            Assert.AreEqual(1,eventsList.Count());
            Assert.AreEqual("1",singleEventBelongingToWorkflow1.Id);
            Assert.AreEqual("1", singleEventBelongingToWorkflow1.ServerWorkflowModelId);
            Assert.AreEqual("http://testing.com",singleEventBelongingToWorkflow1.Uri);
            Assert.AreEqual("1",singleEventBelongingToWorkflow1.ServerWorkflowModel.Id);
            Assert.AreEqual("TestingName",singleEventBelongingToWorkflow1.ServerWorkflowModel.Name);
        }

        #endregion

        #region GetHistoryForWorkflow

        [Test]
        public void GetHistoryForWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerHistoryStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetHistoryForWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void GetHistoryForWorkflow_WhenWorkflowDoesNotExistNotFoundExceptionIsRaised()
        {
            // Arrange
            var storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetHistoryForWorkflow("NonExistingWorkflowId"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion

        #region GetRole
        [TestCase(null, null)]
        [TestCase("Doctor", null)]
        [TestCase(null, "Healtchcare")]
        public void GetRole_HandlesNullArgument(string rolename, string workflowId)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetRole(rolename,workflowId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void GetRole_WhenWorkflowDoesNotExistNotFoundExceptionIsRaised()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetRole("Patient","nonexistingworkflowid"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public async Task GetRole_GetsRole()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var actualRole = await storage.GetRole("1", "1");
            
            // Assert
            Assert.AreEqual("1", actualRole.Id);
            Assert.AreEqual("1",actualRole.ServerWorkflowModelId);
        }

        [Test]
        public async Task GetRole_ReturnsNullWhenRoleIsNotInDatabase()
        {
            // Arrange 
            IServerStorage storage = new ServerStorage(_context);
            
            // Act
            var result = await storage.GetRole("nonExistingRole", "1");

            // Assert
            Assert.IsNull(result);
        }
        #endregion

        #region GetUser
        [Test]
        public async Task GetUser()
        {
            var toTest = new ServerStorage(_context);
            var result = await toTest.GetUser("TestingName", "TestingPassword");

            Assert.IsNotNull(result);
            Assert.AreEqual("TestingName", result.Name);

            Assert.IsTrue(PasswordHasher.VerifyHashedPassword("TestingPassword", result.Password));
        }

        [Test]
        public void GetUser_HandlesEmptyStringsForInput()
        {
            // Arrange
            var toTest = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await toTest.GetUser("", ""));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task GetUser_ReturnsNullWhenUserDoesNotExist()
        {
            // Arrange
            var user = "Spock";
            var password = "MSEnterprise";
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var result = await storage.GetUser(user, password);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetUser_ReturnsNonNullWhenUserExists()
        {
            // Arrange
            var user = "TestingName";
            var password = "TestingPassword";
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var result = await storage.GetUser(user, password);

            // Assert
            Assert.IsNotNull(result);
        }
        #endregion

        #region GetWorkflow

        [Test]
        public void GetWorkflow_HandlessNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void GetWorkflow_WhenWorkflowDoesNotExistNotFoundExceptionIsThrown()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.GetWorkflow("NonexistingWorkflow"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public async Task GetWorkflow_WhenWorkflowExistsWorkflowIsReturned()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var actualWorkflow = await storage.GetWorkflow("1");

            var expectedWorkflow = new ServerWorkflowModel
            {
                Id = "1",
                Name = "TestingName"
            };

            // Assert
            Assert.AreEqual(expectedWorkflow.Id,actualWorkflow.Id);
            Assert.AreEqual(expectedWorkflow.Name,actualWorkflow.Name);
        }
        #endregion

        #region Login

        [Test]
        public void Login_WillHandleNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.Login(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        #endregion

        #region RemoveEventFromWorkflow

        [TestCase(null, null)]
        [TestCase("Healtchcare", null)]
        [TestCase(null, "RegisterPatient")]
        public void RemoveEventFromWorkflow_HandlesNullArgument(string workflowId, string eventId)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RemoveEventFromWorkflow(workflowId,eventId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [TestCase("nonExistingWorkflow","1")]
        [TestCase("1", "nonExistingEventId")]
        public void RemoveEventFromWorkflow_WhenWorkflowOrEventDoesNotExistNotFoundExceptionIsThrown(string workflowId, string eventId)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RemoveEventFromWorkflow("nonExistingWorkflow", "1"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion

        #region RemoveWorkflow
        [Test]
        public void RemoveWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RemoveWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void RemoveWorkflow_WhenWorkflowDoesNotExistNotFoundExceptionIsThrown()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RemoveWorkflow("nonExistingWorkflow"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public void RemoveWorkflow_WhenIllegalStorageStateExceptionIsThrown()
        {
            // Arrange
            var context = new Mock<IServerContext>(MockBehavior.Strict);

            //WORKFLOWS:
            _workflows = new List<ServerWorkflowModel>
            {
                new ServerWorkflowModel { Id = "1", Name = "TestingName" },
                new ServerWorkflowModel{ Id = "1", Name = "AnotherWorkflowId"}  // Two workflows sharing the same ID!
            };
            var fakeWorkflowsSet = new FakeDbSet<ServerWorkflowModel>(_workflows.AsQueryable()).Object;

            // Final prep
            context.Setup(m => m.Workflows).Returns(fakeWorkflowsSet);

            context.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

            //Assign the mocked StorageContext for use in tests.
            _context = context.Object;

            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RemoveWorkflow("1"));

            // Assert
            Assert.ThrowsAsync<IllegalStorageStateException>(testDelegate);
        }
        #endregion

        #region RoleExists

        [Test]
        public void RoleExists_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.RoleExists(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [TestCase("1", "1", true)]            // Both role and workflow exist 
        [TestCase("2", "1", false)]           // Role does not exist, while workflow does
        [TestCase("1", "NonExistingWorkflow",false)]    // Role exist, workflow does not exist
        [TestCase("2", "NonExistingWorkflow", false)]   // Role and workflow does not exist
        public async Task RoleExists_WorksAsIntended(string roleId,string workflowId, bool expectedResult)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);
            var argument = new ServerRoleModel
            {
                Id = roleId,
                ServerWorkflowModelId = workflowId
            };

            // Act
            var actualResult = await storage.RoleExists(argument);

            // Assert
            Assert.AreEqual(expectedResult,actualResult);
        }
        #endregion

        #region SaveHistory
        [Test]
        public void SaveHistory_HandlesNullArgument()
        {
            // Arrange
            IServerHistoryStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.SaveHistory(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void SaveHistory_WhenWorkflowDoesNotExistNotFoundExceptionIsThrown()
        {
            // Arrange
            IServerHistoryStorage storage = new ServerStorage(_context);

            var argument = new ActionModel
            {
                WorkflowId = "2"
            };

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.SaveHistory(argument));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }

        [Test]
        public void SaveHistory_WhenWorkflowExistNoExceptionIsThrown()
        {
            // Arrange
            IServerHistoryStorage storage = new ServerStorage(_context);

            var argument = new ActionModel
            {
                WorkflowId = "1"
            };

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.SaveHistory(argument));

            // Assert
            Assert.DoesNotThrowAsync(testDelegate);
        }
        #endregion

        #region SaveNonWorkflowSpecificHistory
        [Test]
        public void SaveNonWorkflowSpecificHistory_HandlesNullArgument()
        {
            // Arrange
            IServerHistoryStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.SaveNonWorkflowSpecificHistory(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }
        #endregion

        #region UpdateWorkflow
        [Test]
        public void UpdateWorkflow_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.UpdateWorkflow(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void UpdateWorkflow_WhenWorkflowDoesNotExistNotFoundExceptionIsThrown()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            var argument = new ServerWorkflowModel
            {
                Id = "2"
            };

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.UpdateWorkflow(argument));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion

        #region UserExists

        [Test]
        public void UserExists_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.UserExists(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [TestCase("TestingName", true)]
        [TestCase("NonExistingUser", false)]
        [TestCase("", false)]
        [TestCase("  ", false)]
        public async Task UserExists_ReturnsCorrectBooleanValue(string user, bool expectedResult)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var actualResult = await storage.UserExists(user);

            // Assert
            Assert.AreEqual(expectedResult,actualResult);
        }
        #endregion

        #region WorkflowExists
        [Test]
        public void WorkflowExists_HandlesNullArgument()
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var testDelegate = new AsyncTestDelegate(async () => await storage.WorkflowExists(null));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [TestCase("1", true)]
        [TestCase("NonExistingWorkflow", false)]
        [TestCase("  ", false)]
        public async Task WorkflowExists_ReturnsCorrectBooleanValue(string workflowId, bool expectedResult)
        {
            // Arrange
            IServerStorage storage = new ServerStorage(_context);

            // Act
            var actualResult = await storage.WorkflowExists(workflowId);

            // Assert
            Assert.AreEqual(expectedResult, actualResult);
        }
        #endregion
    }
}
