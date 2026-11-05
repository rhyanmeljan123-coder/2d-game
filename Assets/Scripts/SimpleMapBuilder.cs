using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a simple 2D platformer map (ground, walls, floating platforms) at runtime.
/// If no SimpleMapBuilder exists in the scene, one is created automatically when Play starts.
/// To make it a permanent part of a scene: Tools > Simple Map > Add Map Builder To Scene.
/// </summary>
public class SimpleMapBuilder : MonoBehaviour
{
    [Header("Map size (in tiles)")]
    public int width = 48;
    public int height = 20;
    public int groundHeight = 3; // rows of ground (top row is grass)

    [Header("Colors")]
    public Color skyColor = new Color(0.53f, 0.81f, 0.92f);
    public Color grassColor = new Color(0.30f, 0.69f, 0.31f);
    public Color dirtColor = new Color(0.55f, 0.38f, 0.25f);
    public Color stoneColor = new Color(0.45f, 0.47f, 0.50f);

    // Floating platforms: x, y, length
    static readonly int[][] Platforms =
    {
        new[] { 8, 6, 5 },
        new[] { 16, 9, 4 },
        new[] { 24, 6, 5 },
        new[] { 31, 11, 4 },
        new[] { 38, 7, 5 },
    };

    Tilemap tilemap;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<SimpleMapBuilder>() != null) return;
        new GameObject("SimpleMap").AddComponent<SimpleMapBuilder>();
    }

    void Awake()
    {
        Build();
        SetupCamera();
    }

    void Build()
    {
        // Grid + Tilemap with a merged static collider
        var gridGo = new GameObject("MapGrid");
        gridGo.transform.SetParent(transform, false);
        gridGo.AddComponent<Grid>();

        var mapGo = new GameObject("Ground");
        mapGo.transform.SetParent(gridGo.transform, false);
        tilemap = mapGo.AddComponent<Tilemap>();
        mapGo.AddComponent<TilemapRenderer>();

        var rb = mapGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        mapGo.AddComponent<CompositeCollider2D>();
        var tc = mapGo.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;

        Tile grass = MakeTile(grassColor);
        Tile dirt = MakeTile(dirtColor);
        Tile stone = MakeTile(stoneColor);

        // Ground
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < groundHeight; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), y == groundHeight - 1 ? grass : dirt);
        }

        // Side walls and ceiling (keeps the player inside the map)
        for (int y = 0; y < height; y++)
        {
            tilemap.SetTile(new Vector3Int(0, y, 0), stone);
            tilemap.SetTile(new Vector3Int(width - 1, y, 0), stone);
        }
        for (int x = 0; x < width; x++)
            tilemap.SetTile(new Vector3Int(x, height - 1, 0), stone);

        // Floating platforms
        foreach (var p in Platforms)
            for (int i = 0; i < p[2]; i++)
                tilemap.SetTile(new Vector3Int(p[0] + i, p[1], 0), grass);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.backgroundColor = skyColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.orthographicSize = height * 0.5f;
        cam.transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
    }

    static Tile MakeTile(Color color)
    {
        const int size = 16;
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var darker = Color.Lerp(color, Color.black, 0.2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, (x == 0 || y == 0 || x == size - 1 || y == size - 1) ? darker : color);
        tex.Apply();

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        tile.colliderType = Tile.ColliderType.Grid;
        return tile;
    }
}
