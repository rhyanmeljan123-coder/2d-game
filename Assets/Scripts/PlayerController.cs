using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player: WASD / arrow keys to move, Left Shift to run, Space / J / left mouse to swing the sword.
/// Uses the Cute Fantasy Player.png sheet (6 columns x 10 rows of 32px frames).
/// Has health: monsters hurt you, food and sleeping heal you, and if you pass out you wake up at the farm.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(SimpleAnimator))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public int swordDamage = 2;
    public int maxHealth = 100;

    // Sheet rows (row 0 = top of the image)
    const int RowIdle = 0, RowWalk = 3, RowAttack = 6; // each has Down, Side, Up in consecutive rows
    enum Facing { Down = 0, Side = 1, Up = 2 }

    Rigidbody2D rb;
    SpriteRenderer sr;
    SimpleAnimator anim;
    Sprite[][] idle = new Sprite[3][], walk = new Sprite[3][], attack = new Sprite[3][];

    Facing facing = Facing.Down;
    bool faceLeft;
    Vector2 input;
    Vector2 knockback;
    float attackTimer, attackLength, flashUntil;
    bool swingHit;
    const float AttackFps = 12f;

    public Health Health { get; private set; }

    float speedBoostUntil, strengthBoostUntil;
    public float SpeedBoostLeft => Mathf.Max(0f, speedBoostUntil - Time.time);
    public float StrengthBoostLeft => Mathf.Max(0f, strengthBoostUntil - Time.time);
    public void AddSpeedBoost(float seconds) => speedBoostUntil = Mathf.Max(speedBoostUntil, Time.time) + seconds;
    public void AddStrengthBoost(float seconds) => strengthBoostUntil = Mathf.Max(strengthBoostUntil, Time.time) + seconds;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SimpleAnimator>();
        Health = GetComponent<Health>();
        if (Health == null) Health = gameObject.AddComponent<Health>(); // RequireComponent allows only 3 types, so add it here
        SpriteMaterial.Apply(sr);

        Health.Configure(maxHealth, 0.8f);
        Health.Damaged += OnDamaged;
        Health.Died += OnDied;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // so traps keep detecting a player who stands still
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Pivot sits on the character's feet/shadow (9px above the bottom of the 32px cell)
        var sheet = SpriteSheet.Slice("Characters/player", 32, new Vector2(16, 9));
        for (int i = 0; i < 3; i++)
        {
            idle[i] = SpriteSheet.Row(sheet, RowIdle + i, 0, 6);
            walk[i] = SpriteSheet.Row(sheet, RowWalk + i, 0, 6);
            attack[i] = SpriteSheet.Row(sheet, RowAttack + i, 0, 4);
        }
        anim.Play(idle[0], 6f);
    }

    void OnDestroy()
    {
        if (Health == null) return;
        Health.Damaged -= OnDamaged;
        Health.Died -= OnDied;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        input = Vector2.zero;
        bool run = false;
        bool attackPressed = false;
        if (kb != null)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            run = kb.leftShiftKey.isPressed;
            attackPressed = kb.spaceKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame;
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) attackPressed = true;
        input = Vector2.ClampMagnitude(input, 1f);
        if (GameHud.IsFading || GameHud.UiOpen || Health.Dead) { input = Vector2.zero; attackPressed = false; }

        // Swing the sword
        if (attackTimer <= 0f && attackPressed)
        {
            attackLength = attack[(int)facing].Length / AttackFps;
            attackTimer = attackLength;
            swingHit = false;
            anim.Play(attack[(int)facing], AttackFps, false, true);
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            // The blade connects partway through the swing
            if (!swingHit && attackLength - attackTimer >= 0.12f) { swingHit = true; SwordHit(); }
            if (attackTimer <= 0f) attackTimer = 0f;
        }
        else
        {
            UpdateFacing();
            bool moving = input.sqrMagnitude > 0.01f;
            anim.Play(moving ? walk[(int)facing] : idle[(int)facing], moving ? (run ? 11f : 8f) : 5f);
        }

        sr.flipX = faceLeft;
        sr.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
        speedThisFrame = (run ? runSpeed : walkSpeed) * (SpeedBoostLeft > 0f ? 1.4f : 1f);
    }

    float speedThisFrame;

    /// <summary>The direction the player is looking (used for interacting).</summary>
    public Vector2 FacingDir =>
        facing == Facing.Up ? Vector2.up : facing == Facing.Down ? Vector2.down : (faceLeft ? Vector2.left : Vector2.right);

    void FixedUpdate()
    {
        // No walking while swinging, but knockback still pushes
        Vector2 walkVelocity = attackTimer > 0f ? Vector2.zero : input * speedThisFrame;
        rb.linearVelocity = walkVelocity + knockback;
        knockback = Vector2.MoveTowards(knockback, Vector2.zero, 25f * Time.fixedDeltaTime);
    }

    void UpdateFacing()
    {
        if (input.sqrMagnitude < 0.01f) return;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y) + 0.01f)
        {
            facing = Facing.Side;
            faceLeft = input.x < 0f; // the sheet's side frames face right
        }
        else
        {
            facing = input.y > 0f ? Facing.Up : Facing.Down;
        }
    }

    // ------------------------------------------------------------ combat & health

    void SwordHit()
    {
        Vector2 centre = (Vector2)transform.position + FacingDir * 0.9f + Vector2.up * 0.4f;
        foreach (var hit in Physics2D.OverlapCircleAll(centre, 0.95f))
        {
            var enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            var hp = enemy.GetComponent<Health>();
            if (hp != null) hp.TakeDamage(swordDamage + (StrengthBoostLeft > 0f ? 2 : 0), transform.position);
        }
    }

    void OnDamaged(int amount, Vector2 from)
    {
        Vector2 away = ((Vector2)transform.position - from).normalized;
        knockback = away * 6f;
        flashUntil = Time.time + 0.2f;
    }

    void OnDied()
    {
        GameHud.Toast("You passed out...");
        StartCoroutine(RespawnWhenReady());
    }

    System.Collections.IEnumerator RespawnWhenReady()
    {
        while (GameHud.IsFading) yield return null; // wait if a door/sleep fade is still running
        GameHud.Fade(() =>
        {
            Health.Revive(Health.max / 2);
            knockback = Vector2.zero;
            attackTimer = 0f;
            Vector2 home = GameSetup.PlayerStart;
            rb.linearVelocity = Vector2.zero;
            rb.position = home;
            transform.position = home;
            GameHud.Toast("You woke up back at the farm.");
        });
    }
}
