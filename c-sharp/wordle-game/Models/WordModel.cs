using System.Collections.Generic;

namespace WordleGame.Models
{
    public class WordModel
    {
        public Dictionary<int, List<string>> WordsByDifficulty { get; private set; } = new Dictionary<int, List<string>>
        {
            { 1, new List<string> { "APPLE", "PLANT", "TRAIN", "WATER", "HOUSE", "MOUSE", "LIGHT" } }, // Easy
            { 2, new List<string> { "MANGO", "GRAPE", "BRAIN", "CHAIR", "CLOCK", "PHONE", "TABLE" } }, // Medium
            { 3, new List<string> { "FJORD", "XYLEM", "NYMPH", "CRYPT", "PIQUE", "AZURE", "KAZOO" } }  // Hard
        };
    }
}
