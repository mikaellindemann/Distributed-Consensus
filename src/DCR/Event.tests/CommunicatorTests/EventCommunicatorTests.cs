using System;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Tools;
using Event.Communicators;
using Event.Exceptions;
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
        public async void IsExecuted_Succes_BaseAddressGetsSetCorrect()
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
        public async void IsExecuted_Succes_EventReturnsTrue()
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
        public async void IsExecuted_Succes_EventReturnsFalse()
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
        [ExpectedException(typeof(FailedToGetExecutedFromAnotherEventException))]
        public void IsExecuted_FailsOnUriNotPointingAnEventMachine()
        {
            var eventCommunicator = new EventCommunicator();

            try
            {
                var result = eventCommunicator.IsExecuted(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId").Result;
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }
        #endregion

        #region IsIncluded

        [Test]
        public async void IsIncluded_Succes_BaseAddressGetsSetCorrect()
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
        [ExpectedException(typeof(FailedToGetIncludedFromAnotherEventException))]
        public void IsIncluded_FailsOnWrongUri()
        {
            var eventCommunicator = new EventCommunicator();
            try
            {
                var result = eventCommunicator.IsIncluded(new Uri("http://test.dk/"), "targetWorkflowId", "TargetID", "SenderId").Result;
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }
        #endregion

        #region SendPending

        [Test]
        public async void SendPending_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.SendPending(inputUri, new EventAddressDto{WorkflowId = "targetWorkflowId"}, "TargetID", "SenderId");
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendPending(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");
            
            //Assert
            Assert.Throws<FailedToUpdatePendingAtAnotherEventException>(testdelegate);
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendPending(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrow(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region SendIncluded
        [Test]
        public async void SendIncluded_Succes_BaseAddressGetsSetCorrect()
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendIncluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.Throws<FailedToUpdateIncludedAtAnotherEventException>(testdelegate);
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendIncluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrow(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region SendExcluded
        [Test]
        public async void SendExcluded_Succes_BaseAddressGetsSetCorrect()
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendExcluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.Throws<FailedToUpdateExcludedAtAnotherEventException>(testdelegate);
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
            TestDelegate testdelegate = async () => await eventCommunicator.SendExcluded(uri, new EventAddressDto { WorkflowId = "targetWorkflowId", Id = "id", Uri = uri }, "TargetID", "SenderId");

            //Assert
            Assert.DoesNotThrow(testdelegate);
            mock.Verify(t => t.Update<EventAddressDto, int>(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }
        #endregion

        #region Lock
        [Test]
        public async void Lock_Succes_BaseAddressGetsSetCorrect()
        {
            //Arrange

            EventCommunicator eventCommunicator = new EventCommunicator(new HttpClientToolbox());

            Uri inputUri = new Uri("http://test.dk/");

            //Act
            try
            {
                await eventCommunicator.Lock(inputUri,new LockDto(), "Wid","Eid");
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
            TestDelegate testdelegate = async () => await eventCommunicator.Lock(uri, new LockDto(), "Wid", "Eid");

            //Assert
            Assert.Throws<FailedToLockOtherEventException>(testdelegate);
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
            TestDelegate testdelegate = async () => await eventCommunicator.Lock(uri, new LockDto(), "Wid", "Eid");

            //Assert
            Assert.DoesNotThrow(testdelegate);
            mock.Verify(t => t.Create<LockDto, int>(It.IsAny<string>(), It.IsAny<LockDto>()), Times.Once);
        }
        #endregion

        #region Unlock
        [Test]
        public async void Unlock_Succes_BaseAddressGetsSetCorrect()
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
            TestDelegate testdelegate = async () => await eventCommunicator.Unlock(uri, "LockOwner", "Wid", "Eid");

            //Assert
            Assert.Throws<FailedToUnlockOtherEventException>(testdelegate);
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
            TestDelegate testdelegate = async () => await eventCommunicator.Unlock(uri, "LockOwner", "Wid", "Eid");

            //Assert
            Assert.DoesNotThrow(testdelegate);
            mock.Verify(t => t.Delete<int>(It.IsAny<string>()), Times.Once);
        }
        #endregion
    }
}
