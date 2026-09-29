using Godot;
using System;
using System.Collections.Generic;

public partial class Trash_CollectorRobot : Area2D
{
    // ========================================
    // ENUMS
    // ========================================
    public enum RobotState
    {
        Idle,                // Yerinde durur
        QuestSelection,
        QuestActive,
        EscortWaiting,
        EscortCountdown,     // Geri sayım (3..2..1)
        EscortMoving,
        EscortCompleted,
        Completed
    }

    public enum TrashQuestType
    {
        None, Plastic, Metal, Glass, Wood, Food, Mix
    }

    public enum QuestCategory
    {
        Collect, Escort
    }

    // ========================================
    // NODE REFERANSLARI
    // ========================================
    private AnimatedSprite2D animatedSprite;
    private RayCast2D rayCast2DLeft;
    private RayCast2D rayCast2DRight;
    private RayCast2D wallRayCast;
    private Timer questTimer;

    // ========================================
    // QUEST AYARLARI
    // ========================================
    [ExportGroup("Görev Ayarları")]
    [Export] public float QuestTimeLimit = 120.0f;

    [ExportGroup("Escort Ayarları")]
    [Export] public float EscortSpeed = 80.0f;
    [Export] public float EscortTimeLimit = 180.0f;
    [Export] public int EscortBaseReward = 100;
    [Export] public int EscortBonusReward = 150;
    [Export] public int RobotMaxHealth = 3;
    [Export] public float PlayerFollowCheckRadius = 400f;
    [Export] public float EscortCountdownTime = 3.0f;

    [ExportGroup("Engel Aşma")]
    [Export] public float JumpForce = -350.0f;
    [Export] public float WallDetectionDistance = 30.0f;
    [Export] public float JumpCooldown = 0.5f;

    // ✅ YENİ: Sağlık Barı Görselleri (Inspector'dan ata!)
    [ExportGroup("Sağlık Barı Görselleri")]
    [Export] public Texture2D HealthBarUnderTexture;    // Arka plan resmi
    [Export] public Texture2D HealthBarOverTexture;     // Ön plan çerçeve resmi
    [Export] public Texture2D HealthBarProgressTexture; // Doluluk resmi (yeşil kısım)
    [Export] public Vector2 HealthBarSize = new Vector2(60, 10);
    [Export] public Vector2 HealthBarOffset = new Vector2(-30, -50);

    private Dictionary<TrashQuestType, int> requiredAmounts = new Dictionary<TrashQuestType, int>()
    {
        { TrashQuestType.Plastic, 5 }, { TrashQuestType.Metal, 5 },
        { TrashQuestType.Glass, 5 }, { TrashQuestType.Wood, 5 },
        { TrashQuestType.Food, 5 }, { TrashQuestType.Mix, 30 }
    };

    private Dictionary<TrashQuestType, int> rewardPoints = new Dictionary<TrashQuestType, int>()
    {
        { TrashQuestType.Plastic, 50 }, { TrashQuestType.Metal, 50 },
        { TrashQuestType.Glass, 50 }, { TrashQuestType.Wood, 50 },
        { TrashQuestType.Food, 50 }, { TrashQuestType.Mix, 200 }
    };

    private Dictionary<TrashQuestType, string> questNames = new Dictionary<TrashQuestType, string>()
    {
        { TrashQuestType.Plastic, "Plastik" }, { TrashQuestType.Metal, "Metal" },
        { TrashQuestType.Glass, "RECYCLE_TRASH_GLASS" }, { TrashQuestType.Wood, "RECYCLE_TRASH_PAPER" },
        { TrashQuestType.Food, "RECYCLE_TRASH_ORGANIC" }, { TrashQuestType.Mix, "RECYCLE_TRASH_MIX" }
    };

    // ========================================
    // DURUM
    // ========================================
    private RobotState currentState = RobotState.Idle;
    private QuestCategory currentCategory = QuestCategory.Collect;
    private TrashQuestType currentQuest = TrashQuestType.None;
    private Node2D player;
    private bool playerInRange = false;
    private bool facingRight = true;
    private float gravity = 980.0f;
    private Vector2 velocity = Vector2.Zero;
    private float jumpCooldownTimer = 0;

    [Export] public float MoveSpeed = 50.0f;

