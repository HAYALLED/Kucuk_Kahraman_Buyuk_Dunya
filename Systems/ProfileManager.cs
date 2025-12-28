using System;
using Godot;

public partial class ProfileManager : Control
{
    // Mevcut değişkenler...
    private Label currentProfileLabel;
    private LineEdit usernameInput;
    private LineEdit passwordInput;
    private Button loginButton;
    private Button registerButton;
    private Button logoutButton;
    private TextureButton backButton;

    //  Math Questions Remove
    private OptionButton removeDifficultyOption;
    private ItemList removeQuestionsList;
    private Button removeQuestionButton;

    //  Math Questions Add
    private OptionButton addDifficultyOption;
    private LineEdit addQuestionInput;
    private Button addQuestionButton;
    //  Math Questions Brink Back
    private Button addBringDeletedQuestionBackButton;
    //  Math Questions aktif mi?
    private bool mathQuestionsEnabled = false;

    //  MİNİMUM SORU SAYISI
    private const int MINIMUM_QUESTIONS_PER_DIFFICULTY = 3;

    public override void _Ready()
    {
        //  TEMEL NODE'LAR
        currentProfileLabel = GetNode<Label>("Profil/VBoxContainer/CurrentProfileLabel");
        usernameInput = GetNode<LineEdit>("Profil/VBoxContainer/UsernameInput");
        passwordInput = GetNode<LineEdit>("Profil/VBoxContainer/PasswordInput");
        loginButton = GetNode<Button>("Profil/VBoxContainer/HBoxContainer/LoginButton");
        registerButton = GetNode<Button>("Profil/VBoxContainer/HBoxContainer/RegisterButton");
        logoutButton = GetNode<Button>("Profil/VBoxContainer/LogoutButton");
        backButton = GetNode<TextureButton>("BackButton");

        //  MATH QUESTIONS NODE'LARINI GÜVENLİ ŞEKİLDE YÜK
        TryLoadMathQuestionsNodes();
        BrinkBackMathQuestionsNodes();

        SyncUserProfile();
        UpdateUI();
        ConnectSignals();

        //  Math Questions aktifse başlat
        if (mathQuestionsEnabled)
        {
            // ⚠️ Deferred çağrı - tüm node'lar hazır olsun
            CallDeferred(nameof(InitializeMathQuestions));
        }
    }
    private void BrinkBackMathQuestionsNodes() 
    {
        addBringDeletedQuestionBackButton = GetNode<Button>("BringDeletedQuestion");






    }
    private void TryLoadMathQuestionsNodes()
    {
        GD.Print("[PROFILE] ========================================");
        GD.Print("[PROFILE] TryLoadMathQuestionsNodes BAŞLADI!");
        GD.Print("[PROFILE] ========================================");

        try
        {
            //  REMOVE node'larını kontrol et
            if (HasNode("MathquestionsRemove/VBoxContainer/Difficulty") &&
                HasNode("MathquestionsRemove/VBoxContainer/MathQuestion") &&
                HasNode("MathquestionsRemove/VBoxContainer/Button"))
            {
                GD.Print("[PROFILE]  REMOVE node path'leri BULUNDU!");

                var difficultyNode = GetNode("MathquestionsRemove/VBoxContainer/Difficulty");
                var mathQuestionNode = GetNode("MathquestionsRemove/VBoxContainer/MathQuestion");
                var buttonNode = GetNode("MathquestionsRemove/VBoxContainer/Button");

                GD.Print($"[PROFILE] Difficulty type: {difficultyNode.GetType().Name}");
                GD.Print($"[PROFILE] MathQuestion type: {mathQuestionNode.GetType().Name}");
                GD.Print($"[PROFILE] Button type: {buttonNode.GetType().Name}");

                //  TİP KONTROLÜ!
                if (difficultyNode is OptionButton &&
                    mathQuestionNode is ItemList &&
                    buttonNode is Button)
                {
                    removeDifficultyOption = (OptionButton)difficultyNode;
                    removeQuestionsList = (ItemList)mathQuestionNode;
                    removeQuestionButton = (Button)buttonNode;

                    GD.Print($"[PROFILE]  MathquestionsRemove node'ları yüklendi!");
                }
                else
                {
                    GD.PrintErr("[PROFILE] ❌ MathquestionsRemove node tipleri hatalı!");
                    GD.PrintErr($"  - Difficulty: {difficultyNode.GetType().Name} (beklenen: OptionButton)");
                    GD.PrintErr($"  - MathQuestion: {mathQuestionNode.GetType().Name} (beklenen: ItemList)");
                    GD.PrintErr($"  - Button: {buttonNode.GetType().Name} (beklenen: Button)");
                    return;
                }
            }
            else
            {
                GD.PrintErr("[PROFILE] ❌ MathquestionsRemove node'ları bulunamadı!");
                GD.PrintErr("[PROFILE]    Difficulty: " + HasNode("MathquestionsRemove/VBoxContainer/Difficulty"));
                GD.PrintErr("[PROFILE]    MathQuestion: " + HasNode("MathquestionsRemove/VBoxContainer/MathQuestion"));
                GD.PrintErr("[PROFILE]    Button: " + HasNode("MathquestionsRemove/VBoxContainer/Button"));
                return;
            }

            //  ADD node'larını kontrol et
            if (HasNode("MathquestionsAdd/VBoxContainer/Difficulty") &&
                HasNode("MathquestionsAdd/VBoxContainer/LineEdit") &&
                HasNode("MathquestionsAdd/VBoxContainer/Button"))
            {
                GD.Print("[PROFILE]  ADD node path'leri BULUNDU!");

                var difficultyNode = GetNode("MathquestionsAdd/VBoxContainer/Difficulty");
                var lineEditNode = GetNode("MathquestionsAdd/VBoxContainer/LineEdit");
                var buttonNode = GetNode("MathquestionsAdd/VBoxContainer/Button");

                GD.Print($"[PROFILE] Difficulty type: {difficultyNode.GetType().Name}");
                GD.Print($"[PROFILE] LineEdit type: {lineEditNode.GetType().Name}");
                GD.Print($"[PROFILE] Button type: {buttonNode.GetType().Name}");

                //  TİP KONTROLÜ!
                if (difficultyNode is OptionButton &&
                    lineEditNode is LineEdit &&
                    buttonNode is Button)
                {
                    addDifficultyOption = (OptionButton)difficultyNode;
                    addQuestionInput = (LineEdit)lineEditNode;
                    addQuestionButton = (Button)buttonNode;

                    GD.Print($"[PROFILE]  MathquestionsAdd node'ları yüklendi!");
                }
                else
                {
                    GD.PrintErr("[PROFILE] ❌ MathquestionsAdd node tipleri hatalı!");
                    GD.PrintErr($"  - Difficulty: {difficultyNode.GetType().Name} (beklenen: OptionButton)");
                    GD.PrintErr($"  - LineEdit: {lineEditNode.GetType().Name} (beklenen: LineEdit)");
                    GD.PrintErr($"  - Button: {buttonNode.GetType().Name} (beklenen: Button)");
                    return;
                }
            }
            else
            {
                GD.PrintErr("[PROFILE] ❌ MathquestionsAdd node'ları bulunamadı!");
                GD.PrintErr("[PROFILE]    Difficulty: " + HasNode("MathquestionsAdd/VBoxContainer/Difficulty"));
                GD.PrintErr("[PROFILE]    LineEdit: " + HasNode("MathquestionsAdd/VBoxContainer/LineEdit"));
                GD.PrintErr("[PROFILE]    Button: " + HasNode("MathquestionsAdd/VBoxContainer/Button"));
                return;
            }

            //  HER ŞEY BAŞARILI!
            mathQuestionsEnabled = true;
            GD.Print("[PROFILE]  Math Questions sistemi AKTIF! ");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PROFILE] ❌❌❌ EXCEPTION: {ex.Message}");
            GD.PrintErr($"[PROFILE] StackTrace: {ex.StackTrace}");
            mathQuestionsEnabled = false;
        }

