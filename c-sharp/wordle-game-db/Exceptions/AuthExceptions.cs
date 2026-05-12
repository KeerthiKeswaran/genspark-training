using System;

namespace WordleGame.Exceptions
{
    public class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException(string message) : base(message) { }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string message) : base(message) { }
    }
}
