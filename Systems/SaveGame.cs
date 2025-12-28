using Godot;
using System.Collections.Generic;

public partial class SaveGame : Node
{
    private const string SAVE_PATH = "user://save_game.json";

    // Singleton pattern
    private static SaveGame instance;
    public static SaveGame Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SaveGame();
            }
            return instance;
        }
    }

    private int currentUserId = -1;

    // Level tamamlanma durumları (backward compatibility)
    private Dictionary<string, bool> completedLevels = new Dictionary<string, bool>();

    public SaveGame()
    {
        LoadGame();
    }

    // ========================================
    // USER ID MANAGEMENT
    // ========================================

    public void SetCurrentUserId(int userId)
    {
        currentUserId = userId;
        SaveToFile(); // ✅ Değişikliği kaydet!
        GD.Print($"[SAVE GAME] ✅ Aktif kullanıcı ID: {userId}");
    }

    public int GetCurrentUserId()
    {
        return currentUserId;
    }

    public void ClearCurrentUserId()
    {
        currentUserId = -1;
        SaveToFile(); // ✅ Değişikliği kaydet!
        GD.Print("[SAVE GAME] Kullanıcı çıkış yaptı, ID sıfırlandı.");
    }

    // ========================================
    // LEVEL COMPLETION
    // ========================================

    public void MarkLevelCompleted(string levelName)
    {
        if (currentUserId <= 0)
        {
            GD.PrintErr("[SAVE GAME] ❌ Kullanıcı ID geçersiz! Level kaydedilemedi.");
            return;
        }

        int levelId = GetLevelIdFromName(levelName);

        if (levelId < 0)
        {
            GD.PrintErr($"[SAVE GAME] ❌ Geçersiz level name: {levelName}");
            return;
        }

        completedLevels[levelName] = true;
        SaveToFile();

        GD.Print($"[SAVE GAME] ✅ {levelName} tamamlandı olarak kaydedildi!");
    }

    public bool IsLevelCompleted(string levelName)
    {
        if (currentUserId <= 0)
        {
            GD.PrintErr("[SAVE GAME] ❌ Kullanıcı ID geçersiz!");
            return false;
        }

        int levelId = GetLevelIdFromName(levelName);

        if (levelId < 0)
        {
            return false;
        }

        // ✅ DATABASE'DEN KONTROL ET!
        bool completed = Database.HasUserCompletedLevel(currentUserId, levelId);

        GD.Print($"[SAVE GAME] Level {levelName} (ID: {levelId}) tamamlanma durumu: {completed}");

        return completed;
    }

    private int GetLevelIdFromName(string levelName)
    {
        switch (levelName.ToLower())
        {
            case "tutorial":
                return 0;
            case "level_1":
            case "level1":
                return 1;
            case "level_2":
            case "level2":
                return 2;
            case "level_3":
            case "level3":
                return 3;
            case "level_4":
            case "level4":
                return 4;
            default:
                return -1;
        }
    }

    public void ResetAllLevels()
    {
        completedLevels.Clear();
        SaveToFile();
        GD.Print("[SAVE GAME] Tüm levellar sıfırlandı!");
    }

    private void SaveToFile()
    {
        var saveData = new Godot.Collections.Dictionary();

        saveData["currentUserId"] = currentUserId;

        foreach (var kvp in completedLevels)
        {
            saveData[kvp.Key] = kvp.Value;
        }

        var file = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(Json.Stringify(saveData));
            file.Close();
            GD.Print($"[SAVE GAME] Dosyaya kaydedildi! UserID: {currentUserId}");
        }
    }

    private void LoadGame()
    {
        if (!FileAccess.FileExists(SAVE_PATH))
        {
            GD.Print("[SAVE GAME] Kayıt dosyası bulunamadı, yeni dosya oluşturulacak.");
            return;
        }

        var file = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
        if (file != null)
        {
            string jsonString = file.GetAsText();
            file.Close();

            var saveData = Json.ParseString(jsonString).AsGodotDictionary();

            // Kullanıcı ID'sini yükle
            if (saveData.ContainsKey("currentUserId"))
            {
                currentUserId = (int)saveData["currentUserId"];
                GD.Print($"[SAVE GAME] Son kullanıcı ID yüklendi: {currentUserId}");
            }

            completedLevels.Clear();
            foreach (var key in saveData.Keys)
            {
                if (key.ToString() != "currentUserId")
                {
                    completedLevels[key.ToString()] = (bool)saveData[key];
                }
            }

            GD.Print($"[SAVE GAME] Kayıt yüklendi! UserID: {currentUserId}");
        }
    }
}