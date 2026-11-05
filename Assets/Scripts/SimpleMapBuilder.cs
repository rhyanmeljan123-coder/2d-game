using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a simple top-down map from the Cute Fantasy tiles at runtime:
/// grass field, a sand path, a pond (with collision), and a few trees, a house and a chest.
/// Sprites are loaded from Assets/Resources/Map. If no SimpleMapBuilder exists in the scene,
/// one is created automatically when Play starts.
/// Optional: Tools > Simple Map > Add Map Builder To Scene.
/// </summary>
public class SimpleMapBuilder : MonoBehaviour
{
    [Header("Map size (in tiles, 1 tile = 16px = 1 unit)")]
    public int width = 40;
    public int height = 24;

    const string ResPath = "Map/";

    readonly Dictionary<string, Tile> tileCache = new Dictionary<string, Tile>();
    Tilemap groundMap, pathMap, waterMap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<SimpleMapBuilder>() != null) return;
        new GameObject("SimpleMap").AddComponent<SimpleMapBuilder>();
    }

    void Awake()
    {
        var gridGo = new GameObject("MapGrid");
        gridGo.transform.SetParent(transform, false);
        gridGo.AddComponent<Grid>(); // default cell size 1x1

        groundMap = CreateLayer(gridGo.transform, "Ground", -30, false);
        pathMap = CreateLayer(gridGo.transform, "Path", -20, false);
        waterMap = CreateLayer(gridGo.transform, "Water", -10, true);

        BuildGround();
        BuildPath(3, 9, 34, 3);      // x, y, w, h  (sand road across the map)
        BuildPond(27, 14, 8, 6);     // pond in the upper right
        BuildDecorations();
        SetupCamera();
    }

    // ---------- layers ----------

    Tilemap CreateLayer(Transform parent, string name, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var map = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = order;

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

    // ---------- decorations ----------

    void BuildDecorations()
    {
        var root = new GameObject("Decorations").transform;
        root.SetParent(transform, false);

        // House above the road
        Place(root, "deco_house", 18f, 12.2f, 13, new Vector2(3.4f, 1.6f), new Vector2(0f, 0.9f));

        // Chest next to the house
        Place(root, "deco_chest", 23.5f, 12.4f, 0, Vector2.zero, Vector2.zero);

        // Trees (x, y of the trunk base)
        float[,] trees =
        {
            { 5.5f, 18.5f }, { 9.5f, 20.0f }, { 13.5f, 17.5f }, { 2.5f, 14.5f },
            { 8.5f, 14.0f }, { 22.5f, 20.0f }, { 36.5f, 21.0f }, { 37.5f, 12.8f },
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
        sr.sortingOrder = 100 - Mathf.RoundToInt(y * 10f); // lower on screen = drawn in front

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

        var sprite = Resources.Load<Sprite>(ResPath + spriteName);
        if (sprite == null) Debug.LogError($"SimpleMapBuilder: missing sprite Resources/{ResPath}{spriteName}");

        var tile = ScriptableObject.CreateInstance<Tile>();
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
        cam.orthographicSize = Mathf.Max(height * 0.5f, width * 0.5f / cam.aspect);
        cam.transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
    }
}
