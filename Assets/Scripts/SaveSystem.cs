using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable] class WorldEntry { public string id, state; }

[Serializable] class SaveData
{
    public int version = 1;
    public string savedAt;
    public List<Inventory.SlotData> slots, equipment;
    public int hotbar, health;
    public float x, y;
    public int day;
    public float minutes;
    public List<WorldEntry> world = new List<WorldEntry>();
}

/// <summary>
/// Save/load to a JSON file. F5 saves, F9 loads (sleeping in the bed also saves).
/// Saves: inventory, equipment, hotbar, player health and position, crop plots, opened chests/crates, collected items.
/// Loading rebuilds the world fresh and then re-applies the saved state, so monsters respawn.
/// </summary>
public static class SaveSystem
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "farm_save.json");
    public static bool HasSave => File.Exists(FilePath);

    public static void Save()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null || player.Health.Dead) return;

        var data = new SaveData
        {
            savedAt = DateTime.Now.ToString("g"),
            slots = Inventory.CaptureSlots(),
            equipment = Inventory.CaptureEquipment(),
            hotbar = Inventory.SelectedHotbar,
            health = player.Health.current,
            day = GameClock.TotalDays,
            minutes = GameClock.Minutes,
            x = player.transform.position.x,
            y = player.transform.position.y,
        };
        foreach (var kv in WorldState.All)
        {
            string state = kv.Value.CaptureState();
            if (state != null) data.world.Add(new WorldEntry { id = kv.Key, state = state });
        }

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            GameHud.Toast("Game saved");
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
            GameHud.Toast("Could not save the game (see Console).");
        }
    }

    public static bool Load()
    {
        if (!HasSave) { GameHud.Toast("No save file yet. Press F5 to save."); return false; }
        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
        catch (Exception e) { Debug.LogError("Load failed: " + e.Message); GameHud.Toast("The save file could not be read."); return false; }

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return false;

        // Loose items dropped by monsters belong to the old world
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            if (p.transform.parent == null) UnityEngine.Object.Destroy(p.gameObject);

        // Rebuild the world from scratch (fresh plots, chests, monsters), then apply what was saved
        var builder = UnityEngine.Object.FindFirstObjectByType<SimpleMapBuilder>();
        if (builder != null) { builder.enabled = false; builder.enabled = true; }

        foreach (var entry in data.world)
            if (WorldState.TryGet(entry.id, out var obj)) obj.RestoreState(entry.state);

        Inventory.Restore(data.slots, data.equipment, data.hotbar);
        GameClock.Restore(data.day, data.minutes);

        var pos = new Vector2(data.x, data.y);
        var body = player.GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.position = pos;
        player.transform.position = pos;
        player.Health.Revive(Mathf.Max(1, data.health));

        GameHud.Toast("Game loaded  (" + data.savedAt + ")");
        return true;
    }
}

/// <summary>F5 = save, F9 = load. Also tells you at the start if a save exists.</summary>
public class SaveHotkeys : MonoBehaviour
{
    void Start()
    {
        if (SaveSystem.HasSave) GameHud.Toast("Save found: press F9 to continue it, or just start a new game.");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || GameHud.IsFading || GameHud.UiOpen) return;
        if (kb.f5Key.wasPressedThisFrame) SaveSystem.Save();
        if (kb.f9Key.wasPressedThisFrame) SaveSystem.Load();
    }
}
