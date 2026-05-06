using System;

namespace WordleGame.Exceptions
{
    public class InvalidGuessException : Exception
    {
        public InvalidGuessException(string message) : base(message) { }
    }
}
