using System;

namespace Client.Exceptions
{
    public class HostNotFoundException : Exception
    {
        public HostNotFoundException(Exception innerException) : base("Host not found", innerException) { }
    }
}
