using System;
using System.Collections.Generic;
using WordleGame.Services;
using WordleGame.Interfaces;
using WordleGame.Models;

namespace WordleGame.App
{
    public class Program
    {

        public static void ShowInstructions()
        {
            Console.WriteLine("--- WORDLE INSTRUCTIONS ---");
            Console.WriteLine("Guess the 5-letter secret word within 6 attempts.");
            Console.Write("Feedback will be given as ");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("G (Green - Correct)");
            Console.ResetColor();
            Console.Write(", ");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Y (Yellow - Wrong Position)");
            Console.ResetColor();
            Console.Write(", ");
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("X (Incorrect)");
            Console.ResetColor();
            Console.WriteLine(".");
            Console.WriteLine("The default difficulty level is 1 (Easy).");
            Console.WriteLine("---------------------------");
        }

        private static void DisplayComment(int attempt)
        {
            string comment = attempt switch
            {
                1 => "Genius!",
                2 => "Excellent!",
                3 => "Great job!",
                4 => "Good work!",
                5 => "Nice try!",
                6 => "That was close!",
                _ => "Congratulations!"
            };
            Console.WriteLine(comment);
        }


                private static void DisplayUserScores(string name, List<ScoreModel> scores)
        {
            Console.WriteLine($"\n--- SCORE HISTORY FOR {name} ---");
            if (scores.Count == 0)
            {
                Console.WriteLine("No games played yet.");
                return;
            }

            foreach (var s in scores)
            {
                // Weighted score calculation for display only
                int weightedScore = s.ScoreValue * s.Difficulty;
                Console.WriteLine($"[{s.PlayedAt:yyyy-MM-dd HH:mm}] Difficulty: {s.Difficulty} | Base Score: {s.ScoreValue} | Weighted Rank: {weightedScore}");
            }
        }

        private static void DisplayLeaderboard(List<ScoreModel> topScores)
        {
            Console.WriteLine("\n--- GLOBAL LEADERBOARD (Weighted Ranking) ---");
            if (topScores.Count == 0)
            {
                Console.WriteLine("No scores recorded yet.");
                return;
            }

            int rank = 1;
            foreach (var s in topScores)
            {
                int weightedScore = s.ScoreValue * s.Difficulty;
                Console.WriteLine($"{rank}. {s.UserName} | Weighted Score: {weightedScore} (Base: {s.ScoreValue}, Diff: {s.Difficulty})");
                rank++;
            }
        }

        public static void Main()
        {
            Console.WriteLine("Welcome to Wordle!");
            IGameService service = new GameService();
            bool running = true;

            while (running)
            {
                if (!service.IsAuthenticated)
                {
                    Console.WriteLine("\n--- AUTHENTICATION ---");
                    Console.WriteLine("1. Login");
                    Console.WriteLine("2. Register");
                    Console.WriteLine("3. Quit");
                    Console.Write("Input: ");
                    string authChoice = Console.ReadLine() ?? string.Empty;

                    switch (authChoice)
                    {
                        case "1":
                            try { service.AuthenticateUser(); }
                            catch (Exception ex) { 
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"\nLogin Failed: {ex.Message}"); 
                                Console.ResetColor();
                            }
                            break;
                        case "2":
                            try { service.RegisterUser(); }
                            catch (Exception ex) { 
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"\nRegistration Failed: {ex.Message}"); 
                                Console.ResetColor();
                            }
                            break;
                        case "3":
                            running = false;
                            Console.WriteLine("Exiting...");
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("\n--- MAIN MENU ---");
                    Console.WriteLine("1. Read Instructions");
                    Console.WriteLine("2. Start Game");
                    Console.WriteLine("3. Set Difficulty");
                    Console.WriteLine("4. Get Scores");
                    Console.WriteLine("5. Logout");
                    Console.WriteLine("6. Quit");
                    Console.Write("Input: ");
                    string choice = Console.ReadLine() ?? string.Empty;

                    switch (choice)
                    {
                        case "1":
                            ShowInstructions();
                            break;
                        case "2":
                            bool replay = true;
                            while (replay)
                            {
                                int attempt = service.StartGame();
                                DisplayComment(attempt);
                                DisplayLeaderboard(service.GetTopScores());
                                Console.Write("\nWould you like to play again right away? (Y/N): ");
                                string replayChoice = Console.ReadLine()?.Trim().ToUpper() ?? "N";
                                
                                if (replayChoice != "Y" && replayChoice != "YES")
                                {
                                    replay = false;
                                    Console.WriteLine("\nReturning to Main Menu...\n");
                                }
                            }
                            break;
                        case "3":
                            service.SetDifficulty();
                            break;
                        case "4":
                            DisplayUserScores(service.CurrentUserName, service.GetScores());
                            break;
                        case "5":
                            service.Logout();
                            Console.WriteLine("Logged out successfully.");
                            break;
                        case "6":
                            running = false;
                            Console.WriteLine("Exiting simulation...");
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                }
            }
        }
    }
}