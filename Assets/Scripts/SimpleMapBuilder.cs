using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a simple top-down map from the Cute Fantasy tiles at runtime:
/// grass field, a sand path, a pond (with collision), and a few trees, a house and a chest.
/// Sprites are loaded from Assets/Resources/Map.
/// Runs in Edit mode too (ExecuteAlways), so the map shows in the Scene view. The generated
/// objects are marked DontSave, so they are never written into the scene file.
/// If no SimpleMapBuilder exists in the scene, one is created automatically when Play starts.
/// </summary>
[ExecuteAlways]
public class SimpleMapBuilder : MonoBehaviour
{
    [Header("Map size (in tiles, 1 tile = 16px = 1 unit)")]
    public int width = 40;          // first map
    public int height = 24;
    public int springMapWidth = 40; // second map (placed right next to the first one)

    /// <summary>Total world width: both maps side by side.</summary>
    public int TotalWidth => width + springMapWidth;

    [Header("Farmhouse interior (a separate room to the right of the maps)")]
    public int interiorWidth = 14;
    public int interiorHeight = 10;
    /// <summary>Bottom-left tile of the interior room.</summary>
    public Vector2Int InteriorOrigin => new Vector2Int(TotalWidth + 10, 0);

    /// <summary>Bottom-left tile of the dungeon (above the farmhouse interior).</summary>
    public Vector2Int DungeonOrigin => new Vector2Int(TotalWidth + 10, 20);

    const string ResPath = "Map/";

    readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();
    Tilemap groundMap, detailMap, pathMap, waterMap, interiorFloor, interiorWalls;
    GameObject generatedRoot;
    const string RootName = "Generated";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<SimpleMapBuilder>() != null) return;
        new GameObject("SimpleMap").AddComponent<SimpleMapBuilder>();
    }

    void OnEnable()
    {
        Cleanup();
        Build();
    }

    void OnDisable() => Cleanup();

    void Cleanup()
    {
        // Remove anything we generated earlier (also catches leftovers after a script reload)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.name == RootName) SafeDestroy(child);
        }
        generatedRoot = null;
        tileCache.Clear();
    }

    static void SafeDestroy(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    void Build()
    {
        WorldState.Clear();
        generatedRoot = new GameObject(RootName);
        generatedRoot.transform.SetParent(transform, false);

        var gridGo = new GameObject("MapGrid");
        gridGo.transform.SetParent(generatedRoot.transform, false);
        gridGo.AddComponent<Grid>(); // default cell size 1x1

        groundMap = CreateLayer(gridGo.transform, "Ground", -30, false);
        detailMap = CreateLayer(gridGo.transform, "Details", -25, false);
        pathMap = CreateLayer(gridGo.transform, "Path", -20, false);
        waterMap = CreateLayer(gridGo.transform, "Water", -10, true);
        interiorFloor = CreateLayer(gridGo.transform, "InteriorFloor", -40, false);
        interiorWalls = CreateLayer(gridGo.transform, "InteriorWalls", -15, true);

        BuildGround();
        BuildPond(27, 14, 8, 6);     // pond in the upper right
        BuildDecorations();
        BuildSpringMap(width);                          // second map, right beside the first
        BuildInterior();
        BuildDungeon();
        FarmWorld.Build(generatedRoot.transform, width, InteriorOrigin, interiorWidth, interiorHeight, DungeonOrigin); // objects, items, furniture, dungeon
        BuildBoundaries();
        if (!Application.isPlaying) GameSetup.CreatePreviews(generatedRoot.transform); // visible in Scene view

        // Everything generated is rebuilt on load, so keep it out of the saved scene file
        foreach (var t in generatedRoot.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave;

        if (Application.isPlaying) SetupCamera(); // don't touch the camera in Edit mode
    }

    // ---------- layers ----------

    Tilemap CreateLayer(Transform parent, string name, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var map = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        SpriteMaterial.Apply(renderer);

        if (solid)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            go.AddComponent<CompositeCollider2D>();
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        return map;
    }

    void BuildGround()
    {
        var grass = GetTile("grass");
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                groundMap.SetTile(new Vector3Int(x, y, 0), grass);
    }

    void BuildPath(int x0, int y0, int w, int h) => Fill9(pathMap, "path", x0, y0, w, h);

    void BuildPond(int x0, int y0, int w, int h) => Fill9(waterMap, "water", x0, y0, w, h);

    /// <summary>Fills a rectangle using the 9-slice tiles prefix_tl, _t, _tr, _l, _c, _r, _bl, _b, _br.</summary>
    void Fill9(Tilemap map, string prefix, int x0, int y0, int w, int h)
    {
        for (int x = x0; x < x0 + w; x++)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                string col = x == x0 ? "l" : (x == x0 + w - 1 ? "r" : "c");
                string row = y == y0 + h - 1 ? "t" : (y == y0 ? "b" : "c");
                string key = row == "c" ? (col == "c" ? "c" : col) : (col == "c" ? row : row + col);
                map.SetTile(new Vector3Int(x, y, 0), GetTile(prefix + "_" + key));
            }
        }
    }

    // ---------- second map (spring farm) ----------

    void BuildSpringMap(int ox)
    {
        int w = springMapWidth;
        var grass = GetTile("Map2/sp_grass");
        for (int x = 0; x < w; x++)
            for (int y = 0; y < height; y++)
                groundMap.SetTile(new Vector3Int(ox + x, y, 0), grass);

        // Blend the two kinds of grass together so the maps merge into one world (dithered strip across the seam)
        const int Blend = 7;
        var oldGrass = GetTile("grass");
        var seamRng = new System.Random(21);
        for (int x = ox - Blend; x < ox + Blend; x++)
        {
            float chanceSpring = (x - (ox - Blend) + 0.5f) / (2f * Blend); // 0 on the left ... 1 on the right
            for (int y = 0; y < height; y++)
            {
                bool spring = seamRng.NextDouble() < chanceSpring;
                groundMap.SetTile(new Vector3Int(x, y, 0), spring ? grass : oldGrass);
            }
        }

        // Scatter small flowers / grass tufts on the open grass
        var rng = new System.Random(7);
        for (int i = 0; i < 90; i++)
        {
            int x = rng.Next(0, w), y = rng.Next(0, height);
            int variant = rng.Next(0, 4);
            if (InSpringBuildings(x, y)) continue;
            detailMap.SetTile(new Vector3Int(ox + x, y, 0), GetTile("Map2/sp_detail_" + variant));
        }
        for (int i = 0; i < 30; i++) // a few across the seam on the first map's side
        {
            int x = rng.Next(-Blend, 0), y = rng.Next(0, height);
            if (y >= 8 && y <= 12) continue;
            detailMap.SetTile(new Vector3Int(ox + x, y, 0), GetTile("Map2/sp_detail_" + rng.Next(0, 4)));
        }

        Fill9(pathMap, "Map2/sp_path", 3, 9, ox + 34 - 3, 3); // ONE road running across both maps
        Fill9(pathMap, "Map2/sp_path", ox + 12, 13, 14, 8); // farm yard
        Fill9(waterMap, "Map2/sp_pond", ox + 29, 14, 7, 6); // pond with a cliff edge
    }

    // Areas of the spring map that should stay free of grass details (relative tile coordinates)
    static bool InSpringBuildings(int x, int y) =>
        (y >= 8 && y <= 12) ||                 // road
        (x >= 11 && x <= 27 && y >= 12 && y <= 22) || // yard
        (x >= 28 && x <= 37 && y >= 13 && y <= 21) || // pond
        (x >= 3 && x <= 10 && y >= 11 && y <= 19);    // house

    // ---------- farmhouse interior ----------

    void BuildInterior()
    {
        var o = InteriorOrigin;
        int w = interiorWidth, h = interiorHeight;

        // Wooden floor (drawn in code), inside the walls
        var floorA = MakeFloorTile(0);
        var floorB = MakeFloorTile(1);
        for (int x = 1; x < w - 1; x++)
            for (int y = 1; y < h - 2; y++)
                interiorFloor.SetTile(new Vector3Int(o.x + x, o.y + y, 0), (x + y) % 2 == 0 ? floorA : floorB);

        // Brick walls: back wall is 2 tiles tall (top row + shadowed row), sides and front are 1 tile
        for (int x = 0; x < w; x++)
        {
            bool alt = x % 2 == 0;
            interiorWalls.SetTile(new Vector3Int(o.x + x, o.y + h - 1, 0), GetTile(alt ? "Interior/in_wall_a" : "Interior/in_wall_b"));
            interiorWalls.SetTile(new Vector3Int(o.x + x, o.y + h - 2, 0), GetTile(alt ? "Interior/in_wall_c" : "Interior/in_wall_d"));
            interiorWalls.SetTile(new Vector3Int(o.x + x, o.y, 0), GetTile(alt ? "Interior/in_wall_a" : "Interior/in_wall_b"));
        }
        for (int y = 1; y < h - 2; y++)
        {
            var t = GetTile(y % 2 == 0 ? "Interior/in_wall_e" : "Interior/in_wall_a");
            interiorWalls.SetTile(new Vector3Int(o.x, o.y + y, 0), t);
            interiorWalls.SetTile(new Vector3Int(o.x + w - 1, o.y + y, 0), t);
        }
    }

    void BuildDungeon()
    {
        var o = DungeonOrigin;
        var floorA = GetTile("Interior/dun_floor_a");
        var floorB = GetTile("Interior/dun_floor_b");
        for (int x = 0; x < DungeonWorld.Width; x++)
        {
            for (int y = 0; y < DungeonWorld.Height; y++)
            {
                var cell = new Vector3Int(o.x + x, o.y + y, 0);
                if (DungeonWorld.IsFloor(x, y))
                {
                    interiorFloor.SetTile(cell, (x * 3 + y) % 5 == 0 ? floorB : floorA);
                }
                else if (DungeonWorld.IsVisibleWall(x, y))
                {
                    // rock with a floor tile below it gets the shadowed brick
                    bool shadow = DungeonWorld.IsFloor(x, y - 1);
                    string name = shadow ? (x % 2 == 0 ? "dun_wall_c" : "dun_wall_d")
                                         : (x % 2 == 0 ? "dun_wall_a" : (y % 2 == 0 ? "dun_wall_b" : "dun_wall_e"));
                    interiorWalls.SetTile(cell, GetTile("Interior/" + name));
                }
            }
        }
    }

    static Tile MakeFloorTile(int variant)
    {
        const int n = 16;
        var tex = new Texture2D(n, n) { filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
        var baseCol = variant == 0 ? new Color(0.78f, 0.54f, 0.33f) : new Color(0.74f, 0.50f, 0.30f);
        var line = new Color(0.52f, 0.33f, 0.21f);
        var light = new Color(0.85f, 0.62f, 0.40f);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                var c = baseCol;
                if (y % 8 == 0) c = line;            // gap between planks
                else if (y % 8 == 7) c = light;      // highlight on the plank edge
                // plank end joints, staggered between rows of planks
                int joint = (y / 8) % 2 == 0 ? 5 : 12;
                if (x == joint && y % 8 != 0) c = line;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.hideFlags = HideFlags.DontSave;
        var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), SpriteSheet.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.DontSave;
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }

    // ---------- boundaries ----------

    /// <summary>Invisible walls around the map so nobody walks off the edge.</summary>
    void BuildBoundaries()
    {
        var root = new GameObject("Boundaries").transform;
        root.SetParent(generatedRoot.transform, false);
        int w = TotalWidth;
        AddWall(root, new Vector2(-0.5f, height * 0.5f), new Vector2(1f, height + 2f));
        AddWall(root, new Vector2(w + 0.5f, height * 0.5f), new Vector2(1f, height + 2f));
        AddWall(root, new Vector2(w * 0.5f, -0.5f), new Vector2(w + 2f, 1f));
        AddWall(root, new Vector2(w * 0.5f, height + 0.5f), new Vector2(w + 2f, 1f));
    }

    static void AddWall(Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    // ---------- decorations ----------

    void BuildDecorations()
    {
        var root = new GameObject("Decorations").transform;
        root.SetParent(generatedRoot.transform, false);

        // House above the road
        Place(root, "deco_house", 18f, 12.2f, 13, new Vector2(3.4f, 1.6f), new Vector2(0f, 0.9f));

        // Chest next to the house
        Place(root, "deco_chest", 23.5f, 12.4f, 0, Vector2.zero, Vector2.zero);

        // Trees (x, y of the trunk base)
        float[,] trees =
        {
            { 5.5f, 18.5f }, { 9.5f, 20.0f }, { 13.5f, 17.5f }, { 2.5f, 14.5f },
            { 8.5f, 14.0f }, { 36.5f, 21.0f }, { 37.5f, 12.8f },
            { 6.5f, 5.0f }, { 14.5f, 3.0f }, { 24.5f, 5.5f }, { 33.5f, 4.0f },
            { 19.5f, 1.8f },
        };
        for (int i = 0; i < trees.GetLength(0); i++)
            Place(root, "deco_tree", trees[i, 0], trees[i, 1], 8, new Vector2(0.9f, 0.5f), new Vector2(0f, 0.25f));
    }

    /// <summary>
    /// Places a sprite so its visual base sits at (x, y). basePx = transparent pixels under the art.
    /// colliderSize/offset (in units, relative to the base) add a small blocking box if size > 0.
    /// </summary>
    void Place(Transform parent, string spriteName, float x, float y, int basePx, Vector2 colliderSize, Vector2 colliderOffset)
    {
        var sprite = Resources.Load<Sprite>(ResPath + spriteName);
        if (sprite == null) { Debug.LogError($"SimpleMapBuilder: missing sprite Resources/{ResPath}{spriteName}"); return; }

        var go = new GameObject(spriteName);
        go.transform.SetParent(parent, false);
        float halfH = sprite.bounds.extents.y;
        go.transform.position = new Vector3(x, y - basePx / 16f + halfH, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(y * 10f); // lower on screen = drawn in front

        if (colliderSize.x > 0f)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            // offset is relative to the visual base; convert to sprite-center space
            box.offset = new Vector2(colliderOffset.x, colliderOffset.y - halfH + basePx / 16f);
        }
    }

    // ---------- helpers ----------

    Tile GetTile(string spriteName)
    {
        if (tileCache.TryGetValue(spriteName, out var cached)) return cached;

        string path = spriteName.Contains("/") ? spriteName : ResPath + spriteName;
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null) Debug.LogError($"SimpleMapBuilder: missing sprite Resources/{path}");

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.hideFlags = HideFlags.DontSave;
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        tileCache[spriteName] = tile;
        return tile;
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        // Fit the whole map on screen
        cam.orthographicSize = Mathf.Max(height * 0.5f, TotalWidth * 0.5f / cam.aspect);
        cam.transform.position = new Vector3(TotalWidth * 0.5f, height * 0.5f, -10f);
    }
}
