namespace WordleGame.Interfaces
{
    public interface IGameService
    {
        void StartGame();
        void ShowInstructions();
        void SetDifficulty();
        void CalculateAndPrintScore(int attempt, bool isWon);
    }
}
