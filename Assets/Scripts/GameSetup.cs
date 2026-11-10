using UnityEngine;

/// <summary>
/// Spawns the player, the farm animals and the following camera when Play starts.
/// Nothing needs to be placed in the scene. If you drop your own PlayerController into the scene,
/// that one is used instead of spawning a new player.
/// </summary>
public static class GameSetup
{
    public static readonly Vector2 PlayerStart = new Vector2(20.5f, 7.5f);

    public struct AnimalGroup
    {
        public string sheet; public float pivotYPx; public Vector2 colliderSize; public Vector2[] positions;
    }

    // sheet, pivot (px above cell bottom), collider size, spawn positions
    public static readonly AnimalGroup[] Groups =
    {
        new AnimalGroup { sheet = "chicken", pivotYPx = 9f, colliderSize = new Vector2(0.5f, 0.35f), positions = new[] { new Vector2(13f, 7.5f), new Vector2(15f, 6.5f), new Vector2(14.5f, 8f) } },
        new AnimalGroup { sheet = "cow",     pivotYPx = 7f, colliderSize = new Vector2(1.1f, 0.5f),  positions = new[] { new Vector2(28f, 6.5f), new Vector2(31f, 8f) } },
        new AnimalGroup { sheet = "pig",     pivotYPx = 9f, colliderSize = new Vector2(0.9f, 0.4f),  positions = new[] { new Vector2(8f, 7f), new Vector2(10.5f, 5f) } },
        new AnimalGroup { sheet = "sheep",   pivotYPx = 9f, colliderSize = new Vector2(0.9f, 0.4f),  positions = new[] { new Vector2(24f, 17f), new Vector2(21f, 16f) } },
    };

    /// <summary>
    /// Edit-mode only: places plain sprites where the player and animals will spawn,
    /// so they are visible in the Scene view. (The real, moving ones are spawned on Play.)
    /// </summary>
    public static void CreatePreviews(Transform parent)
    {
        var root = new GameObject("Characters (preview)").transform;
        root.SetParent(parent, false);

        var playerSheet = SpriteSheet.Slice("Characters/player", 32, new Vector2(16, 9));
        if (playerSheet.Length > 0) CreatePreview(root, "Player (preview)", playerSheet[0, 0], PlayerStart);

        foreach (var g in Groups)
        {
            var sheet = SpriteSheet.Slice("Characters/" + g.sheet, 32, new Vector2(16, g.pivotYPx));
            if (sheet.Length == 0) continue;
            for (int i = 0; i < g.positions.Length; i++)
                CreatePreview(root, g.sheet + "_" + (i + 1) + " (preview)", sheet[0, 0], g.positions[i]);
        }
    }

    static void CreatePreview(Transform parent, string name, Sprite sprite, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        SpriteMaterial.Apply(sr);
        sr.sortingOrder = 1000 - Mathf.RoundToInt(position.y * 10f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        var map = Object.FindFirstObjectByType<SimpleMapBuilder>();
        var mapSize = map != null ? new Vector2(map.width, map.height) : new Vector2(40f, 24f);

        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null) player = CreatePlayer(PlayerStart);

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.mapSize = mapSize;
        }

        if (Object.FindFirstObjectByType<AnimalWander>() != null) return; // animals already in the scene

        var animals = new GameObject("Animals").transform;
        foreach (var g in Groups) SpawnGroup(animals, g.sheet, g.pivotYPx, g.colliderSize, g.positions);
    }

    static PlayerController CreatePlayer(Vector2 position)
    {
        var go = new GameObject("Player");
        go.transform.position = position;
        go.tag = "Player";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        var box = go.AddComponent<BoxCollider2D>(); // small box at the feet
        box.size = new Vector2(0.6f, 0.35f);
        box.offset = new Vector2(0f, 0.15f);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<SimpleAnimator>();
        return go.AddComponent<PlayerController>();
    }

    static void SpawnGroup(Transform parent, string sheet, float pivotYPx, Vector2 colliderSize, Vector2[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            var go = new GameObject(sheet + "_" + (i + 1));
            go.transform.SetParent(parent, false);
            go.transform.position = positions[i];
            go.AddComponent<Rigidbody2D>();
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = new Vector2(0f, colliderSize.y * 0.5f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<SimpleAnimator>();
            go.AddComponent<AnimalWander>().Init("Characters/" + sheet, pivotYPx);
        }
    }
}
