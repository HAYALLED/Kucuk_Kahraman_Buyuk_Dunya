using Godot;

public partial class Door : Area2D
{
    [Export] public bool StartsOpen = false;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape; // Area2D'nin shape'i (detection)
    private StaticBody2D doorBlocker;        // Gerçek fizik engeli
    private CollisionShape2D blockerShape;   // Blocker'ın shape'i

    private bool isOpen = false;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        // ✅ StaticBody2D blocker'ı bul
        doorBlocker = GetNodeOrNull<StaticBody2D>("DoorBlocker");
        if (doorBlocker != null)
            blockerShape = doorBlocker.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (animatedSprite != null)
            animatedSprite.AnimationFinished += OnAnimationFinished;

        isOpen = StartsOpen;

        if (isOpen)
        {
            PlayAnim("default_open");
            SetBlocking(false);
        }
        else
        {
            PlayAnim("default_close");
            SetBlocking(true);
        }
    }

    public void Activate(bool state)
    {
        if (state) Open();
        else Close();
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        PlayAnim("opening");
        // Açılma animasyonu sırasında hâlâ geçilemez
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        PlayAnim("closing");
        SetBlocking(true); // ✅ Kapanmaya başlayınca hemen engelle
    }

    private void OnAnimationFinished()
    {
        if (animatedSprite == null) return;
        string current = animatedSprite.Animation;

        if (current == "opening")
        {
            PlayAnim("default_open");
            SetBlocking(false); // ✅ Tamamen açıldı → geçilebilir
        }
        else if (current == "closing")
        {
            PlayAnim("default_close");
            SetBlocking(true);  // ✅ Tamamen kapandı → geçilemez
        }
    }

    private void SetBlocking(bool blocking)
    {
        // ✅ StaticBody2D'nin shape'ini kontrol et
        if (blockerShape != null)
            blockerShape.SetDeferred("disabled", !blocking);
        else
            GD.PrintErr("[DOOR] ❌ DoorBlocker/CollisionShape2D bulunamadı!");
    }

    private void PlayAnim(string animName)
    {
        if (animatedSprite?.SpriteFrames?.HasAnimation(animName) == true)
            animatedSprite.Play(animName);
    }
}