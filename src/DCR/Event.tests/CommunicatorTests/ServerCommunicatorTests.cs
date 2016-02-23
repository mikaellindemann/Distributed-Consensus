using System;
using System.Threading.Tasks;
using Common.DTO.Shared;
using Common.Tools;
using Event.Communicators;
using Event.Exceptions;
using Event.Exceptions.ServerInteraction;
using Moq;
using NUnit.Framework;

namespace Event.Tests.CommunicatorTests
{
    [TestFixture]
    public class ServerCommunicatorTests
    {
        private ServerCommunicator _toTest;
        private Mock<HttpClientToolbox> _toolBoxMock;
        
        [TestFixtureSetUp]
        public void Setup() {
            var mock = new Mock<HttpClientToolbox>();

            mock.Setup(m => m.Create(It.IsAny<string>(), It.IsAny<EventAddressDto>())).Returns(Task.Run(() => Console.WriteLine())).Verifiable();
            mock.Setup(m => m.Delete(It.IsAny<string>())).Returns(Task.Run(() => Console.WriteLine())).Verifiable();
            mock.Setup(m => m.Dispose()).Verifiable();

            _toolBoxMock = mock;
            _toTest = new ServerCommunicator("testingEventId", "testingWorkflowId", mock.Object);

        }

        [Test]
        public void ConstructorTest()
        {
            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator(null, "", ""));
            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator("", null, ""));
            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator("", "", (string)null));

            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator(null, "", new HttpClientToolbox()));
            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator("", null, new HttpClientToolbox()));
            Assert.Throws<ArgumentNullException>(() => new ServerCommunicator("", "", (HttpClientToolbox)null));
        }

        [Test]
        public void PostEventToServerTestThrowsException()
        {
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Create(It.IsAny<string>(), It.IsAny<EventAddressDto>())).Throws(new Exception());
            var toTest = new ServerCommunicator("testingEventId", "testingWorkflowId", mock.Object);

            Assert.Throws<FailedToPostEventAtServerException>(async () => await toTest.PostEventToServer(new EventAddressDto()));
        }

        [Test]
        public void PostEventToServerTestSuccedes()
        {
            Assert.DoesNotThrow(async () => await _toTest.PostEventToServer(new EventAddressDto()));
            _toolBoxMock.Verify(t => t.Create(It.IsAny<string>(), It.IsAny<EventAddressDto>()), Times.Once);
        }

        [Test]
        public void DeleteEventFromServerTestThrowsException() {
            var mock = new Mock<HttpClientToolbox>();
            mock.Setup(m => m.Delete(It.IsAny<string>())).Throws(new Exception());
            var toTest = new ServerCommunicator("testingEventId", "testingWorkflowId", mock.Object);

            Assert.Throws<FailedToDeleteEventFromServerException>(async () => await toTest.DeleteEventFromServer());
        }

        [Test]
        public void DeleteEventFromServerTestSuccedes()
        {
            Assert.DoesNotThrow(async () => await _toTest.DeleteEventFromServer());
            _toolBoxMock.Verify(t => t.Delete(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void DisposeTest() {
            using (_toTest)
            {
                
            }

            _toolBoxMock.Verify(m => m.Dispose(), Times.Once);
        }
    }
}
