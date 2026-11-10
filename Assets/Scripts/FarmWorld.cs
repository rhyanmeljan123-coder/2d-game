using UnityEngine;

/// <summary>
/// Everything "placed" in the world from the objects &amp; items packs:
/// farm plots, chests, pickups, fences, houses, trees, rocks and haystacks for BOTH maps.
/// Built by SimpleMapBuilder (so it also shows in the Scene view in Edit mode).
/// All positions are in tiles; the second map starts at x = mapOffset.
/// </summary>
public static class FarmWorld
{
    const string Objects = "Objects/farm_objects";

    // ---- sprites cut from farm objects free.png (x, y, w, h in pixels from top-left) ----
    static Sprite Obj(int x, int y, int w, int h, float pivotY = 0f) =>
        SpriteSheet.Cut(Objects, x, y, w, h, new Vector2(w * 0.5f, pivotY));

    static Sprite BirchA => Obj(54, 0, 35, 60, 3);
    static Sprite BirchB => Obj(102, 0, 35, 60, 3);
    static Sprite Stump => Obj(105, 64, 25, 12, 1);
    static Sprite ChestClosed => Obj(17, 68, 14, 12);
    static Sprite ChestOpen => Obj(17, 81, 14, 15);
    static Sprite Flowers => Obj(33, 70, 14, 9);
    static Sprite Rock => Obj(32, 80, 16, 16);
    static Sprite Tulips => Obj(50, 66, 13, 13);
    static Sprite Clover => Obj(48, 81, 16, 15);
    static Sprite Haystack => Obj(64, 65, 32, 31);
    static Sprite House => Obj(8, 96, 81, 92);
    static Sprite FenceH => Obj(0, 32, 48, 16);
    static Sprite Post => Obj(20, 32, 8, 16);
    static Sprite DoorClosed => Obj(102, 113, 21, 27);

    // ---- sprites cut from furniture free.png ----
    const string Furniture = "Objects/furniture";
    static Sprite Furn(int x, int y, int w, int h) => SpriteSheet.Cut(Furniture, x, y, w, h, new Vector2(w * 0.5f, 0f));
    static Sprite Bed => Furn(0, 0, 16, 32);
    static Sprite TableTall => Furn(16, 0, 16, 32);
    static Sprite TableWide => Furn(32, 0, 24, 18);
    static Sprite Window => Furn(64, 16, 16, 16);
    static Sprite Stool => Furn(0, 32, 16, 16);
    static Sprite Picture => Furn(16, 32, 16, 16);
    static Sprite Lamp => Furn(32, 32, 16, 16);

    static Sprite soil;

