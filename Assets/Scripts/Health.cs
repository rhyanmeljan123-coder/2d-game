using System;
using UnityEngine;

/// <summary>Hit points for the player and the enemies.</summary>
public class Health : MonoBehaviour
{
    public int max = 100;
    public int current = 100;
    public float invulnerableSeconds = 0.5f;

    float lastHit = -99f;

    public bool Dead { get; private set; }
    public bool Invulnerable => Time.time < lastHit + invulnerableSeconds;

    /// <summary>(damage, position the hit came from)</summary>
    public event Action<int, Vector2> Damaged;
    public event Action Died;
    public event Action Changed;

    public void Configure(int maxHp, float invulnerable)
    {
        max = maxHp;
        current = maxHp;
        invulnerableSeconds = invulnerable;
        Dead = false;
    }

    public bool TakeDamage(int amount, Vector2 from)
    {
        if (Dead || Invulnerable || amount <= 0) return false;
        lastHit = Time.time;
        current = Mathf.Max(0, current - amount);
        Damaged?.Invoke(amount, from);
        Changed?.Invoke();
        if (current == 0) { Dead = true; Died?.Invoke(); }
        return true;
    }

    public void Heal(int amount)
    {
        if (Dead) return;
        current = Mathf.Min(max, current + amount);
        Changed?.Invoke();
    }

    public void Revive(int hp)
    {
        Dead = false;
        current = Mathf.Clamp(hp, 1, max);
        Changed?.Invoke();
    }
}
