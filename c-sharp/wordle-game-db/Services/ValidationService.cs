using System;
using WordleGame.Exceptions;
using WordleGame.Interfaces;

namespace WordleGame.Services
{
    public class ValidationService : IValidationService
    {
        public void Validate(string guess)
        {
            if (string.IsNullOrWhiteSpace(guess))
                throw new ArgumentNullException();

            if (guess.Length < 5)
                throw new InvalidGuessException("Input less than 5 letters.");

            if (guess.Length > 5)
                throw new InvalidGuessException("Input greater than 5 letters.");

            foreach (char c in guess)
            {
                if (char.IsDigit(c))
                    throw new InvalidGuessException("Input contains numbers.");
                if (!char.IsLetter(c))
                    throw new InvalidGuessException("Input contains special characters.");
            }
        }
        public int ValidateDifficulty(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException();

            if (!int.TryParse(input, out int level))
                throw new InvalidDifficultyException("Difficulty must be a number.");

            if (level < 1 || level > 3)
                throw new InvalidDifficultyException("Difficulty must be between 1 and 3.");

            return level;
        }
    }
}
