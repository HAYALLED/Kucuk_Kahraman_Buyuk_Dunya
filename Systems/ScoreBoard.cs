using Godot;
using System;

public partial class ScoreBoard : Control
{
    private ScrollContainer scrollContainer;
    private VBoxContainer scoreList;
    private Label titleLabel;
    private TextureButton backButton;
    private Button refreshButton;

    public override void _Ready()
    {
        GD.Print("[SCOREBOARD] ========== BAŞLATILIYOR ==========");

        Database.Init();
        Database.InsertLevels();
        Database.InsertSampleMathQuestions();

        CreateUI();
        LoadScoreboard();

        GD.Print("[SCOREBOARD] ========== HAZIR ==========");
    }

    private void CreateUI()
    {
        // ===== NODE BAĞLANTILARI =====
        titleLabel = GetNode<Label>("TitleLabel");
        scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
        scoreList = GetNode<VBoxContainer>("ScrollContainer/ScoreList");
        refreshButton = GetNode<Button>("RefreshButton");
        backButton = GetNode<TextureButton>("BackButton");

        // ===== SİGNAL BAĞLANTILARI =====
        refreshButton.Pressed += LoadScoreboard;
        backButton.Pressed += OnBackPressed;

        GD.Print("[SCOREBOARD] ✅ UI bağlandı!");
    }

    private void LoadScoreboard()
    {
        GD.Print("[SCOREBOARD] 📊 Skorlar yükleniyor...");

        foreach (Node child in scoreList.GetChildren())
        {
            child.QueueFree();
        }

        var scoreboard = Database.GetScoreboard();

        if (scoreboard.Count == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = "❌ Henüz kayıtlı skor yok!";
            emptyLabel.AddThemeColorOverride("font_color", Colors.Gray);
            emptyLabel.AddThemeFontSizeOverride("font_size", 20);
            scoreList.AddChild(emptyLabel);
            return;
        }

        var headerPanel = CreateScorePanel(
            "SIRA", "KULLANICI", "LEVEL 1", "LEVEL 2", "TOPLAM",
            Colors.DarkGoldenrod, 24
        );
        scoreList.AddChild(headerPanel);

        int rank = 1;
        foreach (var entry in scoreboard)
        {
            string userName = (string)entry["userName"];
            int totalScore = (int)entry["totalScore"];
            string levelScoresStr = (string)entry["levelScores"];

            int level1Score = 0;
            int level2Score = 0;

            if (!string.IsNullOrEmpty(levelScoresStr))
            {
                string[] levels = levelScoresStr.Split('|');
                foreach (string level in levels)
                {
                    string[] parts = level.Split(':');
                    if (parts.Length == 2)
                    {
                        string levelName = parts[0].Trim();
                        int score = int.Parse(parts[1]);

                        if (levelName == "Level 1")
                            level1Score = score;
                        else if (levelName == "Level 2")
                            level2Score = score;
                    }
                }
            }

            Color rowColor = rank switch
            {
                1 => new Color(1.0f, 0.84f, 0.0f),
                2 => new Color(0.75f, 0.75f, 0.75f),
                3 => new Color(0.8f, 0.5f, 0.2f),
                _ => Colors.White
            };

            var panel = CreateScorePanel(
                $"{rank}.",
                userName,
                level1Score > 0 ? $"{level1Score}p" : "-",
                level2Score > 0 ? $"{level2Score}p" : "-",
                $"{totalScore}p",
                rowColor,
                18
            );

            scoreList.AddChild(panel);
            rank++;
        }

        GD.Print($"[SCOREBOARD] ✅ {scoreboard.Count} kullanıcı yüklendi!");
    }

    private PanelContainer CreateScorePanel(
        string rank, string userName, string level1, string level2, string total,
        Color textColor, int fontSize)
    {
        var panel = new PanelContainer();
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        styleBox.BorderWidthBottom = 1;
        styleBox.BorderColor = new Color(0.3f, 0.3f, 0.3f);
        panel.AddThemeStyleboxOverride("panel", styleBox);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(hbox);

        hbox.AddChild(CreateLabel(rank, 80, textColor, fontSize));
        hbox.AddChild(CreateLabel(userName, 250, textColor, fontSize));
        hbox.AddChild(CreateLabel(level1, 150, textColor, fontSize));
        hbox.AddChild(CreateLabel(level2, 150, textColor, fontSize));
        hbox.AddChild(CreateLabel(total, 150, new Color(0.2f, 1.0f, 0.2f), fontSize + 2));

        return panel;
    }

    private Label CreateLabel(string text, int minWidth, Color color, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        label.CustomMinimumSize = new Vector2(minWidth, 0);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private void OnBackPressed()
    {
        GD.Print("[SCOREBOARD] 🚪 Geri tuşuna basıldı!");
        GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
    }
}