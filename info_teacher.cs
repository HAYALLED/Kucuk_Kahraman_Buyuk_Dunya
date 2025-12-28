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

    // ✅ İLK MESAJ - SABIT!
    private const string FIRST_MESSAGE = "Merhaba! Ben bilgi öğretmeniyim.\nNPC'lere ve objelere E tuşuna basarak etkileşim kurabilirsin!";

    // ✅ DİĞER MESAJLAR - RANDOM SIRADA GELİR!
    private string[] messages = new string[]
    {
        // Kostümler
        "SPIDERBOY KOSTÜMÜ:\nE tuşuna basarak ağ atabilir ve duvarlarda sallanabilirsin!",

        "BATBOY KOSTÜMÜ:\nE tuşuna basarak kancayı fırlatıp çekebilirsin!",

        "KOSTÜMLER:\nE tuşuna veya sağ klik basarsan o kostümün özelliklerini kullanabilirsin!Hepsi birbirinden havalı!",        
        // Düşmanlar
        "Güçlü Çöp Adam:\nBu dev düşman seni görünce koşarak üstüne gelir!\nÇarparsa çok hasar alırsın, dikkatli ol!",
        
        "Çöp Adam:\nBu düşman genelde dolaşmayı sever ve zayıftırlardır ama dikkat et birlikteykenler güçlülerdir!",

        "Çöp Konteyner Büyücüsü:\nBu dev düşman genelde arkadaşlarını çağırmayı sever\ndüşünüce kim arkadaş istemez ki?",

        "Çöp Konteyner Büyücüsü:\nBu dev düşman seni görünce sana arkadaşlarını fırlatır!\neğimi biraz kötüdür ama neden arkadaşlarını sana atıyor??",

        "Çöp Canavarı:\nYavaş hareket eder ama seni görünce sana uzaktan saldırır ve hasar alabilirsin!\nÜstünden atla veya uzak dur.",

        "Kalkanlı Çöp Adam:\nBu dev düşman genelde uykucu ama uykusu çok hafiftir ve arada bir etrafı kontrol eder güvende mi diye ama dikkat et çok yakına gelirsen veya uzaktan ilk defa saldırırsan uyanır!\nBir yandan ona kızamayız onu uykusundan uyandırdık!",
        
        "Kalkanlı Çöp Adam:\nBu dev düşman şapkasını kalkan gibi kullanır ve kalkanı düşünceyi kadar gerçekten hasar alamaz!\nŞapkası havada dönerken bundan faydalan ve dikkat et şapkasına zarar gelmesinden nefret eder!",

        "Çöp Kuşu:\nBu düşman genelde bir alanda dolaşmayı sever seni görür görmez balık görmüş gibi dalarlar!\nVe dikkat et birlikteykenler çok güçlülerdir ve birlikte hareket ederler!",

        "Çöp Kuşu Spawner (Secured):\nBu bina çöp kuşların bulunduğu binadır ama Kalkanlı Çöp Adamlar tarafından korunuyordur!\nDüşündüğünde tatlı bir arkadaşlıkları var!",

        "Çöp Kuşu Spawner (hermit):\nBu bina çöp kuşların bulunduğu binadır ve tatlı hermit onları yaşamasını izin veriyor ama dikkat et hermit çok utangaç yakınına gelirsen anında kabuğuna çekilir ve hasar verilemez konuma gelir!\nDüşündüğünde tatlı bir arkadaşlıkları var!",
        // Gameplay bilgileri
        " ÇÖP GERİ DÖNÜŞÜM KUTULARI:\nBölümün en sağ tarafında bulunur.\nÇöpleri oraya götürüp E tuşuna bas!",

        " CAN SİSTEMİ:\nSol üstte kalp simgeleri canını gösterir.\nDüşmanlara sana vurursa can kaybedersin!",

        " PUAN SİSTEMİ:\nBölümü geçmek için minimum 100 puan gerekir.\nÇöp topla ve geri dönüştür!",

        " MATEMATİK SORULARI:\nÖğretmen NPC'sine E basınca soru sorar.\nDoğru cevap verirsen puan kazanırsın!",

        " MATEMATİK SORULARI:\nTerzi NPC'sine E basınca soru sorar.\nTüm sorulara Doğru cevap verirsen kostümünü onarır!Ama dikkat et terzi sorduğu soruların tamamını doğru cevaplanmasını ister",
        "",
        " İPUCU:\nGizli bölümler var! Bazen garip görünen yerlere gitmek seni başka bir mini levele götürebilir!",

        " KONTROLLER:\nOk tuşları veya WASD ile hareket, Space ile zıpla!",

         " GİZLİ ALANLAR:\nDuvarların arkasını kontrol et, sürprizler olabilir!",

        // Bitiş
        "Bu kadar bilgi yeterli! İyi şanslar!\n(Tekrar gelirsen yeni infolar öğrenebilirsin! )"
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
            messageToShow = FIRST_MESSAGE;
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
            messageToShow = remainingMessages[randomIndex];
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