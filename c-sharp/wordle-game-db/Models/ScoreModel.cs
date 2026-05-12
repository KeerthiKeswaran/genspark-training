using System;

namespace WordleGame.Models
{
    public class ScoreModel
    {
        public int ScoreId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int ScoreValue { get; set; }
        public int Difficulty { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }
}
