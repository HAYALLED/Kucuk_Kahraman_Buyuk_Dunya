using Godot;
using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;

public partial class UserProfile : Node
{
    private const string SETTINGS_PATH = "user://settings.json";

    // Singleton
    private static UserProfile instance;
    public static UserProfile Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UserProfile();
                instance.LoadSettings();
            }
            return instance;
        }
    }

    // ===== KULLANICI BİLGİLERİ =====
    public int CurrentUserID { get; private set; } = -1;
    public string CurrentUserName { get; private set; } = "Misafir";
    public bool IsLoggedIn => CurrentUserID > 0;

    // ===== OYUN AYARLARI =====
    public string Difficulty { get; set; } = "Orta";

    // ===== EKRAN AYARLARI =====
    public bool IsFullscreen { get; set; } = false;
    public Vector2I Resolution { get; set; } = new Vector2I(1280, 720);
    public bool VSync { get; set; } = true;

    // ===== SES AYARLARI =====
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SFXVolume { get; set; } = 1.0f;

    public UserProfile()
    {
        LoadSettings();
    }

    // ========================================
    // ŞİFRE HASH (Güvenlik için)
    // ========================================
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
    public void RestoreSession(int userId, string userName)
    {
        CurrentUserID = userId;
        CurrentUserName = userName;

        GD.Print($"[USER PROFILE] ✅ Oturum geri yüklendi: {userName} (ID: {userId})");
        GD.Print($"[USER PROFILE] IsLoggedIn durumu: {IsLoggedIn}");
    }
    // ========================================
    // KAYIT OL (Create Account)
    // ========================================
    public bool CreateAccount(string userName, string password)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            GD.PrintErr("[PROFILE] Kullanıcı adı veya şifre boş!");
            return false;
        }

        if (password.Length < 4)
        {
            GD.PrintErr("[PROFILE] Şifre en az 4 karakter olmalı!");
            return false;
        }

        try
        {
            string connStr = GetConnectionString();
            using (var connection = new SqliteConnection(connStr))
            {
                connection.Open();

                // Kullanıcı adı zaten var mı?
                string checkSql = "SELECT COUNT(*) FROM Users WHERE UserNickName = @name";
                using (var cmd = new SqliteCommand(checkSql, connection))
                {
                    cmd.Parameters.AddWithValue("@name", userName);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        GD.PrintErr("[PROFILE] Bu kullanıcı adı zaten alınmış!");
                        return false;
                    }
                }

                // Yeni kullanıcı oluştur
                string hashedPassword = HashPassword(password);
                string insertSql = @"
                    INSERT INTO Users (UserNickName, Email, PasswordHash) 
                    VALUES (@name, @email, @hash);
                    SELECT last_insert_rowid();";

                using (var cmd = new SqliteCommand(insertSql, connection))
                {
                    cmd.Parameters.AddWithValue("@name", userName);
                    cmd.Parameters.AddWithValue("@email", $"{userName}@local.com");
                    cmd.Parameters.AddWithValue("@hash", hashedPassword);

                    CurrentUserID = Convert.ToInt32(cmd.ExecuteScalar());
                    CurrentUserName = userName;

                    GD.Print($"[PROFILE] ✅ Hesap oluşturuldu: {userName} (ID: {CurrentUserID})");
                    SaveSettings();
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PROFILE] Hesap oluşturma hatası: {ex.Message}");
            return false;
        }
    }

    // ========================================
    // GİRİŞ YAP (Login)
    // ========================================
    public bool Login(string userName, string password)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            GD.PrintErr("[PROFILE] Kullanıcı adı veya şifre boş!");
            return false;
        }

        try
        {
            string connStr = GetConnectionString();
            using (var connection = new SqliteConnection(connStr))
            {
                connection.Open();

                string hashedPassword = HashPassword(password);
                string loginSql = "SELECT UserID, UserNickName FROM Users WHERE UserNickName = @name AND PasswordHash = @hash";

                using (var cmd = new SqliteCommand(loginSql, connection))
                {
                    cmd.Parameters.AddWithValue("@name", userName);
                    cmd.Parameters.AddWithValue("@hash", hashedPassword);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentUserID = reader.GetInt32(0);
                            CurrentUserName = reader.GetString(1);

                            // Last login güncelle
                            UpdateLastLogin();

                            GD.Print($"[PROFILE] ✅ Giriş başarılı: {CurrentUserName} (ID: {CurrentUserID})");
                            SaveSettings();
                            return true;
                        }
                        else
                        {
                            GD.PrintErr("[PROFILE] ❌ Kullanıcı adı veya şifre hatalı!");
                            return false;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PROFILE] Giriş hatası: {ex.Message}");
            return false;
        }
    }

    // ========================================
    // ÇIKIŞ YAP (Logout)
    // ========================================
    public void Logout()
    {
        CurrentUserID = -1;
        CurrentUserName = "Misafir";
        SaveSettings();
        GD.Print("[PROFILE] Çıkış yapıldı (Misafir modu)");
    }

    // ========================================
    // LAST LOGIN GÜNCELLE
    // ========================================
    private void UpdateLastLogin()
    {
        try
        {
            string connStr = GetConnectionString();
            using (var connection = new SqliteConnection(connStr))
            {
                connection.Open();
                string sql = "UPDATE Users SET LastLoginDate = CURRENT_TIMESTAMP WHERE UserID = @id";
                using (var cmd = new SqliteCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@id", CurrentUserID);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PROFILE] LastLogin güncelleme hatası: {ex.Message}");
        }
    }

    // ========================================
    // AYARLAR - JSON KAYIT/YÜKLEME
    // ========================================

    public void SaveSettings()
    {
        var settings = new Godot.Collections.Dictionary
        {
            { "userID", CurrentUserID },
            { "userName", CurrentUserName },
            { "difficulty", Difficulty },
            { "fullscreen", IsFullscreen },
            { "resolutionX", Resolution.X },
            { "resolutionY", Resolution.Y },
            { "vsync", VSync },
            { "masterVolume", MasterVolume },
            { "musicVolume", MusicVolume },
            { "sfxVolume", SFXVolume }
        };

        var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(Json.Stringify(settings));
            file.Close();
            GD.Print("[PROFILE] Ayarlar kaydedildi!");
        }
    }

    public void LoadSettings()
    {
        if (!FileAccess.FileExists(SETTINGS_PATH))
        {
            GD.Print("[PROFILE] Ayar dosyası bulunamadı, varsayılanlar kullanılıyor.");
            return;
        }

        var file = FileAccess.Open(SETTINGS_PATH, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            string jsonString = file.GetAsText();
            file.Close();

            var settings = Json.ParseString(jsonString).AsGodotDictionary();

            CurrentUserID = settings.ContainsKey("userID") ? (int)settings["userID"] : -1;
            CurrentUserName = settings.ContainsKey("userName") ? (string)settings["userName"] : "Misafir";
            Difficulty = settings.ContainsKey("difficulty") ? (string)settings["difficulty"] : "Orta";
            IsFullscreen = settings.ContainsKey("fullscreen") ? (bool)settings["fullscreen"] : false;

            int resX = settings.ContainsKey("resolutionX") ? (int)settings["resolutionX"] : 1280;
            int resY = settings.ContainsKey("resolutionY") ? (int)settings["resolutionY"] : 720;
            Resolution = new Vector2I(resX, resY);

            VSync = settings.ContainsKey("vsync") ? (bool)settings["vsync"] : true;
            MasterVolume = settings.ContainsKey("masterVolume") ? (float)(double)settings["masterVolume"] : 1.0f;
            MusicVolume = settings.ContainsKey("musicVolume") ? (float)(double)settings["musicVolume"] : 0.8f;
            SFXVolume = settings.ContainsKey("sfxVolume") ? (float)(double)settings["sfxVolume"] : 1.0f;

            GD.Print($"[PROFILE] Ayarlar yüklendi! Kullanıcı: {CurrentUserName}");
        }
    }

    public void ApplyDisplaySettings()
    {
        // ✅ 1. WINDOW MODE'U AYARLA
        if (IsFullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            GD.Print("[PROFILE] 🖥️ Fullscreen mode aktif");
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            GD.Print("[PROFILE] 🖥️ Windowed mode aktif");
        }

        // ✅ 2. RESOLUTION'U HER DURUMDA SET ET
        // Fullscreen'dayken de resolution ayarlanır - windowed'a geçince bu değer kullanılır
        DisplayServer.WindowSetSize(Resolution);
        GD.Print($"[PROFILE] 📐 Resolution ayarlandı: {Resolution.X}x{Resolution.Y}");

        // ✅ 3. VSYNC'İ HER DURUMDA UYGULA
        if (VSync)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
            GD.Print("[PROFILE] 🔄 VSync: ENABLED");
        }
        else
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            GD.Print("[PROFILE] 🔄 VSync: DISABLED");
        }

        GD.Print($"[PROFILE] ✅ Tüm ekran ayarları uygulandı!");
    }

    public void ApplyAudioSettings()
    {
        GD.Print($"[PROFILE] Ses ayarları: Master={MasterVolume}, Music={MusicVolume}, SFX={SFXVolume}");
    }

    private string GetConnectionString()
    {
        string userDir = ProjectSettings.GlobalizePath("user://");
        string dbPath = System.IO.Path.Combine(userDir, "game_data.db");
        return $"Data Source={dbPath};";
    }

    public void ResetAllProgress()
    {
        SaveGame.Instance.ResetAllLevels();
        GD.Print("[PROFILE] Tüm ilerleme sıfırlandı!");
    }
}