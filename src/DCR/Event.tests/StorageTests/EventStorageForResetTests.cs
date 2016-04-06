using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Interfaces;
using Event.Models;
using Event.Storage;
using Moq;
using NUnit.Framework;

namespace Event.Tests.StorageTests
{
    [TestFixture]
    class EventStorageForResetTests
    {
        private EventStorageForReset _storageForReset;
        private Mock<IEventContext> _contextMock;
        private List<EventModel> _eventModels;

        [SetUp]
        public void SetUp()
        {
            _eventModels = new List<EventModel>();

            _contextMock = new Mock<IEventContext>(MockBehavior.Strict);
            _contextMock.Setup(c => c.Dispose()).Verifiable();
            _contextMock.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
            _contextMock.Setup(c => c.Events).Returns(new FakeDbSet<EventModel>(_eventModels.AsQueryable()).Object);

            _storageForReset = new EventStorageForReset(_contextMock.Object);
        }

        #region Constructor and Dispose
        [Test]
        public void Constructor_Null()
        {
            // Act
            TestDelegate testDelegate = () => new EventStorageForReset(null);

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void Constructor_ValidArgument()
        {
            // Act
            var storageForReset = new EventStorageForReset(_contextMock.Object);

            // Assert
            Assert.IsNotNull(storageForReset);
        }

        [Test]
        public void Dispose_Ok()
        {
            // Act
            using (_storageForReset)
            {

            }

            // Assert
            _contextMock.Verify(c => c.Dispose(), Times.Once);
        }
        #endregion

        #region Exists
        [TestCase(null, "eventId"),
         TestCase(null, null),
         TestCase("workflowId", null)]
        public void Exists_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new AsyncTestDelegate(async () => await _storageForReset.Exists(workflowId, eventId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public async Task Exists_True()
        {
            // Arrange
            _eventModels.Add(new EventModel
            {
                WorkflowId = "workflowId",
                Id = "eventId"
            });

            // Act
            var result = await _storageForReset.Exists("workflowId", "eventId");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task Exists_False()
        {
            // Arrange

            // Act
            var result = await _storageForReset.Exists("workflowId", "eventId");

            // Assert
            Assert.IsFalse(result);
        }
        #endregion

        #region ResetToInitialState

        [TestCase(true, true, true),
         TestCase(true, true, false),
         TestCase(true, false, true),
         TestCase(true, false, false),
         TestCase(false, true, true),
         TestCase(false, true, false),
         TestCase(false, false, true),
         TestCase(false, false, false)]
        public async Task ResetToInitialState_Ok(bool initialExecuted, bool initialIncluded, bool initialPending)
        {
            // Arrange
            _eventModels.Add(new EventModel
            {
                WorkflowId = "workflowId",
                Id = "eventId",
                Executed = !initialExecuted,
                Included = !initialIncluded,
                Pending = !initialPending,
                InitialExecuted = initialExecuted,
                InitialIncluded = initialIncluded,
                InitialPending = initialPending
            });

            // Act
            await _storageForReset.ResetToInitialState("workflowId", "eventId");

            // Assert
            Assert.AreEqual(initialExecuted, _eventModels.First().Executed);
            Assert.AreEqual(initialIncluded, _eventModels.First().Included);
            Assert.AreEqual(initialPending, _eventModels.First().Pending);
        }

        [TestCase(null, "eventId"),
         TestCase("workflowId", null),
         TestCase(null, null)]
        public void ResetToInitialState_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new AsyncTestDelegate(async () => await _storageForReset.ResetToInitialState(workflowId, eventId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void ResetToInitialState_NotFound()
        {
            // Arrange
            
            // Act
            var testDelegate =
                new AsyncTestDelegate(async () => await _storageForReset.ResetToInitialState("workflowId", "eventId"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion

        #region ClearLock

        [Test]
        public async Task ClearLock_Ok()
        {
            // Arrange
            _eventModels.Add(new EventModel
            {
                WorkflowId = "workflowId",
                Id = "eventId",
                LockOwner = "SomeoneHasALockHere!"
            });

            // Act
            await _storageForReset.ClearLock("workflowId", "eventId");

            // Assert
            Assert.IsNull(_eventModels.First().LockOwner);
        }

        [TestCase(null, "eventId"),
         TestCase("workflowId", null),
         TestCase(null, null)]
        public void ClearLock_ArgumentNull(string workflowId, string eventId)
        {
            // Act
            var testDelegate = new AsyncTestDelegate(async () => await _storageForReset.ClearLock(workflowId, eventId));

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(testDelegate);
        }

        /// <summary>
        /// This test fails because it is never checked whether the event exists or not.
        /// Therefore the delegate throws another kind of exceptio.
        /// </summary>
        [Test]
        public void ClearLock_NotFound()
        {
            // Act
            var testDelegate = new AsyncTestDelegate(async () => await _storageForReset.ClearLock("NotId", "NotId"));

            // Assert
            Assert.ThrowsAsync<NotFoundException>(testDelegate);
        }
        #endregion
    }
}
