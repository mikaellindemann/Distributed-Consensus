using System;
using Client.ViewModels;
using Common.DTO.History;
using NUnit.Framework;

namespace Client.Tests.ViewModels
{
    [TestFixture]
    class HistoryViewModelTests
    {
        private HistoryViewModel _model;
        private ActionDto _dto;

        [SetUp]
        public void SetUp()
        {
            _dto = new ActionDto();

            _model = new HistoryViewModel(_dto);
        }

        #region Constructors

        [Test]
        public void Constructor_NoParameter()
        {
            // Act
            var model = new HistoryViewModel();

            // Assert
            Assert.IsNotNull(model);
        }

        [Test]
        public void Constructor_NullParameter()
        {
            // Act
            // Assert
            Assert.Throws<ArgumentNullException>(() => new HistoryViewModel(null));
        }

        [Test]
        public void Constructor_Parameter()
        {
            // Act
            var model = new HistoryViewModel(new ActionDto());

            // Assert
            Assert.IsNotNull(model);
        }
        #endregion

        #region Databindings
        [TestCase(""),
         TestCase(null),
         TestCase("Rubbish"),
         TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long WorkflowId")]
        public void WorkflowId_PropertyChanged(string workflowId)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "WorkflowId") changed = true; };

            // Act
            _model.WorkflowId = workflowId;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(workflowId, _model.WorkflowId);
        }

        [TestCase(""),
         TestCase(null),
         TestCase("Rubbish"),
         TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long EventId")]
        public void EventId_PropertyChanged(string eventId)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "EventId") changed = true; };

            // Act
            _model.EventId = eventId;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(eventId, _model.EventId);
        }

        [TestCase(""),
         TestCase(null),
         TestCase("Rubbish"),
         TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Message")]
        public void Message_PropertyChanged(string counterPartId)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "CounterpartId") changed = true; };

            // Act
            _model.CounterpartId = counterPartId;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(counterPartId, _model.CounterpartId);
        }

        [TestCase(""),
         TestCase(null),
         TestCase("Rubbish"),
         TestCase("Very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, very, long Title")]
        public void Title_PropertyChanged(string title)
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "Title") changed = true; };

            // Act
            _model.Title = title;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(title, _model.Title);
        }

        [Test]
        public void TimeStamp_PropertyChanged()
        {
            // Arrange
            var changed = false;
            _model.PropertyChanged += (o, s) => { if (s.PropertyName == "TimeStamp") changed = true; };
            var dt = new Random().Next(4124924);


            // Act
            _model.TimeStamp = dt;

            // Assert
            Assert.IsTrue(changed);
            Assert.AreEqual(dt, _model.TimeStamp);
        }
        #endregion
    }
}
