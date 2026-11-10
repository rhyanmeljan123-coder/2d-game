using UnityEngine;

/// <summary>Animated decorations and small interactive props built from the Resources/Traps strips.</summary>
public static class Props
{
    static int OrderFor(float y) => 1000 - Mathf.RoundToInt(y * 10f);

    /// <summary>A looping animated sprite (torch, flag, candlestick...). Optional solid collider.</summary>
    public static GameObject Animated(Transform parent, string name, string strip, int w, int h, Vector2 pivotPx,
                                      Vector2 position, float fps, int? order = null,
                                      Vector2 solid = default, Vector2 solidOffset = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = order ?? OrderFor(position.y);
        var anim = go.AddComponent<SimpleAnimator>();
        anim.Play(TrapSprites.Frames(strip, w, h, pivotPx), fps);
        if (solid.x > 0f)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = solid;
            box.offset = solidOffset;
        }
        return go;
    }

    public static void Torch(Transform parent, float x, float y) =>
        Animated(parent, "torch", "torch", 16, 16, new Vector2(8, 2), new Vector2(x, y), 8f, null, new Vector2(0.4f, 0.3f), new Vector2(0f, 0.2f));

    public static void Candle(Transform parent, float x, float y, bool second = false) =>
        Animated(parent, "candlestick", second ? "candle_2" : "candle_1", 16, 16, new Vector2(8, 0), new Vector2(x, y), 7f, null, new Vector2(0.4f, 0.3f), new Vector2(0f, 0.2f));

    /// <summary>A torch mounted on a wall: drawn above the wall tiles but behind characters.</summary>
    public static void WallTorch(Transform parent, float x, float y) =>
        Animated(parent, "wall torch", "side_torch", 16, 16, new Vector2(8, 2), new Vector2(x, y), 8f, -14);

    public static void Flag(Transform parent, float x, float y) =>
        Animated(parent, "flag", "flag", 16, 16, new Vector2(8, 1), new Vector2(x, y), 5f);

    // ---------------------------------------------------------------- traps

    public static void Spikes(Transform parent, float x, float y, float startDelay)
    {
        var go = new GameObject("spike trap");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = -12; // on the floor
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.8f);
        go.AddComponent<SpikeTrap>().Init(startDelay);
    }

    /// <summary>Vertical flame jet shooting up from (x, y), or a horizontal one shooting right (or left).</summary>
    public static void Flames(Transform parent, float x, float y, bool horizontal, bool faceLeft, float startDelay)
    {
        var go = new GameObject("flamethrower");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = OrderFor(y) + 5;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<FlameTrap>().Init(horizontal, faceLeft, startDelay);
    }

    /// <summary>A wall arrow launcher. (x, y) is its bottom: the launcher hole is the top half, arrows fly down.</summary>
    public static void Arrows(Transform parent, float x, float y, float interval, float startDelay)
    {
        var go = new GameObject("arrow trap");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = -13;
        go.AddComponent<ArrowTrap>().Init(interval, startDelay);
    }

    // ---------------------------------------------------------------- boxes & flasks

    /// <summary>variant 0-3: silver box, wooden box, mini silver box, mini wooden box.</summary>
    public static void Box(Transform parent, float x, float y, int variant)
    {
        string strip = variant == 0 ? "box_1" : variant == 1 ? "box_2" : variant == 2 ? "mini_box_1" : "mini_box_2";
        var go = Animated(parent, "box", strip, 16, 16, new Vector2(8, 2), new Vector2(x, y), 1f, null,
                          new Vector2(0.8f, 0.5f), new Vector2(0f, 0.3f));
        var anim = go.GetComponent<SimpleAnimator>();
        anim.Play(new[] { TrapSprites.Frames(strip, 16, 16, new Vector2(8, 2))[0] }, 1f); // closed (first frame only)
        go.AddComponent<LootBox>().Init(anim, TrapSprites.Frames(strip, 16, 16, new Vector2(8, 2)));
    }

    /// <summary>kind 0 = speed flask (small blue), 1 = strength flask (big blue). Instant boost when picked up.</summary>
    public static void BuffFlask(Transform parent, float x, float y, int kind)
    {
        var go = Animated(parent, kind == 0 ? "speed flask" : "strength flask", kind == 0 ? "flask_2" : "flask_3",
                          16, 16, new Vector2(8, 8), new Vector2(x, y), 5f);
        go.transform.localScale = Vector3.one * 0.9f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;
        go.AddComponent<FlaskPickup>().kind = kind;
    }
}

/// <summary>A crate: open it with E for a few coins and the odd ingredient.</summary>
public class LootBox : Interactable
{
    SimpleAnimator anim;
    Sprite[] frames;
    bool opened;

    public void Init(SimpleAnimator a, Sprite[] openFrames) { anim = a; frames = openFrames; }

    public override string Prompt => opened ? null : "Open box [E]";

    public override void Interact()
    {
        if (opened) return;
        opened = true;
        anim.Play(frames, 8f, false, true);

        int coins = Random.Range(2, 7);
        Inventory.Add(Item.Coin, coins);
        string msg = $"Box: +{coins} coins";
        float roll = Random.value;
        if (roll < 0.30f)
        {
            var seed = Inventory.SeedFor(Random.Range(0, Inventory.CropCount));
            Inventory.Add(seed, 1);
            msg += $", +1 {Inventory.Name(seed).ToLower()}";
        }
        else if (roll < 0.50f) { Inventory.Add(Item.Herb, 1); msg += ", +1 herb"; }
        else if (roll < 0.60f) { Inventory.Add(Item.RedFlask, 1); msg += ", +1 red flask"; }
        GameHud.Toast(msg);
    }
}

/// <summary>A blue flask lying around. Walk over it for a temporary boost.</summary>
public class FlaskPickup : MonoBehaviour
{
    public int kind; // 0 speed, 1 strength

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        if (kind == 0) { player.AddSpeedBoost(25f); GameHud.Toast("Speed flask: you move faster for 25 seconds!"); }
        else { player.AddStrengthBoost(30f); GameHud.Toast("Strength flask: your sword hits harder for 30 seconds!"); }
        Destroy(gameObject);
    }
}
