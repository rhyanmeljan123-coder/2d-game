using System;
using UnityEngine;

public enum Item
{
    Pumpkin, Potato, Carrot, Tomato, Peas,                                  // harvested crops (0-4)
    PumpkinSeeds, PotatoSeeds, CarrotSeeds, TomatoSeeds, PeaSeeds,           // seed packets (5-9)
    Milk, Herb, Flower                                                       // extras
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

    /// <summary>Icon from items.png (5 columns x 3 rows of 16px).</summary>
    public static Sprite Icon(Item item)
    {
        int i = (int)item, row, col;
        if (i < 5) { row = 0; col = i; }
        else if (i < 10) { row = 1; col = i - 5; }
        else { row = 2; col = i - 10; }
        var sheet = SpriteSheet.Slice("Items/items", 16, new Vector2(8, 8));
        return sheet.Length == 0 ? null : sheet[row, col];
    }
}
