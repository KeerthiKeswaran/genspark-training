using WordleGame.Interfaces;

namespace WordleGame.Services
{
    public class FeedbackService : IFeedbackService
    {
        public string GenerateFeedback(string guess, string secretWord)
        {
            char[] feedback = new char[5];
            bool[] secretUsed = new bool[5];

            // First pass: Check for exact matches (G)
            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == secretWord[i])
                {
                    feedback[i] = 'G';
                    secretUsed[i] = true;
                }
            }

            // Second pass: Check for correct letters in wrong positions (Y) and wrong letters (X)
            for (int i = 0; i < 5; i++)
            {
                if (feedback[i] == 'G') continue;

                bool found = false;
                for (int j = 0; j < 5; j++)
                {
                    if (!secretUsed[j] && guess[i] == secretWord[j])
                    {
                        feedback[i] = 'Y';
                        secretUsed[j] = true;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    feedback[i] = 'X';
                }
            }
            return new string(feedback);
        }
    }
}
