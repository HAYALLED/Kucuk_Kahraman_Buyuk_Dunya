using Godot;

[GlobalClass]
public partial class CostumeResource : Resource
{
    [Export] public string CostumeName = "Default";
    [Export] public SpriteFrames Sprites;

    // ===== CAN =====
    [Export] public int MaxHealth = 1;

    // ===== UI =====
    [Export] public Texture2D Icon;

    // ===== ÇARPANLAR =====
    [Export] public float DamageMultiplier = 1.0f;

    // ===== TEMEL YETENEKLERİ =====
    [Export] public bool CanWallClimb = false;
    [Export] public bool CanSwing = false;
    [Export] public bool CanGrapple = false;
    [Export] public bool CanFly = false;
    [Export] public float FlyTimeDuration = 15.0f;       // Etki süresi
    [Export] public float FlyTimeCooldown = 30.0f;      // Bekleme süresi
    [Export] public float FlyEfficiency = 1.0f;          // 1.0 = normal, 1.2 = %20 daha yüksek
    // ===== HOVER (Yavaş Düşme) =====
    [Export] public bool CanHover = false;
    [Export] public float HoverGravityMultiplier = 0.5f;  // %50 daha yavaş düşer

    // ===== PROJECTILE (Mermi Atma) =====
    [Export] public bool CanThrowProjectile = false;
    [Export] public int ProjectileDamage = 1;             // 0 olabilir (hasar vermez)
    [Export] public float ProjectileCooldown = 1.0f;      // Atışlar arası bekleme (saniye)
    [Export] public bool ProjectileCanStun = false;
    [Export] public int ProjectileStunHitCount = 3;       // 10 saniyede kaç vuruş gerekli
    [Export] public float ProjectileStunDuration = 2.0f;  // Stun süresi (saniye)
    [Export] public PackedScene ProjectileScene;          // Mermi scene'i

    // ===== PROJECTILE PLANT (Mayın/Tuzak) =====
    [Export] public bool CanPlantProjectile = false;
    [Export] public int MaxProjectilePlants = 3;          // Max aktif tuzak sayısı
    [Export] public int PlantDamage = 1;
    [Export] public float PlantExplosionRadius = 50.0f;   // Patlama yarıçapı
    [Export] public PackedScene PlantScene;               // Tuzak scene'i

    // ===== ATTACK DRONE (Iron Man) =====
    [Export] public bool HasDroneSupport = false;
    [Export] public PackedScene DroneScene;
    [Export] public float DroneSpawnInterval = 25.0f;     
    [Export] public int MaxActiveDrones = 2;              
    [Export] public float DroneDetectionRadius = 500.0f;  // ⚡ YENİ: Düşman algılama mesafesi
    [Export] public int DroneDamage = 2;                 
    [Export] public float DroneSpeed = 400.0f;            
    [Export] public float DroneLifetime = 10.0f;          // ⚡ YENİ: Max yaşam süresi (10sn)
    // ===== FROZE TIME (Zaman Yavaşlatma) =====
    [Export] public bool CanFreezeTime = false;
    [Export] public float FreezeTimeDuration = 10.0f;     // Donma süresi
    [Export] public float FreezeTimeCooldown = 25.0f;     // Bekleme süresi
                                                          // ===== AQUAMAN BUBBLE TRAP =====
    [Export] public bool CanUseBubbleTrap = false; 
    [Export] public PackedScene BubbleScene;           // ✅ YENİ
    [Export] public float BubbleStunDuration = 4.0f;   // ✅ YENİ
    [Export] public float BubbleStunCooldown = 25.0f;  // ✅ YENİ
    [Export] public float BubbleStunRadius = 200.0f;   // ✅ YENİ
    // ===== WALL JUMP (Duvar Zıplama) =====
    [Export] public bool CanWallJump = false;
    [Export] public int MaxWallJumps = 1;                 // Üst üste kaç duvar zıplaması
    [Export] public float WallJumpEfficiency = 1.0f;      // 1.0 = normal, 1.5 = %50 daha yüksek

    // ===== TELEPORT (Işınlanma) =====
    [Export] public bool CanTeleport = false;
    [Export] public float TeleportDistance = 100.0f;      // Işınlanma mesafesi
    [Export] public float TeleportCooldown = 3.0f;        // Bekleme süresi
    [Export] public bool TeleportPreventsFalling = true;  // Platformdan düşmeyi engeller

    // ===== JUMP (Zıplama Güçlendirme) =====
    [Export] public float JumpEfficiency = 1.0f;          // 1.0 = normal, 1.2 = %20 daha yüksek

    [Export] public float SpeedEfficiency = 1.0f;         // 1.0 = normal, 1.2 = %20 daha hızlı
}