    public static void Build(Transform parent, int mapOffset, Vector2Int interior, int interiorWidth, int interiorHeight, Vector2Int dungeon)
    {
        var root = new GameObject("FarmWorld").transform;
        root.SetParent(parent, false);
        int ox = mapOffset;
        MapOffsetForHouse = mapOffset;

        // ================= MAP 1 (first map) =================
        Add(root, "rock", Rock, 20.5f, 14.2f, solid: new Vector2(0.9f, 0.55f), solidOffset: new Vector2(0f, 0.3f));
        Add(root, "haystack", Haystack, 12.5f, 6.2f, solid: new Vector2(1.8f, 0.8f), solidOffset: new Vector2(0f, 0.5f));
        Add(root, "tulips", Tulips, 31.5f, 6.5f);
        Add(root, "flowers", Flowers, 11f, 17f);
        Add(root, "clover", Clover, 16f, 15f);
        AddChest(root, 31f, 12.8f);

        // Cave entrance to the dungeon (top of the first map)
        var dungeonEntry = new Vector2(dungeon.x + DungeonWorld.EntrySpawn.x, dungeon.y + DungeonWorld.EntrySpawn.y);
        DungeonWorld.AddEntrance(root, new Vector2(24f, 19.8f), dungeonEntry);

        AddPickup(root, Item.Carrot, 11.5f, 13f);
        AddPickup(root, Item.Tomato, 30f, 7f);
        AddPickup(root, Item.Potato, 17f, 4.5f);
        AddPickup(root, Item.Milk, 4f, 7f);
        AddPickup(root, Item.Herb, 26f, 12.5f);
        AddPickup(root, Item.Flower, 36.3f, 16.5f);
        AddPickup(root, Item.PumpkinSeeds, 9f, 10f);
        AddPickup(root, Item.PeaSeeds, 30f, 10f);

        // Market stall (sell crops & loot for coins, buy seeds) with banners
        var stall = Add(root, "Market stall", TableWide, 18f, 6f, new Vector2(1.4f, 0.5f), new Vector2(0f, 0.3f));
        stall.AddComponent<ShopStall>();
        Props.Flag(root, 16.5f, 5.9f);
        Props.Flag(root, 19.5f, 5.9f);

        // Banners at the cave entrance, boxes, flasks and coins
        Props.Flag(root, 21.3f, 19.8f);
        Props.Flag(root, 26.7f, 19.8f);
        Props.Box(root, 5.5f, 13.2f, 1);
        Props.Box(root, 33f, 9.2f, 3);
        Props.BuffFlask(root, 36f, 4f, 0);
        AddPickup(root, Item.RedFlask, 28f, 3f);
        AddPickup(root, Item.Coin, 7f, 11f, 3);
        AddPickup(root, Item.Coin, 21f, 3f, 2);
        AddPickup(root, Item.Coin, 35f, 20f, 4);

        // ================= MAP 2 (spring farm) =================
        // Red farmhouse
        var house = Add(root, "house", House, ox + 6.5f, 12.2f, solid: new Vector2(1.5f, 2.4f), solidOffset: new Vector2(-1.55f, 1.2f));
        var rightWall = house.AddComponent<BoxCollider2D>(); // second wall box, leaving a doorway in the middle
        rightWall.size = new Vector2(1.5f, 2.4f);
        rightWall.offset = new Vector2(1.55f, 1.2f);
        Add(root, "house door", DoorClosed, ox + 6.5f, 12.25f, order: OrderFor(12.2f) + 1);
        Vector2 interiorSpawn = new Vector2(interior.x + interiorWidth * 0.5f, interior.y + 2.6f);
        AddTeleporter(root, new Vector2(ox + 6.5f, 12.7f), new Vector2(1.2f, 0.7f), interiorSpawn);

        // Birch trees
        float[,] birches =
        {
            { 2f, 19.5f }, { 10f, 20.8f }, { 27.5f, 21f }, { 33.5f, 21.5f }, { 38f, 17.5f }, { 37.5f, 7.5f },
            { 21f, 6f }, { 35.5f, 4.5f }, { 3.5f, 5f }, { 16f, 2.8f }, { 28f, 2.5f }, { 9f, 7f },
        };
        for (int i = 0; i < birches.GetLength(0); i++)
            Add(root, "birch", i % 2 == 0 ? BirchA : BirchB, ox + birches[i, 0], birches[i, 1],
                solid: new Vector2(0.8f, 0.5f), solidOffset: new Vector2(0f, 0.3f));

        // Rocks, stumps, haystack, small plants
        Add(root, "rock", Rock, ox + 25.5f, 7.2f, solid: new Vector2(0.9f, 0.55f), solidOffset: new Vector2(0f, 0.3f));
        Add(root, "rock", Rock, ox + 7f, 3.5f, solid: new Vector2(0.9f, 0.55f), solidOffset: new Vector2(0f, 0.3f));
        Add(root, "stump", Stump, ox + 19f, 4.2f, solid: new Vector2(1f, 0.45f), solidOffset: new Vector2(0f, 0.25f));
        Add(root, "stump", Stump, ox + 36.5f, 19.5f, solid: new Vector2(1f, 0.45f), solidOffset: new Vector2(0f, 0.25f));
        Add(root, "haystack", Haystack, ox + 27.5f, 12.4f, solid: new Vector2(1.8f, 0.8f), solidOffset: new Vector2(0f, 0.5f));
        Add(root, "tulips", Tulips, ox + 14f, 6f);
        Add(root, "flowers", Flowers, ox + 16f, 7f);
        Add(root, "tulips", Tulips, ox + 30f, 7f);
        Add(root, "clover", Clover, ox + 34f, 8f);
        Add(root, "flowers", Flowers, ox + 9f, 21f);
        Add(root, "clover", Clover, ox + 24f, 22f);
        Add(root, "tulips", Tulips, ox + 36f, 13f);
        Add(root, "flowers", Flowers, ox + 12f, 3f);

        // Fence along the top of the farm yard
        for (int i = 0; i < 4; i++)
            Add(root, "fence", FenceH, ox + 13.5f + i * 3f, 21.1f, solid: new Vector2(3f, 0.4f), solidOffset: new Vector2(0f, 0.2f));
        Add(root, "post", Post, ox + 25.3f, 21.1f, solid: new Vector2(0.5f, 0.4f), solidOffset: new Vector2(0f, 0.2f));

        // Crop plots in the farm yard: 5 columns x 2 rows
        for (int cx = 0; cx < 5; cx++)
            for (int cy = 0; cy < 2; cy++)
                AddPlot(root, ox + 14.5f + cx * 2f, 15.5f + cy * 2f);

        // Chests
        AddChest(root, ox + 10.8f, 12.6f);
        AddChest(root, ox + 27.4f, 17f);

        // Items lying around
        AddPickup(root, Item.Pumpkin, ox + 3f, 8f);
        AddPickup(root, Item.Carrot, ox + 31f, 6.5f);
        AddPickup(root, Item.Potato, ox + 9f, 3f);
        AddPickup(root, Item.Tomato, ox + 22f, 5f);
        AddPickup(root, Item.Peas, ox + 36f, 12f);
        AddPickup(root, Item.CarrotSeeds, ox + 14f, 5f);
        AddPickup(root, Item.TomatoSeeds, ox + 28f, 3.5f);
        AddPickup(root, Item.Milk, ox + 6f, 20f);
        AddPickup(root, Item.Flower, ox + 2f, 21f);
        AddPickup(root, Item.Herb, ox + 33f, 10f);
        AddPickup(root, Item.PotatoSeeds, ox + 20f, 8f);
        Props.Box(root, ox + 12f, 5.5f, 2);
        Props.Box(root, ox + 24f, 7.5f, 3);
        Props.Box(root, ox + 2f, 14f, 0);
        Props.BuffFlask(root, ox + 20f, 3.5f, 1);
        AddPickup(root, Item.RedFlask, ox + 36f, 15f);
        AddPickup(root, Item.Coin, ox + 5f, 8f, 3);
        AddPickup(root, Item.Coin, ox + 30f, 3f, 2);
        AddPickup(root, Item.Coin, ox + 33f, 19f, 5);
        Props.Flag(root, ox + 3.2f, 11.9f);
        Props.Flag(root, ox + 9.8f, 11.9f);

        // ================= FARMHOUSE INTERIOR =================
        BuildInterior(root, interior, interiorWidth, interiorHeight);

        // ================= DUNGEON =================
        DungeonWorld.Build(root, dungeon, new Vector2(24f, 18.2f));
    }

