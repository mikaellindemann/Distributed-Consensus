using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Common.DTO.Event;
using Common.DTO.History;
using Common.DTO.Shared;
using Moq;
using NUnit.Framework;
using Server.Controllers;
using Server.Exceptions;
using Server.Interfaces;
using Server.Models;

namespace Server.Tests.ControllerTests
{
    [TestFixture]
    class WorkflowsControllerTests
    {
        private Mock<IServerLogic> _mock;
        private WorkflowsController _controller;
        private Mock<IWorkflowHistoryLogic> _historyLogic;

        [SetUp]
        public void SetUp()
        {
            _mock = new Mock<IServerLogic>();
            _historyLogic = new Mock<IWorkflowHistoryLogic>();
            _historyLogic.Setup(h => h.SaveHistory(It.IsAny<ActionModel>())).Returns((ActionModel model) => Task.Run(() => 1+1));

            _mock.Setup(logic => logic.Dispose());

            _controller = new WorkflowsController(_mock.Object, _historyLogic.Object) { Request = new HttpRequestMessage() };
        }

        #region GET Workflows
        [Test]
        public async Task GetWorkflows_0_elements()
        {
            // Arrange
            _mock.Setup(logic => logic.GetAllWorkflows()).ReturnsAsync(new List<WorkflowDto>());

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetWorkflows_1_element()
        {
            _mock.Setup(logic => logic.GetAllWorkflows()).ReturnsAsync(new List<WorkflowDto>{ new WorkflowDto { Id = "testWorkflow", Name = "Test Workflow"}});

            // Act
            var result = (await _controller.Get()).ToList();

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0]);
            Assert.AreEqual("testWorkflow", result[0].Id);
            Assert.AreEqual("Test Workflow", result[0].Name);
        }

        [Test]
        public async Task GetWorkflows_10_elements()
        {
            // Arrange
            var workflowDtos = new List<WorkflowDto>();
            for (var i = 0; i < 10; i++)
            {
                workflowDtos.Add(new WorkflowDto { Id = $"testWorkflow{i}", Name = $"Test Workflow {i}"});
            }

            _mock.Setup(logic => logic.GetAllWorkflows()).ReturnsAsync(workflowDtos);

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.AreEqual(10, result.Count());
        }
        #endregion

        #region POST Workflow

        [Test]
        public async void PostWorkflowAddsANewWorkflow()
        {
            var list = new List<WorkflowDto>();
            // Arrange
            _mock.Setup(logic => logic.AddNewWorkflow(It.IsAny<WorkflowDto>()))
                .Returns((WorkflowDto workflowDto) => Task.Run(() => list.Add(workflowDto)));

            var workflow = new WorkflowDto {Id = "id", Name = "name"};

            Assert.IsEmpty(list);

            // Act
            await _controller.PostWorkFlow(workflow);

            // Assert
            Assert.IsNotEmpty(list);
            Assert.AreEqual(workflow.Id, list.First().Id);
            Assert.AreEqual(workflow.Name, list.First().Name);
        }


        [Test]
        [TestCase("testWorkflow1")]
        [TestCase("IdMedSværeBogstaverÅØOgTegn$")]
        public async void PostWorkflow_id_that_does_not_exist(string workflowId)
        {
            var list = new List<ServerWorkflowModel>();
            _mock.Setup(logic => logic.AddNewWorkflow(It.IsAny<WorkflowDto>()))
                .Returns((WorkflowDto incoming) => Task.Run(() => list.Add(new ServerWorkflowModel {Id = incoming.Id, Name = incoming.Id})));

            // Arrange
            var dto = new WorkflowDto {Id = workflowId, Name = "Workflow Name"};

            // Act
            await _controller.PostWorkFlow(dto);

            // Assert
            Assert.IsNotEmpty(list);
            Assert.IsNotNull(list.First(w => w.Id == workflowId));
        }

        [Test]
        [TestCase("NonexistentWorkflowId")]
        [TestCase("EtAndetWorkflowSomIkkeEksisterer")]
        [TestCase(null)]
        public async void PostWorkflow_id_already_exists(string workflowId)
        {
            // Arrange
            var dto = new WorkflowDto { Id = workflowId, Name = "Workflow Name" };

            _mock.Setup(logic => logic.AddNewWorkflow(dto)).Throws<WorkflowAlreadyExistsException>();

            try {
                await _controller.PostWorkFlow(dto);
            }
            catch (Exception e) {
                Assert.IsInstanceOf<HttpResponseException>(e);
                var ex = (HttpResponseException) e;
                Assert.AreEqual(HttpStatusCode.Conflict, ex.Response.StatusCode);
            }
        }

