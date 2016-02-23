using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Exceptions;
using Event.Interfaces;
using Event.Logic;
using Moq;
using NUnit.Framework;

namespace Event.Tests.LogicTests
{
    [TestFixture]
    class AuthLogicTests
    {
        private Mock<IEventStorage> _storageMock;
        private AuthLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _storageMock = new Mock<IEventStorage>(MockBehavior.Strict);
            _storageMock.Setup(s => s.Dispose());

            _logic = new AuthLogic(_storageMock.Object);
        }

        [Test]
        public void Dispose_Test()
        {
            _storageMock.Setup(s => s.Dispose()).Verifiable();

            using (_logic)
            {
                
            }

            _storageMock.Verify(s => s.Dispose(), Times.Once);
        }

        [TestCase("Student")]
        [TestCase("Teacher")]
        public async Task IsAuthorized_Returns_True(string role)
        {
            // Arrange
            _storageMock.Setup(s => s.GetRoles(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HashSet<string>
            {
                "Student",
                "Teacher"
            });
            
            // Act
            var result = await _logic.IsAuthorized("workflowId", "eventId", new List<string> { role });

            // Assert
            Assert.IsTrue(result);
        }

        [TestCase("Miner")]
        [TestCase("GasStationOwner")]
        public async Task IsAuthorized_Returns_False(string role)
        {
            // Arrange
            _storageMock.Setup(s => s.GetRoles(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HashSet<string>
            {
                "Student",
                "Teacher"
            });

            // Act
            var result = await _logic.IsAuthorized("workflowId", "eventId", new List<string> { role });

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsAuthorized_Throws_ArgumentNullException_When_Passed_Roles_Is_NULL()
        {
            // Arrange
            _storageMock.Setup(s => s.GetRoles(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new HashSet<string>
            {
                "Student",
                "Teacher"
            });

            // Act
            var testDelegate = new TestDelegate(async () => await _logic.IsAuthorized("workflowId", "eventId", null));

            // Assert
            Assert.Throws<ArgumentNullException>(testDelegate);
        }

        [Test]
        public void IsAuthorized_Throws_NotFoundException_When_EventId_Is_Not_Found()
        {
            // Arrange
            _storageMock.Setup(s => s.GetRoles(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new NotFoundException());

            // Act
            var testDelegate = new TestDelegate(async () => await _logic.IsAuthorized("workflowId", "eventId", new HashSet<string>
            {
                "Student",
                "Teacher"
            }));

            // Assert
            Assert.Throws<NotFoundException>(testDelegate);
        }
    }
}