    // ========================================
    // ESCORT SİSTEMİ
    // ========================================
    private Vector2 escortTargetPosition;
    private bool playerIsFollowing = false;
    private bool playerChoseToFollow = false;
    private int robotHealth;
    private float escortTimer = 0;
    private bool escortReachedTarget = false;
    private float playerDistanceCheckTimer = 0;
    private float playerDistanceCheckInterval = 1.0f;
    private TextureProgressBar healthBar;  // ✅ TextureProgressBar

    // Geri sayım
    private float countdownTimer = 0;
    private int lastCountdownNumber = 0;

    // ========================================
    // DİYALOG
    // ========================================
    private Control dialogPanel;
    private Label dialogLabel;
    private VBoxContainer buttonContainer;
    private Button followButton;
    private Button dontFollowButton;

    public override void _Ready()
    {
        GD.Print("[ROBOT] ========== ROBOT BAŞLATILIYOR ==========");

        LoadNodes();
        CreateWallRayCast();
        ConnectSignals();
        CreateTimer();
        CreateDialogUI();
        CreateEscortUI();

        currentState = RobotState.Idle;
        currentQuest = TrashQuestType.None;
        robotHealth = RobotMaxHealth;

        AddToGroup("npc");
        AddToGroup("robot");

        GD.Print("[ROBOT] ✅ Robot hazır! Durum: Idle (yerinde duruyor)");
    }

    private void LoadNodes()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        rayCast2DLeft = GetNodeOrNull<RayCast2D>("RayCast2Dleft");
        if (rayCast2DLeft == null)
            rayCast2DLeft = GetNodeOrNull<RayCast2D>("RayCast2DLeft");

        rayCast2DRight = GetNodeOrNull<RayCast2D>("RayCast2Dright");
        if (rayCast2DRight == null)
            rayCast2DRight = GetNodeOrNull<RayCast2D>("RayCast2DRight");

