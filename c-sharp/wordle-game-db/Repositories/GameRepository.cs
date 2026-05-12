using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WordleGame.Interfaces;
using WordleGame.Models;

namespace WordleGame.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly string _connectionString;

        public GameRepository()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
        }

        public List<string> GetWordsByDifficulty(int difficulty)
        {
            var words = new List<string>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT word_text FROM words WHERE difficulty = @diff", conn);
            cmd.Parameters.AddWithValue("diff", difficulty);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                words.Add(reader.GetString(0));
            }
            return words;
        }

        public void CreateUser(UserModel user)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO users (user_id, name, email, password_hash) VALUES (@id, @name, @email, @hash)", conn);
            cmd.Parameters.AddWithValue("id", user.UserId);
            cmd.Parameters.AddWithValue("name", user.Name);
            cmd.Parameters.AddWithValue("email", user.Email);
            cmd.Parameters.AddWithValue("hash", user.PasswordHash);

            cmd.ExecuteNonQuery();
        }

        public UserModel GetUserById(string userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM users WHERE user_id = @id", conn);
            cmd.Parameters.AddWithValue("id", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserModel
                {
                    UserId = reader.GetString(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3)
                };
            }
            return null!;
        }

        public UserModel GetUserByEmailOrName(string identifier)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM users WHERE email = @id OR name = @id", conn);
            cmd.Parameters.AddWithValue("id", identifier);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserModel
                {
                    UserId = reader.GetString(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3)
                };
            }
            return null!;
        }

        public void SaveScore(ScoreModel score)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "INSERT INTO scores (user_id, score_value, difficulty, played_at) VALUES (@uid, @val, @diff, @date)", conn);
            cmd.Parameters.AddWithValue("uid", score.UserId);
            cmd.Parameters.AddWithValue("val", score.ScoreValue);
            cmd.Parameters.AddWithValue("diff", score.Difficulty);
            cmd.Parameters.AddWithValue("date", score.PlayedAt);

            cmd.ExecuteNonQuery();
        }

        public List<ScoreModel> GetUserScores(string userId)
        {
            var scores = new List<ScoreModel>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM scores WHERE user_id = @uid ORDER BY played_at DESC", conn);
            cmd.Parameters.AddWithValue("uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scores.Add(new ScoreModel
                {
                    ScoreId = reader.GetInt32(0),
                    UserId = reader.GetString(1),
                    ScoreValue = reader.GetInt32(2),
                    Difficulty = reader.GetInt32(3),
                    PlayedAt = reader.GetDateTime(4)
                });
            }
            return scores;
        }

        public List<ScoreModel> GetTopScores(int limit)
        {
            var scores = new List<ScoreModel>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            string sql = @"
                SELECT * FROM (
                    SELECT DISTINCT ON (s.user_id) 
                        u.name as user_name, s.user_id, s.score_id, s.score_value, s.difficulty, s.played_at
                    FROM scores s
                    JOIN users u ON s.user_id = u.user_id
                    ORDER BY s.user_id, (s.score_value * s.difficulty) DESC, s.played_at DESC
                ) as unique_top_scores
                ORDER BY (score_value * difficulty) DESC
                LIMIT @limit";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                scores.Add(new ScoreModel
                {
                    ScoreId = reader.GetInt32(reader.GetOrdinal("score_id")),
                    UserId = reader.GetString(reader.GetOrdinal("user_id")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name")),
                    ScoreValue = reader.GetInt32(reader.GetOrdinal("score_value")),
                    Difficulty = reader.GetInt32(reader.GetOrdinal("difficulty")),
                    PlayedAt = reader.GetDateTime(reader.GetOrdinal("played_at"))
                });
            }
            return scores;
        }
    }
}
