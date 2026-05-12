namespace WordleGame.Interfaces
{
    public interface IFeedbackService
    {
        string GenerateFeedback(string guess, string secretWord);
    }
}
