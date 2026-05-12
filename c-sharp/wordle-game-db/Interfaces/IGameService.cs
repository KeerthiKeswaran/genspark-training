using System.Collections.Generic;
using WordleGame.Models;

namespace WordleGame.Interfaces
{
    public interface IGameService
    {
        // Authentication State
        bool IsAuthenticated { get; }
        string CurrentUserName { get; }

        // Core Game Methods
        int StartGame();
        void SetDifficulty();
        
        // Operations (Database interaction)
        void AuthenticateUser();
        void RegisterUser();
        void StoreScores(int finalScore, int attempts, bool isWon);
        List<ScoreModel> GetScores();
        List<ScoreModel> GetTopScores();
        void Logout();
    }
}
