using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class info_teacher : Area2D
{
    private bool playerInRange = false;
    private Node2D player;

    // UI Elementleri
    private Label interactionLabel;
    private Panel dialogBox;
    private Label dialogText;

    // Mesaj sistemi
    private bool firstMessageShown = false;
    private List<string> remainingMessages; // Henüz gösterilmemiş mesajlar
    private Random random = new Random();
    //Dialog kontrol flag'leri
    private bool isDialogActive = false;  // Dialog gösterilirken true
    private bool isTypewriting = false;    // Yazma efekti devam ederken true
    private bool justEntered = false;      // Yeni girişi algılamak için

    // ✅ DİĞER MESAJLAR - RANDOM SIRADA GELİR! (çeviri anahtarları)
    private string[] messages = new string[]
    {
        // Kostümler
        "TEACHER_MSG_01",
        "TEACHER_MSG_02",
        "TEACHER_MSG_03",
        // Düşmanlar
        "TEACHER_MSG_04",
        "TEACHER_MSG_05",
        "TEACHER_MSG_06",
        "TEACHER_MSG_07",
        "TEACHER_MSG_08",
        "TEACHER_MSG_09",
        "TEACHER_MSG_10",
        "TEACHER_MSG_11",
        "TEACHER_MSG_12",
        "TEACHER_MSG_13",
        // Gameplay bilgileri
        "TEACHER_MSG_14",
        "TEACHER_MSG_15",
        "TEACHER_MSG_16",
        "TEACHER_MSG_17",
        "TEACHER_MSG_18",
        "",
        "TEACHER_MSG_19",
        "TEACHER_MSG_20",
        "TEACHER_MSG_21",
        // Bitiş
        "TEACHER_MSG_22"
    };

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
        dialogBox = GetNodeOrNull<Panel>("DialogBox");
        dialogText = GetNodeOrNull<Label>("DialogBox/DialogText");

        if (interactionLabel != null)
            interactionLabel.Visible = false;

        if (dialogBox != null)
            dialogBox.Visible = false;

        ShuffleMessages();

        if (interactionLabel != null)
        {
            var tween = CreateTween().SetLoops();
            tween.TweenProperty(interactionLabel, "modulate:a", 0.3f, 0.5f);
            tween.TweenProperty(interactionLabel, "modulate:a", 1.0f, 0.5f);
        }

        CollisionMask = 2;
        GD.Print($"[INFO_TEACHER] Hazır! İlk mesaj: Sabit | Diğer mesajlar: {messages.Length} (random)");
    }

    public override void _Process(double delta)
    {
        // ✅ YENİ: Dialog aktif değilken E tuşuna izin ver
        if (playerInRange && !isDialogActive && Input.IsActionJustPressed("interaction"))
        {
            ShowNextMessage();
        }
    }

    private void ShuffleMessages()
    {
        remainingMessages = messages.OrderBy(x => random.Next()).ToList();
        GD.Print($"[INFO_TEACHER] {remainingMessages.Count} mesaj karıştırıldı!");
    }

    private void ShowNextMessage()
    {
        // ✅ YENİ: Zaten mesaj gösteriliyorsa yeni mesaj başlatma
        if (isTypewriting || isDialogActive)
        {
            GD.Print("[INFO_TEACHER] Zaten bir mesaj gösteriliyor, bekleniyor...");
            return;
        }

        if (dialogBox == null || dialogText == null)
        {
            GD.PrintErr("[INFO_TEACHER] DialogBox veya DialogText bulunamadı!");
            return;
        }

        string messageToShow;

        if (!firstMessageShown)
        {
            messageToShow = Tr("TEACHER_MSG_FIRST").Replace("\\n", "\n");
            firstMessageShown = true;
            GD.Print("[INFO_TEACHER] İlk mesaj gösterildi (sabit)");
        }
        else
        {
            if (remainingMessages.Count == 0)
            {
                ShuffleMessages();
                GD.Print("[INFO_TEACHER] Tüm mesajlar gösterildi, liste yenilendi!");
            }

            int randomIndex = random.Next(remainingMessages.Count);
            messageToShow = Tr(remainingMessages[randomIndex]).Replace("\\n", "\n");
            remainingMessages.RemoveAt(randomIndex);

            GD.Print($"[INFO_TEACHER] Random mesaj gösterildi. Kalan: {remainingMessages.Count}");
        }

        // ✅ YENİ: Dialog'u aktif olarak işaretle
        isDialogActive = true;
        dialogBox.Visible = true;

        StartTypewriter(messageToShow);
    }

    private async void StartTypewriter(string text)
    {
        if (dialogText == null) return;

        // ✅ YENİ: Yazma başladı
        isTypewriting = true;
        dialogText.Text = "";

        foreach (char c in text)
        {
            // ✅ YENİ: Oyuncu uzaklaştıysa efekti durdur
            if (!playerInRange)
            {
                GD.Print("[INFO_TEACHER] Oyuncu uzaklaştı, typewriter iptal edildi.");
                isTypewriting = false;
                isDialogActive = false;
                return;
            }

            dialogText.Text += c;
            await ToSignal(GetTree().CreateTimer(0.02), "timeout");
        }

        // ✅ YENİ: Yazma bitti
        isTypewriting = false;

        // ✅ YENİ: 0.5 saniye bekle, sonra dialog'u kapat
        await ToSignal(GetTree().CreateTimer(1.0), "timeout");

        // Oyuncu hala yakındaysa dialog'u kapat
        if (playerInRange)
        {
            dialogBox.Visible = false;
            isDialogActive = false;
            GD.Print("[INFO_TEACHER] Dialog otomatik kapandı.");
        }
    }

    private async void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;
            justEntered = true;

            if (interactionLabel != null)
                interactionLabel.Visible = true;


            // (Hızlı girip çıkmayı engellemek için)
            await ToSignal(GetTree().CreateTimer(0.2), "timeout");

            // Hala yakındaysa ve yeni giriş ise ilk mesajı göster
            if (playerInRange && justEntered && !firstMessageShown)
            {
                ShowNextMessage();
                justEntered = false;
            }

            GD.Print("[INFO_TEACHER] Oyuncu yakında!");
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
            player = null;
            justEntered = false;

            if (interactionLabel != null)
                interactionLabel.Visible = false;

            // ✅ YENİ: Dialog'u kapat ve flag'leri sıfırla
            if (dialogBox != null)
                dialogBox.Visible = false;

            isDialogActive = false;
            isTypewriting = false;

            GD.Print("[INFO_TEACHER] Oyuncu uzaklaştı.");
        }
    }
}