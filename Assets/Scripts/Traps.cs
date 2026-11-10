using UnityEngine;

/// <summary>Helpers for the 4-frame animation strips in Resources/Traps.</summary>
public static class TrapSprites
{
    /// <summary>Cuts the frames of a strip (frames laid out left to right). Pivot is in pixels from the frame's bottom-left.</summary>
    public static Sprite[] Frames(string strip, int frameW, int frameH, Vector2 pivotPx, int count = 4)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = SpriteSheet.Cut("Traps/" + strip, i * frameW, 0, frameW, frameH, pivotPx);
        return frames;
    }
}

/// <summary>A trap that hurts the player while it is "Dangerous".</summary>
public abstract class Hazard : MonoBehaviour
{
    public int damage = 12;
    protected bool Dangerous;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!Dangerous) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) player.Health.TakeDamage(damage, transform.position);
    }
}

/// <summary>Floor spikes: hidden holes, then a warning, then spikes shoot up for a moment.</summary>
public class SpikeTrap : Hazard
{
    SpriteRenderer sr;
    Sprite[] f;
    int state;
    float timer;
    const float OffTime = 2.2f;

    public void Init(float startDelay)
    {
        sr = GetComponent<SpriteRenderer>();
        f = TrapSprites.Frames("peaks", 16, 16, new Vector2(8, 8));
        damage = 15;
        sr.sprite = f[2]; // retracted
        state = 0;
        timer = startDelay;
    }

    void Update()
    {
        if (!Application.isPlaying || f == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case 0: state = 1; timer = 0.5f; sr.sprite = f[3]; break;                  // warning: tiny tips
            case 1: state = 2; timer = 0.12f; sr.sprite = f[1]; Dangerous = true; break; // rising
            case 2: state = 3; timer = 0.9f; sr.sprite = f[0]; break;                  // fully up
            case 3: state = 4; timer = 0.12f; sr.sprite = f[1]; break;                  // lowering
            default: state = 0; timer = OffTime; sr.sprite = f[2]; Dangerous = false; break;
        }
    }
}

/// <summary>A wall flamethrower that bursts fire in a line now and then. Vertical shoots up, horizontal shoots sideways.</summary>
public class FlameTrap : Hazard
{
    SpriteRenderer sr;
    BoxCollider2D box;
    Sprite[] f;
    int state;
    float timer;
    const float OffTime = 2.6f;

    public void Init(bool horizontal, bool faceLeft, float startDelay)
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        damage = 12;
        if (horizontal)
        {
            f = TrapSprites.Frames("flame_h", 32, 16, new Vector2(0, 8));
            sr.flipX = faceLeft;
            box.size = new Vector2(1.7f, 0.7f);
            box.offset = new Vector2(faceLeft ? -1.05f : 1.05f, 0f);
        }
        else
        {
            f = TrapSprites.Frames("flame_v", 16, 32, new Vector2(8, 0));
            box.size = new Vector2(0.7f, 1.7f);
            box.offset = new Vector2(0f, 1.05f);
        }
        box.isTrigger = true;

        if (Application.isPlaying) { sr.enabled = false; timer = startDelay; }
        else sr.sprite = f[1]; // show the flames in the Scene view
    }

    void Update()
    {
        if (!Application.isPlaying || f == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case 0: state = 1; timer = 0.5f; sr.enabled = true; sr.sprite = f[0]; break;                // warning flicker
            case 1: state = 2; timer = 0.7f; sr.sprite = f[1]; Dangerous = true; break;                // burst
            case 2: state = 3; timer = 0.15f; sr.sprite = f[2]; Dangerous = false; break;
            case 3: state = 4; timer = 0.15f; sr.sprite = f[3]; break;
            default: state = 0; timer = OffTime; sr.enabled = false; break;
        }
    }
}

/// <summary>A wall launcher that shoots arrows downward at intervals.</summary>
public class ArrowTrap : MonoBehaviour
{
    SpriteRenderer sr;
    Sprite[] f;
    int state;
    float timer, interval;

    public void Init(float shotInterval, float startDelay)
    {
        sr = GetComponent<SpriteRenderer>();
        f = TrapSprites.Frames("arrow", 16, 32, new Vector2(8, 0));
        sr.sprite = f[0];
        interval = shotInterval;
        timer = startDelay;
    }

    void Update()
    {
        if (!Application.isPlaying || f == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        switch (state)
        {
            case 0: state = 1; timer = 0.2f; sr.sprite = f[1]; break;
            case 1: state = 2; timer = 0.2f; sr.sprite = f[2]; break;
            case 2:
                state = 3; timer = 0.2f; sr.sprite = f[3];
                ArrowProjectile.Spawn(transform.position + new Vector3(0f, 0.4f, 0f), Vector2.down);
                break;
            default: state = 0; timer = interval; sr.sprite = f[0]; break;
        }
    }
}

/// <summary>An arrow in flight. Hurts the player; disappears when it hits anything solid.</summary>
public class ArrowProjectile : MonoBehaviour
{
    Vector2 dir;
    const float Speed = 6.5f;

    public static void Spawn(Vector3 position, Vector2 direction)
    {
        var go = new GameObject("arrow");
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteSheet.Cut("Traps/just_arrow", 0, 0, 16, 16, new Vector2(8, 8));
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 2000;
        var arrow = go.AddComponent<ArrowProjectile>();
        arrow.dir = direction.normalized;
        Destroy(go, 4f);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * Speed * Time.deltaTime);
        Vector2 tip = (Vector2)transform.position + dir * 0.35f;
        foreach (var hit in Physics2D.OverlapCircleAll(tip, 0.12f))
        {
            var player = hit.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.Health.TakeDamage(10, transform.position - (Vector3)dir);
                Destroy(gameObject);
                return;
            }
            if (!hit.isTrigger) { Destroy(gameObject); return; } // wall, pillar, furniture...
        }
    }
}
