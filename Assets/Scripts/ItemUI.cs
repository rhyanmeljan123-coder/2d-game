using System.Text;
using UnityEngine;

/// <summary>Shared IMGUI drawing for item slots and tooltips (hotbar, inventory screen).</summary>
public static class ItemUI
{
    static GUIStyle countStyle, keyStyle, tipTitle, tipBody;

    static void EnsureStyles()
    {
        if (countStyle != null) return;
        countStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
        countStyle.normal.textColor = Color.white;
        keyStyle = new GUIStyle(countStyle) { alignment = TextAnchor.UpperLeft, fontSize = 10 };
        tipTitle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
        tipBody = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
        tipBody.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
    }

    public static void Icon(Rect r, Item item, float alpha = 1f)
    {
        var icon = Inventory.Icon(item);
        if (icon == null) return;
        var tex = icon.texture; var tr = icon.rect;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTextureWithTexCoords(r, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
        GUI.color = Color.white;
    }

    /// <summary>One inventory slot: frame (rarity-coloured when filled), icon, count, optional key label.</summary>
    public static void Slot(Rect r, ItemStack stack, bool selected, string key)
    {
        EnsureStyles();
        TinyWonderUI.DrawSlot(r, selected);

        if (!stack.IsEmpty)
        {
            Icon(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), stack.item);
            if (stack.count > 1) GUI.Label(new Rect(r.x, r.y, r.width - 3f, r.height - 1f), stack.count.ToString(), countStyle);
        }
        if (key != null) GUI.Label(new Rect(r.x + 3f, r.y + 1f, 16f, 14f), key, keyStyle);
    }

    /// <summary>A floating tooltip box next to the mouse.</summary>
    public static void Tooltip(Vector2 mouse, Item item, float screenW, float screenH)
    {
        EnsureStyles();
        var d = Inventory.Def(item);
        var color = ItemDatabase.RarityColor(d.rarity);

        var sb = new StringBuilder();
        sb.Append(d.rarity).Append(' ').Append(d.category).AppendLine();
        if (!string.IsNullOrEmpty(d.description)) sb.AppendLine().Append(d.description).AppendLine();
        sb.AppendLine();
        if (d.heal > 0) sb.Append("Heals ").Append(d.heal).AppendLine(" HP");
        if (d.attack > 0) sb.Append("+").Append(d.attack).AppendLine(" attack");
        if (d.defense > 0) sb.Append("+").Append(d.defense).AppendLine(" defense");
        if (d.IsEquipment) sb.Append("Equip: ").AppendLine(d.equip.ToString());
        if (d.sell > 0) sb.Append("Sells for ").Append(d.sell).AppendLine(" coins");
        string body = sb.ToString().TrimEnd();

        float w = 230f;
        float h = 26f + tipBody.CalcHeight(new GUIContent(body), w - 16f) + 8f;
        var rect = new Rect(mouse.x + 16f, mouse.y + 16f, w, h);
        if (rect.xMax > screenW) rect.x = mouse.x - w - 12f;
        if (rect.yMax > screenH) rect.y = screenH - h - 4f;

        GUI.color = new Color(0f, 0f, 0f, 0.92f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        tipTitle.normal.textColor = color;
        GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, w - 16f, 22f), d.name, tipTitle);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 26f, w - 16f, h - 26f), body, tipBody);
    }
}
