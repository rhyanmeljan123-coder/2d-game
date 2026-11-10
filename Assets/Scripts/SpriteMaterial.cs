using UnityEngine;

/// <summary>
/// An unlit sprite material, so the map, player and animals are fully visible
/// without needing a Global Light 2D in the scene.
/// </summary>
public static class SpriteMaterial
{
    static Material unlit;

    public static Material Unlit
    {
        get
        {
            if (unlit == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default"); // fallback
                unlit = new Material(shader) { name = "SpriteUnlit (runtime)", hideFlags = HideFlags.DontSave };
            }
            return unlit;
        }
    }

    public static void Apply(Renderer renderer)
    {
        if (renderer != null) renderer.sharedMaterial = Unlit;
    }
}
