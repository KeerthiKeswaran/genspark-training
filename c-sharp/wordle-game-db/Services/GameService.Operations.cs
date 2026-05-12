using System;
using System.Collections.Generic;
using WordleGame.Models;
using WordleGame.Exceptions;

namespace WordleGame.Services
{
    public partial class GameService
    {
        // --- Word Operations ---
        public string GetRandomWord(int difficulty)
        {
            List<string> wordList = _repository.GetWordsByDifficulty(difficulty);
            if (wordList == null || wordList.Count == 0)
            {
                return "APPLE"; // Fallback
            }
            Random random = new Random();
            int index = random.Next(wordList.Count);
            return wordList[index];
        }

        // --- Authentication Operations ---
        public void AuthenticateUser()
        {
            Console.WriteLine("\n--- LOGIN ---");
            Console.Write("Enter Username or Email: ");
            string identifier = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Password: ");
            string password = Console.ReadLine() ?? string.Empty;

            UserModel user = _repository.GetUserByEmailOrName(identifier);

            if (user == null)
            {
                throw new UserNotFoundException("User not found with the provided identifier.");
            }

            // Hash the input password to check against the stored hash
            string hashedInput = GenerateHash(10, password);

            if (user.PasswordHash == hashedInput) 
            {
                _currentUser = user;
                IsAuthenticated = true;
                Console.WriteLine($"\nWelcome back, {user.Name}!");
            }
            else
            {
                throw new AuthenticationFailedException("Invalid password. Please try again.");
            }
        }

        public void RegisterUser()
        {
            Console.WriteLine("\n--- REGISTER ---");
            Console.Write("Enter Name: ");
            string name = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Email: ");
            string email = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Password: ");
            string password = Console.ReadLine() ?? string.Empty;

            if (_repository.GetUserByEmailOrName(email) != null || _repository.GetUserByEmailOrName(name) != null)
            {
                throw new UserAlreadyExistsException("A user with this name or email already exists.");
            }

            string userId = GenerateHash(5, name + email);
            string passwordHash = GenerateHash(10, password);

            UserModel newUser = new UserModel
            {
                UserId = userId,
                Name = name,
                Email = email,
                PasswordHash = passwordHash
            };

            _repository.CreateUser(newUser);
            Console.WriteLine($"\nRegistration successful! Your UserID is: {userId}");
            Console.WriteLine("You can now login.");
        }

        // --- Score Operations ---
        public void StoreScores(int finalScore, int attempts, bool isWon)
        {
            if (_currentUser == null) return;

            ScoreModel score = new ScoreModel
            {
                UserId = _currentUser.UserId,
                ScoreValue = finalScore,
                Difficulty = _currentDifficulty,
                PlayedAt = DateTime.Now
            };

            _repository.SaveScore(score);
            Console.WriteLine($"\nScore of {finalScore} saved for {_currentUser.Name}.");
        }

        public List<ScoreModel> GetScores()
        {
            if (_currentUser == null) return new List<ScoreModel>();
            return _repository.GetUserScores(_currentUser.UserId);
        }

        public List<ScoreModel> GetTopScores()
        {
            return _repository.GetTopScores(5);
        }
    }
}
