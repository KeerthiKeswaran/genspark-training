using System;

namespace WordleGame.Exceptions
{
    public class InvalidDifficultyException : Exception
    {
        public InvalidDifficultyException(string message) : base(message) { }
    }
}
