using System.Collections.Generic;
using WordleGame.Models;

namespace WordleGame.Interfaces
{
    public interface IGameRepository
    {
        List<string> GetWordsByDifficulty(int difficulty);
        void CreateUser(UserModel user);
        UserModel GetUserById(string userId);
        UserModel GetUserByEmailOrName(string identifier);
        void SaveScore(ScoreModel score);
        List<ScoreModel> GetUserScores(string userId);
        List<ScoreModel> GetTopScores(int limit);
    }
}
