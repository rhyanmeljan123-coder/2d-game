using UnityEngine;

/// <summary>
/// Spawns the player, the farm animals and the following camera when Play starts.
/// Nothing needs to be placed in the scene. If you drop your own PlayerController into the scene,
/// that one is used instead of spawning a new player.
/// </summary>
public static class GameSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        var map = Object.FindFirstObjectByType<SimpleMapBuilder>();
        var mapSize = map != null ? new Vector2(map.width, map.height) : new Vector2(40f, 24f);

        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null) player = CreatePlayer(new Vector2(20.5f, 7.5f));

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
        // sheet, pivot (px above cell bottom), collider size, positions
        SpawnGroup(animals, "chicken", 9f, new Vector2(0.5f, 0.35f), new[] { new Vector2(13f, 7.5f), new Vector2(15f, 6.5f), new Vector2(14.5f, 8f) });
        SpawnGroup(animals, "cow",     7f, new Vector2(1.1f, 0.5f),  new[] { new Vector2(28f, 6.5f), new Vector2(31f, 8f) });
        SpawnGroup(animals, "pig",     9f, new Vector2(0.9f, 0.4f),  new[] { new Vector2(8f, 7f), new Vector2(10.5f, 5f) });
        SpawnGroup(animals, "sheep",   9f, new Vector2(0.9f, 0.4f),  new[] { new Vector2(24f, 17f), new Vector2(21f, 16f) });
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
