using UnityEngine;

/// <summary>
/// A dungeon monster (slime or skeleton). Wanders until the player gets close, then chases and hurts on contact.
/// Dies when its health reaches 0 and drops items used for crafting.
/// Create with Enemy.Create(...), which also shows a static preview sprite in Edit mode.
/// </summary>
public class Enemy : MonoBehaviour
{
    public enum Kind { Slime, Skeleton }

    public Kind kind;

    Rigidbody2D rb;
    SpriteRenderer sr;
    SimpleAnimator anim;
    Health health;
    Collider2D body;

    Sprite[][] skelIdle = new Sprite[3][], skelWalk = new Sprite[3][];
    Sprite[] slimeIdle, slimeMove, deathFrames;
    float deathFps;

    float speed, aggro, touchRange;
    int damage;
    Vector2 wanderDir, knock, desiredVelocity;
    float wanderTimer, nextAttack, flashUntil;
    bool dying, faceLeft;
    int skelFacing; // 0 down, 1 side, 2 up
    Transform player;
    PlayerController playerCtl;

    // ------------------------------------------------------------ creation

    public static Enemy Create(Transform parent, Kind kind, Vector2 position)
    {
        var go = new GameObject(kind + " (enemy)");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var rbody = go.AddComponent<Rigidbody2D>();
        rbody.bodyType = RigidbodyType2D.Dynamic;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = kind == Kind.Slime ? new Vector2(0.8f, 0.45f) : new Vector2(0.6f, 0.35f);
        box.offset = new Vector2(0f, 0.2f);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<SimpleAnimator>();
        go.AddComponent<Health>();
        var e = go.AddComponent<Enemy>();
        e.Init(kind);
        return e;
    }

    void Init(Kind k)
    {
        kind = k;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SimpleAnimator>();
        health = GetComponent<Health>();
        body = GetComponent<Collider2D>();

        if (kind == Kind.Slime)
        {
            var sheet = SpriteSheet.Slice("Characters/slime", 64, new Vector2(32, 24));
            if (sheet.Length == 0) return;
            slimeIdle = SpriteSheet.Row(sheet, 0, 0, 4);
            slimeMove = SpriteSheet.Row(sheet, 1, 0, 8);
            deathFrames = SpriteSheet.Row(sheet, 2, 0, 8);
            deathFps = 12f;
            health.Configure(4, 0.15f);
            speed = 1.6f; aggro = 4.5f; damage = 6; touchRange = 0.85f;
            sr.sprite = slimeIdle[0];
        }
        else
        {
            var sheet = SpriteSheet.Slice("Characters/skeleton", 32, new Vector2(16, 9));
            if (sheet.Length == 0) return;
            for (int i = 0; i < 3; i++)
            {
                skelIdle[i] = SpriteSheet.Row(sheet, i, 0, 6);
                skelWalk[i] = SpriteSheet.Row(sheet, 3 + i, 0, 6);
            }
            deathFrames = SpriteSheet.Row(sheet, 6, 0, 4);
            deathFps = 8f;
            health.Configure(6, 0.15f);
            speed = 2.0f; aggro = 6f; damage = 10; touchRange = 0.9f;
            sr.sprite = skelIdle[0][0];
        }

        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);

        if (!Application.isPlaying) return; // Edit mode: just a static preview

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        wanderTimer = Random.Range(0.5f, 2f);
        PlayIdle();
    }

    // ------------------------------------------------------------ behaviour

    void Update()
    {
        if (dying || health == null || !Application.isPlaying) return;

        if (player == null)
        {
            playerCtl = FindFirstObjectByType<PlayerController>();
            if (playerCtl != null) player = playerCtl.transform;
        }

        Vector2 pos = transform.position;
        Vector2 dir = Vector2.zero;
        bool chasing = false;
        if (player != null && !playerCtl.Health.Dead && !GameHud.IsFading)
        {
            float dist = Vector2.Distance(pos, player.position);
            if (dist < aggro)
            {
                chasing = true;
                dir = ((Vector2)player.position - pos).normalized;
                if (dist < touchRange && Time.time >= nextAttack && playerCtl.Health.TakeDamage(damage, pos))
                    nextAttack = Time.time + 0.9f;
            }
        }

        if (!chasing)
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(1f, 3f);
                wanderDir = Random.value < 0.4f ? Vector2.zero : Random.insideUnitCircle.normalized;
            }
            dir = wanderDir * 0.4f;
        }

        float s = speed;
        if (kind == Kind.Slime) s *= 0.35f + 0.9f * Mathf.Abs(Mathf.Sin(Time.time * 5f)); // hopping
        desiredVelocity = dir * s;

        bool moving = dir.sqrMagnitude > 0.01f;
        if (kind == Kind.Slime)
        {
            anim.Play(moving ? slimeMove : slimeIdle, moving ? 10f : 6f);
            if (Mathf.Abs(dir.x) > 0.1f) sr.flipX = dir.x < 0f;
        }
        else
        {
            if (moving)
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) { skelFacing = 1; faceLeft = dir.x < 0f; }
                else skelFacing = dir.y > 0f ? 2 : 0;
            }
            anim.Play(moving ? skelWalk[skelFacing] : skelIdle[skelFacing], moving ? 8f : 5f);
            sr.flipX = !faceLeft && skelFacing == 1; // the skeleton's side frames face left
        }

        sr.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : Color.white;
        sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
    }

    void FixedUpdate()
    {
        if (rb == null || !Application.isPlaying) return;
        rb.linearVelocity = dying ? Vector2.zero : desiredVelocity + knock;
        knock = Vector2.MoveTowards(knock, Vector2.zero, 20f * Time.fixedDeltaTime);
    }

    void PlayIdle() => anim.Play(kind == Kind.Slime ? slimeIdle : skelIdle[0], 5f);

    void OnDamaged(int amount, Vector2 from)
    {
        knock = ((Vector2)transform.position - from).normalized * 5f;
        flashUntil = Time.time + 0.15f;
    }

    void OnDied()
    {
        dying = true;
        desiredVelocity = Vector2.zero;
        sr.color = Color.white;
        if (body != null) body.enabled = false;
        anim.Play(deathFrames, deathFps, false, true);

        if (kind == Kind.Slime)
        {
            if (Random.value < 0.75f) FarmWorld.DropPickup(Item.SlimeGel, transform.position);
            if (Random.value < 0.35f) FarmWorld.DropPickup(Item.Herb, transform.position + Vector3.right * 0.4f);
        }
        else
        {
            if (Random.value < 0.85f) FarmWorld.DropPickup(Item.Bone, transform.position);
            if (Random.value < 0.45f) FarmWorld.DropPickup(Item.Flower, transform.position + Vector3.right * 0.4f);
        }
        Destroy(gameObject, deathFrames.Length / deathFps + 0.7f);
    }
}
