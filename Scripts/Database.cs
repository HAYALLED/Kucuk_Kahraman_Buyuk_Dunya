using System;
using Godot;
using Microsoft.Data.Sqlite;

public static class Database
{
    private static string _connectionString;

    public static void Init()
    {
        try
        {
            string userDir = ProjectSettings.GlobalizePath("user://");
            string dbPath = System.IO.Path.Combine(userDir, "game_data.db");
            _connectionString = $"Data Source={dbPath};";

            GD.Print($"[DB] Using file: {dbPath}");

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users (
    UserID        INTEGER PRIMARY KEY AUTOINCREMENT,
    UserNickName  TEXT    NOT NULL,
    Email         TEXT    NOT NULL,
    PasswordHash  TEXT    NOT NULL,
    CreatedDate   TEXT    DEFAULT (CURRENT_TIMESTAMP),
    LastLoginDate TEXT,
    IsActive      INTEGER DEFAULT 1,
    TotalScore    INTEGER DEFAULT 0,
    UNIQUE (UserNickName),
    UNIQUE (Email),
    CHECK (Email LIKE '%@%.%')
);

CREATE TABLE IF NOT EXISTS Levels (
    LevelID                INTEGER PRIMARY KEY AUTOINCREMENT,
    LevelName              TEXT    NOT NULL,
    LevelDescription       TEXT,
    MinimumScoreRequirement INTEGER NOT NULL DEFAULT 0,
    IsActive               INTEGER DEFAULT 1,
    UNIQUE (LevelName)
);

CREATE TABLE IF NOT EXISTS Math_Questions (
    MathQuestionID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID         INTEGER NOT NULL,
    MathQuestion   TEXT,
    MathAnswer     TEXT,
    MathDifficulty TEXT,
    IsActive       INTEGER DEFAULT 1,
    UNIQUE (MathQuestion),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    CHECK (MathDifficulty IN ('Zor', 'Orta', 'Kolay'))
);

CREATE TABLE IF NOT EXISTS Scores (
    ScoreID        INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID         INTEGER NOT NULL,
    LevelID        INTEGER NOT NULL,
    Score          INTEGER NOT NULL,
    CompletionTime INTEGER,
    AchievedDate   TEXT DEFAULT (CURRENT_TIMESTAMP),
    UNIQUE (UserID, LevelID),
    CHECK (Score >= 0),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (LevelID) REFERENCES Levels(LevelID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ScoreHistory (
    ScoreHistoryID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserID         INTEGER NOT NULL,
    LevelID        INTEGER NOT NULL,
    Score          INTEGER NOT NULL,
    CompletionTime INTEGER,
    AchievedDate   TEXT DEFAULT (CURRENT_TIMESTAMP),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (LevelID) REFERENCES Levels(LevelID) ON DELETE CASCADE
);
";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            GD.Print("[DB] Init finished OK");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] Init FAILED: " + ex.Message);
        }
    }

    public static bool HealthCheck()
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    var result = cmd.ExecuteScalar();
                    bool ok = result != null && result.ToString() == "Users";

                    if (!ok)
                        GD.PrintErr("[DB] HealthCheck: Users table NOT found");
                    else
                        GD.Print("[DB] HealthCheck: OK (Users table exists)");

                    return ok;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] HealthCheck FAILED: " + ex.Message);
            return false;
        }
    }

    // Database.cs'in sonuna ekle (closing brace'den önce)

    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetMathQuestions(string difficulty = null, int limit = 10)
    {
        var questions = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = "SELECT MathQuestionID, MathQuestion, MathAnswer, MathDifficulty FROM Math_Questions WHERE IsActive = 1";

                if (!string.IsNullOrEmpty(difficulty))
                    sql += " AND MathDifficulty = @difficulty";

                sql += " ORDER BY RANDOM() LIMIT @limit";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(difficulty))
                        cmd.Parameters.AddWithValue("@difficulty", difficulty);
                    cmd.Parameters.AddWithValue("@limit", limit);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var q = new Godot.Collections.Dictionary
                        {
                            { "id", reader.GetInt32(0) },
                            { "question", reader.GetString(1) },
                            { "answer", reader.GetString(2) },
                            { "difficulty", reader.GetString(3) }
                        };
                            questions.Add(q);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetMathQuestions FAILED: " + ex.Message);
        }

        return questions;
    }

    public static int GetMathQuestionCount()
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT COUNT(*) FROM Math_Questions WHERE IsActive = 1";
                using (var cmd = new SqliteCommand(sql, connection))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetMathQuestionCount FAILED: " + ex.Message);
            return 0;
        }
    }

    // Database.cs'in sonuna ekle (GetMathQuestionCount'dan sonra)

    public static void InsertSampleMathQuestions()
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Önce test kullanıcısı oluştur (yoksa)
                string createUser = @"
                INSERT OR IGNORE INTO Users (UserID, UserNickName, Email, PasswordHash) 
                VALUES (1, 'Teacher', 'teacher@school.com', 'hash123');
            ";
                using (var cmd = new SqliteCommand(createUser, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Örnek sorular ekle
                string[] questions = new string[]
                {
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '5 + 3 = ?', '8', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '12 - 7 = ?', '5', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '4 x 6 = ?', '24', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '45 / 9 = ?', '5', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '15 + 27 = ?', '42', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '8 x 7 = ?', '56', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '100 - 37 = ?', '63', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '9 x 9 = ?', '81', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '144 / 12 = ?', '12', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '25 x 4 = ?', '100', 'Orta');"
                };

                foreach (var q in questions)
                {
                    using (var cmd = new SqliteCommand(q, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                GD.Print("[DB] Örnek matematik soruları eklendi!");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] InsertSampleMathQuestions FAILED: " + ex.Message);
        }
    }

}