    /// <summary>Spawns an item on the ground (used for monster drops).</summary>
    public static void DropPickup(Item item, Vector2 position) => AddPickup(null, item, position.x, position.y);

    // ---------------------------------------------------------------- interior

    static void BuildInterior(Transform root, Vector2Int o, int w, int h)
    {
        float ix = o.x, iy = o.y;
        const int OnWall = -14; // wall decorations: above the wall tiles, below characters

        // Front door (leads back outside)
        Add(root, "interior door", DoorClosed, ix + w * 0.5f, iy + 0.05f, order: OnWall);
        AddTeleporter(root, new Vector2(ix + w * 0.5f, iy + 1.4f), new Vector2(1.8f, 0.6f), new Vector2(MapOffsetForHouse + 6.5f, 11.0f));

        // On the back wall
        Add(root, "window", Window, ix + 4.6f, iy + 8.2f, order: OnWall);
        Add(root, "window", Window, ix + 9.4f, iy + 8.2f, order: OnWall);
        Add(root, "picture", Picture, ix + 7f, iy + 8.5f, order: OnWall);

        // Bed (sleep with E) and a lamp
        var bed = Add(root, "Bed", Bed, ix + 2.2f, iy + 6.0f, new Vector2(0.9f, 1.7f), new Vector2(0f, 0.9f));
        bed.AddComponent<global::Bed>();
        Add(root, "lamp", Lamp, ix + 3.6f, iy + 6.8f, new Vector2(0.4f, 0.3f), new Vector2(0f, 0.2f));

        // Dining table with two stools
        var craftTable = Add(root, "Crafting table", TableWide, ix + 7.2f, iy + 3.8f, new Vector2(1.4f, 0.5f), new Vector2(0f, 0.3f));
        craftTable.AddComponent<CraftingTable>();
        Add(root, "stool", Stool, ix + 5.8f, iy + 3.4f, new Vector2(0.5f, 0.3f), new Vector2(0f, 0.2f));
        Add(root, "stool", Stool, ix + 8.6f, iy + 3.4f, new Vector2(0.5f, 0.3f), new Vector2(0f, 0.2f));

        // Long table by the right wall
        Add(root, "table", TableTall, ix + 11.5f, iy + 5.4f, new Vector2(0.9f, 1.6f), new Vector2(0f, 1f));

        // Loot
        AddChest(root, ix + 11.7f, iy + 2f);
        AddPickup(root, Item.Milk, ix + 3.5f, iy + 2.8f);
        AddPickup(root, Item.Flower, ix + 4.6f, iy + 5.3f);
        AddPickup(root, Item.Herb, ix + 9.8f, iy + 2.5f);
    }

