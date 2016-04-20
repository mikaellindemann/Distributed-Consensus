using System;
using System.Runtime.Serialization;

namespace Event.Logic
{
    [Serializable]
    internal class FailedToSaveHistoryException : Exception
    {
        public FailedToSaveHistoryException()
        {
        }

        public FailedToSaveHistoryException(string message) : base(message)
        {
        }

        public FailedToSaveHistoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FailedToSaveHistoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}