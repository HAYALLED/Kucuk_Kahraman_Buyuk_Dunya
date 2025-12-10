using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Player_controller : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -500.0f;
    [Export] public float Acceleration = 5000f;
    [Export] public float Friction = 8000f;
    [Export] public float AirAcceleration = 1500f;
    [Export] public float AirFriction = 800f;
    [Export] public float CoyoteTime = 0.15f;
    [Export] public float JumpBufferTime = 0.1f;
    [Export] public float GravityScale = 0.9f;

    // Costume slot UI
    private List<TextureRect> costumeSlotIcons = new List<TextureRect>();
    private int currentCostumeIndex = -1;

    // Kostüm sistemi
    [Export] public CostumeResource CurrentCostume;
    [Export] public CostumeResource[] CostumeSlots = new CostumeResource[3];

    // ===== SCORES UI =====
    private Label trashCountLabel;
    private Label currentScoreLabel;
    private Label requiredScoreLabel;

    // ===== AKTİF YETENEKLER (Kostümden okunur) =====
    private bool canWallClimb = false;
    private bool canSwing = false;
    private bool canGrapple = false;
    private bool canFly = false;
    private float damageMultiplier = 1.0f;

    // ===== SUPERMAN FLY =====
    private float flyTimeDuration = 15.0f;
    private float flyTimeCooldown = 30.0f;
    private float flyEfficiency = 1.0f;
    private float flyTimer = 0;
    private float flyCooldownTimer = 0;
    private bool isFlying = false;

    // ===== SPIDERMAN SWING =====
    private bool isSwinging = false;
    private Vector2 swingAnchorPoint;
    private float swingAngle = 0;
    private float swingAngularVelocity = 0;
    private float swingRadius = 150f;
    private float swingGravity = 35f;
    private float swingDamping = 0.995f;
    private float swingMaxDuration = 6.0f;
    private float swingTimer = 0;
    private float swingCooldown = 0.1f;
    private float swingCooldownTimer = 0;
    private Line2D webLine;
    private Sprite2D webAnchorSprite;
    [Export] public Texture2D WebAnchorTexture;

    // ===== BATMAN GRAPPLE =====
    private bool isGrappling = false;
    private Vector2 grappleTargetPoint;
    private float grappleSpeed = 600f;
    private float grappleCooldown = 0.1f;
    private float grappleCooldownTimer = 0;
    private Line2D hookLine;
    private Sprite2D hookSprite;
    [Export] public Texture2D HookTexture;
    // ===== AQUAMAN ÖZEL =====
    private float aquamanStunCooldown = 25.0f;
    private float aquamanStunCooldownTimer = 0;
    private float aquamanStunRadius = 200f;
    private float aquamanAttackRange = 1.0f; // Çarpan (1.0 = normal, 2.0 = 2kat)
    // ===== INTERACTION =====
    private bool isNearInteractable = false;
    private Node2D currentInteractable = null;
    private Area2D interactionDetector;

    // Hover
    private bool canHover = false;
    private float hoverGravityMultiplier = 0.5f;

    // Projectile
    private bool canThrowProjectile = false;
    private int projectileDamage = 1;
    private float projectileCooldown = 1.0f;
    private float projectileCooldownTimer = 0;
    private bool projectileCanStun = false;
    private int projectileStunHitCount = 3;
    private float projectileStunDuration = 2.0f;
    private PackedScene projectileScene;

    // Plant
    private bool canPlantProjectile = false;
    private int maxProjectilePlants = 3;
    private int plantDamage = 1;
    private float plantExplosionRadius = 50.0f;
    private PackedScene plantScene;
    private List<Node2D> activePlants = new List<Node2D>();

    // Drone
    private bool hasDroneSupport = false;
    private float droneCollectInterval = 45.0f;
    private float droneCollectRadius = 200.0f;
    private float droneTimer = 0;

    // Froze Time
    private bool canFrozeTime = false;
    private float frozeTimeSlowPercent = 0.3f;
    private float frozeTimeDuration = 5.0f;
    private float frozeTimeCooldown = 30.0f;
    private float frozeTimeCooldownTimer = 0;
    private bool isFrozeTimeActive = false;

    // Wall Jump
    private bool canWallJump = false;
    private int maxWallJumps = 1;
    private float wallJumpEfficiency = 1.0f;
    private int wallJumpsRemaining = 0;

    // Teleport
    private bool canTeleport = false;
    private float teleportDistance = 100.0f;
    private float teleportCooldown = 3.0f;
    private float teleportCooldownTimer = 0;
    private bool teleportPreventsFalling = true;

    // Jump
    private float jumpEfficiency = 1.0f;
    private float speedMultiplier = 1.0f;

    // ===== MEVCUT DEĞİŞKENLER =====
    private int jumpsRemaining = 0;
    private float coyoteTimer = 0.0f;
    private float jumpBufferTimer = 0.0f;
    private AnimatedSprite2D animatedSprite;
    private bool facingRight = true;
    private Dictionary<int, int> costumeHealthStates = new Dictionary<int, int>();
    [Export] public int MaxHealth = 1;
    private bool isClimbing = false;
    private float climbSpeed = 200f;
    // Puan sistemi
    private int metalCount = 0;
    private int glassCount = 0;
    private int plasticCount = 0;
    private int foodCount = 0;
    private int woodCount = 0;
    public int TotalPoints => metalCount + glassCount + plasticCount + foodCount + woodCount;

    private int currentHealth;
    private List<AnimatedSprite2D> heartSprites = new List<AnimatedSprite2D>();
    private bool isDead = false;

    [Export] public float InvincibilityTime = 1.0f;
    private float invincibilityTimer = 0;
    private AnimatedSprite2D playerSprite;
    private Area2D attackArea;
    private bool isAttacking = false;
    [Export] public float AttackDuration = 0.3f;

    private CollisionShape2D attackCollision;
    private int comboCount = 0;
    private float comboTimer = 0;
    [Export] public float ComboResetTime = 0.8f;
    [Export] public float AttackCooldown = 0.2f;
    private float attackCooldownTimer = 0;

    public override void _Ready()
    {
        GD.Print("========== PLAYER READY ==========");

        playerSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite = playerSprite;

        currentHealth = MaxHealth;
        FindHeartNodes();
        UpdateHealthUI();

        jumpsRemaining = 1;

        CreateAttackArea();
        AddToGroup("player");

        // İlk kostümü başlat
        if (currentCostumeIndex < 0)
        {
            for (int i = 0; i < CostumeSlots.Length; i++)
            {
                if (CostumeSlots[i] != null)
                {
                    currentCostumeIndex = i;
                    CurrentCostume = CostumeSlots[i];
                    ApplyCostume();
                    break;
                }
            }
        }

        FindCostumeSlotUI();
        UpdateCostumeSlotUI();
        FindScoresUI();

        // ✅ Ability görselleri oluştur
        CreateAbilityVisuals();

        // ✅ Interaction detector oluştur
        CreateInteractionDetector();

        GD.Print("========== READY BİTTİ ==========");
    }

    // ========================================
    // GÖRSEL EFEKTLER OLUŞTUR
    // ========================================
    private void CreateAbilityVisuals()
    {
        // ===== WEB LINE (Spiderman Swing) =====
        webLine = new Line2D();
        webLine.Name = "WebLine";
        webLine.Width = 8;  // ⚡ 2 → 8 (texture için daha kalın)
        webLine.DefaultColor = Colors.White;  // ✅ Texture rengi
        webLine.Visible = false;
        webLine.ZIndex = -1;
        webLine.TopLevel = true;
        // ✅ YENİ: TEXTURE EKLE!
        if (WebAnchorTexture != null)
        {
            webLine.Texture = WebAnchorTexture;
            webLine.TextureMode = Line2D.LineTextureMode.Stretch;  // Texture'ı uzat
        }
        AddChild(webLine);
        // ✅ FIX: Web Anchor Sprite - Player'a child olarak ekle
        webAnchorSprite = new Sprite2D();
        webAnchorSprite.Name = "WebAnchor";
        webAnchorSprite.Visible = false;
        webAnchorSprite.ZIndex = 10;
        webAnchorSprite.TopLevel = true; // ✅ Global pozisyon kullan
        if (WebAnchorTexture != null)
            webAnchorSprite.Texture = WebAnchorTexture;
        AddChild(webAnchorSprite);

        // ===== HOOK LINE (Batman Grapple) =====
        hookLine = new Line2D();
        hookLine.Name = "HookLine";
        hookLine.Width = 10;  // ⚡ 3 → 10 (texture için daha kalın)
        hookLine.DefaultColor = Colors.White;  // ✅ Texture rengi
        hookLine.Visible = false;
        hookLine.ZIndex = -1;
        hookLine.TopLevel = true;

        // ✅ YENİ: TEXTURE EKLE!
        if (HookTexture != null)
        {
            hookLine.Texture = HookTexture;
            hookLine.TextureMode = Line2D.LineTextureMode.Stretch;  // Texture'ı uzat
        }

        AddChild(hookLine);
        // ✅ FIX: Hook Sprite - Player'a child olarak ekle
        hookSprite = new Sprite2D();
        hookSprite.Name = "HookSprite";
        hookSprite.Visible = false;
        hookSprite.ZIndex = 10;
        hookSprite.TopLevel = true; // ✅ Global pozisyon kullan
        if (HookTexture != null)
            hookSprite.Texture = HookTexture;
        AddChild(hookSprite);

        GD.Print("[VISUALS] ✅ Ability görselleri oluşturuldu!");
    }

    // ========================================
    // INTERACTION DETECTOR
    // ========================================
    private void CreateInteractionDetector()
    {
        interactionDetector = new Area2D();
        interactionDetector.Name = "InteractionDetector";
        interactionDetector.CollisionLayer = 0;
        // ✅ FIX: Hem Layer 4 (8) hem de diğer interactable layer'ları dinle
        interactionDetector.CollisionMask = 8 | 16 | 32; // Layer 4, 5, 6

        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 60;
        shape.Shape = circle;
        interactionDetector.AddChild(shape);

        // ✅ FIX: Hem Body hem Area signal'lerini bağla
        interactionDetector.BodyEntered += OnInteractableBodyEntered;
        interactionDetector.BodyExited += OnInteractableBodyExited;
        interactionDetector.AreaEntered += OnInteractableAreaEntered;
        interactionDetector.AreaExited += OnInteractableAreaExited;

        AddChild(interactionDetector);
        GD.Print("[INTERACTION] ✅ Detector oluşturuldu! Mask: " + interactionDetector.CollisionMask);
    }

    private void OnInteractableBodyEntered(Node2D body)
    {
        if (body.IsInGroup("interactable") || body.IsInGroup("npc") || body.IsInGroup("building"))
        {
            isNearInteractable = true;
            currentInteractable = body;
            GD.Print($"[INTERACTION] ✅ Yaklaşıldı (Body): {body.Name}");
        }
    }

    private void OnInteractableBodyExited(Node2D body)
    {
        if (body == currentInteractable)
        {
            isNearInteractable = false;
            currentInteractable = null;
            GD.Print($"[INTERACTION] Uzaklaşıldı (Body): {body.Name}");
        }
    }

    private void OnInteractableAreaEntered(Area2D area)
    {
        if (area.IsInGroup("interactable") || area.IsInGroup("npc") || area.IsInGroup("building"))
        {
            isNearInteractable = true;
            currentInteractable = area;
            GD.Print($"[INTERACTION] ✅ Yaklaşıldı (Area): {area.Name}");
        }
    }

    private void OnInteractableAreaExited(Area2D area)
    {
        if (area == currentInteractable)
        {
            isNearInteractable = false;
            currentInteractable = null;
            GD.Print($"[INTERACTION] Uzaklaşıldı (Area): {area.Name}");
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null)
        {
            GD.Print("[INTERACTION] ❌ currentInteractable NULL!");
            return;
        }

        GD.Print($"[INTERACTION] ✅ Etkileşim başlatılıyor: {currentInteractable.Name}");

        // ✅ Farklı metod isimlerini dene
        if (currentInteractable.HasMethod("Interact"))
        {
            GD.Print("[INTERACTION] Interact() çağrılıyor...");
            currentInteractable.Call("Interact", this);
        }
        else if (currentInteractable.HasMethod("OnInteract"))
        {
            GD.Print("[INTERACTION] OnInteract() çağrılıyor...");
            currentInteractable.Call("OnInteract", this);
        }
        else if (currentInteractable.HasMethod("_on_player_interact"))
        {
            GD.Print("[INTERACTION] _on_player_interact() çağrılıyor...");
            currentInteractable.Call("_on_player_interact", this);
        }
        else
        {
            GD.PrintErr($"[INTERACTION] ❌ {currentInteractable.Name} için Interact metodu bulunamadı!");
        }
    }

    private void FindScoresUI()
    {
        GD.Print("[UI] ========== SCORES UI ARAMA BAŞLIYOR ==========");

        var scoresLayer = GetNodeOrNull<CanvasLayer>("scores");
        if (scoresLayer == null)
        {
            GD.PrintErr("[UI] ❌ scores CanvasLayer bulunamadı!");
            return;
        }

        scoresLayer.Layer = 100;

        try
        {
            trashCountLabel = scoresLayer.GetNode<Label>("VBoxContainer/trashCountLabel");
            GD.Print("[UI] ✅ trashCountLabel bulundu!");
        }
        catch
        {
            GD.PrintErr("[UI] ❌ trashCountLabel bulunamadı!");
        }

        try
        {
            currentScoreLabel = scoresLayer.GetNode<Label>("VBoxContainer/currentScoreLabel");
            GD.Print("[UI] ✅ currentScoreLabel bulundu!");
        }
        catch
        {
            GD.PrintErr("[UI] ❌ currentScoreLabel bulunamadı!");
        }

        try
        {
            requiredScoreLabel = scoresLayer.GetNode<Label>("VBoxContainer/requiredScoreLabel");
            GD.Print("[UI] ✅ requiredScoreLabel bulundu!");
        }
        catch
        {
            GD.PrintErr("[UI] ❌ requiredScoreLabel bulunamadı!");
        }

        if (trashCountLabel != null)
        {
            trashCountLabel.Visible = true;
            trashCountLabel.Modulate = Colors.White;
            trashCountLabel.Text = "Çöp: 0";
        }

        if (currentScoreLabel != null)
        {
            currentScoreLabel.Visible = true;
            currentScoreLabel.Modulate = Colors.White;
            currentScoreLabel.Text = "Skor: 0";
        }

        if (requiredScoreLabel != null)
        {
            requiredScoreLabel.Visible = true;
            requiredScoreLabel.Modulate = Colors.White;
            requiredScoreLabel.Text = "Hedef: 100";
        }

        GD.Print("[UI] ========== SCORES UI ARAMA BİTTİ ==========");
    }

    public void UpdateScoresUI(int currentLevelScore = 0, int requiredScore = 0)
    {
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";

        if (currentScoreLabel != null)
            currentScoreLabel.Text = $"Skor: {currentLevelScore}";

        if (requiredScoreLabel != null)
            requiredScoreLabel.Text = $"Hedef: {requiredScore}";
    }

    private void FindCostumeSlotUI()
    {
        costumeSlotIcons.Clear();

        var costumeSlots = GetNodeOrNull<CanvasLayer>("costume_slots");
        if (costumeSlots == null)
        {
            GD.Print("[UI] costume_slots bulunamadı!");
            return;
        }

        var hbox = costumeSlots.GetNodeOrNull<HBoxContainer>("HBoxContainer");
        if (hbox == null)
        {
            GD.Print("[UI] HBoxContainer bulunamadı!");
            return;
        }

        foreach (Node child in hbox.GetChildren())
        {
            if (child is TextureRect slotRect)
            {
                foreach (Node subChild in slotRect.GetChildren())
                {
                    if (subChild is TextureRect iconRect)
                    {
                        costumeSlotIcons.Add(iconRect);
                        break;
                    }
                }
            }
        }

        GD.Print($"[UI] Toplam {costumeSlotIcons.Count} kostüm slot'u bulundu!");
    }

    private void UpdateCostumeSlotUI()
    {
        for (int i = 0; i < costumeSlotIcons.Count; i++)
        {
            if (i < CostumeSlots.Length && CostumeSlots[i] != null)
            {
                if (CostumeSlots[i].Icon != null)
                {
                    costumeSlotIcons[i].Texture = CostumeSlots[i].Icon;
                }
                costumeSlotIcons[i].Visible = true;

                if (i == currentCostumeIndex)
                {
                    costumeSlotIcons[i].Modulate = new Color(1, 1, 1, 1);
                }
                else
                {
                    costumeSlotIcons[i].Modulate = new Color(0.5f, 0.5f, 0.5f, 1);
                }
            }
            else
            {
                costumeSlotIcons[i].Texture = null;
                costumeSlotIcons[i].Visible = false;
            }
        }
    }
    // ========================================
    // ✅ FIX: _PhysicsProcess - DOĞRU SATIR SIRASI
    // ========================================
    // SATIR ~465-560 (TAMAMI DEĞİŞTİR)

    public override void _PhysicsProcess(double delta)
    {
        if (isDead) return;

        float dt = (float)delta;

        HandleCostumeSwitch();
        HandleInvincibility(dt);
        UpdateCooldowns(dt);

        // ✅ Swing aktifse özel fizik
        if (isSwinging)
        {
            UpdateSwing(dt);
            UpdateAnimations();
            return;
        }

        // ✅ Grapple aktifse özel fizik
        if (isGrappling)
        {
            UpdateGrapple(dt);
            UpdateAnimations();
            return;
        }

        // ✅ CRITICAL FIX: velocity değişkenini ÖNCE tanımla!
        Vector2 velocity = Velocity;

        // ✅ YENİ: Climbing kontrolü (velocity tanımlandıktan SONRA)
        if (canWallClimb && IsOnWall() && Input.IsActionPressed("climb"))
        {
            HandleClimbing(ref velocity, dt);
            Velocity = velocity;
            MoveAndSlide();
            UpdateAnimations();
            return;
        }
        else
        {
            isClimbing = false;
        }

        // ✅ Flying aktifken yerçekimi farklı
        if (isFlying)
        {
            HandleFlyingPhysics(ref velocity, dt);
        }
        else
        {
            HandleGravity(ref velocity, dt);
        }

        if (IsOnFloor())
        {
            coyoteTimer = CoyoteTime;
            jumpsRemaining = 1;
            wallJumpsRemaining = maxWallJumps;
        }
        else
        {
            coyoteTimer -= dt;
        }

        if (jumpBufferTimer > 0)
            jumpBufferTimer -= dt;

        if (Input.IsActionJustPressed("jump"))
            jumpBufferTimer = JumpBufferTime;

        if (!isFlying)
        {
            HandleJump(ref velocity);
            HandleWallJump(ref velocity);
        }

        HandleMovement(ref velocity, dt);
        HandleAbilities(dt);

        if (comboTimer > 0)
        {
            comboTimer -= dt;
            if (comboTimer <= 0)
                comboCount = 0;
        }

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= dt;

        HandleAttack();

        Velocity = velocity;
        MoveAndSlide();

        UpdateAnimations();
    }

    private void HandleCostumeSwitch()
    {
        if (Input.IsActionJustPressed("costume_1"))
        {
            EquipCostume(0);
        }
        else if (Input.IsActionJustPressed("costume_2"))
        {
            EquipCostume(1);
        }
        else if (Input.IsActionJustPressed("costume_3"))
        {
            EquipCostume(2);
        }
    }
    private void UpdateCooldowns(float delta)
    {
        if (projectileCooldownTimer > 0)
            projectileCooldownTimer -= delta;

        if (teleportCooldownTimer > 0)
            teleportCooldownTimer -= delta;

        if (flyCooldownTimer > 0)
            flyCooldownTimer -= delta;

        if (frozeTimeCooldownTimer > 0)
            frozeTimeCooldownTimer -= delta;

        if (swingCooldownTimer > 0)
            swingCooldownTimer -= delta;

        if (grappleCooldownTimer > 0)
            grappleCooldownTimer -= delta;

        // ✅ YENİ: Aquaman cooldown
        if (aquamanStunCooldownTimer > 0)
            aquamanStunCooldownTimer -= delta;

        if (droneTimer > 0)
        {
            droneTimer -= delta;
            if (droneTimer <= 0 && hasDroneSupport)
            {
                DroneCollect();
                droneTimer = droneCollectInterval;
            }
        }
    }
    private void ApplyCostumeAbilities()
    {
        if (CurrentCostume == null) return;

        canWallClimb = CurrentCostume.CanWallClimb;
        canSwing = CurrentCostume.CanSwing;
        canGrapple = CurrentCostume.CanGrapple;
        canFly = CurrentCostume.CanFly;
        damageMultiplier = CurrentCostume.DamageMultiplier;

        flyTimeDuration = CurrentCostume.FlyTimeDuration;
        flyTimeCooldown = CurrentCostume.FlyTimeCooldown;
        flyEfficiency = CurrentCostume.FlyEfficiency;

        canHover = CurrentCostume.CanHover;
        hoverGravityMultiplier = CurrentCostume.HoverGravityMultiplier;

        canThrowProjectile = CurrentCostume.CanThrowProjectile;
        projectileDamage = CurrentCostume.ProjectileDamage;
        projectileCooldown = CurrentCostume.ProjectileCooldown;
        projectileCanStun = CurrentCostume.ProjectileCanStun;
        projectileStunHitCount = CurrentCostume.ProjectileStunHitCount;
        projectileStunDuration = CurrentCostume.ProjectileStunDuration;
        projectileScene = CurrentCostume.ProjectileScene;

        canPlantProjectile = CurrentCostume.CanPlantProjectile;
        maxProjectilePlants = CurrentCostume.MaxProjectilePlants;
        plantDamage = CurrentCostume.PlantDamage;
        plantExplosionRadius = CurrentCostume.PlantExplosionRadius;
        plantScene = CurrentCostume.PlantScene;

        hasDroneSupport = CurrentCostume.HasDroneSupport;
        droneCollectInterval = CurrentCostume.DroneCollectInterval;
        droneCollectRadius = CurrentCostume.DroneCollectRadius;
        if (hasDroneSupport) droneTimer = droneCollectInterval;

        canFrozeTime = CurrentCostume.CanFrozeTime;
        frozeTimeSlowPercent = CurrentCostume.FrozeTimeSlowPercent;
        frozeTimeDuration = CurrentCostume.FrozeTimeDuration;
        frozeTimeCooldown = CurrentCostume.FrozeTimeCooldown;

        canWallJump = CurrentCostume.CanWallJump;
        maxWallJumps = CurrentCostume.MaxWallJumps;
        wallJumpEfficiency = CurrentCostume.WallJumpEfficiency;
        wallJumpsRemaining = maxWallJumps;

        canTeleport = CurrentCostume.CanTeleport;
        teleportDistance = CurrentCostume.TeleportDistance;
        teleportCooldown = CurrentCostume.TeleportCooldown;
        teleportPreventsFalling = CurrentCostume.TeleportPreventsFalling;

        jumpEfficiency = CurrentCostume.JumpEfficiency;
        speedMultiplier = CurrentCostume.SpeedEfficiency;

        // ✅ YENİ: Aquaman özel yetenekler
        if (CurrentCostume.CostumeName == "aquaBoy")
        {
            aquamanAttackRange = 2.0f; // Saldırı menzili 2x
            GD.Print("[AQUAMAN] Pasif: Saldırı menzili 2x!");
        }
        else
        {
            aquamanAttackRange = 1.0f;
        }

        GD.Print($"[COSTUME] Yetenekler: Fly={canFly}, Swing={canSwing}, Grapple={canGrapple}");
    }

    private void HandleGravity(ref Vector2 velocity, float delta)
    {
        if (!IsOnFloor())
        {
            float gravityMult = 1.0f;

            if (canHover && Input.IsActionPressed("jump") && velocity.Y > 0)
            {
                gravityMult = hoverGravityMultiplier;
            }

            velocity += GetGravity() * GravityScale * gravityMult * delta;
        }
    }

    // ========================================
    // SUPERMAN - FLY SİSTEMİ
    // ========================================
    private void HandleFlyingPhysics(ref Vector2 velocity, float delta)
    {
        flyTimer -= delta;

        if (flyTimer <= 0)
        {
            StopFlying();
            return;
        }

        if (Input.IsActionPressed("jump"))
        {
            velocity.Y = -250 * flyEfficiency;
        }
        else if (Input.IsActionPressed("ui_down"))
        {
            velocity.Y = 200 * flyEfficiency;
        }
        else
        {
            velocity.Y += GetGravity().Y * 0.3f * delta;
            if (velocity.Y > 150) velocity.Y = 150;
        }
    }

    private void StartFlying()
    {
        if (isFlying) return;

        isFlying = true;
        flyTimer = flyTimeDuration;

        GD.Print($"[FLY] ✅ Superman uçuşu başladı! Süre: {flyTimeDuration}sn");
    }

    private void StopFlying()
    {
        if (!isFlying) return;

        isFlying = false;
        flyCooldownTimer = flyTimeCooldown;

        GD.Print($"[FLY] Uçuş bitti! Cooldown: {flyTimeCooldown}sn");
    }

    // ========================================
    // SPIDERMAN - SWING SİSTEMİ
    private void TryStartSwing()
    {
        if (swingCooldownTimer > 0)
        {
            GD.Print($"[SWING] ⏱️ Cooldown: {swingCooldownTimer:F1}sn");
            return;
        }

        var spaceState = GetWorld2D().DirectSpaceState;

        // ✅ DAHA FAZLA YÖN - 10 farklı açı
        Vector2[] directions = new Vector2[]
        {
    new Vector2(0, -400),       // Tam yukarı
    new Vector2(0, -300),
    new Vector2(0, -200),
    new Vector2(-350, -300),    // Sol üst
    new Vector2(350, -300),     // Sağ üst
    new Vector2(-400, -200),
    new Vector2(400, -200),
    new Vector2(-300, -150),
    new Vector2(300, -150),
    new Vector2(-250, -100),
    new Vector2(250, -100),
    new Vector2(-400, 0),       // ✅ YENİ: Yanlara
    new Vector2(400, 0),
    new Vector2(-300, 100),     // ✅ YENİ: Aşağıya
    new Vector2(300, 100),
        };

        Vector2? bestAnchor = null;
        float bestScore = float.MinValue;
        int hitCount = 0;

        foreach (var dir in directions)
        {
            var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + dir);
            query.CollisionMask = 1;
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;

            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                hitCount++;
                Vector2 hitPoint = (Vector2)result["position"];
                float distance = GlobalPosition.DistanceTo(hitPoint);

                // ✅ MESAFE GEVŞETİLDİ: 50-400px (önceden 80-280)
                if (distance >= 30 && distance <= 1000)  // ⚡ 30-1000px
                {
                    float heightBonus = GlobalPosition.Y - hitPoint.Y;

                    float directionBonus = facingRight ? hitPoint.X - GlobalPosition.X : GlobalPosition.X - hitPoint.X;
                    float score = heightBonus * 1.5f + directionBonus * 0.3f - distance * 0.05f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAnchor = hitPoint;
                        GD.Print($"[SWING DEBUG] ✅ Potansiyel: {hitPoint}, mesafe: {distance:F0}px, yükseklik: {heightBonus:F0}px, skor: {score:F1}");
                    }
                }
            }
        }

        GD.Print($"[SWING DEBUG] Toplam {hitCount} raycast çarpması bulundu");

        if (bestAnchor.HasValue)
        {
            StartSwing(bestAnchor.Value);
        }
        else
        {
            GD.Print("[SWING] ❌ Tutunacak platform bulunamadı! (Yukarıda 50-400px arası platform olmalı)");
        }
    }

    private void StartSwing(Vector2 anchorPoint)
    {
        isSwinging = true;
        swingAnchorPoint = anchorPoint;
        swingRadius = GlobalPosition.DistanceTo(anchorPoint);
        swingTimer = swingMaxDuration;

        Vector2 diff = GlobalPosition - anchorPoint;
        swingAngle = Mathf.Atan2(diff.X, diff.Y);

        float tangentialVelocity = facingRight ? 7.0f : -7.0f;
        if (Mathf.Abs(Velocity.X) > 50)
        {
            tangentialVelocity = Velocity.X / swingRadius * 1.0f;
        }
        swingAngularVelocity = tangentialVelocity;

        // ✅ FIX: Web görselini güncelle (TopLevel = true olduğu için global pozisyon)
        if (webLine != null)
        {
            webLine.ClearPoints();
            webLine.AddPoint(GlobalPosition);
            webLine.AddPoint(anchorPoint);
            webLine.Visible = true;
        }

        if (webAnchorSprite != null)
        {
            webAnchorSprite.GlobalPosition = anchorPoint;
            webAnchorSprite.Visible = true;
        }

        GD.Print($"[SWING] ✅ Swing başladı! Anchor: {anchorPoint}, Radius: {swingRadius:F0}");
    }

    private void UpdateSwing(float delta)
    {
        swingTimer -= delta;

        float gravity = swingGravity;
        float pendulumAcceleration = -gravity / swingRadius * Mathf.Sin(swingAngle);
        swingAngularVelocity += pendulumAcceleration * delta;
        swingAngularVelocity *= swingDamping;
        swingAngle += swingAngularVelocity * delta;

        float newX = swingAnchorPoint.X + Mathf.Sin(swingAngle) * swingRadius;
        float newY = swingAnchorPoint.Y + Mathf.Cos(swingAngle) * swingRadius;
        GlobalPosition = new Vector2(newX, newY);

        facingRight = swingAngularVelocity > 0;
        if (animatedSprite != null)
            animatedSprite.FlipH = !facingRight;

        // ✅ FIX: Web çizgisini güncelle (global pozisyon)
        if (webLine != null && webLine.Visible)
        {
            webLine.SetPointPosition(0, GlobalPosition);
            webLine.SetPointPosition(1, swingAnchorPoint);
        }

        // Bitirme koşulları
        if (Input.IsActionJustPressed("jump"))
        {
            EndSwingWithLaunch();
            return;
        }

        if (Input.IsActionJustPressed("special_ability") || Input.IsActionJustPressed("interaction"))
        {
            EndSwing();
            return;
        }

        if (IsOnFloor())
        {
            EndSwing();
            return;
        }

        if (swingTimer <= 0)
        {
            EndSwingWithLaunch();
            return;
        }

        if (IsOnWall())
        {
            EndSwing();
            return;
        }
    }

    private void EndSwing()
    {
        isSwinging = false;
        swingCooldownTimer = swingCooldown;
        Velocity = Vector2.Zero;

        if (webLine != null) webLine.Visible = false;
        if (webAnchorSprite != null) webAnchorSprite.Visible = false;

        GD.Print("[SWING] Swing bitti!");
    }

    private void EndSwingWithLaunch()
    {
        float tangentialSpeed = swingAngularVelocity * swingRadius;
        float launchAngle = swingAngle + Mathf.Pi / 2 * Mathf.Sign(swingAngularVelocity);
        float launchSpeedX = tangentialSpeed * Mathf.Cos(launchAngle) * 3.5f;
        float launchSpeedY = Mathf.Min(tangentialSpeed * Mathf.Sin(launchAngle) * 2.0f, JumpVelocity * 1.0f);

        if (Mathf.Abs(launchSpeedX) < 300)
            launchSpeedX = (facingRight ? 1 : -1) * 500;
        if (launchSpeedY > -200)
            launchSpeedY = JumpVelocity * 0.8f;

        Velocity = new Vector2(launchSpeedX, launchSpeedY);

        isSwinging = false;
        swingCooldownTimer = swingCooldown;

        if (webLine != null) webLine.Visible = false;
        if (webAnchorSprite != null) webAnchorSprite.Visible = false;

        GD.Print($"[SWING] ✅ Fırlatıldı! Velocity: {Velocity}");
    }

    // ========================================
    // BATMAN - GRAPPLE SİSTEMİ
    private void TryStartGrapple()
    {
        if (grappleCooldownTimer > 0)
        {
            GD.Print($"[GRAPPLE] ⏱️ Cooldown: {grappleCooldownTimer:F1}sn");
            return;
        }

        var spaceState = GetWorld2D().DirectSpaceState;

        // ✅ DAHA FAZLA AÇI - 13 farklı açı
        float[] angles = { 90, 85, 95, 80, 100, 75, 105, 70, 110, 65, 115, 60, 120, 55, 125, 50, 130 };
        float maxDistance = 400f;  // ✅ 350'den 400'e çıkarıldı

        Vector2? bestTarget = null;
        float bestScore = float.MinValue;
        int hitCount = 0;

        foreach (float angleDeg in angles)
        {
            float angleRad = Mathf.DegToRad(angleDeg);
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), -Mathf.Sin(angleRad));

            var query = PhysicsRayQueryParameters2D.Create(
                GlobalPosition,
                GlobalPosition + direction * maxDistance
            );
            query.CollisionMask = 1;
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;

            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                hitCount++;
                Vector2 hitPoint = (Vector2)result["position"];
                float distance = GlobalPosition.DistanceTo(hitPoint);
                float heightDiff = GlobalPosition.Y - hitPoint.Y;

                // ✅ YÜKSEKLIK GEVŞETİLDİ: 20px+ (önceden 50px+)
                if (distance > 20 && distance < 1000)    // ⚡ 20-1000px
                {
                    float dirBonus = facingRight ? (hitPoint.X - GlobalPosition.X) : (GlobalPosition.X - hitPoint.X);
                    float score = heightDiff * 2 + dirBonus * 0.5f - distance * 0.05f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = hitPoint;
                        GD.Print($"[GRAPPLE DEBUG] ✅ Potansiyel nokta: {hitPoint}, mesafe: {distance:F0}px, yükseklik: {heightDiff:F0}px, skor: {score:F1}");
                    }
                }
            }
        }

        GD.Print($"[GRAPPLE DEBUG] Toplam {hitCount} raycast çarpması bulundu");

        if (bestTarget.HasValue)
        {
            StartGrapple(bestTarget.Value);
        }
        else
        {
            GD.Print("[GRAPPLE] ❌ Tutunacak platform bulunamadı! (Yukarıda 40-400px arası + 20px+ yüksek platform olmalı)");
        }
    }

    private void StartGrapple(Vector2 targetPoint)
    {
        isGrappling = true;
        grappleTargetPoint = targetPoint + new Vector2(0, -20);

        // ✅ FIX: Hook görselini güncelle (TopLevel = true)
        if (hookLine != null)
        {
            hookLine.ClearPoints();
            hookLine.AddPoint(GlobalPosition);
            hookLine.AddPoint(targetPoint);
            hookLine.Visible = true;
        }

        if (hookSprite != null)
        {
            hookSprite.GlobalPosition = targetPoint;
            hookSprite.Visible = true;
        }

        GD.Print($"[GRAPPLE] ✅ Grapple başladı! Target: {targetPoint}");
    }

    private void UpdateGrapple(float delta)
    {
        Vector2 direction = (grappleTargetPoint - GlobalPosition).Normalized();
        float distance = GlobalPosition.DistanceTo(grappleTargetPoint);

        if (distance > 15)
        {
            GlobalPosition += direction * grappleSpeed * delta;

            // ✅ FIX: Hook çizgisini güncelle (global pozisyon)
            if (hookLine != null && hookLine.Visible)
            {
                hookLine.SetPointPosition(0, GlobalPosition);
                hookLine.SetPointPosition(1, grappleTargetPoint);
            }

            facingRight = grappleTargetPoint.X > GlobalPosition.X;
            if (animatedSprite != null)
                animatedSprite.FlipH = !facingRight;
        }
        else
        {
            EndGrapple(true);
            return;
        }

        if (Input.IsActionJustPressed("special_ability") ||
            Input.IsActionJustPressed("interaction") ||
            Input.IsActionJustPressed("jump"))
        {
            EndGrapple(false);
        }
    }

    private void EndGrapple(bool reachedTarget)
    {
        isGrappling = false;
        grappleCooldownTimer = grappleCooldown;
        Velocity = Vector2.Zero;

        if (hookLine != null) hookLine.Visible = false;
        if (hookSprite != null) hookSprite.Visible = false;

        if (reachedTarget)
        {
            GD.Print("[GRAPPLE] ✅ Hedefe ulaşıldı!");
        }
        else
        {
            GD.Print("[GRAPPLE] İptal edildi!");
        }
    }

    private void HandleWallJump(ref Vector2 velocity)
    {
        if (!canWallJump) return;
        if (IsOnFloor()) return;

        if (IsOnWall() && Input.IsActionJustPressed("jump") && wallJumpsRemaining > 0)
        {
            float jumpForce = JumpVelocity * wallJumpEfficiency;
            velocity.Y = jumpForce;
            velocity.X = facingRight ? -Speed : Speed;
            wallJumpsRemaining--;
            GD.Print($"[WALL JUMP] Kalan: {wallJumpsRemaining}");
        }
    }

    private void PerformTeleport()
    {
        if (teleportCooldownTimer > 0)
        {
            GD.Print($"[TELEPORT] ⏱️ Cooldown: {teleportCooldownTimer:F1}sn");
            return;
        }
        Vector2 direction = facingRight ? Vector2.Right : Vector2.Left;
        Vector2 targetPos = GlobalPosition + direction * teleportDistance;

        if (teleportPreventsFalling)
        {
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = PhysicsRayQueryParameters2D.Create(targetPos, targetPos + Vector2.Down * 100);
            query.CollisionMask = 1;
            var result = spaceState.IntersectRay(query);

            if (result.Count == 0)
            {
                GD.Print("[TELEPORT] Platform yok, iptal!");
                return;
            }
        }

        GlobalPosition = targetPos;
        teleportCooldownTimer = teleportCooldown;
        GD.Print("[TELEPORT] Işınlandı!");
    }

    private void ThrowProjectile()
    {
        if (projectileScene == null) return;
        // ✅ YENİ: THROW ANİMASYONU OYNA
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("throw"))
        {
            animatedSprite.Play("throw");
        }
        var projectile = projectileScene.Instantiate<Node2D>();
        projectile.GlobalPosition = GlobalPosition + new Vector2(facingRight ? 30 : -30, 0);

        if (projectile.HasMethod("Setup"))
        {
            projectile.Call("Setup", facingRight ? 1 : -1, projectileDamage, projectileCanStun, projectileStunDuration);
        }
        if (projectile.HasMethod("SetStunHitCount"))
        {
            projectile.Call("SetStunHitCount", projectileStunHitCount);
        }

        GetTree().CurrentScene.AddChild(projectile);
        projectileCooldownTimer = projectileCooldown;
        GD.Print("[PROJECTILE] Atıldı!");
    }

    private void PlacePlant()
    {
        if (plantScene == null) return;

        if (activePlants.Count >= maxProjectilePlants)
        {
            var oldestPlant = activePlants[0];
            if (IsInstanceValid(oldestPlant))
            {
                if (oldestPlant.HasMethod("Explode"))
                    oldestPlant.Call("Explode");
                else
                    oldestPlant.QueueFree();
            }
            activePlants.RemoveAt(0);
        }

        var plant = plantScene.Instantiate<Node2D>();
        plant.GlobalPosition = GlobalPosition;

        if (plant.HasMethod("Setup"))
        {
            plant.Call("Setup", plantDamage, plantExplosionRadius);
        }

        GetTree().CurrentScene.AddChild(plant);
        activePlants.Add(plant);
        plant.TreeExited += () => activePlants.Remove(plant);

        GD.Print($"[PLANT] Yerleştirildi! Aktif: {activePlants.Count}");
    }

    private void ActivateFrozeTime()
    {
        isFrozeTimeActive = true;
        frozeTimeCooldownTimer = frozeTimeCooldown;

        var enemies = GetTree().GetNodesInGroup("enemy");
        foreach (var enemy in enemies)
        {
            if (enemy.HasMethod("ApplySlow"))
            {
                enemy.Call("ApplySlow", frozeTimeSlowPercent, frozeTimeDuration);
            }
        }

        GD.Print($"[FROZE TIME] {enemies.Count} düşman yavaşlatıldı!");

        GetTree().CreateTimer(frozeTimeDuration).Timeout += () =>
        {
            isFrozeTimeActive = false;
            GD.Print("[FROZE TIME] Bitti!");
        };
    }

    private void DroneCollect()
    {
        var points = GetTree().GetNodesInGroup("points");
        int collected = 0;

        foreach (var point in points)
        {
            if (point is Node2D pointNode)
            {
                float distance = GlobalPosition.DistanceTo(pointNode.GlobalPosition);
                if (distance <= droneCollectRadius)
                {
                    if (pointNode.HasMethod("CollectByDrone"))
                    {
                        pointNode.Call("CollectByDrone", this);
                    }
                    collected++;
                }
            }
        }

        GD.Print($"[DRONE] {collected} çöp toplandı!");
    }

    public void SetCostumeAndEquip(int slotIndex, CostumeResource costume)
    {
        if (slotIndex < 0 || slotIndex >= CostumeSlots.Length || costume == null)
        {
            GD.PrintErr($"[COSTUME] Geçersiz parametre: slot={slotIndex}, costume={costume}");
            return;
        }

        GD.Print($"[COSTUME] === SetCostumeAndEquip BAŞLADI ===");
        GD.Print($"[COSTUME] Slot: {slotIndex}, Yeni Kostüm: {costume.CostumeName}");

        if (currentCostumeIndex >= 0 && currentCostumeIndex < CostumeSlots.Length)
        {
            costumeHealthStates[currentCostumeIndex] = currentHealth;
        }

        costumeHealthStates.Remove(slotIndex);

        CostumeSlots[slotIndex] = costume;
        currentCostumeIndex = slotIndex;
        CurrentCostume = costume;

        if (costume.Sprites != null && animatedSprite != null)
        {
            animatedSprite.SpriteFrames = costume.Sprites;
            animatedSprite.Play("idle");
        }

        MaxHealth = costume.MaxHealth;
        currentHealth = MaxHealth;
        costumeHealthStates[slotIndex] = currentHealth;
        UpdateHealthUI();

        ApplyCostumeAbilities();
        UpdateCostumeSlotUI();

        StopAllAbilities();

        GD.Print($"[COSTUME] ✅ {costume.CostumeName} giyildi! HP: {currentHealth}/{MaxHealth}");
    }
    private void StopAllAbilities()
    {
        // Flying
        if (isFlying)
        {
            isFlying = false;
            flyCooldownTimer = flyTimeCooldown;
        }

        // Swinging
        if (isSwinging)
        {
            isSwinging = false;
            swingCooldownTimer = swingCooldown;
            Velocity = Vector2.Zero;

            if (webLine != null) webLine.Visible = false;
            if (webAnchorSprite != null) webAnchorSprite.Visible = false;
        }

        // Grappling
        if (isGrappling)
        {
            isGrappling = false;
            grappleCooldownTimer = grappleCooldown;
            Velocity = Vector2.Zero;

            if (hookLine != null) hookLine.Visible = false;
            if (hookSprite != null) hookSprite.Visible = false;
        }

        // Climbing
        isClimbing = false;

        // ✅ CRITICAL FIX: Attack monitoring - DOĞRU C# SYNTAX!
        if (isAttacking && attackArea != null)
        {
            isAttacking = false;
            attackCooldownTimer = AttackCooldown;

            // ✅ Callable ile güvenli call
            Callable.From(() =>
            {
                if (attackArea != null && IsInstanceValid(attackArea))
                {
                    attackArea.Monitoring = false;
                }
            }).CallDeferred();
        }
    }


    public void EquipCostume(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CostumeSlots.Length)
            return;

        if (CostumeSlots[slotIndex] == null)
            return;

        if (slotIndex == currentCostumeIndex)
        {
            GD.Print($"[COSTUME] Zaten bu kostüm giyili!");
            return;
        }

        StopAllAbilities();

        if (currentCostumeIndex >= 0)
        {
            costumeHealthStates[currentCostumeIndex] = currentHealth;
        }

        currentCostumeIndex = slotIndex;
        CurrentCostume = CostumeSlots[slotIndex];
        ApplyCostume();
        UpdateCostumeSlotUI();

        GD.Print($"[COSTUME] {CurrentCostume.CostumeName} giyildi! (Slot {slotIndex + 1})");
    }

    private void ApplyCostume()
    {
        if (CurrentCostume == null) return;

        if (CurrentCostume.Sprites != null && animatedSprite != null)
        {
            animatedSprite.SpriteFrames = CurrentCostume.Sprites;
            animatedSprite.Play("idle");
        }

        MaxHealth = CurrentCostume.MaxHealth;

        if (costumeHealthStates.ContainsKey(currentCostumeIndex))
        {
            currentHealth = costumeHealthStates[currentCostumeIndex];
        }
        else
        {
            currentHealth = MaxHealth;
            costumeHealthStates[currentCostumeIndex] = currentHealth;
        }

        UpdateHealthUI();
        ApplyCostumeAbilities();
    }

    private void HandleInvincibility(float delta)
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= delta;
            float alpha = (Mathf.Sin(invincibilityTimer * 20) + 1) / 2;
            playerSprite.Modulate = new Color(1, 1, 1, alpha);
        }
        else
        {
            playerSprite.Modulate = new Color(1, 1, 1, 1);
        }
    }

    private void HandleJump(ref Vector2 velocity)
    {
        if (jumpBufferTimer > 0 && (IsOnFloor() || coyoteTimer > 0))
        {
            velocity.Y = JumpVelocity * jumpEfficiency;
            jumpBufferTimer = 0;
            jumpsRemaining = 0;
        }

        if (Input.IsActionJustReleased("jump") && velocity.Y < 0)
            velocity.Y *= 0.5f;
    }
    private void CreateAttackArea()
    {
        attackArea = new Area2D();
        attackArea.Name = "AttackArea";
        attackArea.CollisionLayer = 0;
        attackArea.CollisionMask = 15;
        AddChild(attackArea);

        attackCollision = new CollisionShape2D();
        attackCollision.Name = "AttackCollision";
        var shape = new RectangleShape2D();
        shape.Size = new Vector2(40, 30);
        attackCollision.Shape = shape;
        attackCollision.Position = new Vector2(30, 0);

        attackArea.AddChild(attackCollision);
        attackArea.Monitoring = false;
        attackArea.BodyEntered += OnAttackHitEnemy;

        GD.Print("[ATTACK] ✅ Attack Area oluşturuldu! Mask: " + attackArea.CollisionMask);
    }
    private void OnAttackHitEnemy(Node2D body)
    {
        if (body.IsInGroup("enemy") && body.HasMethod("TakeDamage"))
        {
            int damage = (int)(1 * damageMultiplier);
            body.Call("TakeDamage", damage);
            GD.Print($"[ATTACK] ✅ {body.Name} düşmana {damage} hasar verildi!");
        }
        else
        {
            GD.Print($"[ATTACK] ❌ {body.Name} enemy değil veya TakeDamage yok!");
        }
    }

    private void FindHeartNodes()
    {
        heartSprites.Clear();

        var healthBar = GetNodeOrNull<CanvasLayer>("health_bar");
        if (healthBar == null) return;

        var hbox = healthBar.GetNodeOrNull<HBoxContainer>("HBoxContainer");
        if (hbox == null) return;

        foreach (Node child in hbox.GetChildren())
        {
            if (child is TextureRect textureRect)
            {
                foreach (Node subChild in textureRect.GetChildren())
                {
                    if (subChild is AnimatedSprite2D anim)
                    {
                        heartSprites.Add(anim);
                        break;
                    }
                }
            }
        }
    }

    public void UpdateHealthUI()
    {
        if (heartSprites.Count == 0) return;

        for (int i = 0; i < heartSprites.Count; i++)
        {
            if (i < currentHealth)
            {
                heartSprites[i].Play("health");
                heartSprites[i].Visible = true;
            }
            else
            {
                heartSprites[i].Visible = false;
            }
        }
    }

    private void HandleAttack()
    {
        if (Input.IsActionJustPressed("attack") && !isDead && attackCooldownTimer <= 0)
        {
            StartAttack();
        }
    }

    private async void StartAttack()
    {
        isAttacking = true;
        attackCooldownTimer = AttackCooldown;

        if (comboTimer > 0)
        {
            comboCount++;
            if (comboCount > 2)
                comboCount = 0;
        }
        else
        {
            comboCount = 0;
        }

        comboTimer = ComboResetTime;

        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("attack"))
        {
            animatedSprite.Play("attack");

            int startFrame, endFrame;
            switch (comboCount)
            {
                case 0: startFrame = 0; endFrame = 1; break;
                case 1: startFrame = 2; endFrame = 3; break;
                case 2: startFrame = 4; endFrame = 5; break;
                default: startFrame = 0; endFrame = 1; break;
            }

            animatedSprite.Frame = startFrame;
            await PlayAttackFrames(startFrame, endFrame);
        }

        isAttacking = false;
    }
    private async Task PlayAttackFrames(int startFrame, int endFrame)
    {
        if (attackCollision != null)
        {
            // ✅ Aquaman için menzil artışı
            float rangeMultiplier = aquamanAttackRange;
            float baseDistance = 30f;

            attackCollision.Position = new Vector2(
                (facingRight ? baseDistance : -baseDistance) * rangeMultiplier,
                0
            );

            // Shape boyutunu da artır
            if (attackCollision.Shape is RectangleShape2D rectShape)
            {
                rectShape.Size = new Vector2(40 * rangeMultiplier, 30);
            }
        }

        for (int frame = startFrame; frame <= endFrame; frame++)
        {
            if (animatedSprite != null && animatedSprite.Animation == "attack")
            {
                animatedSprite.Frame = frame;

                if (frame == startFrame || frame == endFrame)
                {
                    if (!isSwinging && !isGrappling && !isFlying)
                    {
                        attackArea.CallDeferred("set_monitoring", true);
                        GD.Print($"[ATTACK] ⚔️ Monitoring AÇIK! Frame: {frame}, Range: {aquamanAttackRange}x");

                        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

                        attackArea.CallDeferred("set_monitoring", false);
                        GD.Print("[ATTACK] Monitoring KAPALI!");
                    }
                    else
                    {
                        GD.Print("[ATTACK] ❌ Yetenek aktif, attack iptal!");
                    }
                }

                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
            }
        }
    }



    public void TakeDamage(int damage = 1)
    {
        if (isDead || invincibilityTimer > 0) return;

        StopAllAbilities();

        currentHealth -= damage;

        if (currentCostumeIndex >= 0)
        {
            costumeHealthStates[currentCostumeIndex] = currentHealth;
        }

        UpdateHealthUI();
        invincibilityTimer = InvincibilityTime;
        FlashWhite();

        if (currentHealth <= 0)
            Die();
    }

    private async void FlashWhite()
    {
        playerSprite.Modulate = new Color(1, 0, 0, 1);
        await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        if (!isDead)
            playerSprite.Modulate = new Color(1, 1, 1, 1);
    }

    private void Die()
    {
        isDead = true;
        Velocity = Vector2.Zero;
        SetCollisionLayerValue(1, false);
        StopAllAbilities();

        var level = GetTree().CurrentScene;
        if (level.HasMethod("ResetLevelScore"))
        {
            level.Call("ResetLevelScore");
        }

        GetTree().CreateTimer(2.0).Timeout += () => GetTree().ReloadCurrentScene();
    }

    // Puan sistemi
    public void AddMetal(int value)
    {
        metalCount += value;
        GD.Print($"[PLAYER] 🔩 Metal +{value}, Toplam çöp: {TotalPoints}");
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";
    }

    public void AddGlass(int value)
    {
        glassCount += value;
        GD.Print($"[PLAYER] 🫙 Cam +{value}, Toplam çöp: {TotalPoints}");
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";
    }

    public void AddPlastic(int value)
    {
        plasticCount += value;
        GD.Print($"[PLAYER] 🧴 Plastik +{value}, Toplam çöp: {TotalPoints}");
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";
    }

    public void AddFood(int value)
    {
        foodCount += value;
        GD.Print($"[PLAYER] 🍎 Food +{value}, Toplam çöp: {TotalPoints}");
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";
    }

    public void AddWood(int value)
    {
        woodCount += value;
        GD.Print($"[PLAYER] 📄 Wood +{value}, Toplam çöp: {TotalPoints}");
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: {TotalPoints}";
    }

    public int[] GetAllPoints()
    {
        return new int[] { metalCount, glassCount, plasticCount, foodCount, woodCount };
    }

    public void ResetPoints()
    {
        metalCount = glassCount = plasticCount = foodCount = woodCount = 0;
        if (trashCountLabel != null)
            trashCountLabel.Text = $"Çöp: 0";
    }

    private void HandleMovement(ref Vector2 velocity, float delta)
    {
        if (isSwinging || isGrappling) return;

        Vector2 inputDirection = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");

        if (inputDirection.X != 0)
        {
            facingRight = inputDirection.X > 0;
            float accel = IsOnFloor() ? Acceleration : AirAcceleration;
            float targetSpeed = inputDirection.X * Speed * speedMultiplier;

            if (isFlying)
                accel *= 0.7f;

            velocity.X = Mathf.MoveToward(velocity.X, targetSpeed, accel * delta);
        }
        else
        {
            float friction = IsOnFloor() ? Friction : AirFriction;

            if (isFlying)
                friction *= 0.3f;

            velocity.X = Mathf.MoveToward(velocity.X, 0, friction * delta);
        }
    }

    private void UpdateAnimations()
    {
        if (animatedSprite == null) return;

        animatedSprite.FlipH = !facingRight;

        if (isAttacking) return;

        // ✅ YENİ: Climbing animasyonu
        if (isClimbing)
        {
            PlayAnimation("climb");  // ✅ "climb" animasyonu yoksa "idle" kullan
            return;
        }

        if (isSwinging)
        {
            PlayAnimation("swinging");
            return;
        }

        if (isGrappling)
        {
            PlayAnimation("hooking");
            return;
        }

        if (isFlying)
        {
            PlayAnimation("flying");
            return;
        }

        if (!IsOnFloor())
        {
            if (Velocity.Y < 0)
                PlayAnimation("jump");
            else
                PlayAnimation("fall");
        }
        else
        {
            if (Mathf.Abs(Velocity.X) > 5)
                PlayAnimation("run");
            else
                PlayAnimation("idle");
        }
    }

    private void HandleAbilities(float delta)
    {
        // ✅ SAĞ TIK - TEK BİR YER
        if (Input.IsActionJustPressed("right_click"))
        {
            HandleRightClick();
        }

        if (canTeleport && Input.IsActionJustPressed("teleport") && teleportCooldownTimer <= 0)
        {
            PerformTeleport();
        }

        // ✅ Q TUŞU - PROJECTILE
        if (canThrowProjectile && Input.IsActionJustPressed("throw_projectile") && projectileCooldownTimer <= 0)
        {
            ThrowProjectile();
        }

        // ✅ F TUŞU - PLANT
        if (canPlantProjectile && Input.IsActionJustPressed("plant"))
        {
            PlacePlant();
        }

        if (Input.IsActionJustPressed("special_ability") || Input.IsActionJustPressed("interaction"))
        {
            HandleSpecialAbilityOrInteraction();
        }
    }

    // ========================================
    // E TUŞU - ANA KONTROL
    // ========================================
    private void HandleSpecialAbilityOrInteraction()
    {
        GD.Print($"[E TUŞU] isNearInteractable={isNearInteractable}, currentInteractable={currentInteractable?.Name ?? "NULL"}");

        // ✅ ÖNCELİK 1: NPC/Building etkileşimi
        if (isNearInteractable && currentInteractable != null)
        {
            GD.Print("[E TUŞU] Etkileşim öncelikli!");
            TryInteract();
            return;
        }

        // ✅ ÖNCELİK 2: Aktif yetenek varsa kapat/iptal et
        if (isFlying)
        {
            StopFlying();
            return;
        }

        if (isSwinging)
        {
            EndSwingWithLaunch();
            return;
        }

        if (isGrappling)
        {
            EndGrapple(false);
            return;
        }

        // ✅ ÖNCELİK 3: Yeni yetenek başlat
        ActivateSpecialAbility();
    }
    private void ActivateSpecialAbility()
    {
        if (isAttacking) return;

        // ✅ AQUAMAN - SU BALONU TUZAĞI (E TUŞU)
        if (CurrentCostume != null && CurrentCostume.CostumeName == "aquaBoy")
        {
            if (canFrozeTime && aquamanStunCooldownTimer <= 0)
            {
                ActivateAquamanBubbleTrap();
                return;
            }
            else if (aquamanStunCooldownTimer > 0)
            {
                GD.Print($"[AQUAMAN] ⏱️ Su balonu cooldown: {aquamanStunCooldownTimer:F1}sn");
                return;
            }
        }

        if (canSwing)
        {
            TryStartSwing();
            return;
        }

        if (canGrapple)
        {
            TryStartGrapple();
            return;
        }

        if (canFly && flyCooldownTimer <= 0)
        {
            StartFlying();
            return;
        }

        if (canFrozeTime && frozeTimeCooldownTimer <= 0)
        {
            ActivateFrozeTime();
            return;
        }

        GD.Print("[ABILITY] Kullanılabilir yetenek yok!");
    }
    // Satır ~1700 civarında, diğer metodların sonuna:
    private void HandleRightClick()
    {
        // ===== SPIDERMAN - WEB PROJECTILE =====
        if (canThrowProjectile && projectileCooldownTimer <= 0)
        {
            ThrowProjectile();
            GD.Print("[RIGHT CLICK] Spiderman ağ attı!");
            return;
        }

        // ===== BATMAN - BATARANG TRAP =====
        if (canPlantProjectile)
        {
            PlacePlant();
            GD.Print("[RIGHT CLICK] Batman batarang yerleştirdi!");
            return;
        }

        // ===== FLASH - TELEPORT (öncelikli) =====
        if (canTeleport && teleportCooldownTimer <= 0)
        {
            PerformTeleport();
            GD.Print("[RIGHT CLICK] Flash ışınlandı!");
            return;
        }

        // ===== FLASH - FROZE TIME (alternatif) =====
        if (canFrozeTime && frozeTimeCooldownTimer <= 0)
        {
            ActivateFrozeTime();
            GD.Print("[RIGHT CLICK] Flash FrozeTime kullandı!");
            return;
        }

        GD.Print("[RIGHT CLICK] Bu kostümde sağ tık özelliği yok!");
    }
    private void HandleClimbing(ref Vector2 velocity, float delta)
    {
        isClimbing = true;

        // ✅ Yerçekimini iptal et
        velocity.Y = 0;

        // ✅ W tuşu ile yukarı tırman
        if (Input.IsActionPressed("climb"))  // W tuşu
        {
            velocity.Y = -climbSpeed;
            GD.Print("[CLIMB] Yukarı tırmanıyor...");
        }
        // ✅ S tuşu ile aşağı in
        else if (Input.IsActionPressed("ui_down"))  // S tuşu
        {
            velocity.Y = climbSpeed * 0.5f;  // Aşağı daha yavaş
        }

        // ✅ Sağ/sol hareket
        Vector2 inputDirection = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
        if (inputDirection.X != 0)
        {
            facingRight = inputDirection.X > 0;
            velocity.X = inputDirection.X * Speed * 0.5f;  // Yatay hareket yarı hızda
        }
        else
        {
            velocity.X = 0;
        }

        // ✅ Jump tuşu ile duvardan atla
        if (Input.IsActionJustPressed("jump"))
        {
            velocity.Y = JumpVelocity * 0.8f;
            velocity.X = facingRight ? -Speed : Speed;  // Ters yöne zıpla
            isClimbing = false;
            GD.Print("[CLIMB] Duvardan atladı!");
        }
    }


    private void PlayAnimation(string animationName)
    {
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation(animationName))
        {
            if (animatedSprite.Animation != animationName)
                animatedSprite.Play(animationName);
        }
    }

    public CostumeResource GetCurrentCostume()
    {
        return CurrentCostume;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);

        if (currentCostumeIndex >= 0)
        {
            costumeHealthStates[currentCostumeIndex] = currentHealth;
        }

        UpdateHealthUI();
        GD.Print($"[HEAL] +{amount} can! Güncel: {currentHealth}/{MaxHealth}");
    }

    public void HealCostumeSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CostumeSlots.Length)
            return;

        if (CostumeSlots[slotIndex] == null)
            return;

        int maxHealth = CostumeSlots[slotIndex].MaxHealth;
        costumeHealthStates[slotIndex] = maxHealth;

        if (slotIndex == currentCostumeIndex)
        {
            currentHealth = maxHealth;
            UpdateHealthUI();
        }

        GD.Print($"[HEAL SLOT] Slot {slotIndex} canı full yapıldı: {maxHealth}");
    }

    public void DestroyCostumeSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CostumeSlots.Length)
            return;

        if (CostumeSlots[slotIndex] == null)
            return;

        GD.Print($"[COSTUME] Slot {slotIndex} yok edildi: {CostumeSlots[slotIndex].CostumeName}");

        if (slotIndex == currentCostumeIndex)
        {
            StopAllAbilities();
            CurrentCostume = null;
            currentCostumeIndex = -1;

            for (int i = 0; i < CostumeSlots.Length; i++)
            {
                if (i != slotIndex && CostumeSlots[i] != null)
                {
                    EquipCostume(i);
                    break;
                }
            }
        }

        CostumeSlots[slotIndex] = null;
        costumeHealthStates.Remove(slotIndex);
        UpdateCostumeSlotUI();
    }
    // ========================================
    // AQUAMAN - SU BALONU TUZAĞI
    // ========================================
    private void ActivateAquamanBubbleTrap()
    {
        aquamanStunCooldownTimer = aquamanStunCooldown;

        var enemies = GetTree().GetNodesInGroup("enemy");
        int trappedCount = 0;

        foreach (var enemy in enemies)
        {
            if (enemy is Node2D enemyNode)
            {
                float distance = GlobalPosition.DistanceTo(enemyNode.GlobalPosition);

                if (distance <= aquamanStunRadius)
                {
                    // Düşmanı stun et
                    if (enemy.HasMethod("ApplySlow"))
                    {
                        enemy.Call("ApplySlow", 1.0f, frozeTimeDuration); // 1.0 = tamamen durdur
                        trappedCount++;
                    }

                    // Görsel efekt (isteğe bağlı - su baloncuğu sprite'ı eklenebilir)
                    GD.Print($"[AQUAMAN] 💧 {enemyNode.Name} su baloncuğunda!");
                }
            }
        }

        GD.Print($"[AQUAMAN] ✅ {trappedCount} düşman tuzaklandı! Cooldown: {aquamanStunCooldown}sn");
    }
    // Geçici kostüm
    private CostumeResource originalCostume;
    private int originalSlotIndex;
    private bool hasTemporaryCostume = false;

    public void AddTemporaryCostume(CostumeResource costume, int slot, float duration)
    {
        if (costume == null) return;

        originalCostume = CurrentCostume;
        originalSlotIndex = currentCostumeIndex;

        if (slot >= 0 && slot < CostumeSlots.Length)
        {
            CostumeSlots[slot] = costume;
            EquipCostume(slot);
            hasTemporaryCostume = true;

            GD.Print($"[COSTUME] Geçici kostüm eklendi: {costume.CostumeName}");

            if (duration > 0)
            {
                GetTree().CreateTimer(duration).Timeout += RemoveTemporaryCostume;
            }
        }

        UpdateCostumeSlotUI();
    }

    private void RemoveTemporaryCostume()
    {
        if (!hasTemporaryCostume) return;

        GD.Print("[COSTUME] Geçici kostüm süresi doldu!");

        hasTemporaryCostume = false;
        StopAllAbilities();

        if (currentCostumeIndex >= 0 && currentCostumeIndex < CostumeSlots.Length)
        {
            CostumeSlots[currentCostumeIndex] = null;
            costumeHealthStates.Remove(currentCostumeIndex);
        }

        if (originalCostume != null && originalSlotIndex >= 0)
        {
            CostumeSlots[originalSlotIndex] = originalCostume;
            EquipCostume(originalSlotIndex);
        }

        UpdateCostumeSlotUI();
    }

    public void OnLevelEnd()
    {
        StopAllAbilities();
        if (hasTemporaryCostume)
        {
            RemoveTemporaryCostume();
        }
    }

    public int GetCurrentCostumeIndex()
    {
        return currentCostumeIndex;
    }

    public void UpdateTeacherScore(int points)
    {
        var level = GetTree().CurrentScene;
        if (level != null && level.HasMethod("AddTeacherScore"))
        {
            level.Call("AddTeacherScore", points);
        }

        int currentScore = 0;
        int requiredScore = 100;

        if (level != null && level.HasMethod("GetCurrentScore"))
        {
            currentScore = (int)level.Call("GetCurrentScore");
        }

        if (level != null && level.HasMethod("GetRequiredScore"))
        {
            requiredScore = (int)level.Call("GetRequiredScore");
        }

        if (currentScoreLabel != null)
            currentScoreLabel.Text = $"Skor: {currentScore}";

        if (requiredScoreLabel != null)
            requiredScoreLabel.Text = $"Hedef: {requiredScore}";
    }

    public void UpdateMinigameScore(int minigamePoints)
    {
        var level = GetTree().CurrentScene;
        if (level != null && level.HasMethod("AddMinigameScore"))
        {
            level.Call("AddMinigameScore", minigamePoints);
        }

        int currentScore = 0;
        int requiredScore = 100;

        if (level != null && level.HasMethod("GetCurrentScore"))
        {
            currentScore = (int)level.Call("GetCurrentScore");
        }

        if (level != null && level.HasMethod("GetRequiredScore"))
        {
            requiredScore = (int)level.Call("GetRequiredScore");
        }

        if (currentScoreLabel != null)
            currentScoreLabel.Text = $"Skor: {currentScore}";

        if (requiredScoreLabel != null)
            requiredScoreLabel.Text = $"Hedef: {requiredScore}";
    }

    // ===== GETTER'LAR =====
    public bool IsPlayerFlying() => isFlying;
    public bool IsPlayerSwinging() => isSwinging;
    public bool IsPlayerGrappling() => isGrappling;
    public bool IsPlayerNearInteractable() => isNearInteractable;

    // ========================================
    // KOSTÜM RESTORE (Level Transfer)
    // ========================================
    public void RestoreCostume(int costumeIndex)
    {
        if (costumeIndex < 0 || costumeIndex >= CostumeSlots.Length)
        {
            GD.PrintErr($"[PLAYER] ❌ Geçersiz kostüm index: {costumeIndex}");
            return;
        }

        if (CostumeSlots[costumeIndex] == null)
        {
            GD.PrintErr($"[PLAYER] ❌ Slot {costumeIndex} boş!");
            return;
        }

        GD.Print($"[PLAYER] 🔄 Kostüm geri yükleniyor: Slot {costumeIndex} - {CostumeSlots[costumeIndex].CostumeName}");

        // ✅ Mevcut EquipCostume metodunu kullan
        EquipCostume(costumeIndex);

        GD.Print($"[PLAYER] ✅ Kostüm aktif: {CurrentCostume?.CostumeName ?? "NULL"}");
    }
}