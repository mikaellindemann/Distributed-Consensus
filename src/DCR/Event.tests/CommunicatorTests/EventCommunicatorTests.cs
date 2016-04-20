using System;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Tools;
using Event.Communicators;
using Event.Exceptions.EventInteraction;
using Event.Interfaces;
using Event.Models;
using Moq;
using NUnit.Framework;

namespace Event.Tests.CommunicatorTests
{
    [TestFixture]
    public class EventCommunicatorTests
    {
        #region SetupMocks

        public IEventFromEvent DefaultMockSetup()
        {
            var mock = new Mock<HttpClientToolbox>();

            return new EventCommunicator(mock.Object);
        }

        #endregion



        #region constructor and dispose
        [Test]
        public void DefaultConstructor_Runs()
        {
            //Act
            var eventCommunicator = new EventCommunicator();

            //Assert
            Assert.IsNotNull(eventCommunicator);
        }

        [Test]
        public void SecondConstructor_Runs()
        {
            //Act
            var eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            //Assert
            Assert.IsNotNull(eventCommunicator);
        }


        [Test]
        public void Dispose()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Dispose()).Verifiable("");

            IEventFromEvent communicator = new EventCommunicator(mock.Object);

            //Act
            using (communicator)
            {

            }
            //Assert
            mock.Verify(t => t.Dispose(), Times.Once);
        }

        #endregion

        #region IsExecuted

        [Test]
        public async Task IsExecuted_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.IsExecuted(inputUri, "targetWorkflowId", "TargetID", "SenderId");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }

        [Test]
        public async Task IsExecuted_Succes_EventReturnsTrue()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Read<bool>(It.IsAny<string>())).ReturnsAsync(true).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);

            //Act
            var result = await eventCommunicator.IsExecuted(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId");

            //Assert
            mock.Verify(t => t.Read<bool>(It.IsAny<string>()), Times.Once);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task IsExecuted_Succes_EventReturnsFalse()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Read<bool>(It.IsAny<string>())).ReturnsAsync(false).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);

            //Act
            var result = await eventCommunicator.IsExecuted(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId");

            //Assert
            mock.Verify(t => t.Read<bool>(It.IsAny<string>()), Times.Once);
            Assert.IsFalse(result);
        }


        [Test]
        public void IsExecuted_FailsOnUriNotPointingAnEventMachine()
        {
            var eventCommunicator = new EventCommunicator();

            AsyncTestDelegate testDelegate = async () => await eventCommunicator.IsExecuted(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId");

            Assert.ThrowsAsync<FailedToGetExecutedFromAnotherEventException>(testDelegate);
        }
        #endregion

        #region IsIncluded

        [Test]
        public async Task IsIncluded_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.IsIncluded(inputUri, "targetWorkflowId", "TargetID", "SenderId");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }

        [Test]
        public void IsIncluded_Succes_EventReturnsTrue()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Read<bool>(It.IsAny<string>())).ReturnsAsync(true).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);

            //Act
            var result = eventCommunicator.IsIncluded(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId").Result;

            //Assert
            mock.Verify(t => t.Read<bool>(It.IsAny<string>()), Times.Once);
            Assert.IsTrue(result);
        }

        [Test]
        public void IsIncluded_Succes_EventReturnsFalse()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Read<bool>(It.IsAny<string>())).ReturnsAsync(false).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);

            //Act
            var result = eventCommunicator.IsIncluded(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId").Result;

            //Assert
            mock.Verify(t => t.Read<bool>(It.IsAny<string>()), Times.Once);
            Assert.IsFalse(result);
        }

        [Test]
        public void IsIncluded_FailsOnWrongUri()
        {
            var eventCommunicator = new EventCommunicator();

            AsyncTestDelegate testDelegate = async () => await eventCommunicator.IsIncluded(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId");

            Assert.ThrowsAsync<FailedToGetIncludedFromAnotherEventException>(testDelegate);
        }
        #endregion

        #region SendPending

        [Test]
        public async Task SendPending_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.SendPending(inputUri, new EventAddressDto { WorkflowId = "targetWorkflowId" }, "TargetID", "SenderId");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }

        [Test]
        public void SendPending_FailsOnUriNotPointingAnEventMachine()
        {
            //Arrange
            var eventCommunicator = new EventCommunicator();
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendPending(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.ThrowsAsync<FailedToUpdatePendingAtAnotherEventException>(testdelegate);
        }

        [Test]
        public void SendPending_Succes_WhenNoExceptionFromSubLayerDoesNotTrowExceptions()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>())).Returns(Task.FromResult(3)).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendPending(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrowAsync(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region SendIncluded
        [Test]
        public async Task SendIncluded_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.SendIncluded(inputUri, new EventAddressDto { WorkflowId = "targetWorkflowId" }, "TargetID", "SenderId");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }

        [Test]
        public void SendIncluded_FailsOnUriNotPointingAnEventMachine()
        {
            //Arrange
            var eventCommunicator = new EventCommunicator();
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendIncluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.ThrowsAsync<FailedToUpdateIncludedAtAnotherEventException>(testdelegate);
        }

        [Test]
        public void SendIncluded_Succes_WhenNoExceptionFromSubLayerDoesNotTrowExceptions()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>())).Returns(Task.FromResult(0)).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendIncluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrowAsync(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region SendExcluded
        [Test]
        public async Task SendExcluded_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.SendExcluded(inputUri, new EventAddressDto { WorkflowId = "targetWorkflowId" }, "TargetID", "SenderId");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }


        [Test]
        public void SendExcluded_FailsOnUriNotPointingAnEventMachine()
        {
            //Arrange
            var eventCommunicator = new EventCommunicator();
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendExcluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.ThrowsAsync<FailedToUpdateExcludedAtAnotherEventException>(testdelegate);
        }

        [Test]
        public void SendExcluded_Succes_WhenNoExceptionFromSubLayerDoesNotTrowExceptions()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Update(It.IsAny<string>(), It.IsAny<EventAddressDto>())).Returns(Task.FromResult(0)).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.SendExcluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrowAsync(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region Lock
        [Test]
        public async Task Lock_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.Lock(inputUri, new LockDto(), "Wid", "Eid");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }


        [Test]
        public void Lock_FailsOnUriNotPointingAnEventMachine()
        {
            //Arrange
            var eventCommunicator = new EventCommunicator();
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.Lock(uri, new LockDto(), "Wid", "Eid");

            //Assert
            Assert.ThrowsAsync<FailedToLockOtherEventException>(testdelegate);
        }

        [Test]
        public void Lock_Succes_WhenNoExceptionFromSubLayerDoesNotTrowExceptions()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Create(It.IsAny<string>(), It.IsAny<LockDto>())).Returns(Task.FromResult(0)).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.Lock(uri, new LockDto(), "Wid", "Eid");

            //Assert
            Assert.DoesNotThrowAsync(testdelegate);
            mock.Verify(t => t.Create<LockDto, int>(It.IsAny<string>(), It.IsAny<LockDto>()), Times.Once);
        }
        #endregion

        #region Unlock
        [Test]
        public async Task Unlock_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.Unlock(inputUri, "LockOwner", "Wid", "Eid");
            }
            catch (Exception)
            {
                // its okay
            }

            //Assert
            Assert.AreEqual(inputUri, eventCommunicator.HttpClient.HttpClient.BaseAddress);
        }


        [Test]
        public void Unlock_FailsOnUriNotPointingAnEventMachine()
        {
            //Arrange
            var eventCommunicator = new EventCommunicator();
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.Unlock(uri, "LockOwner", "Wid", "Eid");

            //Assert
            Assert.ThrowsAsync<FailedToUnlockOtherEventException>(testdelegate);
        }

        [Test]
        public void Unlock_Succes_WhenNoExceptionFromSubLayerDoesNotTrowExceptions()
        {
            //Arrange
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Delete(It.IsAny<string>())).Returns(Task.FromResult(0)).Verifiable();

            IEventFromEvent eventCommunicator = new EventCommunicator(mock.Object);
            Uri uri = new Uri("http://test.dk/");

            //Act
            AsyncTestDelegate testdelegate = async () => await eventCommunicator.Unlock(uri, "LockOwner", "Wid", "Eid");

            //Assert
            Assert.DoesNotThrowAsync(testdelegate);
            mock.Verify(t => t.Delete<int>(It.IsAny<string>()), Times.Once);
        }
        #endregion
    }
}
