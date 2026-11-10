using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuts sprites out of a sprite-sheet texture (in Assets/Resources) at runtime,
/// so no manual slicing in the Sprite Editor is needed.
/// </summary>
public static class SpriteSheet
{
    public const float PixelsPerUnit = 16f;
    static readonly Dictionary<string, Sprite[,]> cache = new Dictionary<string, Sprite[,]>();

    /// <summary>
    /// Returns sprites[row, column] with row 0 = TOP row of the image.
    /// pivotPx is measured in pixels from the bottom-left corner of each cell.
    /// </summary>
    public static Sprite[,] Slice(string resourcePath, int cellSize, Vector2 pivotPx)
    {
        string key = resourcePath + "|" + cellSize + "|" + pivotPx;
        if (cache.TryGetValue(key, out var cached) && cached != null && cached[0, 0] != null) return cached;

        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null)
        {
            Debug.LogError("SpriteSheet: could not load Resources/" + resourcePath);
            return new Sprite[0, 0];
        }

        int cols = tex.width / cellSize, rows = tex.height / cellSize;
        var result = new Sprite[rows, cols];
        var pivot = new Vector2(pivotPx.x / cellSize, pivotPx.y / cellSize);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rect(c * cellSize, tex.height - (r + 1) * cellSize, cellSize, cellSize);
                var s = Sprite.Create(tex, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                s.name = $"{tex.name}_{r}_{c}";
                result[r, c] = s;
            }
        }
        cache[key] = result;
        return result;
    }

    /// <summary>Gets frames [startCol, startCol+count) from one row.</summary>
    public static Sprite[] Row(Sprite[,] sheet, int row, int startCol, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++) frames[i] = sheet[row, startCol + i];
        return frames;
    }
}
