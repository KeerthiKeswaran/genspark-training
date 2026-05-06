using System;

namespace WordleGame.Services
{
    public partial class GameService
    {
        public void ShowInstructions()
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
            
            Console.WriteLine("---------------------------");
        }

        private void DisplayComment(int attempt)
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

        public void CalculateAndPrintScore(int attempt, bool isWon)
        {
            if (!isWon)
            {
                Console.WriteLine("\nFinal Score: 0 / 100");
                return;
            }

            // Using double to prevent integer division resulting in 0
            // Formula: ( (MaxAttempts - attempt + 1) / MaxAttempts ) * 100
            int maxAttempts = Models.GameModel.MaxAttempts;
            int score = (int)(((maxAttempts - attempt + 1) / (double)maxAttempts) * 100);
            
            Console.WriteLine($"\nFinal Score: {score} / 100");
        }
    }
}
