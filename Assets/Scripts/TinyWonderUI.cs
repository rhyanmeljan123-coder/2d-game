using UnityEngine;

/// <summary>
/// Small IMGUI theme adapter for the Tiny Wonder UI Free spritesheets.
/// Textures are copied into Resources by the editor importer so the runtime
/// HUD can load them in builds without depending on AssetDatabase.
/// </summary>
public static class TinyWonderUI
{
    static Texture2D baseSheet, buttonSheet, barsSheet, guiSheet, iconSheet, scrollbarSheet;

    static void Load()
    {
        if (baseSheet != null) return;
        baseSheet = Resources.Load<Texture2D>("TinyWonder/base free");
        buttonSheet = Resources.Load<Texture2D>("TinyWonder/buttons free");
        barsSheet = Resources.Load<Texture2D>("TinyWonder/bars free");
        guiSheet = Resources.Load<Texture2D>("TinyWonder/gui_free");
        iconSheet = Resources.Load<Texture2D>("TinyWonder/icons free");
        scrollbarSheet = Resources.Load<Texture2D>("TinyWonder/scrollbars free");
    }

    static void Fill(Rect rect, Color color)
    {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // The free pack's base sheet is a 3-by-3 set of slot states.
    public static void DrawSlot(Rect rect, bool selected)
    {
        Load();
        if (baseSheet != null)
        {
            var old = GUI.color;
            GUI.color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f, 1f);
            GUI.DrawTextureWithTexCoords(rect, baseSheet, new Rect(0f, 0f, 1f / 3f, 1f / 3f));
            GUI.color = old;
        }
        else
        {
            Fill(rect, new Color(0.24f, 0.16f, 0.13f, 1f));
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.78f, 0.48f, 0.31f));
        }

        if (selected)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), new Color(1f, 0.83f, 0.38f));
            Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), new Color(1f, 0.83f, 0.38f));
            Fill(new Rect(rect.x, rect.y, 2f, rect.height), new Color(1f, 0.83f, 0.38f));
            Fill(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), new Color(1f, 0.83f, 0.38f));
        }
    }

    public static void DrawPanel(Rect rect)
    {
        Fill(rect, new Color(0.19f, 0.12f, 0.10f, 0.97f));
        Fill(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.77f, 0.47f, 0.31f));
        Fill(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(0.77f, 0.47f, 0.31f));
        Fill(new Rect(rect.x, rect.y, 3f, rect.height), new Color(0.77f, 0.47f, 0.31f));
        Fill(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), new Color(0.77f, 0.47f, 0.31f));
        Fill(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 1f), new Color(1f, 0.82f, 0.62f, 0.35f));
    }

    public static bool DrawButton(Rect rect, string label, GUIStyle style = null)
    {
        Load();
        bool hover = rect.Contains(Event.current.mousePosition);
        if (buttonSheet != null)
        {
            var old = GUI.color;
            GUI.color = hover ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f);
            GUI.DrawTextureWithTexCoords(rect, buttonSheet, new Rect(0f, 0f, 1f / 3f, 1f / 3f));
            GUI.color = old;
        }
        else
        {
            Fill(rect, hover ? new Color(0.69f, 0.37f, 0.25f) : new Color(0.52f, 0.27f, 0.19f));
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 };
            style.normal.textColor = new Color(0.25f, 0.13f, 0.09f);
        }
        GUI.Label(rect, label, style);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    public static void DrawHealthBar(Rect rect, float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        Fill(rect, new Color(0.20f, 0.10f, 0.10f, 1f));
        Fill(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fraction, rect.height - 4f),
            Color.Lerp(new Color(0.75f, 0.20f, 0.20f), new Color(0.35f, 0.72f, 0.34f), fraction));
        Load();
        if (barsSheet != null)
        {
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, rect.width, 2f), barsSheet,
                new Rect(0f, 0f, 1f, 0.25f));
            GUI.color = old;
        }
    }
}
