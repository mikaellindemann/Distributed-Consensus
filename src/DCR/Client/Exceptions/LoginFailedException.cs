using System;

namespace Client.Exceptions
{
    public class LoginFailedException : Exception
    {
        public LoginFailedException(Exception innerException) : base("Username or password didn't match", innerException) { }
    }
}