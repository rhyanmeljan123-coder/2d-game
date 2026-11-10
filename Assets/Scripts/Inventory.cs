using System;
using UnityEngine;

public enum Item
{
    Pumpkin, Potato, Carrot, Tomato, Peas,                                  // harvested crops (0-4)
    PumpkinSeeds, PotatoSeeds, CarrotSeeds, TomatoSeeds, PeaSeeds,           // seed packets (5-9)
    Milk, Herb, Flower,                                                      // found items
    SlimeGel, Bone,                                                          // dungeon drops
    Salad, VegStew, PumpkinSoup, BoneBroth, HealingPotion, HeartyPie,        // crafted food & potions
    Coin, SilverKey, GoldKey,                                                // money and dungeon keys
    RedFlask, BigRedFlask                                                    // healing flasks
}

/// <summary>The player's items. Static, so anything can add to it. Reset every time Play starts.</summary>
public static class Inventory
{
    public const int CropCount = 5;
    static readonly int[] counts = new int[Enum.GetValues(typeof(Item)).Length];

    static readonly string[] names =
    {
        "Pumpkin", "Potato", "Carrot", "Tomato", "Peas",
        "Pumpkin seeds", "Potato seeds", "Carrot seeds", "Tomato seeds", "Pea seeds",
        "Milk", "Herb", "Flower",
        "Slime gel", "Bone",
        "Salad", "Vegetable stew", "Pumpkin soup", "Bone broth", "Healing potion", "Hearty pie",
        "Coins", "Silver key", "Gold key",
        "Red flask", "Big red flask",
    };

    // How much health each item restores when eaten (0 = can't be eaten)
    static readonly int[] heal =
    {
        12, 6, 8, 10, 5,          // raw crops
        0, 0, 0, 0, 0,            // seeds
        10, 15, 0,                // milk, herb, flower
        0, 0,                     // slime gel, bone
        30, 40, 50, 45, 70, 100,  // crafted
        0, 0, 0,                  // coins, keys
        25, 60,                   // red flasks
    };

    // What the market stall pays for one of each item (0 = can't be sold)
    static readonly int[] sellPrice =
    {
        14, 8, 9, 10, 7,
        0, 0, 0, 0, 0,
        6, 8, 5,
        12, 10,
        40, 50, 60, 55, 80, 110,
        0, 0, 0,
        30, 70,
    };

    // What the market stall charges (0 = not for sale)
    static readonly int[] buyPrice =
    {
        0, 0, 0, 0, 0,
        8, 5, 5, 6, 5,
        10, 0, 0,
        0, 0,
        0, 0, 0, 0, 0, 0,
        0, 0, 0,
        45, 0,
    };

    /// <summary>Which crop (0-4) is picked for planting. Change with keys 1-5.</summary>
    public static int SelectedCrop;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Array.Clear(counts, 0, counts.Length);
        for (int i = 0; i < CropCount; i++) counts[(int)SeedFor(i)] = 3; // start with 3 of each seed
        SelectedCrop = 0;
        Changed = null;
    }

    public static Item SeedFor(int crop) => (Item)(CropCount + crop);
    public static Item CropItem(int crop) => (Item)crop;
    public static string Name(Item item) => names[(int)item];
    public static string CropName(int crop) => names[crop];
    public static int HealAmount(Item item) => heal[(int)item];
    public static int SellPrice(Item item) => sellPrice[(int)item];
    public static int BuyPrice(Item item) => buyPrice[(int)item];

    public static int Get(Item item) => counts[(int)item];

    public static void Add(Item item, int amount = 1)
    {
        counts[(int)item] += amount;
        Changed?.Invoke();
    }

    public static bool TryRemove(Item item, int amount = 1)
    {
        if (counts[(int)item] < amount) return false;
        counts[(int)item] -= amount;
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Eats the most suitable food: the smallest one that heals all missing health,
    /// or the strongest one if nothing is big enough.
    /// </summary>
    public static bool EatBest(Health hp)
    {
        if (hp == null || hp.Dead) return false;
        int missing = hp.max - hp.current;
        if (missing <= 0) { GameHud.Toast("You're already at full health."); return false; }

        bool found = false;
        Item best = default;
        int bestHeal = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            int h = heal[i];
            if (h <= 0 || counts[i] <= 0) continue;
            bool covers = h >= missing, bestCovers = bestHeal >= missing;
            if (!found || (covers && !bestCovers) || (covers && bestCovers && h < bestHeal) || (!covers && !bestCovers && h > bestHeal))
            {
                found = true; best = (Item)i; bestHeal = h;
            }
        }
        if (!found) { GameHud.Toast("Nothing to eat! Harvest crops or craft food."); return false; }

        TryRemove(best);
        hp.Heal(bestHeal);
        GameHud.Toast($"Ate {Name(best).ToLower()}  +{bestHeal} HP");
        return true;
    }

    /// <summary>Icon from items.png (5x3 of 16px), crafted.png (4x2 of 16px) or the Traps strips.</summary>
    public static Sprite Icon(Item item)
    {
        int i = (int)item;
        if (i < (int)Item.SlimeGel)
        {
            int row, col;
            if (i < 5) { row = 0; col = i; }
            else if (i < 10) { row = 1; col = i - 5; }
            else { row = 2; col = i - 10; }
            var sheet = SpriteSheet.Slice("Items/items", 16, new Vector2(8, 8));
            return sheet.Length == 0 ? null : sheet[row, col];
        }
        if (i < (int)Item.Coin)
        {
            int j = i - (int)Item.SlimeGel;
            var crafted = SpriteSheet.Slice("Items/crafted", 16, new Vector2(8, 8));
            return crafted.Length == 0 ? null : crafted[j / 4, j % 4];
        }
        string strip = item == Item.Coin ? "coin" : item == Item.SilverKey ? "key_silver" : item == Item.GoldKey ? "key_gold"
                     : item == Item.RedFlask ? "flask_1" : "flask_4";
        return SpriteSheet.Cut("Traps/" + strip, 0, 0, 16, 16, new Vector2(8, 8));
    }
}
