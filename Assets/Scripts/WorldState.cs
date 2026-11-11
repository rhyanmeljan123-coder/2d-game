using System.Collections.Generic;

/// <summary>Anything in the world whose state must survive saving (crop plots, chests, crates, pickups).</summary>
public interface ISaveable
{
    /// <summary>The state as text, or null if it is still in its default state (nothing to save).</summary>
    string CaptureState();
    void RestoreState(string state);
}

/// <summary>
/// Registry of saveable world objects. The world is built the same way every time, so the Nth chest/plot/crate
/// always gets the same id (chest#2, plot#7...), which is how a save file finds them again.
/// </summary>
public static class WorldState
{
    static readonly Dictionary<string, ISaveable> objects = new Dictionary<string, ISaveable>();
    static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    public static IEnumerable<KeyValuePair<string, ISaveable>> All => objects;

    public static void Clear()
    {
        objects.Clear();
        counters.Clear();
    }

    public static void Register(string kind, ISaveable obj)
    {
        counters.TryGetValue(kind, out int n);
        counters[kind] = n + 1;
        objects[kind + "#" + n] = obj;
    }

    public static bool TryGet(string id, out ISaveable obj) => objects.TryGetValue(id, out obj);
}
