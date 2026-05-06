using System;
using WordleGame.Services;
using WordleGame.Interfaces;

namespace WordleGame.App{
    public class Program{
        public static void Main(){
            Console.WriteLine("Welcome to Wordle!");
            IGameService service = new GameService();
            bool handleLoop = true;
            while(handleLoop){
                Console.WriteLine("Choose the options from the menu to proceed:");
                Console.WriteLine("1. Read Instructions");
                Console.WriteLine("2. Start Game");
                Console.WriteLine("3. Set Difficulty");
                Console.WriteLine("4. Quit");
                Console.Write("Input: ");
                string choice = Console.ReadLine() ?? string.Empty;

                switch(choice)
                {
                    case "1":
                        service.ShowInstructions();
                        break;
                    case "2":
                        bool replay = true;
                        while (replay)
                        {
                            service.StartGame();
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
                        Console.WriteLine("Exiting simulation...");
                        handleLoop = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
                
            }

        }
    }
}