        GD.Print("[PROFILE] ========================================");
    }

    private void InitializeMathQuestions()
    {
        if (!mathQuestionsEnabled)
        {
            GD.Print("[PROFILE] ⚠️ Math Questions devre dışı!");
            return;
        }

        GD.Print("[PROFILE] ========================================");
        GD.Print("[PROFILE] InitializeMathQuestions başlatılıyor...");
        GD.Print("[PROFILE] ========================================");

        //  ÖNCE MEVCUT ITEM'LARI KONTROL ET
        GD.Print($"[PROFILE] Remove OptionButton - ÖNCE: {removeDifficultyOption.ItemCount} item");
        for (int i = 0; i < removeDifficultyOption.ItemCount; i++)
        {
            GD.Print($"[PROFILE]   Item {i}: '{removeDifficultyOption.GetItemText(i)}'");
        }

        //  TEMİZLE
        removeDifficultyOption.Clear();
        addDifficultyOption.Clear();

        GD.Print($"[PROFILE] Clear() sonrası: {removeDifficultyOption.ItemCount} item");

        //  ZORLUK SEÇENEKLERİNİ EKLE
        removeDifficultyOption.AddItem("Kolay");    // ID: 0
        removeDifficultyOption.AddItem("Orta");     // ID: 1
        removeDifficultyOption.AddItem("Zor");      // ID: 2

        addDifficultyOption.AddItem("Kolay");       // ID: 0
        addDifficultyOption.AddItem("Orta");        // ID: 1
        addDifficultyOption.AddItem("Zor");         // ID: 2

        //  DOĞRULAMA
        GD.Print($"[PROFILE] AddItem() sonrası:");
        GD.Print($"[PROFILE]   Remove: {removeDifficultyOption.ItemCount} item");
        GD.Print($"[PROFILE]   Add: {addDifficultyOption.ItemCount} item");

        for (int i = 0; i < removeDifficultyOption.ItemCount; i++)
        {
            GD.Print($"[PROFILE]     Remove Item {i}: '{removeDifficultyOption.GetItemText(i)}'");
        }

        //  DEFAULT SEÇİM
        removeDifficultyOption.Selected = 1; // Orta
        addDifficultyOption.Selected = 1;    // Orta

        GD.Print($"[PROFILE] Selected index ayarlandı: {removeDifficultyOption.Selected}");
        GD.Print($"[PROFILE] Selected text: '{removeDifficultyOption.GetItemText(removeDifficultyOption.Selected)}'");

        //  İLK YÜKLEME - Biraz daha bekle (frame sonunu garantile)
        GetTree().CreateTimer(0.1).Timeout += LoadQuestionsForRemove;

        GD.Print("[PROFILE] ========================================");
    }

    private void ConnectSignals()
    {
        // Mevcut signal'lar...
        loginButton.Pressed += OnLoginPressed;
        registerButton.Pressed += OnRegisterPressed;
        logoutButton.Pressed += OnLogoutPressed;
        backButton.Pressed += OnBackPressed;

        //  Math Questions signal'ları (sadece aktifse)
        if (mathQuestionsEnabled)
        {
            removeDifficultyOption.ItemSelected += OnRemoveDifficultyChanged;
            removeQuestionButton.Pressed += OnRemoveQuestionPressed;
            addQuestionButton.Pressed += OnAddQuestionPressed;

            GD.Print("[PROFILE] Math Questions signal'ları bağlandı!");
        }

        // ✅ BRING BACK BUTTON SIGNAL
        if (addBringDeletedQuestionBackButton != null)
        {
            addBringDeletedQuestionBackButton.Pressed += OnBringDeletedQuestionBackPressed;
            GD.Print("[PROFILE] Bring Back Questions button signal bağlandı!");
        }
    }

    private void OnBringDeletedQuestionBackPressed()
    {
        int userId = UserProfile.Instance.CurrentUserID;

        if (userId <= 0)
        {
            ShowErrorPopup("Giriş yapmalısınız!");
            return;
        }

        // ✅ ONAY DİALOGU
        var confirm = new ConfirmationDialog();
        confirm.DialogText = "Devre dışı bıraktığınız TÜM soruları tekrar aktif etmek istediğinize emin misiniz?\n\nBu işlem geri alınamaz!";
        confirm.Title = "TÜM SORULARI TEKRAR AKTİF ET";
        confirm.OkButtonText = "EVET, HEPSİNİ AKTİF ET";
        confirm.CancelButtonText = "HAYIR";

        confirm.Confirmed += () =>
        {
            // ✅ Database'den tüm devre dışı soruları sil
            bool success = Database.ReEnableAllQuestionsForUser(userId);

            if (success)
            {
                ShowMessage("Tüm sorular tekrar aktif edildi!", Colors.Green);

                // ✅ Listeyi yenile
                if (mathQuestionsEnabled)
                {
                    LoadQuestionsForRemove();
                }
            }
            else
            {
                ShowErrorPopup("Hiç devre dışı soru bulunamadı!");
            }

            confirm.QueueFree();
        };

        confirm.Canceled += () =>
        {
            GD.Print("[PROFILE] Soru geri getirme iptal edildi.");
            confirm.QueueFree();
        };

        AddChild(confirm);
        confirm.PopupCentered(new Vector2I(500, 200));
    }
    // ========================================
    // MATH QUESTIONS REMOVE
    // ========================================

    private void OnRemoveDifficultyChanged(long index)
    {
        if (!mathQuestionsEnabled) return;

        GD.Print($"[PROFILE] ========================================");
        GD.Print($"[PROFILE] Difficulty değişti: Index={index}");
        GD.Print($"[PROFILE] Text: '{removeDifficultyOption.GetItemText((int)index)}'");
        GD.Print($"[PROFILE] ========================================");

        LoadQuestionsForRemove();
    }

    private void LoadQuestionsForRemove()
    {
        if (!mathQuestionsEnabled)
        {
            GD.Print("[PROFILE] ⚠️ Math Questions devre dışı!");
            return;
        }

        GD.Print("[PROFILE] ========================================");
        GD.Print("[PROFILE] LoadQuestionsForRemove başlatıldı...");

        //  UserID KONTROLÜ - ÖNCELİK!
        int userId = UserProfile.Instance.CurrentUserID;

        if (userId <= 0)
        {
            GD.Print("[PROFILE] ⚠️ Giriş yapılmamış! (UserID = 0)");
            removeQuestionsList.Clear();
            GD.Print("[PROFILE] ========================================");
            return;
        }

        GD.Print($"[PROFILE] UserID: {userId}");

        //  SEÇİLİ INDEX VE TEXT AL
        int selectedIndex = removeDifficultyOption.Selected;

        //  GÜVENLİK KONTROLÜ
        if (selectedIndex < 0 || selectedIndex >= removeDifficultyOption.ItemCount)
        {
            GD.PrintErr($"[PROFILE] ❌ Geçersiz selected index: {selectedIndex} (item count: {removeDifficultyOption.ItemCount})");
            removeQuestionsList.Clear();
            GD.Print("[PROFILE] ========================================");
            return;
        }

        string difficulty = removeDifficultyOption.GetItemText(selectedIndex);

        GD.Print($"[PROFILE] Selected Index: {selectedIndex}");
        GD.Print($"[PROFILE] Difficulty Text: '{difficulty}'");

        //  BOŞ TEXT KONTROLÜ
        if (string.IsNullOrWhiteSpace(difficulty))
        {
            GD.PrintErr("[PROFILE] ❌ Difficulty text boş!");
            removeQuestionsList.Clear();
            GD.Print("[PROFILE] ========================================");
            return;
        }

        //  LİSTEYİ TEMİZLE
        removeQuestionsList.Clear();

        //  DATABASE'DEN SORULARI ÇEK
        var questions = Database.GetMathQuestionsByDifficulty(difficulty, userId);

        GD.Print($"[PROFILE] Database'den {questions.Count} soru geldi");

        int addedCount = 0;
        int disabledCount = 0;

        foreach (var q in questions)
        {
            string questionText = q["question"].ToString();
            bool isDisabled = (bool)q["isDisabled"];
            int id = (int)q["id"];

            GD.Print($"[PROFILE]   Soru #{id}: '{questionText}' - Disabled: {isDisabled}");

            //  Sadece AKTİF soruları göster
            if (!isDisabled)
            {
                removeQuestionsList.AddItem(questionText);
                removeQuestionsList.SetItemMetadata(removeQuestionsList.ItemCount - 1, id);
                addedCount++;
            }
            else
            {
                disabledCount++;
            }
        }

        GD.Print($"[PROFILE]  {addedCount} aktif soru eklendi (ItemList)");
        GD.Print($"[PROFILE] ⚠️ {disabledCount} devre dışı soru (gösterilmiyor)");
        GD.Print($"[PROFILE] ℹ️ Toplam ItemList item count: {removeQuestionsList.ItemCount}");
        GD.Print("[PROFILE] ========================================");
    }

    private void OnRemoveQuestionPressed()
    {
        if (!mathQuestionsEnabled) return;

        int selectedIndex = removeQuestionsList.GetSelectedItems().Length > 0
            ? removeQuestionsList.GetSelectedItems()[0]
            : -1;

        if (selectedIndex < 0)
        {
            ShowMessage("Lütfen bir soru seçin!", Colors.Orange);
            return;
        }

        string questionText = removeQuestionsList.GetItemText(selectedIndex);
        int questionId = (int)removeQuestionsList.GetItemMetadata(selectedIndex);

        //  MİNİMUM SORU KONTROLÜ
        int activeQuestionCount = removeQuestionsList.ItemCount;

        if (activeQuestionCount <= MINIMUM_QUESTIONS_PER_DIFFICULTY)
        {
            ShowErrorPopup($"Bu zorluk seviyesinde en az {MINIMUM_QUESTIONS_PER_DIFFICULTY} soru kalmalı!\n\nŞu an: {activeQuestionCount} aktif soru var.");
            return;
        }

        var confirm = new ConfirmationDialog();
        confirm.DialogText = $"Bu soruyu devre dışı bırakmak istediğinize emin misiniz?\n\n\"{questionText}\"\n\nKalan aktif soru sayısı: {activeQuestionCount - 1}";
        confirm.Title = "SORU DEVRE DIŞI BIRAKMA";
        confirm.OkButtonText = "EVET, DEVRE DIŞI BIRAK";
        confirm.CancelButtonText = "HAYIR";

        confirm.Confirmed += () =>
        {
            int userId = UserProfile.Instance.CurrentUserID;

            if (Database.DisableQuestionForUser(userId, questionId))
            {
                ShowMessage("Soru başarıyla devre dışı bırakıldı!", Colors.Green);
                LoadQuestionsForRemove();
            }
            else
            {
                ShowMessage("Soru devre dışı bırakılamadı!", Colors.Red);
            }

            confirm.QueueFree();
        };

        confirm.Canceled += () =>
        {
            GD.Print("[PROFILE] Soru devre dışı bırakma iptal edildi.");
            confirm.QueueFree();
        };

        AddChild(confirm);
        confirm.PopupCentered(new Vector2I(500, 250));
    }

    // ========================================
    // MATH QUESTIONS ADD
    // ========================================

    private void OnAddQuestionPressed()
    {
        if (!mathQuestionsEnabled) return;

        string question = addQuestionInput.Text.Trim();
        string difficulty = addDifficultyOption.GetItemText(addDifficultyOption.Selected);

        if (string.IsNullOrEmpty(question))
        {
            ShowErrorPopup("Soru boş olamaz!");
            return;
        }

        int userId = UserProfile.Instance.CurrentUserID;

        if (userId <= 0)
        {
            ShowErrorPopup("Soru eklemek için giriş yapmalısınız!");
            return;
        }

        var confirm = new ConfirmationDialog();
        confirm.DialogText = $"Bu soru TÜM KULLANICILAR için eklenecek!\n\nSoru: {question}\nZorluk: {difficulty}\n\nCevap otomatik hesaplanacak. Emin misiniz?";
        confirm.Title = "SORU EKLEME";
        confirm.OkButtonText = "EVET, EKLE";
        confirm.CancelButtonText = "HAYIR";

        confirm.Confirmed += () =>
        {
            bool success = Database.AddMathQuestion(userId, question, difficulty);

            if (success)
            {
                ShowMessage("Soru başarıyla eklendi!", Colors.Green);
                addQuestionInput.Text = "";
                LoadQuestionsForRemove();
            }
            else
            {
                ShowErrorPopup("Soru eklenemedi! Lütfen konsolu kontrol edin.");
            }

            confirm.QueueFree();
        };

        confirm.Canceled += () =>
        {
            GD.Print("[PROFILE] Soru ekleme iptal edildi.");
            confirm.QueueFree();
        };

        AddChild(confirm);
        confirm.PopupCentered(new Vector2I(500, 250));
    }

    private void ShowErrorPopup(string message)
    {
        var errorDialog = new AcceptDialog();
        errorDialog.DialogText = message;
        errorDialog.Title = "❌ HATA";
        errorDialog.OkButtonText = "TAMAM";
        errorDialog.Size = new Vector2I(500, 150);

        AddChild(errorDialog);

        errorDialog.PopupCentered();
        var screenSize = GetViewportRect().Size;
        var dialogSize = errorDialog.Size;
        errorDialog.Position = new Vector2I(
            (int)(screenSize.X / 2 - dialogSize.X / 2),
            (int)(screenSize.Y - dialogSize.Y - 50)
        );

        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            if (IsInstanceValid(errorDialog))
            {
                errorDialog.QueueFree();
            }
        };

        GD.PrintErr($"[PROFILE ERROR] {message}");
    }

    // ========================================
    // MEVCUT METODLAR (AYNI)
    // ========================================

    private void SyncUserProfile()
    {
        int userId = SaveGame.Instance.GetCurrentUserId();

        GD.Print($"[PROFILE MANAGER] Senkronizasyon kontrol: UserID={userId}");

        if (userId > 0)
        {
            if (!UserProfile.Instance.IsLoggedIn)
            {
                var userData = Database.GetUserByUserId(userId);

                if (userData != null)
                {
                    string userName = userData["userName"].ToString();
                    UserProfile.Instance.RestoreSession(userId, userName);

                    GD.Print($"[PROFILE MANAGER]  UserProfile oturum geri yüklendi: {userName}");
                }
                else
                {
                    GD.PrintErr("[PROFILE MANAGER] ❌ Kullanıcı database'de bulunamadı! SaveGame temizleniyor...");
                    SaveGame.Instance.ClearCurrentUserId();
                }
            }
            else
            {
                GD.Print($"[PROFILE MANAGER] UserProfile zaten aktif: {UserProfile.Instance.CurrentUserName}");
            }
        }
        else
        {
            if (UserProfile.Instance.IsLoggedIn)
            {
                GD.Print("[PROFILE MANAGER] UserProfile logout yapılıyor (SaveGame'de kullanıcı yok)");
                UserProfile.Instance.Logout();
            }
        }
    }

    private void UpdateUI()
    {
        int userId = SaveGame.Instance.GetCurrentUserId();
        var profile = UserProfile.Instance;

        GD.Print($"[PROFILE MANAGER] UpdateUI: UserID={userId}, IsLoggedIn={profile.IsLoggedIn}");

        if (userId > 0 && profile.IsLoggedIn)
        {
            currentProfileLabel.Text = $"Aktif Profil: {profile.CurrentUserName}";
            usernameInput.Editable = false;
            passwordInput.Editable = false;
            loginButton.Disabled = true;
            registerButton.Disabled = true;
            logoutButton.Disabled = false;

            GD.Print($"[PROFILE MANAGER] UI: Giriş yapılmış - {profile.CurrentUserName}");
        }
        else
        {
            currentProfileLabel.Text = "Aktif Profil: Misafir";
            usernameInput.Editable = true;
            passwordInput.Editable = true;
            loginButton.Disabled = false;
            registerButton.Disabled = false;
            logoutButton.Disabled = true;

            GD.Print("[PROFILE MANAGER] UI: Misafir modu");
        }
    }

    private void OnLoginPressed()
    {
        string username = usernameInput.Text.Trim();
        string password = passwordInput.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Kullanıcı adı ve şifre boş olamaz!", Colors.Red);
            return;
        }

        var userData = Database.GetUserByUsername(username);

        if (userData == null)
        {
            ShowMessage("Kullanıcı bulunamadı!", Colors.Red);
            return;
        }

        string storedPassword = userData["passwordHash"].ToString();

        if (storedPassword != password)
        {
            ShowMessage("Şifre hatalı!", Colors.Red);
            return;
        }

        int userId = (int)userData["userId"];
        string userName = userData["userName"].ToString();

        GD.Print($"[LOGIN]  Giriş başarılı! User: {userName} (ID: {userId})");

        SaveGame.Instance.SetCurrentUserId(userId);
        UserProfile.Instance.Login(username, password);

        ShowMessage($"Hoş geldin, {username}!", Colors.Green);
        UpdateUI();
        ClearInputs();

        //  GİRİŞ YAPILINCA SORULARI YENİDEN YÜK
        if (mathQuestionsEnabled)
        {
            GetTree().CreateTimer(0.2).Timeout += LoadQuestionsForRemove;
        }

        GetTree().CreateTimer(1.0).Timeout += () =>
        {
            GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
        };
    }

    private void OnRegisterPressed()
    {
        string username = usernameInput.Text.Trim();
        string password = passwordInput.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Kullanıcı adı ve şifre boş olamaz!", Colors.Red);
            return;
        }

        if (password.Length < 4)
        {
            ShowMessage("Şifre en az 4 karakter olmalı!", Colors.Orange);
            return;
        }

        int newUserId = Database.CreateUser(username, password);

        if (newUserId <= 0)
        {
            ShowMessage("Bu kullanıcı adı zaten kullanılıyor!", Colors.Red);
            return;
        }

        GD.Print($"[REGISTER]  Yeni kullanıcı: {username} (ID: {newUserId})");

        SaveGame.Instance.SetCurrentUserId(newUserId);
        UserProfile.Instance.CreateAccount(username, password);

        ShowMessage($"Hesap oluşturuldu! Hoş geldin, {username}!", Colors.Green);
        UpdateUI();
        ClearInputs();

        //  KAYIT OLDUKTAN SONRA SORULARI YÜK
        if (mathQuestionsEnabled)
        {
            GetTree().CreateTimer(0.2).Timeout += LoadQuestionsForRemove;
        }

        GetTree().CreateTimer(1.0).Timeout += () =>
        {
            GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
        };
    }

    private void OnLogoutPressed()
    {
        GD.Print("[LOGOUT] Kullanıcı çıkış yapıyor...");

        SaveGame.Instance.ClearCurrentUserId();
        UserProfile.Instance.Logout();

        ShowMessage("Çıkış yapıldı!", Colors.Yellow);
        UpdateUI();
        ClearInputs();

        //  LOGOUT YAPILINCA LİSTEYİ TEMİZLE
        if (mathQuestionsEnabled)
        {
            removeQuestionsList.Clear();
        }
    }

    private void ClearInputs()
    {
        usernameInput.Text = "";
        passwordInput.Text = "";
    }

    private void ShowMessage(string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 24);
        label.Position = new Vector2(400, 350);
        AddChild(label);

        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            if (IsInstanceValid(label))
                label.QueueFree();
        };

        GD.Print($"[PROFILE] {text}");
    }

    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
    }
}