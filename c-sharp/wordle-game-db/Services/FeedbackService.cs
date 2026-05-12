using WordleGame.Interfaces;

namespace WordleGame.Services
{
    public class FeedbackService : IFeedbackService
    {
        public string GenerateFeedback(string guess, string secretWord)
        {
            char[] feedback = new char[5];
            bool[] secretUsed = new bool[5];

            for (int i = 0; i < 5; i++)
            {
                if (guess[i] == secretWord[i])
                {
                    feedback[i] = 'G';
                    secretUsed[i] = true;
                }
            }

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
