using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Places and persists simple farm structures crafted from gathered materials.</summary>
public static class BuildableSystem
{
    [Serializable] public class PlacedData
    {
        public string kind;
        public float x, y;
        public List<StorageCrate.StoredItemData> contents = new List<StorageCrate.StoredItemData>();
    }

    static readonly List<GameObject> placed = new List<GameObject>();

    public static bool BuildFence()
    {
        if (Inventory.Get(Item.Wood) < 3) { GameHud.Toast("A fence needs 3 wood."); return false; }
        return Place("Fence", 3, 0);
    }

    public static bool BuildCrate()
    {
        if (Inventory.Get(Item.Wood) < 5 || Inventory.Get(Item.Stone) < 2)
        { GameHud.Toast("A storage crate needs 5 wood and 2 stone."); return false; }
        return Place("Crate", 5, 2);
    }

    static bool Place(string kind, int wood, int stone)
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return false;
        Vector2 facing = player.FacingDir.sqrMagnitude > 0.01f ? player.FacingDir.normalized : Vector2.down;
        Vector2 raw = (Vector2)player.transform.position + facing * 1.25f;
        Vector2 point = new Vector2(Mathf.Round(raw.x * 2f) * 0.5f, Mathf.Round(raw.y * 2f) * 0.5f);
        Vector2 checkSize = kind == "Fence" ? new Vector2(1.55f, 0.45f) : new Vector2(0.7f, 0.65f);
        Vector2 checkOffset = kind == "Fence" ? new Vector2(0f, 0.16f) : new Vector2(0f, 0.25f);
        foreach (var hit in Physics2D.OverlapBoxAll(point + checkOffset, checkSize, 0f))
        {
            if (hit == null || hit.isTrigger || hit.GetComponentInParent<PlayerController>() != null) continue;
            GameHud.Toast("There isn't enough clear space to build here.");
            return false;
        }
        if (!Inventory.TryRemove(Item.Wood, wood)) return false;
        if (stone > 0 && !Inventory.TryRemove(Item.Stone, stone))
        {
            Inventory.Add(Item.Wood, wood);
            return false;
        }
        Create(kind, point, null);
        GameHud.Toast(kind == "Fence" ? "Fence placed." : "Storage crate placed. Press E to store items.");
        return true;
    }

    static void Create(string kind, Vector2 point, List<StorageCrate.StoredItemData> contents)
    {
        var go = new GameObject(kind == "Fence" ? "Built Fence" : "Built Storage Crate");
        go.transform.position = point;
        var sr = go.AddComponent<SpriteRenderer>();
        if (kind == "Fence")
            sr.sprite = SpriteSheet.Cut("Objects/farm_objects", 0, 32, 48, 16, new Vector2(24f, 0f));
        else
        {
            var frames = TrapSprites.Frames("mini_chest", 16, 16, new Vector2(8f, 2f));
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
            var storage = go.AddComponent<StorageCrate>();
            storage.Init(contents);
        }
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(point.y * 10f);
        var box = go.AddComponent<BoxCollider2D>();
        if (kind == "Fence") { box.size = new Vector2(1.6f, 0.35f); box.offset = new Vector2(0f, 0.16f); }
        else { box.size = new Vector2(0.7f, 0.55f); box.offset = new Vector2(0f, 0.25f); }
        placed.Add(go);
    }

    public static List<PlacedData> Capture()
    {
        var data = new List<PlacedData>();
        foreach (var go in placed)
        {
            if (go == null) continue;
            var entry = new PlacedData
            {
                kind = go.name.Contains("Fence") ? "Fence" : "Crate",
                x = go.transform.position.x,
                y = go.transform.position.y
            };
            var storage = go.GetComponent<StorageCrate>();
            if (storage != null) entry.contents = storage.Capture();
            data.Add(entry);
        }
        return data;
    }

    public static void Restore(List<PlacedData> data)
    {
        foreach (var go in placed) if (go != null) UnityEngine.Object.Destroy(go);
        placed.Clear();
        if (data == null) return;
        foreach (var entry in data)
        {
            if (entry == null || (entry.kind != "Fence" && entry.kind != "Crate") ||
                float.IsNaN(entry.x) || float.IsInfinity(entry.x) || float.IsNaN(entry.y) || float.IsInfinity(entry.y))
                continue;
            Create(entry.kind, new Vector2(entry.x, entry.y), entry.contents);
        }
    }
}
