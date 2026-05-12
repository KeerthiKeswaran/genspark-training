using System;
using WordleGame.Models;
using WordleGame.Exceptions;
using WordleGame.Interfaces;
using WordleGame.Repositories;

namespace WordleGame.Services
{
    public partial class GameService : IGameService
    {
        private readonly IValidationService _validationService;
        private readonly IFeedbackService _feedbackService;
        private readonly IGameRepository _repository;
        
        private GameModel _gameModel = new GameModel("");
        private int _currentDifficulty = 1;
        
        // Authentication State
        public bool IsAuthenticated { get; private set; }
        public string CurrentUserName => _currentUser?.Name ?? "Guest";
        private UserModel? _currentUser;

        public GameService()
        {
            _validationService = new ValidationService();
            _feedbackService = new FeedbackService();
            _repository = new GameRepository();
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
            catch (WordleGame.Exceptions.InvalidDifficultyException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nInvalid input! {ex.Message}");
                Console.ResetColor();
            }
        }

        public void Logout()
        {
            _currentUser = null;
            IsAuthenticated = false;
        }

        public int StartGame()
        {
            string secretWord = GetRandomWord(_currentDifficulty);
            _gameModel = new GameModel(secretWord);

            Console.WriteLine($"\nWord selected! You have {GameModel.MaxAttempts} attempts to guess the 5-letter word.");

            while (_gameModel.Attempts < GameModel.MaxAttempts && !_gameModel.IsWon)
            {
                Console.Write($"\nAttempt {_gameModel.Attempts + 1}: ");
                string guess = Console.ReadLine()?.ToUpper() ?? string.Empty;

                try
                {
                    _validationService.Validate(guess);
                    _gameModel.RecordGuess(guess);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid input! {ex.Message}");
                    Console.ResetColor();
                    continue;
                }

                _gameModel.Attempts++;
                string feedback = _gameModel.GetFeedback(guess, _feedbackService);
                
                Console.Write("Feedback : ");
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
                }
            }

            if (!_gameModel.IsWon)
            {
                Console.WriteLine($"\nGame Over! The word was: {_gameModel.RevealSecretWord()}");
            }
            
            // Calculate and store score using proportional logic: ((MaxAttempts - attempts + 1) / MaxAttempts) * 100
            int maxAttempts = Models.GameModel.MaxAttempts;
            int finalScore = _gameModel.IsWon 
                ? (int)(((maxAttempts - _gameModel.Attempts + 1) / (double)maxAttempts) * 100) 
                : 0;
            StoreScores(finalScore, _gameModel.Attempts, _gameModel.IsWon);
            GetTopScores();
            return _gameModel.Attempts;
        }
    }
}
