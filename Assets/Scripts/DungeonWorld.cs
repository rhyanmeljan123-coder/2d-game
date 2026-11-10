using UnityEngine;

/// <summary>
/// The dungeon: a cave reached through the entrance in the first map. Full of slimes and skeletons whose drops
/// (slime gel, bones, herbs, flowers) are ingredients for crafting healing food and potions at the farmhouse.
/// '#' = rock, '.' = floor. Row 0 of Layout is the TOP of the dungeon.
/// </summary>
public static class DungeonWorld
{
    public const int Width = 32, Height = 22;

    public static readonly string[] Layout =
    {
        "################################",
        "########................########",
        "########...#........#...########",
        "########................########",
        "#......########..#########.....#",
        "#......########..#########.....#",
        "#......########..#########.....#",
        "#......#................##.....#",
        "#......#...#........#...##.....#",
        "#..............................#",
        "#..............................#",
        "#......#...#........#...##.....#",
        "#......#................##.....#",
        "#......########..#########.....#",
        "###############..###############",
        "###############..###############",
        "###############..###############",
        "###########..........###########",
        "###########..........###########",
        "###########..........###########",
        "###########..........###########",
        "################################",
    };

    /// <summary>x, y are tile coordinates with y = 0 at the BOTTOM of the dungeon.</summary>
    public static bool IsFloor(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return Layout[Height - 1 - y][x] == '.';
    }

    /// <summary>True for rock tiles that touch a floor tile (the only ones we draw).</summary>
    public static bool IsVisibleWall(int x, int y)
    {
        if (IsFloor(x, y) || x < 0 || x >= Width || y < 0 || y >= Height) return false;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && IsFloor(x + dx, y + dy)) return true;
        return false;
    }

    static readonly Vector2[] Slimes = { new Vector2(10.5f, 12.5f), new Vector2(21.5f, 10.5f), new Vector2(3.5f, 10.5f), new Vector2(5.5f, 15.5f) };
    static readonly Vector2[] Skeletons =
        { new Vector2(28.5f, 10.5f), new Vector2(28.5f, 15.5f), new Vector2(12.5f, 19.5f), new Vector2(19.5f, 19.5f), new Vector2(15.5f, 18.5f) };
    static readonly Vector2[] Chests = { new Vector2(2.5f, 17.2f), new Vector2(29.5f, 17.2f), new Vector2(15.5f, 20.2f) };

    /// <summary>Where the player appears when entering (relative to the dungeon origin).</summary>
    public static Vector2 EntrySpawn => new Vector2(16f, 2.6f);

    // ---------------------------------------------------------------- build

    public static void Build(Transform parent, Vector2Int o, Vector2 outsideSpawn)
    {
        var root = new GameObject("Dungeon").transform;
        root.SetParent(parent, false);
        Vector2 origin = o;

        // Exit (back to the cave entrance outside)
        var exit = SpriteSheet.Cut("Objects/cave_exit", 0, 0, 24, 24, new Vector2(12, 0));
        var exitGo = new GameObject("dungeon exit");
        exitGo.transform.SetParent(root, false);
        exitGo.transform.position = origin + new Vector2(16f, 0.05f);
        var exitSr = exitGo.AddComponent<SpriteRenderer>();
        exitSr.sprite = exit;
        SpriteMaterial.Apply(exitSr);
        exitSr.sortingOrder = -14;
        FarmWorld.AddTeleporter(root, origin + new Vector2(16f, 1.4f), new Vector2(1.8f, 0.6f), outsideSpawn);

        foreach (var p in Slimes) Enemy.Create(root, Enemy.Kind.Slime, origin + p);
        foreach (var p in Skeletons) Enemy.Create(root, Enemy.Kind.Skeleton, origin + p);
        foreach (var p in Chests) FarmWorld.AddChest(root, origin.x + p.x, origin.y + p.y);

        // A little supply of herbs and flowers growing in the dungeon
        FarmWorld.AddPickup(root, Item.Herb, origin.x + 2.5f, origin.y + 9.5f);
        FarmWorld.AddPickup(root, Item.Flower, origin.x + 29.5f, origin.y + 9.5f);
        FarmWorld.AddPickup(root, Item.Herb, origin.x + 16f, origin.y + 6.5f);
    }

    /// <summary>The cave mouth on the outdoor map. Walk into the dark opening to go down.</summary>
    public static void AddEntrance(Transform parent, Vector2 basePos, Vector2 destination)
    {
        var sprite = SpriteSheet.Cut("Objects/cave_entrance", 0, 0, 64, 48, new Vector2(32, 0));
        var go = new GameObject("cave entrance");
        go.transform.SetParent(parent, false);
        go.transform.position = basePos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(basePos.y * 10f);

        // Solid rock on both sides and behind the opening
        AddBox(go, new Vector2(1.3f, 2.4f), new Vector2(-1.35f, 1.2f));
        AddBox(go, new Vector2(1.3f, 2.4f), new Vector2(1.35f, 1.2f));
        AddBox(go, new Vector2(1.6f, 0.5f), new Vector2(0f, 1.45f));

        FarmWorld.AddTeleporter(parent, basePos + new Vector2(0f, 0.85f), new Vector2(1.1f, 0.35f), destination);
    }

    static void AddBox(GameObject go, Vector2 size, Vector2 offset)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = offset;
    }
}