    static float MapOffsetForHouse; // x offset of the map the house is on (set in Build)

    // ---------------------------------------------------------------- helpers

    static int OrderFor(float y) => 1000 - Mathf.RoundToInt(y * 10f);

    /// <summary>Places a sprite so its bottom-centre sits at (x, y). Optional solid collider.</summary>
    static GameObject Add(Transform parent, string name, Sprite sprite, float x, float y,
                          Vector2 solid = default, Vector2 solidOffset = default, int? order = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = order ?? OrderFor(y);
        if (solid.x > 0f)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = solid;
            box.offset = solidOffset;
        }
        return go;
    }

    public static void AddTeleporter(Transform parent, Vector2 position, Vector2 size, Vector2 destination)
    {
        var go = new GameObject("Door (teleporter)");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = size;
        go.AddComponent<Teleporter>().Init(destination);
    }

    /// <summary>An animated chest. big = the silver dungeon chest, otherwise the small wooden one.</summary>
    public static void AddChest(Transform parent, float x, float y, LootKind kind = LootKind.Common, bool big = false)
    {
        var closed = TrapSprites.Frames(big ? "chest" : "mini_chest", 16, 16, new Vector2(8, 2));
        var open = TrapSprites.Frames(big ? "chest_open" : "mini_chest_open", 16, 16, new Vector2(8, 2));

        var go = new GameObject(kind == LootKind.Common ? "Chest" : "Locked chest (" + kind + ")");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = OrderFor(y);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.5f);
        box.offset = new Vector2(0f, 0.3f);
        var anim = go.AddComponent<SimpleAnimator>();
        go.AddComponent<Chest>().Init(anim, closed, open, kind);
    }

    public static void AddPickup(Transform parent, Item item, float x, float y, int amount = 1)
    {
        var go = new GameObject("Pickup_" + item);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = Vector3.one * 0.7f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Inventory.Icon(item);
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = OrderFor(y);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.7f;
        go.AddComponent<Pickup>().Init(item, amount);
    }

    static void AddPlot(Transform parent, float x, float y)
    {
        var go = new GameObject("CropPlot");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one;

        var soilGo = new GameObject("Soil");
        soilGo.transform.SetParent(go.transform, false);
        var soilSr = soilGo.AddComponent<SpriteRenderer>();
        soilSr.sprite = SoilSprite();
        SpriteMaterial.Apply(soilSr);
        soilSr.sortingOrder = -5; // above the ground tiles, below everything that stands on it

        var cropGo = new GameObject("Crop");
        cropGo.transform.SetParent(go.transform, false);
        cropGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        var cropSr = cropGo.AddComponent<SpriteRenderer>();
        SpriteMaterial.Apply(cropSr);
        cropSr.sortingOrder = OrderFor(y - 0.4f);

        go.AddComponent<CropPlot>().Init(cropSr);
    }

    /// <summary>A little tilled-soil square, drawn in code (no extra art needed).</summary>
    static Sprite SoilSprite()
    {
        if (soil != null) return soil;
        const int n = 16;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
        var baseCol = new Color(0.52f, 0.32f, 0.20f);
        var edge = new Color(0.38f, 0.22f, 0.13f);
        var speck = new Color(0.62f, 0.40f, 0.26f);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                bool border = x == 0 || y == 0 || x == n - 1 || y == n - 1;
                bool row = y % 4 == 2 && x > 1 && x < n - 2;   // furrows
                tex.SetPixel(x, y, border ? edge : (row ? speck : baseCol));
            }
        }
        tex.Apply();
        soil = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), SpriteSheet.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        soil.hideFlags = HideFlags.DontSave;
        return soil;
    }
}
