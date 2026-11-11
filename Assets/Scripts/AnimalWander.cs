using System.Globalization;
using UnityEngine;

/// <summary>
/// Makes a Cute Fantasy animal wander around: idle for a bit, walk in a random direction, repeat.
/// Animal sheets are 64x64: a 2x2 grid of 32px frames. Top row = idle, bottom row = walking.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(SimpleAnimator))]
public class AnimalWander : MonoBehaviour, ISaveable
{
    public float speed = 0.9f;
    public Vector2 idleTime = new Vector2(1.5f, 4f);
    public Vector2 walkTime = new Vector2(1f, 3f);
    [Tooltip("The animal sheets draw the animals facing LEFT. Tick this if a sheet faces right.")]
    public bool spriteFacesRight = false;

    Rigidbody2D rb;
    SpriteRenderer sr;
    SimpleAnimator anim;
    Sprite[] idle, walk;
    Vector2 dir;
    bool walking;
    float timer;
    string saveId;

    public void Init(string sheetPath, float pivotYPx, string stableId)
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SimpleAnimator>();
        SpriteMaterial.Apply(sr);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        saveId = stableId;
        RegisterSaveable();

        var sheet = SpriteSheet.Slice(sheetPath, 32, new Vector2(16, pivotYPx));
        idle = SpriteSheet.Row(sheet, 0, 0, 2);
        walk = SpriteSheet.Row(sheet, 1, 0, 2);

        speed *= Random.Range(0.85f, 1.15f);
        StartIdle();
        timer = Random.Range(0f, idleTime.y); // desync the animals
    }

    /// <summary>Re-registers this animal after the generated world registry is cleared.</summary>
    public void RegisterSaveable()
    {
        if (!string.IsNullOrEmpty(saveId)) WorldState.RegisterFixed(saveId, this);
    }

    public string CaptureState()
    {
        var p = transform.position;
        return p.x.ToString("R", CultureInfo.InvariantCulture) + "|" +
               p.y.ToString("R", CultureInfo.InvariantCulture) + "|" +
               (sr != null && sr.flipX ? "1" : "0");
    }

    public void RestoreState(string saved)
    {
        if (string.IsNullOrEmpty(saved)) return;
        var parts = saved.Split('|');
        if (parts.Length < 2 ||
            !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            Debug.LogWarning("AnimalWander: invalid saved state for " + saveId);
            return;
        }

        // NumberStyles.Float also accepts NaN and Infinity; never apply non-finite coordinates.
        if (float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y))
        {
            Debug.LogWarning("AnimalWander: saved position is not finite for " + saveId);
            return;
        }

        var position = new Vector2(x, y);
        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
        }
        if (parts.Length >= 3 && sr != null) sr.flipX = parts[2] == "1";
    }

    void Update()
    {
        if (anim == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (walking) StartIdle(); else StartWalk();
        }
        sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        rb.linearVelocity = walking ? dir * speed : Vector2.zero;
    }

    void OnCollisionEnter2D(Collision2D _)
    {
        // Bumped into something (tree, pond, player...): turn around
        if (walking) StartWalk();
    }

    void StartIdle()
    {
        walking = false;
        timer = Random.Range(idleTime.x, idleTime.y);
        anim.Play(idle, 2f);
    }

    void StartWalk()
    {
        walking = true;
        timer = Random.Range(walkTime.x, walkTime.y);
        dir = Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        if (Mathf.Abs(dir.x) > 0.15f) sr.flipX = (dir.x > 0f) != spriteFacesRight;
        anim.Play(walk, 6f);
    }
}
