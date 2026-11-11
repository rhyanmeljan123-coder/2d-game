#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>Copies the Tiny Wonder UI Free textures into Resources for runtime UI loading.</summary>
[InitializeOnLoad]
public static class TinyWonderUIImporter
{
    const string Source = "Assets/Cute_Fantasy_Free/Tiny Wonder UI Free";
    const string Destination = "Assets/Resources/TinyWonder";
    static readonly string[] Files =
    {
        "base free.png", "buttons free.png", "bars free.png",
        "gui_free.png", "icons free.png", "scrollbars free.png"
    };

    static TinyWonderUIImporter()
    {
        EditorApplication.delayCall += ImportTextures;
    }

    static void ImportTextures()
    {
        if (!Directory.Exists(Source)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Destination))
            AssetDatabase.CreateFolder("Assets/Resources", "TinyWonder");

        bool changed = false;
        foreach (string file in Files)
        {
            string from = Source + "/" + file;
            string to = Destination + "/" + file;
            if (!File.Exists(from)) continue;
            if (!File.Exists(to))
                changed |= AssetDatabase.CopyAsset(from, to);
        }

        if (changed) AssetDatabase.Refresh();
    }
}
#endif