        if (rayCast2DLeft != null && rayCast2DRight != null)
            GD.Print("[ROBOT] ✅ Zemin RayCast'ler OK");
        else
            GD.PrintErr("[ROBOT] ❌ Zemin RayCast bulunamadı!");
    }

    private void CreateWallRayCast()
    {
        wallRayCast = new RayCast2D();
        wallRayCast.Name = "WallRayCast";
        wallRayCast.Enabled = true;
        wallRayCast.CollisionMask = 1;
        wallRayCast.CollideWithAreas = false;
        wallRayCast.CollideWithBodies = true;
        wallRayCast.TargetPosition = new Vector2(WallDetectionDistance, 0);
        AddChild(wallRayCast);
    }

    private void ConnectSignals()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void CreateTimer()
    {
        questTimer = new Timer();
        questTimer.Name = "QuestTimer";
        questTimer.OneShot = true;
        questTimer.Autostart = false;
        questTimer.Timeout += OnQuestTimeout;
        AddChild(questTimer);
    }

    private void CreateDialogUI()
    {
        var canvas = new CanvasLayer();
        canvas.Name = "DialogCanvas";
        canvas.Layer = 100;
        AddChild(canvas);

        dialogPanel = new Panel();
        dialogPanel.Name = "DialogPanel";
        dialogPanel.CustomMinimumSize = new Vector2(600, 350);
        dialogPanel.AnchorLeft = 0.5f;
        dialogPanel.AnchorTop = 0.5f;
        dialogPanel.AnchorRight = 0.5f;
        dialogPanel.AnchorBottom = 0.5f;
        dialogPanel.OffsetLeft = -300;
        dialogPanel.OffsetTop = -175;
        dialogPanel.OffsetRight = 300;
        dialogPanel.OffsetBottom = 175;
        dialogPanel.Visible = false;
        canvas.AddChild(dialogPanel);

        var mainVBox = new VBoxContainer();
        mainVBox.Name = "MainVBox";
        mainVBox.AnchorLeft = 0;
        mainVBox.AnchorTop = 0;
        mainVBox.AnchorRight = 1;
        mainVBox.AnchorBottom = 1;
        mainVBox.OffsetLeft = 20;
        mainVBox.OffsetTop = 20;
        mainVBox.OffsetRight = -20;
        mainVBox.OffsetBottom = -20;
        dialogPanel.AddChild(mainVBox);

        dialogLabel = new Label();
        dialogLabel.Name = "DialogLabel";
        dialogLabel.Text = "Merhaba!";
        dialogLabel.HorizontalAlignment = HorizontalAlignment.Center;
        dialogLabel.VerticalAlignment = VerticalAlignment.Center;
        dialogLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        dialogLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        mainVBox.AddChild(dialogLabel);

        buttonContainer = new VBoxContainer();
        buttonContainer.Name = "ButtonContainer";
        buttonContainer.Visible = false;
        mainVBox.AddChild(buttonContainer);

        followButton = new Button();
        followButton.Name = "FollowButton";
        followButton.Text = Tr("ROBOT_FOLLOW_BUTTON");
        followButton.CustomMinimumSize = new Vector2(0, 40);
        followButton.Pressed += OnFollowButtonPressed;
        buttonContainer.AddChild(followButton);

        dontFollowButton = new Button();
        dontFollowButton.Name = "DontFollowButton";
        dontFollowButton.Text = Tr("ROBOT_DONT_FOLLOW_BUTTON");
        dontFollowButton.CustomMinimumSize = new Vector2(0, 40);
        dontFollowButton.Pressed += OnDontFollowButtonPressed;
        buttonContainer.AddChild(dontFollowButton);
    }

    // ✅ YENİ: TextureProgressBar — Inspector'dan özel resim atanabilir
    private void CreateEscortUI()
    {
        healthBar = new TextureProgressBar();
        healthBar.Name = "RobotHealthBar";
        healthBar.CustomMinimumSize = HealthBarSize;
        healthBar.Size = HealthBarSize;
        healthBar.MaxValue = RobotMaxHealth;
        healthBar.Value = RobotMaxHealth;
        healthBar.Position = HealthBarOffset;
        healthBar.Visible = false;

        // ✅ Inspector'dan atanan texture'ları uygula
        if (HealthBarUnderTexture != null)
        {
            healthBar.TextureUnder = HealthBarUnderTexture;
            GD.Print("[ROBOT] ✅ HealthBar Under texture atandı");
        }

        if (HealthBarProgressTexture != null)
        {
            healthBar.TextureProgress = HealthBarProgressTexture;
            GD.Print("[ROBOT] ✅ HealthBar Progress texture atandı");
        }

        if (HealthBarOverTexture != null)
        {
            healthBar.TextureOver = HealthBarOverTexture;
            GD.Print("[ROBOT] ✅ HealthBar Over texture atandı");
        }

        // Texture yoksa varsayılan renk
        if (HealthBarUnderTexture == null && HealthBarProgressTexture == null)
        {
            GD.Print("[ROBOT] ⚠️ HealthBar texture atanmamış, Inspector'dan ayarla!");
            GD.Print("[ROBOT] ⚠️ Şimdilik varsayılan ProgressBar tarzı çalışacak");
        }

        healthBar.FillMode = (int)TextureProgressBar.FillModeEnum.LeftToRight;

        AddChild(healthBar);
    }

    // ========================================
    // PHYSICS
    // ========================================
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= dt;

        UpdateWallRayCastDirection();

        // ✅ Duruma göre hareket
        switch (currentState)
        {
            case RobotState.Idle:
                // ✅ IDLE = YERİNDE DUR!
                velocity.X = 0;
                break;

            case RobotState.EscortMoving:
                EscortMovement(dt);
                break;

            default:
                velocity.X = 0;
                break;
        }

        // Zemin kontrolü
        bool onGround = false;
        if (rayCast2DLeft != null && rayCast2DRight != null)
        {
            onGround = rayCast2DLeft.IsColliding() || rayCast2DRight.IsColliding();
        }

        if (!onGround)
            velocity.Y += gravity * dt;
        else if (velocity.Y > 0)
            velocity.Y = 0;

        // Engel aşma: Sadece EscortMoving sırasında
        if (currentState == RobotState.EscortMoving &&
            IsWallAhead() && onGround && jumpCooldownTimer <= 0)
        {
            velocity.Y = JumpForce;
            jumpCooldownTimer = JumpCooldown;
            GD.Print("[ROBOT] 🦘 Engel! Zıplıyor!");
        }

        GlobalPosition += velocity * dt;
        UpdateAnimation(onGround);
    }

    public override void _Process(double delta)
    {
        if (playerInRange && Input.IsActionJustPressed("interaction"))
            OnInteract();

        // Geri sayım
        if (currentState == RobotState.EscortCountdown)
        {
            countdownTimer -= (float)delta;

            int currentNumber = Mathf.CeilToInt(countdownTimer);
            if (currentNumber != lastCountdownNumber && currentNumber > 0)
            {
                lastCountdownNumber = currentNumber;
                ShowDialog(string.Format(Tr("ROBOT_PREPARE_COUNTDOWN_FORMAT").Replace("\\n", "\n"), currentNumber));
            }

            if (countdownTimer <= 0)
                StartActualEscortMovement();
        }

        // Escort süre takibi
        if (currentState == RobotState.EscortMoving)
        {
            escortTimer -= (float)delta;

            if (playerChoseToFollow)
            {
                playerDistanceCheckTimer -= (float)delta;
                if (playerDistanceCheckTimer <= 0)
                {
                    CheckPlayerFollowing();
                    playerDistanceCheckTimer = playerDistanceCheckInterval;
                }
            }

            if (escortTimer <= 0)
                OnEscortTimeout();
        }
    }

    // ========================================
    // DUVAR ALGILAMA
    // ========================================
    private void UpdateWallRayCastDirection()
    {
        if (wallRayCast == null) return;
        wallRayCast.TargetPosition = new Vector2(WallDetectionDistance * (facingRight ? 1 : -1), 0);
    }

    private bool IsWallAhead()
    {
        if (wallRayCast == null) return false;
        if (Mathf.Abs(velocity.X) < 5) return false;
        return wallRayCast.IsColliding();
    }

    // ========================================
    // ESCORT HAREKET
    // ========================================
    private void EscortMovement(float delta)
    {
        float directionX = escortTargetPosition.X - GlobalPosition.X;
        float distance = Mathf.Abs(directionX);

        if (distance > 20)
        {
            facingRight = directionX > 0;
            velocity.X = (facingRight ? 1 : -1) * EscortSpeed;
        }
        else
        {
            velocity.X = 0;
            OnEscortReachedTarget();
        }
    }

    // ========================================
    // ANİMASYON
    // ========================================
    private void UpdateAnimation(bool onGround)
    {
        if (animatedSprite == null) return;
        animatedSprite.FlipH = !facingRight;

        if (Mathf.Abs(velocity.X) > 5)
            PlayAnimation(onGround ? "ground_walk" : "air_walk");
        else
            PlayAnimation("idle");
    }

    private void PlayAnimation(string animName)
    {
        if (animatedSprite != null &&
            animatedSprite.SpriteFrames != null &&
            animatedSprite.SpriteFrames.HasAnimation(animName) &&
            animatedSprite.Animation != animName)
        {
            animatedSprite.Play(animName);
        }
    }

    // ========================================
    // BODY EVENTS
    // ========================================
    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;
        }

        if (currentState == RobotState.EscortMoving && body.IsInGroup("enemy"))
            TakeRobotDamage(1);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body == player)
        {
            playerInRange = false;
            player = null;
        }
    }

    // ========================================
    // ETKİLEŞİM
    // ========================================
    private void OnInteract()
    {
        GD.Print($"[ROBOT] Etkileşim! Durum: {currentState}");

        switch (currentState)
        {
            case RobotState.Idle:
                ShowQuestSelection();
                break;
            case RobotState.QuestActive:
                CheckQuestProgress();
                break;
            case RobotState.EscortCountdown:
                ShowDialog(Tr("ROBOT_ABOUT_TO_LEAVE"));
                break;
            case RobotState.EscortMoving:
                ShowEscortStatus();
                break;
            case RobotState.Completed:
            case RobotState.EscortCompleted:
                ShowDialog(Tr("ROBOT_THANKS_BYE"));
                break;
        }
    }

    // ========================================
    // GÖREV SEÇİMİ (%60 Collect, %40 Escort)
    // ========================================
    private void ShowQuestSelection()
    {
        float roll = GD.Randf();

        if (roll < 0.6f)
        {
            currentCategory = QuestCategory.Collect;
            TrashQuestType[] quests = {
                TrashQuestType.Plastic, TrashQuestType.Metal,
                TrashQuestType.Glass, TrashQuestType.Wood,
                TrashQuestType.Food, TrashQuestType.Mix
            };
            currentQuest = quests[(int)(GD.Randf() * quests.Length)];
            StartCollectQuest();
        }
        else
        {
            currentCategory = QuestCategory.Escort;
            StartEscortQuestDialog();
        }
    }

    // ========================================
    // COLLECT QUEST
    // ========================================
    private void StartCollectQuest()
    {
        currentState = RobotState.QuestActive;
        int req = requiredAmounts[currentQuest];
        string name = questNames[currentQuest];
        int reward = rewardPoints[currentQuest];

        questTimer.WaitTime = QuestTimeLimit;
        questTimer.Start();

        string desc = currentQuest == TrashQuestType.Mix
            ? string.Format(Tr("ROBOT_QUEST_MIX_FORMAT").Replace("\\n", "\n"), QuestTimeLimit, req, reward)
            : string.Format(Tr("ROBOT_QUEST_SINGLE_FORMAT").Replace("\\n", "\n"), QuestTimeLimit, req, Tr(name), reward);

        ShowDialog(desc + Tr("ROBOT_QUEST_SUFFIX").Replace("\\n", "\n"));
    }

    private void CheckQuestProgress()
    {
        if (player == null) { ShowDialog(Tr("ROBOT_PLAYER_NOT_FOUND")); return; }
        int req = requiredAmounts[currentQuest];
        int has = GetPlayerTrashCount(currentQuest);
        string name = questNames[currentQuest];

        if (has >= req)
            CompleteCollectQuest();
        else
            ShowDialog(string.Format(Tr("ROBOT_QUEST_PROGRESS_FORMAT").Replace("\\n", "\n"), req - has, Tr(name), Mathf.CeilToInt(questTimer.TimeLeft)));
    }

    private int GetPlayerTrashCount(TrashQuestType questType)
    {
        if (player == null) return 0;
        try
        {
            int[] all = (int[])player.Call("GetAllPoints");
            return questType switch
            {
                TrashQuestType.Plastic => all[0],
                TrashQuestType.Metal => all[1],
                TrashQuestType.Glass => all[2],
                TrashQuestType.Food => all[3],
                TrashQuestType.Wood => all[4],
                TrashQuestType.Mix => all[0] + all[1] + all[2] + all[3] + all[4],
                _ => 0
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ROBOT] ❌ GetAllPoints hatası: {ex.Message}");
            return 0;
        }
    }

    private void CompleteCollectQuest()
    {
        questTimer.Stop();
        currentState = RobotState.Completed;
        int req = requiredAmounts[currentQuest];
        int reward = rewardPoints[currentQuest];

        RemoveTrashFromPlayer(currentQuest, req);
        GiveReward(reward);
        ShowDialog(string.Format(Tr("ROBOT_QUEST_REWARD_FORMAT"), reward));
        GetTree().CreateTimer(3.0f).Timeout += () => CallDeferred(Node.MethodName.QueueFree);
    }

    // ========================================
    // ESCORT QUEST
    // ========================================
    private void StartEscortQuestDialog()
    {
        currentState = RobotState.EscortWaiting;
        escortTargetPosition = FindEscortTarget();

        float dist = GlobalPosition.DistanceTo(escortTargetPosition);
        int totalReward = EscortBaseReward + EscortBonusReward;
        string direction = escortTargetPosition.X > GlobalPosition.X ? Tr("ROBOT_ESCORT_DIRECTION_RIGHT") : Tr("ROBOT_ESCORT_DIRECTION_LEFT");

        string text = string.Format(Tr("ROBOT_ESCORT_REQUEST_FORMAT").Replace("\\n", "\n"), direction, dist.ToString("F0"), totalReward);

        ShowDialogWithButtons(text);
    }

    private Vector2 FindEscortTarget()
    {
        var targets = GetTree().GetNodesInGroup("escort_target");
        if (targets.Count > 0)
        {
            int idx = (int)(GD.Randf() * targets.Count);
            if (targets[idx] is Node2D t) return t.GlobalPosition;
        }
        float dir = GD.Randf() > 0.5f ? 1 : -1;
        return GlobalPosition + new Vector2(dir * (float)GD.RandRange(800, 1500), 0);
    }

    private void OnFollowButtonPressed()
    {
        playerChoseToFollow = true;
        playerIsFollowing = true;
        StartEscortCountdown();
    }

    private void OnDontFollowButtonPressed()
    {
        playerChoseToFollow = false;
        playerIsFollowing = false;
        StartEscortCountdown();
    }

    private void StartEscortCountdown()
    {
        currentState = RobotState.EscortCountdown;
        countdownTimer = EscortCountdownTime;
        lastCountdownNumber = Mathf.CeilToInt(EscortCountdownTime) + 1;
        HideDialog();

        string msg = playerChoseToFollow
            ? string.Format(Tr("ROBOT_COUNTDOWN_FOLLOW_FORMAT").Replace("\\n", "\n"), Mathf.CeilToInt(EscortCountdownTime))
            : string.Format(Tr("ROBOT_COUNTDOWN_ALONE_FORMAT").Replace("\\n", "\n"), Mathf.CeilToInt(EscortCountdownTime));
        ShowDialog(msg);
    }

    private void StartActualEscortMovement()
    {
        currentState = RobotState.EscortMoving;
        escortTimer = EscortTimeLimit;
        robotHealth = RobotMaxHealth;
        escortReachedTarget = false;
        playerDistanceCheckTimer = playerDistanceCheckInterval;
        HideDialog();

        if (playerChoseToFollow)
        {
            healthBar.Visible = true;
            healthBar.MaxValue = RobotMaxHealth;
            healthBar.Value = RobotMaxHealth;
            ShowDialog(string.Format(Tr("ROBOT_ESCORT_START_FOLLOW_FORMAT").Replace("\\n", "\n"), EscortTimeLimit));
        }
        else
        {
            ShowDialog(Tr("ROBOT_ESCORT_START_ALONE"));
        }
    }

    private void CheckPlayerFollowing()
    {
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) player = players[0] as Node2D;
        }
        if (player != null)
            playerIsFollowing = GlobalPosition.DistanceTo(player.GlobalPosition) <= PlayerFollowCheckRadius;
    }

    private void ShowEscortStatus()
    {
        if (currentState != RobotState.EscortMoving) return;
        float dist = GlobalPosition.DistanceTo(escortTargetPosition);

        string status = playerChoseToFollow
            ? string.Format(Tr("ROBOT_ESCORT_STATUS_FOLLOW_FORMAT").Replace("\\n", "\n"), dist.ToString("F0"), escortTimer.ToString("F0"), robotHealth, RobotMaxHealth, playerIsFollowing ? Tr("ROBOT_ESCORT_FOLLOWING_YES") : Tr("ROBOT_ESCORT_FOLLOWING_NO"))
            : string.Format(Tr("ROBOT_ESCORT_STATUS_ALONE_FORMAT").Replace("\\n", "\n"), dist.ToString("F0"));
        ShowDialog(status);
    }

    // ========================================
    // ROBOT HASAR
    // ========================================
    public void TakeRobotDamage(int damage)
    {
        if (currentState != RobotState.EscortMoving) return;
        robotHealth -= damage;
        if (healthBar != null) healthBar.Value = robotHealth;
        if (robotHealth <= 0) OnRobotDestroyed();
    }

    public void TakeDamage(int damage) => TakeRobotDamage(damage);

    private void OnRobotDestroyed()
    {
        currentState = RobotState.Completed;
        ShowDialog(Tr("ROBOT_DESTROYED").Replace("\\n", "\n"));
        GetTree().CreateTimer(3.0f).Timeout += () => CallDeferred(Node.MethodName.QueueFree);
    }

    private void OnEscortReachedTarget()
    {
        if (escortReachedTarget) return;
        escortReachedTarget = true;
        currentState = RobotState.EscortCompleted;
        healthBar.Visible = false;

        if (playerChoseToFollow)
        {
            bool nearby = player != null &&
                GlobalPosition.DistanceTo(player.GlobalPosition) <= PlayerFollowCheckRadius;

            if (nearby)
            {
                int total = EscortBaseReward + EscortBonusReward;
                GiveReward(total);
                ShowDialog(string.Format(Tr("ROBOT_ESCORT_REWARD_FULL_FORMAT").Replace("\\n", "\n"), EscortBaseReward, EscortBonusReward, total));
            }
            else
            {
                GiveReward(EscortBaseReward);
                ShowDialog(string.Format(Tr("ROBOT_ESCORT_REWARD_PARTIAL_FORMAT").Replace("\\n", "\n"), EscortBaseReward));
            }
        }
        else
        {
            ShowDialog(Tr("ROBOT_ESCORT_REACHED_ALONE"));
        }
        GetTree().CreateTimer(4.0f).Timeout += () => CallDeferred(Node.MethodName.QueueFree);
    }

    private void OnEscortTimeout()
    {
        currentState = RobotState.Completed;
        healthBar.Visible = false;
        ShowDialog(Tr("ROBOT_ESCORT_TIMEOUT"));
        GetTree().CreateTimer(3.0f).Timeout += () => CallDeferred(Node.MethodName.QueueFree);
    }

    // ========================================
    // ÇÖP ÇIKARMA
    // ========================================
    private void RemoveTrashFromPlayer(TrashQuestType questType, int amount)
    {
        if (player == null) return;
        switch (questType)
        {
            case TrashQuestType.Plastic:
                if (player.HasMethod("AddPlastic")) player.Call("AddPlastic", -amount); break;
            case TrashQuestType.Metal:
                if (player.HasMethod("AddMetal")) player.Call("AddMetal", -amount); break;
            case TrashQuestType.Glass:
                if (player.HasMethod("AddGlass")) player.Call("AddGlass", -amount); break;
            case TrashQuestType.Food:
                if (player.HasMethod("AddFood")) player.Call("AddFood", -amount); break;
            case TrashQuestType.Wood:
                if (player.HasMethod("AddWood")) player.Call("AddWood", -amount); break;
            case TrashQuestType.Mix:
                RemoveMixedTrashRoundRobin(amount); break;
        }
    }

    private void RemoveMixedTrashRoundRobin(int totalAmount)
    {
        if (player == null) return;
        try
        {
            int[] cur = (int[])player.Call("GetAllPoints");
            int remaining = totalAmount;
            int round = 0;
            while (remaining > 0 && round < 100)
            {
                bool any = false;
                for (int i = 0; i < 5 && remaining > 0; i++)
                {
                    if (cur[i] > 0)
                    {
                        int rm = Mathf.Min(cur[i], Mathf.Min(5, remaining));
                        string[] m = { "AddPlastic", "AddMetal", "AddGlass", "AddFood", "AddWood" };
                        player.Call(m[i], -rm);
                        cur[i] -= rm;
                        remaining -= rm;
                        any = true;
                    }
                }
                if (!any) break;
                round++;
            }
        }
        catch (Exception ex) { GD.PrintErr($"[ROBOT] ❌ {ex.Message}"); }
    }

    // ========================================
    // TIMER & REWARD
    // ========================================
    private void OnQuestTimeout()
    {
        currentState = RobotState.Completed;
        ShowDialog(Tr("ROBOT_QUEST_TIMEOUT"));
        GetTree().CreateTimer(3.0f).Timeout += () => CallDeferred(Node.MethodName.QueueFree);
    }

    private void GiveReward(int points)
    {
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) player = players[0] as Node2D;
        }
        if (player != null && player.HasMethod("UpdateTeacherScore"))
        {
            player.Call("UpdateTeacherScore", points);
            GD.Print($"[ROBOT] ✅ {points} puan verildi!");
        }
    }

    // ========================================
    // DİYALOG
    // ========================================
    private void ShowDialog(string text)
    {
        if (dialogPanel != null && dialogLabel != null)
        {
            dialogLabel.Text = text;
            buttonContainer.Visible = false;
            dialogPanel.Visible = true;
            GetTree().CreateTimer(5.0f).Timeout += HideDialog;
        }
    }

    private void ShowDialogWithButtons(string text)
    {
        if (dialogPanel != null && dialogLabel != null)
        {
            dialogLabel.Text = text;
            buttonContainer.Visible = true;
            dialogPanel.Visible = true;
        }
    }

    private void HideDialog()
    {
        if (dialogPanel != null)
        {
            dialogPanel.Visible = false;
            buttonContainer.Visible = false;
        }
    }
}