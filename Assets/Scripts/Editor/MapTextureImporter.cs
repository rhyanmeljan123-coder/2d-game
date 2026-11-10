using UnityEditor;

/// <summary>
/// Makes every PNG in a pixel-art "Resources" folder (Map, Map2, Characters, Items, Objects) import as a crisp 16-PPU pixel-art sprite,
/// so the map tiles line up on a 1x1 unit grid.
/// </summary>
public class MapTextureImporter : AssetPostprocessor
{
    static readonly string[] Folders =
    {
        "/Resources/Map/", "/Resources/Map2/", "/Resources/Characters/", "/Resources/Items/", "/Resources/Objects/", "/Resources/Interior/",
    };

    static bool IsPixelArtFolder(string path)
    {
        foreach (var f in Folders) if (path.Contains(f)) return true;
        return false;
    }

    void OnPreprocessTexture()
    {
        if (!IsPixelArtFolder(assetPath)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
