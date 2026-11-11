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
    public bool hasWateringCanState;
    public int wateringCanCharges;
    public float x, y;
    public List<WorldEntry> world = new List<WorldEntry>();
}

/// <summary>
/// Save/load to a JSON file. F5 saves, F9 loads (sleeping in the bed also saves).
/// Saves: inventory, equipment, hotbar, player health and position, crop plots, opened chests/crates, collected items.
/// Loading rebuilds the world fresh and then re-applies the saved state, so monsters respawn.
/// A previous valid save is kept as a backup in case the newest file is damaged.
/// </summary>
public static class SaveSystem
{
    const int CurrentVersion = 1;

    static string FilePath => Path.Combine(Application.persistentDataPath, "farm_save.json");
    static string BackupPath => FilePath + ".bak";
    static string TempPath => FilePath + ".tmp";

    public static bool HasSave => File.Exists(FilePath) || File.Exists(BackupPath);

    public static void Save()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null || player.Health == null || player.Health.Dead) return;

        var data = new SaveData
        {
            version = CurrentVersion,
            savedAt = DateTime.Now.ToString("g"),
            slots = Inventory.CaptureSlots(),
            equipment = Inventory.CaptureEquipment(),
            hotbar = Inventory.SelectedHotbar,
            health = player.Health.current,
            hasWateringCanState = true,
            wateringCanCharges = WateringCanSystem.Charges,
            x = player.transform.position.x,
            y = player.transform.position.y,
        };

        foreach (var kv in WorldState.All)
        {
            if (kv.Value == null) continue;

            try
            {
                string state = kv.Value.CaptureState();
                if (state != null) data.world.Add(new WorldEntry { id = kv.Key, state = state });
            }
            catch (Exception e)
            {
                // One broken world object should not prevent the rest of the game from saving.
                Debug.LogWarning("Could not save world object '" + kv.Key + "': " + e.Message);
            }
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TempPath, json);

            if (File.Exists(FilePath))
            {
                // Copy the current save first so a failed replacement never destroys the backup.
                File.Copy(FilePath, BackupPath, true);

                try
                {
                    // Replace the primary file in one operation where the platform supports it.
                    File.Replace(TempPath, FilePath, null);
                }
                catch (PlatformNotSupportedException)
                {
                    // Some Unity targets do not implement File.Replace.
                    File.Delete(FilePath);
                    File.Move(TempPath, FilePath);
                }
                catch (IOException)
                {
                    // Keep the backup safe and use a portable replacement fallback.
                    File.Delete(FilePath);
                    File.Move(TempPath, FilePath);
                }
            }
            else
            {
                File.Move(TempPath, FilePath);
            }

            GameHud.Toast("Game saved");
        }
        catch (Exception e)
        {
            TryDeleteTemp();
            Debug.LogError("Save failed: " + e);
            GameHud.Toast("Could not save the game (see Console).");
        }
    }

    public static bool Load()
    {
        if (!HasSave)
        {
            GameHud.Toast("No save file yet. Press F5 to save.");
            return false;
        }

        SaveData data;
        string primaryError;
        bool loadedFromBackup = false;

        if (!TryReadSave(FilePath, out data, out primaryError))
        {
            string backupError;
            if (!TryReadSave(BackupPath, out data, out backupError))
            {
                Debug.LogError("Load failed. Main save: " + primaryError + " Backup: " + backupError);
                GameHud.Toast("The save file and backup could not be read.");
                return false;
            }

            loadedFromBackup = true;
            Debug.LogWarning("The main save could not be loaded. Restored from backup: " + primaryError);
        }

        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return false;

        var savedPosition = new Vector2(data.x, data.y);
        var mapBuilder = UnityEngine.Object.FindFirstObjectByType<SimpleMapBuilder>();
        if (!IsValidPlayerPosition(savedPosition, mapBuilder))
        {
            Debug.LogError("Load failed: saved player position is outside the generated world.");
            GameHud.Toast("Save position is invalid. Your current game was not changed.");
            return false;
        }

        // Loose items dropped by monsters belong to the old world.
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            if (p.transform.parent == null) UnityEngine.Object.Destroy(p.gameObject);

        // Rebuild the world from scratch (fresh plots, chests, monsters), then apply what was saved.
        var builder = UnityEngine.Object.FindFirstObjectByType<SimpleMapBuilder>();
        if (builder != null) { builder.enabled = false; builder.enabled = true; }

        // Re-register persistent animals because rebuilding the generated map clears WorldState.
        foreach (var animal in UnityEngine.Object.FindObjectsByType<AnimalWander>(FindObjectsSortMode.None))
            animal.RegisterSaveable();

        if (data.world != null)
        {
            foreach (var entry in data.world)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.state == null) continue;

                if (WorldState.TryGet(entry.id, out var obj) && obj != null)
                {
                    try
                    {
                        obj.RestoreState(entry.state);
                    }
                    catch (Exception e)
                    {
                        // Continue loading other objects if this object's saved state is malformed.
                        Debug.LogWarning("Could not restore world object '" + entry.id + "': " + e.Message);
                    }
                }
            }
        }

        Inventory.Restore(data.slots, data.equipment, data.hotbar);
        Inventory.EnsureWateringCan(); // older saves predate the watering-can item
        WateringCanSystem.Restore(data.hasWateringCanState ? data.wateringCanCharges : WateringCanSystem.MaxCharges);

        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        if (body != null) body.position = savedPosition;
        player.transform.position = savedPosition;

        int safeHealth = Mathf.Clamp(data.health, 1, Mathf.Max(1, player.Health.max));
        player.Health.Revive(safeHealth);

        GameHud.Toast(loadedFromBackup
            ? "Main save damaged. Loaded your previous backup (" + data.savedAt + ")."
            : "Game loaded  (" + data.savedAt + ")");
        return true;
    }

    static bool IsValidPlayerPosition(Vector2 position, SimpleMapBuilder builder)
    {
        if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
            float.IsNaN(position.y) || float.IsInfinity(position.y))
            return false;

        // Match the generated farm, farmhouse interior, and dungeon with a small safety margin.
        // If no builder exists, use a broad sanity range rather than rejecting a valid custom scene.
        if (builder == null)
            return Mathf.Abs(position.x) <= 1000f && Mathf.Abs(position.y) <= 1000f;

        float minX = -2f;
        float maxX = builder.DungeonOrigin.x + DungeonWorld.Width + 2f;
        float minY = -2f;
        float maxY = builder.DungeonOrigin.y + DungeonWorld.Height + 2f;
        return position.x >= minX && position.x <= maxX &&
               position.y >= minY && position.y <= maxY;
    }

    static bool TryReadSave(string path, out SaveData data, out string error)
    {
        data = null;
        error = null;

        if (!File.Exists(path))
        {
            error = "File does not exist: " + path;
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null) throw new InvalidDataException("Save data is empty or invalid.");
            if (data.version != CurrentVersion)
                throw new InvalidDataException("Unsupported save version: " + data.version);
            if (float.IsNaN(data.x) || float.IsInfinity(data.x) ||
                float.IsNaN(data.y) || float.IsInfinity(data.y))
                throw new InvalidDataException("Player position is invalid.");
            if (data.health <= 0)
                throw new InvalidDataException("Saved player health is not valid.");
            if (data.slots == null || data.slots.Count != Inventory.Capacity)
                throw new InvalidDataException("Inventory slot data is missing or incomplete.");
            if (data.equipment == null || data.equipment.Count != 5)
                throw new InvalidDataException("Equipment data is missing or incomplete.");

            // Older saves may omit the world-state list; world objects can use their defaults.
            if (data.world == null) data.world = new List<WorldEntry>();
            return true;
        }
        catch (Exception e)
        {
            data = null;
            error = e.Message;
            return false;
        }
    }

    static void TryDeleteTemp()
    {
        try { if (File.Exists(TempPath)) File.Delete(TempPath); }
        catch (Exception e) { Debug.LogWarning("Could not remove temporary save file: " + e.Message); }
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