        [Test]
        public async Task PostWorkflow_with_id_and_null_workflow()
        {
            // Arrange
            _mock.Setup(logic => logic.AddNewWorkflow(null)).Throws<ArgumentNullException>();

            try {
                // Act
                await _controller.PostWorkFlow(null);
            }
            catch (Exception ex) {
                // Assert
                Assert.IsInstanceOf<HttpResponseException>(ex);
                var e = (HttpResponseException) ex;
                Assert.AreEqual(HttpStatusCode.BadRequest, e.Response.StatusCode);
            }
        }

        #endregion

        #region DELETE Workflow

        [Test]
        public void Delete_Workflow_That_Does_Exist()
        {
            var list = new List<ServerWorkflowModel> { new ServerWorkflowModel { Id = "DoesExist", Name = "This is a test..."} };

            _mock.Setup(logic => logic.RemoveWorkflow(It.IsAny<string>()))
                .Returns((string incomingId) => Task.Run(() => list.Remove(list.Find(w => w.Id == incomingId))));

            var dto = new WorkflowDto { Id = "DoesExist", Name = "lol"};

            Assert.DoesNotThrow(async () => await _controller.DeleteWorkflow(dto.Id));
            Assert.IsEmpty(list.Where(w => w.Id == "DoesExist"));
        }

        [Test]
        public void Delete_Workflow_That_Does_Not_Exist()
        {
            var list = new List<ServerWorkflowModel> { new ServerWorkflowModel { Id = "DoesNotExist", Name = "This is a test..." } };

            _mock.Setup(logic => logic.RemoveWorkflow(It.IsAny<string>()))
                .Returns((string incomingId) => Task.Run(() =>
                {
                    if (list.Count(w => w.Id == incomingId) != 0) return;

                    list.Remove(list.Find(w => w.Id == incomingId));
                }));

            var dto = new WorkflowDto { Id = "SomeDto", Name = "lol" };

            Assert.DoesNotThrow(async () => await _controller.DeleteWorkflow(dto.Id));
            Assert.IsNotEmpty(list.Where(w => w.Id == "DoesNotExist"));
        }

        #endregion

        #region GET Workflow/Get Events
        [Test]
        [TestCase("workflowId1", 0)]
        [TestCase("workflowId1", 1)]
        [TestCase("workflowId1", 35)]
        public async Task Get_workflow_returns_list_of_EventAddressDto(string workflowId, int numberOfEvents)
        {
            // Arrange
            var list = new List<EventAddressDto>();

            for (var i = 0; i < numberOfEvents; i++)
            {
                list.Add(new EventAddressDto { Id = $"event{i}", Uri = new Uri($"http://www.example.com/test{i}") });
            }

            _mock.Setup(logic => logic.GetEventsOnWorkflow(workflowId)).ReturnsAsync(list);

            // Act
            var result = await _controller.Get(workflowId);

            // Assert
            Assert.IsInstanceOf<IEnumerable<EventAddressDto>>(result);

            Assert.AreEqual(numberOfEvents, result.Count());
        }

        [Test]
        public async Task GetWorkflow_right_list_when_multiple_workflows_exists()
        {
            // Arrange
            _mock.Setup(logic => logic.GetEventsOnWorkflow("id1")).ReturnsAsync(new List<EventAddressDto> { new EventAddressDto { Id = "id1", Uri = null }});
            _mock.Setup(logic => logic.GetEventsOnWorkflow("id2")).ReturnsAsync(new List<EventAddressDto>
            {
                new EventAddressDto { Id = "id1", Uri = null },
                new EventAddressDto { Id = "id2", Uri = null }
            });

            // Act
            var result = await _controller.Get("id1");

            // Assert
            Assert.AreEqual(1, result.Count());
        }
        #endregion

        #region POST Event
        [Test]
        public async Task PostEventToWorkflowAddsEventToWorkflow()
        {
            var list = new List<EventAddressDto>();
            // Arrange
            _mock.Setup(logic => logic.AddEventToWorkflow(It.IsAny<string>(), It.IsAny<EventAddressDto>()))
                .Returns((string s, EventAddressDto eventDto) => Task.Run(() => list.Add(eventDto)));
            _mock.Setup(logic => logic.GetEventsOnWorkflow(It.IsAny<string>())).ReturnsAsync(list);

            var eventAddressDto = new EventAddressDto { Id = "id", Uri = new Uri("http://www.contoso.com/") };

            // Act
            await _controller.PostEventToWorkFlow("workflow", eventAddressDto);

            // Assert
            Assert.AreEqual(eventAddressDto, list.First());
        }

        #endregion

        #region PUT Event

        #endregion

        #region DELETE Event
        #endregion
    }
}
