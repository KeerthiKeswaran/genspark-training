using WordleGame.Services;
using WordleGame.Exceptions;
using System.Collections.Generic;
using WordleGame.Interfaces;

namespace WordleGame.Models
{
    public class GameModel
    {
        private string _secretWord; 
        private readonly HashSet<string> _previousGuesses = new HashSet<string>();

        public int Attempts { get; set; }
        public const int MaxAttempts = 6;
        public bool IsWon { get; set; }

        public GameModel(string secretWord)
        {
            _secretWord = secretWord;
            Attempts = 0;
            IsWon = false;
        }

        public bool Check(string guess) 
        {
            return guess == _secretWord;
        }

        public void RecordGuess(string guess)
        {
            if (!_previousGuesses.Add(guess))
            {
                throw new InvalidGuessException("You have already guessed this word.");
            }
        }

        public string GetFeedback(string guess, IFeedbackService feedbackService)
        {
            return feedbackService.GenerateFeedback(guess, _secretWord);
        }

        public string RevealSecretWord()
        {
            return _secretWord;
        }

    }
}


