using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down player: WASD / arrow keys to move, Left Shift to run, Space / J / left mouse to swing the sword.
/// Uses the Cute Fantasy Player.png sheet (6 columns x 10 rows of 32px frames).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(SimpleAnimator))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

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
    float attackTimer;
    const float AttackFps = 12f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SimpleAnimator>();
        SpriteMaterial.Apply(sr);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
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

        // Swing the sword
        if (attackTimer <= 0f && attackPressed)
        {
            attackTimer = attack[(int)facing].Length / AttackFps;
            anim.Play(attack[(int)facing], AttackFps, false, true);
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) attackTimer = 0f;
        }
        else
        {
            UpdateFacing();
            bool moving = input.sqrMagnitude > 0.01f;
            anim.Play(moving ? walk[(int)facing] : idle[(int)facing], moving ? (run ? 11f : 8f) : 5f);
        }

        sr.flipX = faceLeft;
        sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
        speedThisFrame = run ? runSpeed : walkSpeed;
    }

    float speedThisFrame;

    void FixedUpdate()
    {
        // No movement while swinging
        rb.linearVelocity = attackTimer > 0f ? Vector2.zero : input * speedThisFrame;
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
}
