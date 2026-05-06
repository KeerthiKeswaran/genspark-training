using System;
using WordleGame.Models;
using WordleGame.Interfaces;

namespace WordleGame.Services
{
    public class WordService : IWordService
    {
        private readonly WordModel _wordModel;
        private readonly Random _random;
        
        public WordService()
        {
            _wordModel = new WordModel();
            _random = new Random();
        }
        
        public string GetRandomWord(int difficulty)
        {
            var wordList = _wordModel.WordsByDifficulty[difficulty];
            int index = _random.Next(wordList.Count);
            return wordList[index];
        }
    }
}
