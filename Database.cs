using System;
using Godot;
using Microsoft.Data.Sqlite;
using System.Globalization;
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

CREATE TABLE IF NOT EXISTS User_Disabled_Questions (
    UserID     INTEGER NOT NULL,
    QuestionID INTEGER NOT NULL,
    DisabledAt TEXT DEFAULT (CURRENT_TIMESTAMP),
    PRIMARY KEY (UserID, QuestionID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (QuestionID) REFERENCES Math_Questions(MathQuestionID) ON DELETE CASCADE
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

    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetMathQuestions(string difficulty = null, int limit = 10, int userId = -1)
    {
        var questions = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // ✅ LEFT JOIN ile kullanıcının devre dışı bıraktığı soruları filtrele
                string sql = @"
                SELECT mq.MathQuestionID, mq.MathQuestion, mq.MathAnswer, mq.MathDifficulty 
                FROM Math_Questions mq
                LEFT JOIN User_Disabled_Questions udq 
                    ON mq.MathQuestionID = udq.QuestionID AND udq.UserID = @userId
                WHERE mq.IsActive = 1 AND udq.QuestionID IS NULL";

                if (!string.IsNullOrEmpty(difficulty))
                    sql += " AND mq.MathDifficulty = @difficulty";

                sql += " ORDER BY RANDOM() LIMIT @limit";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

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

                // ========================================
                // KOLAY SORULAR 
                // Tek işlem veya basit iki işlem
                // Sonuç: Tam sayı
                // ========================================
                string[] easyQuestions = new string[]
                {
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '5 + 3 = ?', '8', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '12 - 7 = ?', '5', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '9 x 9 = ?', '81', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '20 / 5 = ?', '4', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '8 + 6 = ?', '14', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '15 - 9 = ?', '6', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '6 x 7 = ?', '42', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '36 / 6 = ?', '6', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '10 + 5 - 3 = ?', '12', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '20 - 8 + 4 = ?', '16', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '3 x 5 = ?', '15', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '18 / 3 = ?', '6', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '7 + 8 = ?', '15', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '25 - 10 = ?', '15', 'Kolay');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '4 x 8 = ?', '32', 'Kolay');"
                };

                // ========================================
                // ORTA SORULAR
                // İki-üç işlem kombinasyonu
                // Işlem önceliği var (çarpma/bölme önce)
                // Sonuç: Tam sayı
                // ========================================
                string[] mediumQuestions = new string[]
                {
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '15 + 27 = ?', '42', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '100 - 37 = ?', '63', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '8 x 7 = ?', '56', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '45 / 9 = ?', '5', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '25 x 4 = ?', '100', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '3 x 6 + 12 = ?', '30', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '50 - 15 + 8 = ?', '43', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '7 x 9 - 13 = ?', '50', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '60 / 5 + 8 = ?', '20', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '12 + 18 - 5 = ?', '25', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '9 x 5 + 7 = ?', '52', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '80 - 24 + 6 = ?', '62', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '6 x 8 - 12 = ?', '36', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '72 / 8 + 15 = ?', '24', 'Orta');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '4 x 6 + 8 - 5 = ?', '27', 'Orta');"
                };

                // ========================================
                // ZOR SORULAR
                // Üç-dört işlem kombinasyonu
                // Karmaşık işlem önceliği
                // Sonuç: Tam sayı (ondalık olabilir ama bu örneklerde tam sayı)
                // ========================================
                string[] hardQuestions = new string[]
                {
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '144 / 12 = ?', '12', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '8 x 9 - 15 + 7 = ?', '64', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '120 / 6 + 18 - 9 = ?', '29', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '5 x 7 + 12 - 8 = ?', '39', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '96 / 8 + 7 x 3 = ?', '33', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '15 + 12 x 3 - 18 = ?', '33', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '100 - 6 x 8 + 12 = ?', '64', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '9 x 7 - 18 / 3 = ?', '57', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '54 / 9 + 8 x 4 = ?', '38', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '7 x 6 + 15 - 9 = ?', '48', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '88 / 11 + 5 x 7 = ?', '43', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '12 + 9 x 5 - 20 = ?', '37', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '75 - 8 x 6 + 18 = ?', '45', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '10 x 8 - 25 + 12 = ?', '67', 'Zor');",
                "INSERT OR IGNORE INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty) VALUES (1, '63 / 7 + 6 x 9 = ?', '63', 'Zor');"
                };

                // ========================================
                // TÜM SORULARI EKLE
                // ========================================
                int totalCount = 0;

                GD.Print("[DB] ========================================");
                GD.Print("[DB] 📚 Matematik soruları ekleniyor...");

                // Kolay soruları ekle
                foreach (var q in easyQuestions)
                {
                    using (var cmd = new SqliteCommand(q, connection))
                    {
                        cmd.ExecuteNonQuery();
                        totalCount++;
                    }
                }
                GD.Print($"[DB] ✅ {easyQuestions.Length} Kolay soru eklendi!");

                // Orta soruları ekle
                foreach (var q in mediumQuestions)
                {
                    using (var cmd = new SqliteCommand(q, connection))
                    {
                        cmd.ExecuteNonQuery();
                        totalCount++;
                    }
                }
                GD.Print($"[DB] ✅ {mediumQuestions.Length} Orta soru eklendi!");

                // Zor soruları ekle
                foreach (var q in hardQuestions)
                {
                    using (var cmd = new SqliteCommand(q, connection))
                    {
                        cmd.ExecuteNonQuery();
                        totalCount++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] InsertSampleMathQuestions FAILED: " + ex.Message);
        }
    }


    // ========================================
    // MATH QUESTIONS MANAGEMENT
    // ========================================

    /// <summary>
    /// Yeni matematik sorusu ekler - validasyon ve otomatik hesaplama ile
    /// </summary>
    public static bool AddMathQuestion(int userId, string question, string difficulty)
    {
        try
        {
            // ✅ 1. SORU FORMATINI KONTROL ET VE CEVABI OTOMATİK HESAPLA
            if (!ValidateMathQuestion(question, difficulty, out string calculatedAnswer, out string errorMessage))
            {
                GD.PrintErr($"[DB] ❌ Soru validasyonu başarısız: {errorMessage}");
                return false;
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // ✅ 2. SORU ZATEN VAR MI KONTROL ET
                string checkSql = "SELECT COUNT(*) FROM Math_Questions WHERE MathQuestion = @question";

                using (var cmd = new SqliteCommand(checkSql, connection))
                {
                    cmd.Parameters.AddWithValue("@question", question.Trim());
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        GD.PrintErr("[DB] ❌ Bu soru zaten mevcut!");
                        return false;
                    }
                }

                // ✅ 3. SORUYU OTOMATİK HESAPLANAN CEVAPLA EKLE
                string sql = @"
                INSERT INTO Math_Questions (UserID, MathQuestion, MathAnswer, MathDifficulty, IsActive)
                VALUES (@userId, @question, @answer, @difficulty, 1);
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@question", question.Trim());
                    cmd.Parameters.AddWithValue("@answer", calculatedAnswer); // ✅ OTOMATİK HESAPLANAN CEVAP!
                    cmd.Parameters.AddWithValue("@difficulty", difficulty);

                    cmd.ExecuteNonQuery();

                    GD.Print($"[DB] ✅ Yeni soru eklendi: {question} = {calculatedAnswer} ({difficulty})");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DB] AddMathQuestion FAILED: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Matematik sorusunu doğrular ve cevabı otomatik hesaplar
    /// </summary>
    private static bool ValidateMathQuestion(string question, string difficulty, out string calculatedAnswer, out string errorMessage)
    {
        calculatedAnswer = "";
        errorMessage = "";

        try
        {
            // ✅ SORUYU TEMIZLE
            string cleanQuestion = question.Trim().Replace(" ", "").Replace("?", "").Replace("=", "");

            // ✅ İŞLEM SAYISI SINIRLARINI BELİRLE
            int maxOperations = 0;
            switch (difficulty)
            {
                case "Kolay":
                    maxOperations = 2;
                    break;
                case "Orta":
                    maxOperations = 4;
                    break;
                case "Zor":
                    maxOperations = 6;
                    break;
                default:
                    errorMessage = "Geçersiz zorluk seviyesi!";
                    return false;
            }

            // ✅ TÜM OPERATÖRLERI VE POZİSYONLARINI BUL
            var operators = new System.Collections.Generic.List<(char op, int pos)>();

            for (int i = 1; i < cleanQuestion.Length; i++) // 1'den başla (negatif sayı için)
            {
                char c = cleanQuestion[i];
                if (c == '+' || c == '-' || c == 'x' || c == 'X' || c == '/' || c == '*')
                {
                    operators.Add((c, i));
                }
            }

            // ✅ OPERATÖR SAYISI KONTROLÜ
            if (operators.Count == 0)
            {
                errorMessage = "Soruda en az bir operatör (+, -, x, /) bulunmalı!";
                return false;
            }

            if (operators.Count > maxOperations)
            {
                errorMessage = $"{difficulty} seviyesinde en fazla {maxOperations} işlem olabilir! (Şu an: {operators.Count})";
                return false;
            }

            // ✅ SAYILARI AYIR
            var numbers = new System.Collections.Generic.List<double>();
            int lastPos = 0;

            foreach (var (op, pos) in operators)
            {
                string numStr = cleanQuestion.Substring(lastPos, pos - lastPos);

                if (!double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    errorMessage = $"Geçersiz sayı: '{numStr}'";
                    return false;
                }

                numbers.Add(num);
                lastPos = pos + 1;
            }

            // Son sayıyı ekle
            string lastNumStr = cleanQuestion.Substring(lastPos);
            if (!double.TryParse(lastNumStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lastNum))
            {
                errorMessage = $"Geçersiz sayı: '{lastNumStr}'";
                return false;
            }
            numbers.Add(lastNum);

            // ✅ OPERATÖR ÖNCELİĞİ İLE HESAPLA
            // Önce çarpma/bölme, sonra toplama/çıkarma
            double result = CalculateWithPrecedence(numbers, operators, out string precedenceError);

            if (!string.IsNullOrEmpty(precedenceError))
            {
                errorMessage = precedenceError;
                return false;
            }

            // ✅ ZORLUK SEVİYESİNE GÖRE ONDALIK KONTROL
            if (difficulty == "Kolay" || difficulty == "Orta")
            {
                // Kolay ve Orta: SADECE TAM SAYI!
                if (result != Math.Floor(result))
                {
                    errorMessage = $"{difficulty} seviyesinde cevap tam sayı olmalı! (Sonuç: {result:F2})";
                    return false;
                }

                calculatedAnswer = ((int)result).ToString(CultureInfo.InvariantCulture);
            }
            else if (difficulty == "Zor")
            {
                // Zor: EN FAZLA 1 ONDALIK BASAMAK!
                double rounded = Math.Round(result, 1);

                // 1 ondalıktan fazla basamak var mı kontrol et
                if (Math.Abs(result - rounded) > 0.0001)
                {
                    errorMessage = $"Zor seviyesinde cevap en fazla 1 ondalık basamak olabilir! (Sonuç: {result:F2})";
                    return false;
                }

                // Sonuç tam sayıysa .0 ekleme
                if (rounded == Math.Floor(rounded))
                {
                    calculatedAnswer = ((int)rounded).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    calculatedAnswer = rounded.ToString("0.0", CultureInfo.InvariantCulture);
                }
            }

            GD.Print($"[DB VALIDATION] ✅ Soru: {question} | Sonuç: {calculatedAnswer} | İşlem Sayısı: {operators.Count}");
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Validasyon hatası: {ex.Message}";
            return false;
        }
    }
    /// <summary>
    /// Kullanıcı için bir soruyu devre dışı bırakır
    /// </summary>
    public static bool DisableQuestionForUser(int userId, int questionId)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
                INSERT OR IGNORE INTO User_Disabled_Questions (UserID, QuestionID)
                VALUES (@userId, @questionId);
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@questionId", questionId);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        GD.Print($"[DB] ✅ Soru {questionId} kullanıcı {userId} için devre dışı bırakıldı!");
                        return true;
                    }
                    else
                    {
                        GD.Print($"[DB] ⚠️ Soru zaten devre dışı!");
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DB] DisableQuestionForUser FAILED: {ex.Message}");
            return false;
        }
    }

    private static double CalculateWithPrecedence(
    System.Collections.Generic.List<double> numbers,
    System.Collections.Generic.List<(char op, int pos)> operators,
    out string error)
    {
        error = "";

        try
        {
            // Kopyalarını oluştur (orijinalleri değiştirmemek için)
            var nums = new System.Collections.Generic.List<double>(numbers);
            var ops = new System.Collections.Generic.List<char>();

            foreach (var (op, _) in operators)
            {
                ops.Add(op);
            }

            // ✅ ÖNCE ÇARPMA VE BÖLME İŞLEMLERİNİ YAP
            for (int i = 0; i < ops.Count; i++)
            {
                char op = ops[i];

                if (op == 'x' || op == 'X' || op == '*' || op == '/')
                {
                    double left = nums[i];
                    double right = nums[i + 1];
                    double result = 0;

                    if (op == 'x' || op == 'X' || op == '*')
                    {
                        result = left * right;
                    }
                    else if (op == '/')
                    {
                        if (right == 0)
                        {
                            error = "Sıfıra bölme hatası!";
                            return 0;
                        }
                        result = left / right;
                    }

                    // Sonucu yerine koy
                    nums[i] = result;
                    nums.RemoveAt(i + 1);
                    ops.RemoveAt(i);
                    i--; // Aynı index'i tekrar kontrol et
                }
            }

            // ✅ SONRA TOPLAMA VE ÇIKARMA İŞLEMLERİNİ YAP
            for (int i = 0; i < ops.Count; i++)
            {
                char op = ops[i];

                if (op == '+' || op == '-')
                {
                    double left = nums[i];
                    double right = nums[i + 1];
                    double result = 0;

                    if (op == '+')
                    {
                        result = left + right;
                    }
                    else if (op == '-')
                    {
                        result = left - right;
                    }

                    // Sonucu yerine koy
                    nums[i] = result;
                    nums.RemoveAt(i + 1);
                    ops.RemoveAt(i);
                    i--; // Aynı index'i tekrar kontrol et
                }
            }

            return nums[0];
        }
        catch (Exception ex)
        {
            error = $"Hesaplama hatası: {ex.Message}";
            return 0;
        }
    }
    /// <summary>
    /// Zorluğa göre matematik sorularını getirir (kullanıcı bazlı filtreleme ile)
    /// </summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetMathQuestionsByDifficulty(string difficulty, int userId = -1)
    {
        var questions = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
                SELECT 
                    mq.MathQuestionID, 
                    mq.MathQuestion, 
                    mq.MathAnswer, 
                    mq.MathDifficulty,
                    CASE WHEN udq.QuestionID IS NOT NULL THEN 1 ELSE 0 END as IsDisabled
                FROM Math_Questions mq
                LEFT JOIN User_Disabled_Questions udq 
                    ON mq.MathQuestionID = udq.QuestionID AND udq.UserID = @userId
                WHERE mq.IsActive = 1 AND mq.MathDifficulty = @difficulty
                ORDER BY mq.MathQuestion;
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@difficulty", difficulty);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var q = new Godot.Collections.Dictionary
                        {
                            { "id", reader.GetInt32(0) },
                            { "question", reader.GetString(1) },
                            { "answer", reader.GetString(2) },
                            { "difficulty", reader.GetString(3) },
                            { "isDisabled", reader.GetInt32(4) == 1 }
                        };
                            questions.Add(q);
                        }
                    }
                }
            }

            GD.Print($"[DB] ✅ {questions.Count} soru yüklendi ({difficulty})");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DB] GetMathQuestionsByDifficulty FAILED: {ex.Message}");
        }

        return questions;
    }


    public static bool ReEnableAllQuestionsForUser(int userId)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Kullanıcının kaç tane devre dışı sorusu var?
                string countSql = "SELECT COUNT(*) FROM User_Disabled_Questions WHERE UserID = @userId";
                int disabledCount = 0;

                using (var cmd = new SqliteCommand(countSql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    disabledCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (disabledCount == 0)
                {
                    GD.Print($"[DB] ⚠️ Kullanıcı {userId} için devre dışı soru yok!");
                    return false;
                }

                // Tüm devre dışı soruları sil (yani tekrar aktif et)
                string sql = "DELETE FROM User_Disabled_Questions WHERE UserID = @userId";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    int rows = cmd.ExecuteNonQuery();

                    GD.Print($"[DB] ✅ {rows} soru tekrar aktif edildi! (Kullanıcı: {userId})");
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DB] ReEnableAllQuestionsForUser FAILED: {ex.Message}");
            return false;
        }
    }

    // ========================================
    // USER MANAGEMENT
    // ========================================
    public static Godot.Collections.Dictionary GetUserByUsername(string username)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = "SELECT UserID, UserNickName, PasswordHash FROM Users WHERE UserNickName = @username AND IsActive = 1";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Godot.Collections.Dictionary
                        {
                            { "userId", reader.GetInt32(0) },
                            { "userName", reader.GetString(1) },
                            { "passwordHash", reader.GetString(2) }
                        };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetUserByUsername FAILED: " + ex.Message);
        }

        return null;
    }

    public static int CreateUser(string username, string password)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Email alanı dummy - gerçekte email de istenebilir
                string email = $"{username}@game.local";

                string sql = @"
                INSERT INTO Users (UserNickName, Email, PasswordHash) 
                VALUES (@username, @email, @password);
                SELECT last_insert_rowid();
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password); // ⚠️ Normalde hash gerekir!

                    object result = cmd.ExecuteScalar();
                    int newUserId = Convert.ToInt32(result);

                    GD.Print($"[DB] ✅ Kullanıcı oluşturuldu: {username} (ID: {newUserId})");
                    return newUserId;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] CreateUser FAILED: " + ex.Message);
            return -1;
        }
    }
    // ========================================
    // LEVEL MANAGEMENT
    // ========================================
    public static void InsertLevels()
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
                INSERT OR IGNORE INTO Levels (LevelID, LevelName, LevelDescription, MinimumScoreRequirement) 
                VALUES 
                (0, 'Tutorial', 'Tutorial Level', 10),
                (1, 'Level 1', 'Birinci seviye', 100),
                (2, 'Level 2', 'İkinci seviye', 100),
                (3, 'Level 3', 'Üçüncü seviye', 150),
                (4, 'Level 4', 'Dördüncü seviye', 200);
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                GD.Print("[DB] ✅ Leveller eklendi/güncellendi! (0-4)");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] InsertLevels FAILED: " + ex.Message);
        }
    }

    // ========================================
    // SCORE SAVE
    // ========================================
    public static bool SaveScore(int userId, int levelId, int score, int completionTime = 0)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // 1. Scores tablosuna kaydet (UNIQUE - üzerine yazar)
                string sql = @"
                INSERT INTO Scores (UserID, LevelID, Score, CompletionTime, AchievedDate)
                VALUES (@userId, @levelId, @score, @time, CURRENT_TIMESTAMP)
                ON CONFLICT(UserID, LevelID) DO UPDATE SET
                    Score = @score,
                    CompletionTime = @time,
                    AchievedDate = CURRENT_TIMESTAMP
                WHERE Score < @score;
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@levelId", levelId);
                    cmd.Parameters.AddWithValue("@score", score);
                    cmd.Parameters.AddWithValue("@time", completionTime);
                    cmd.ExecuteNonQuery();
                }

                // 2. ScoreHistory'ye de ekle (her zaman)
                sql = @"
                INSERT INTO ScoreHistory (UserID, LevelID, Score, CompletionTime)
                VALUES (@userId, @levelId, @score, @time);
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@levelId", levelId);
                    cmd.Parameters.AddWithValue("@score", score);
                    cmd.Parameters.AddWithValue("@time", completionTime);
                    cmd.ExecuteNonQuery();
                }

                // 3. TotalScore güncelle
                sql = @"
                UPDATE Users 
                SET TotalScore = (SELECT SUM(Score) FROM Scores WHERE UserID = @userId)
                WHERE UserID = @userId;
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.ExecuteNonQuery();
                }

                GD.Print($"[DB] ✅ Skor kaydedildi: User={userId}, Level={levelId}, Score={score}");
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] SaveScore FAILED: " + ex.Message);
            return false;
        }
    }

    // ========================================
    // GET SCOREBOARD DATA
    // ========================================
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetScoreboard()
    {
        var scoreboard = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Tüm kullanıcıları ve skorlarını çek
                string sql = @"
                SELECT 
                    u.UserID,
                    u.UserNickName,
                    u.TotalScore,
                    GROUP_CONCAT(l.LevelName || ':' || COALESCE(s.Score, 0), '|') as LevelScores
                FROM Users u
                CROSS JOIN Levels l
                LEFT JOIN Scores s ON u.UserID = s.UserID AND l.LevelID = s.LevelID
                WHERE u.IsActive = 1
                GROUP BY u.UserID
                ORDER BY u.TotalScore DESC;
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entry = new Godot.Collections.Dictionary
                        {
                            { "userId", reader.GetInt32(0) },
                            { "userName", reader.GetString(1) },
                            { "totalScore", reader.GetInt32(2) },
                            { "levelScores", reader.IsDBNull(3) ? "" : reader.GetString(3) }
                        };
                            scoreboard.Add(entry);
                        }
                    }
                }
            }

            GD.Print($"[DB] ✅ Scoreboard yüklendi: {scoreboard.Count} kullanıcı");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetScoreboard FAILED: " + ex.Message);
        }

        return scoreboard;
    }

    // ========================================
    // GET USER SCORES (Detaylı)
    // ========================================
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetUserScores(int userId)
    {
        var scores = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = @"
                SELECT l.LevelName, COALESCE(s.Score, 0) as Score, s.CompletionTime, s.AchievedDate
                FROM Levels l
                LEFT JOIN Scores s ON l.LevelID = s.LevelID AND s.UserID = @userId
                WHERE l.IsActive = 1
                ORDER BY l.LevelID;
            ";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var score = new Godot.Collections.Dictionary
                        {
                            { "levelName", reader.GetString(0) },
                            { "score", reader.GetInt32(1) },
                            { "time", reader.IsDBNull(2) ? 0 : reader.GetInt32(2) },
                            { "date", reader.IsDBNull(3) ? "" : reader.GetString(3) }
                        };
                            scores.Add(score);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetUserScores FAILED: " + ex.Message);
        }

        return scores;
    }
    public static bool HasUserCompletedLevel(int userId, int levelId)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Scores tablosunda bu kullanıcının bu level için skoru var mı?
                string sql = "SELECT COUNT(*) FROM Scores WHERE UserID = @userId AND LevelID = @levelId";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@levelId", levelId);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;  // ✅ Skor varsa tamamlanmış!
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] HasUserCompletedLevel FAILED: " + ex.Message);
            return false;
        }
    }
    public static Godot.Collections.Array<int> GetUserCompletedLevels(int userId)
    {
        var completedLevels = new Godot.Collections.Array<int>();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = "SELECT LevelID FROM Scores WHERE UserID = @userId ORDER BY LevelID";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            completedLevels.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            GD.Print($"[DB] ✅ Kullanıcı {userId} için {completedLevels.Count} level tamamlanmış");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetUserCompletedLevels FAILED: " + ex.Message);
        }

        return completedLevels;
    }
    public static Godot.Collections.Dictionary GetUserByUserId(int userId)
    {
        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string sql = "SELECT UserID, UserNickName, PasswordHash FROM Users WHERE UserID = @userId AND IsActive = 1";

                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Godot.Collections.Dictionary
                        {
                            { "userId", reader.GetInt32(0) },
                            { "userName", reader.GetString(1) },
                            { "passwordHash", reader.GetString(2) }
                        };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DB] GetUserByUserId FAILED: " + ex.Message);
        }

        return null;
    }


}
