using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>Anything in the world whose state must survive saving (crop plots, chests, crates, pickups).</summary>
public interface ISaveable
{
    /// <summary>The state as text, or null if it is still in its default state (nothing to save).</summary>
    string CaptureState();
    void RestoreState(string state);
}

/// <summary>
/// Registry of saveable world objects. New registrations use their world position for stable IDs,
/// so adding another object earlier in the build order does not shift existing save IDs.
/// Legacy order-based IDs are retained as lookup aliases to keep existing version-1 saves readable.
/// </summary>
public static class WorldState
{
    static readonly Dictionary<string, ISaveable> objects = new Dictionary<string, ISaveable>();
    static readonly Dictionary<string, ISaveable> legacyAliases = new Dictionary<string, ISaveable>();
    static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    public static IEnumerable<KeyValuePair<string, ISaveable>> All => objects;

    public static void Clear()
    {
        objects.Clear();
        legacyAliases.Clear();
        counters.Clear();
    }

    /// <summary>Registers an object using its world position as a deterministic identity.</summary>
    public static void Register(string kind, ISaveable obj, Vector3 worldPosition)
    {
        if (obj == null || string.IsNullOrEmpty(kind)) return;

        string id = StableId(kind, worldPosition);
        if (objects.ContainsKey(id))
        {
            Debug.LogError("WorldState: duplicate stable ID '" + id + "'. Check for saveable objects at the same position.");
            return;
        }

        objects.Add(id, obj);

        // Preserve the old order-based key as a read-only alias for existing version-1 save files.
        string legacyId = NextLegacyId(kind);
        legacyAliases[legacyId] = obj;
    }

    /// <summary>
    /// Compatibility overload for callers that do not yet supply a position.
    /// New world objects should use the position-based overload.
    /// </summary>
    public static void Register(string kind, ISaveable obj)
    {
        if (obj == null || string.IsNullOrEmpty(kind)) return;
        string legacyId = NextLegacyId(kind);
        objects[legacyId] = obj;
    }

    static string NextLegacyId(string kind)
    {
        counters.TryGetValue(kind, out int n);
        counters[kind] = n + 1;
        return kind + "#" + n;
    }

    static string StableId(string kind, Vector3 position)
    {
        return kind + "@" +
            position.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
            position.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
            position.z.ToString("0.###", CultureInfo.InvariantCulture);
    }

    public static bool TryGet(string id, out ISaveable obj)
    {
        if (objects.TryGetValue(id, out obj)) return true;
        return legacyAliases.TryGetValue(id, out obj);
    }
}