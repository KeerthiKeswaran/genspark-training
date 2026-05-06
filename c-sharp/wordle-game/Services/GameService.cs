using System;
using WordleGame.Models;
using WordleGame.Exceptions;
using WordleGame.Interfaces;

namespace WordleGame.Services
{
    public partial class GameService : IGameService
    {
        private readonly IWordService _wordService;
        private readonly IValidationService _validationService;
        private readonly IFeedbackService _feedbackService;
        private GameModel _gameModel = new GameModel("");
        private int _currentDifficulty = 1;

        public GameService()
        {
            _wordService = new WordService();
            _validationService = new ValidationService();
            _feedbackService = new FeedbackService();
        }

        public void SetDifficulty()
        {
            Console.WriteLine("\n--- SELECT DIFFICULTY ---");
            Console.WriteLine("1. Easy");
            Console.WriteLine("2. Medium");
            Console.WriteLine("3. Hard");
            Console.Write("Enter your choice (1-3): ");
            
            string input = Console.ReadLine() ?? string.Empty;
            try
            {
                _currentDifficulty = _validationService.ValidateDifficulty(input);
                Console.WriteLine($"\nDifficulty successfully set to Level {_currentDifficulty}.");
            }
            catch (ArgumentNullException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nInvalid input. Difficulty cannot be empty. Difficulty remains unchanged.");
                Console.ResetColor();
            }
            catch (WordleGame.Exceptions.InvalidDifficultyException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nInvalid input! {ex.Message} Difficulty remains unchanged.");
                Console.ResetColor();
            }
            
            Console.WriteLine("\nPress Enter to return to the main menu...");
            Console.ReadLine();
        }


        public void StartGame()
        {
            _gameModel = new GameModel(_wordService.GetRandomWord(_currentDifficulty));

            Console.WriteLine($"You have {GameModel.MaxAttempts} attempts to guess the 5-letter word.");

            while (_gameModel.Attempts < GameModel.MaxAttempts && !_gameModel.IsWon)
            {
                Console.Write($"\nAttempt {_gameModel.Attempts + 1}: ");
                string guess = Console.ReadLine()?.ToUpper() ?? string.Empty;

                try
                {
                    _validationService.Validate(guess);
                    _gameModel.RecordGuess(guess);
                }
                catch (ArgumentNullException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid input! {ex.Message}");
                    Console.ResetColor();
                    continue;
                }
                catch (InvalidGuessException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid input! {ex.Message}");
                    Console.ResetColor();
                    continue;
                }

                _gameModel.Attempts++;
                
                string feedback = _gameModel.GetFeedback(guess, _feedbackService);
                Console.Write("Feedback: ");
                foreach (char c in feedback)
                {
                    if (c == 'G') Console.ForegroundColor = ConsoleColor.Green;
                    else if (c == 'Y') Console.ForegroundColor = ConsoleColor.Yellow;
                    else Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(c);
                }
                Console.ResetColor();
                Console.WriteLine();

                if (_gameModel.Check(guess))
                {
                    _gameModel.IsWon = true;
                    DisplayComment(_gameModel.Attempts);
                }
            }

            if (!_gameModel.IsWon)
            {
                Console.WriteLine($"\nGame Over! The word was: {_gameModel.RevealSecretWord()}");
            }
            
            CalculateAndPrintScore(_gameModel.Attempts, _gameModel.IsWon);
        }

    }
}
