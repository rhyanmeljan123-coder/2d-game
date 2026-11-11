using System;
using UnityEngine;

public enum ItemCategory { Seed, Crop, Material, Food, Potion, Weapon, Armor, Tool, Key, Currency }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
public enum EquipSlot { None, Weapon, Armor, Tool, Charm }

/// <summary>Everything the game knows about one kind of item. Loaded from Resources/Data/items.json.</summary>
public class ItemDef
{
    public Item id;
    public string name = "?", description = "";
    public ItemCategory category;
    public Rarity rarity;
    public int stack = 99, sell, buy, heal, attack, defense;
    public EquipSlot equip;
    public string iconSheet;
    public string iconResource;
    public int iconCell = 16, iconRow, iconCol;

    public bool IsEdible => heal > 0;
    public bool IsSeed => category == ItemCategory.Seed;
    public bool IsEquipment => equip != EquipSlot.None;

    Sprite icon;
    public Sprite Icon
    {
        get
        {
            if (icon == null && !string.IsNullOrEmpty(iconResource))
            {
                var sprites = Resources.LoadAll<Sprite>(iconResource);
                if (sprites.Length > 0) icon = sprites[0];
            }
            if (icon == null && !string.IsNullOrEmpty(iconSheet))
            {
                var sheet = SpriteSheet.Slice(iconSheet, iconCell, new Vector2(iconCell * 0.5f, iconCell * 0.5f));
                if (sheet.Length > 0 && iconRow < sheet.GetLength(0) && iconCol < sheet.GetLength(1)) icon = sheet[iconRow, iconCol];
            }
            // Reuse the farm pack's own art for gathered materials.
            if (icon == null && id == Item.Wood)
                icon = SpriteSheet.Cut("Objects/farm_objects", 105, 64, 25, 12, new Vector2(12.5f, 6f));
            if (icon == null && id == Item.Stone)
                icon = SpriteSheet.Cut("Objects/farm_objects", 32, 80, 16, 16, new Vector2(8f, 8f));
            return icon;
        }
    }
}

[Serializable] class ItemJson
{
    public string id, name, category, rarity, equip, description, iconSheet, iconResource;
    public int stack, sell, buy, heal, attack, defense, iconCell, iconRow, iconCol;
}
[Serializable] class ItemListJson { public ItemJson[] items; }

/// <summary>
/// Data-driven item table. To add an item: add a value to the Item enum (at the END), then add an entry with the same
/// id to Assets/Resources/Data/items.json. Everything else (names, prices, rarity, stats, icons) comes from the file.
/// </summary>
public static class ItemDatabase
{
    static ItemDef[] defs;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => defs = null;

    public static ItemDef Get(Item item)
    {
        if (defs == null) Load();
        return defs[(int)item];
    }

    static void Load()
    {
        int count = Enum.GetValues(typeof(Item)).Length;
        defs = new ItemDef[count];
        for (int i = 0; i < count; i++) defs[i] = new ItemDef { id = (Item)i, name = ((Item)i).ToString() }; // safe defaults

        var asset = Resources.Load<TextAsset>("Data/items");
        if (asset == null) { Debug.LogError("ItemDatabase: Resources/Data/items.json is missing."); return; }

        ItemListJson list;
        try { list = JsonUtility.FromJson<ItemListJson>(asset.text); }
        catch (Exception e) { Debug.LogError("ItemDatabase: could not read items.json: " + e.Message); return; }

        foreach (var j in list.items)
        {
            if (!Enum.TryParse(j.id, out Item id)) { Debug.LogWarning($"ItemDatabase: items.json has unknown id '{j.id}' (add it to the Item enum)."); continue; }
            var d = defs[(int)id];
            d.name = string.IsNullOrEmpty(j.name) ? j.id : j.name;
            d.description = j.description ?? "";
            Enum.TryParse(j.category, out d.category);
            Enum.TryParse(j.rarity, out d.rarity);
            Enum.TryParse(j.equip, out d.equip);
            d.stack = j.stack > 0 ? j.stack : 99;
            d.sell = j.sell; d.buy = j.buy; d.heal = j.heal; d.attack = j.attack; d.defense = j.defense;
            d.iconSheet = j.iconSheet;
            d.iconResource = j.iconResource;
            d.iconCell = j.iconCell > 0 ? j.iconCell : 16;
            d.iconRow = j.iconRow; d.iconCol = j.iconCol;
        }
        for (int i = 0; i < count; i++)
            if (string.IsNullOrEmpty(defs[i].iconSheet) && string.IsNullOrEmpty(defs[i].iconResource) &&
                defs[i].id != Item.Wood && defs[i].id != Item.Stone)
                Debug.LogWarning($"ItemDatabase: no entry for item {(Item)i} in items.json.");
    }

    public static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Uncommon: return new Color(0.45f, 0.9f, 0.45f);
            case Rarity.Rare: return new Color(0.4f, 0.65f, 1f);
            case Rarity.Epic: return new Color(0.75f, 0.45f, 1f);
            case Rarity.Legendary: return new Color(1f, 0.72f, 0.2f);
            default: return new Color(0.85f, 0.85f, 0.85f);
        }
    }
}
