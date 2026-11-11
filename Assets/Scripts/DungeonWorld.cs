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
        // A powerful guardian protects the northern treasure chamber.
        Enemy.Create(root, Enemy.Kind.Guardian, origin + new Vector2(16.5f, 17.5f));

        float ox = origin.x, oy = origin.y;

        // Chests: west is free, east needs the SILVER key (holds the gold key), north needs the GOLD key (the treasure)
        FarmWorld.AddChest(root, ox + 2.5f, oy + 17.2f, LootKind.Common, true);
        FarmWorld.AddChest(root, ox + 29.5f, oy + 17.2f, LootKind.SilverLocked, true);
        FarmWorld.AddChest(root, ox + 15.5f, oy + 20.2f, LootKind.GoldLocked, true);
        FarmWorld.AddPickup(root, Item.SilverKey, ox + 2.5f, oy + 8.7f);

        // Supplies: herbs and flowers for potions, flasks and coins
        FarmWorld.AddPickup(root, Item.Herb, ox + 4.5f, oy + 16.5f);
        FarmWorld.AddPickup(root, Item.Flower, ox + 27.5f, oy + 9.5f);
        FarmWorld.AddPickup(root, Item.Herb, ox + 16f, oy + 6.5f);
        FarmWorld.AddPickup(root, Item.RedFlask, ox + 9.5f, oy + 12.5f);
        Props.BuffFlask(root, ox + 9.5f, oy + 19.5f, 0);
        Props.BuffFlask(root, ox + 29.5f, oy + 9.5f, 1);
        FarmWorld.AddPickup(root, Item.Coin, ox + 13f, oy + 3f, 3);
        FarmWorld.AddPickup(root, Item.Coin, ox + 22f, oy + 13f, 4);
        FarmWorld.AddPickup(root, Item.Coin, ox + 4f, oy + 12f, 3);
        FarmWorld.AddPickup(root, Item.Coin, ox + 20f, oy + 19f, 5);

        // Crates
        Props.Box(root, ox + 6.5f, oy + 8.7f, 0);
        Props.Box(root, ox + 30f, oy + 8.7f, 1);
        Props.Box(root, ox + 9f, oy + 18.7f, 2);
        Props.Box(root, ox + 22f, oy + 18.7f, 3);
        Props.Box(root, ox + 19.5f, oy + 1.6f, 1);

        // Light: torches and candlesticks
        Props.Torch(root, ox + 12.5f, oy + 1.2f);
        Props.Torch(root, ox + 19.5f, oy + 1.2f);
        Props.WallTorch(root, ox + 12.5f, oy + 4.0f);
        Props.WallTorch(root, ox + 19.5f, oy + 4.0f);
        Props.Candle(root, ox + 8.6f, oy + 9.2f);
        Props.Candle(root, ox + 23.4f, oy + 9.2f, true);
        Props.Candle(root, ox + 1.6f, oy + 17.4f);
        Props.Candle(root, ox + 5.6f, oy + 17.4f, true);
        Props.Candle(root, ox + 26.6f, oy + 17.4f);
        Props.Candle(root, ox + 30.0f, oy + 17.4f, true);
        Props.Candle(root, ox + 8.6f, oy + 20.4f);
        Props.Candle(root, ox + 22.6f, oy + 20.4f, true);

        // TRAPS
        // spikes (x, y are tile centres; the last number staggers when each one starts)
        Props.Spikes(root, ox + 15.5f, oy + 6.5f, 0.0f);   // corridor to the hub
        Props.Spikes(root, ox + 16.5f, oy + 6.5f, 0.0f);
        Props.Spikes(root, ox + 13.5f, oy + 12.5f, 0.8f);  // hub
        Props.Spikes(root, ox + 14.5f, oy + 12.5f, 0.8f);
        Props.Spikes(root, ox + 17.5f, oy + 10.5f, 1.6f);
        Props.Spikes(root, ox + 18.5f, oy + 10.5f, 1.6f);
        Props.Spikes(root, ox + 2.5f, oy + 12.5f, 0.4f);   // west room
        Props.Spikes(root, ox + 3.5f, oy + 12.5f, 0.4f);
        Props.Spikes(root, ox + 4.5f, oy + 13.5f, 1.2f);
        Props.Spikes(root, ox + 27.5f, oy + 12.5f, 0.6f);  // east room
        Props.Spikes(root, ox + 28.5f, oy + 12.5f, 0.6f);
        Props.Spikes(root, ox + 27.5f, oy + 14.5f, 1.4f);
        Props.Spikes(root, ox + 13.5f, oy + 19.5f, 0.2f);  // north hall
        Props.Spikes(root, ox + 14.5f, oy + 19.5f, 0.2f);
        Props.Spikes(root, ox + 17.5f, oy + 18.5f, 1.0f);
        Props.Spikes(root, ox + 18.5f, oy + 18.5f, 1.0f);

        // flamethrowers
        Props.Flames(root, ox + 9.5f, oy + 9.0f, false, false, 0.5f);   // hub, shooting up from the floor edge
        Props.Flames(root, ox + 22.5f, oy + 9.0f, false, false, 1.7f);
        Props.Flames(root, ox + 1.0f, oy + 15.5f, true, false, 1.0f);   // west room, shooting right
        Props.Flames(root, ox + 31.0f, oy + 15.0f, true, true, 2.0f);   // east room, shooting left

        // arrow launchers in the hub's top wall (hole in the wall, arrows fly down the room)
        Props.Arrows(root, ox + 9.5f, oy + 14.0f, 2.6f, 0.0f);
        Props.Arrows(root, ox + 13.5f, oy + 14.0f, 2.6f, 0.65f);
        Props.Arrows(root, ox + 18.5f, oy + 14.0f, 2.6f, 1.3f);
        Props.Arrows(root, ox + 22.5f, oy + 14.0f, 2.6f, 1.95f);
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
