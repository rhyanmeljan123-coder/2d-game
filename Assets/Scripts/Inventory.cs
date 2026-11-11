using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Item ids. Add new items at the END, then add a matching entry in Resources/Data/items.json.</summary>
public enum Item
{
    Pumpkin, Potato, Carrot, Tomato, Peas,                                  // harvested crops (0-4)
    PumpkinSeeds, PotatoSeeds, CarrotSeeds, TomatoSeeds, PeaSeeds,           // seed packets (5-9)
    Milk, Herb, Flower,                                                      // found items
    SlimeGel, Bone,                                                          // dungeon drops
    Salad, VegStew, PumpkinSoup, BoneBroth, HealingPotion, HeartyPie,        // crafted food & potions
    Coin, SilverKey, GoldKey,                                                // money and dungeon keys
    RedFlask, BigRedFlask,                                                   // healing flasks
    WoodenSword, IronSword, LeatherTunic, KnightArmor, WateringCan,             // equipment + farm tool
    Wood, Stone,                                                             // gathered materials
    Egg, Wool, Truffle                                                        // animal products
}

public struct ItemStack
{
    public Item item;
    public int count;
    public bool IsEmpty => count <= 0;
    public ItemStack(Item i, int c) { item = i; count = c; }
}

/// <summary>
/// The player's items: 32 slots (the first row of 8 is the hotbar) plus equipment slots.
/// Item data (names, prices, rarity...) lives in ItemDatabase; this class only holds stacks and does the bookkeeping.
/// Everything else in the game keeps using Get / Add / TryRemove / Name / Icon, so nothing else had to change.
/// </summary>
public static class Inventory
{
    public const int CropCount = 5;
    public const int Capacity = 32, HotbarSize = 8, Columns = 8;

    static readonly ItemStack[] slots = new ItemStack[Capacity];
    static readonly ItemStack[] equipment = new ItemStack[5]; // indexed by (int)EquipSlot; index 0 unused
    static int lastSeedCrop;

    public static int SelectedHotbar { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Array.Clear(slots, 0, slots.Length);
        Array.Clear(equipment, 0, equipment.Length);
        for (int i = 0; i < CropCount; i++) slots[i] = new ItemStack(SeedFor(i), 3); // start with 3 of each seed on the hotbar
        slots[5] = new ItemStack(Item.WateringCan, 1); // starter watering can
        SelectedHotbar = 0;
        lastSeedCrop = 0;
        Changed = null;
    }

    // ------------------------------------------------------------ item data shortcuts

    public static ItemDef Def(Item item) => ItemDatabase.Get(item);
    public static Item SeedFor(int crop) => (Item)(CropCount + crop);
    public static Item CropItem(int crop) => (Item)crop;
    public static string Name(Item item) => Def(item).name;
    public static string CropName(int crop) => Def(CropItem(crop)).name;
    public static int HealAmount(Item item) => Def(item).heal;
    public static int SellPrice(Item item) => Def(item).sell;
    public static int BuyPrice(Item item) => Def(item).buy;
    public static Sprite Icon(Item item) => Def(item).Icon;

    // ------------------------------------------------------------ slots

    public static ItemStack GetSlot(int index) => slots[index];
    public static ItemStack GetEquipped(EquipSlot slot) => equipment[(int)slot];

    public static void EnsureWateringCan()
    {
        // Keep the watering can easy to select. If an older save has it in the
        // backpack, move it to an empty hotbar slot without overwriting anything.
        for (int i = 0; i < Capacity; i++)
        {
            if (slots[i].IsEmpty || slots[i].item != Item.WateringCan) continue;
            if (i < HotbarSize) return;

            for (int hotbar = 0; hotbar < HotbarSize; hotbar++)
            {
                if (!slots[hotbar].IsEmpty) continue;
                slots[hotbar] = slots[i];
                slots[i] = default;
                Changed?.Invoke();
                return;
            }
            return; // Hotbar is full; leave the existing can safely in the backpack.
        }

        // Prefer slot 6 (index 5), then any other empty hotbar slot.
        if (slots[5].IsEmpty)
        {
            slots[5] = new ItemStack(Item.WateringCan, 1);
            Changed?.Invoke();
            return;
        }
        for (int i = 0; i < HotbarSize; i++)
        {
            if (!slots[i].IsEmpty) continue;
            slots[i] = new ItemStack(Item.WateringCan, 1);
            Changed?.Invoke();
            return;
        }

        // If every hotbar slot is occupied, safely place it in the backpack.
        Add(Item.WateringCan, 1);
    }

    public static int Get(Item item)
    {
        int total = 0;
        for (int i = 0; i < Capacity; i++) if (!slots[i].IsEmpty && slots[i].item == item) total += slots[i].count;
        return total;
    }

    /// <summary>True if the whole amount fits in the inventory right now.</summary>
    public static bool CanAdd(Item item, int amount)
    {
        int max = Def(item).stack, space = 0;
        for (int i = 0; i < Capacity; i++)
        {
            if (slots[i].IsEmpty) space += max;
            else if (slots[i].item == item) space += max - slots[i].count;
        }
        return space >= amount;
    }

    public static void Add(Item item, int amount = 1)
    {
        int left = AddToSlots(item, amount);
        if (left > 0) DropOverflow(item, left);
        Changed?.Invoke();
    }

    /// <summary>Puts items into existing stacks first, then empty slots. Returns how many did not fit.</summary>
    static int AddToSlots(Item item, int amount)
    {
        int max = Def(item).stack;
        for (int i = 0; i < Capacity && amount > 0; i++)
        {
            if (slots[i].IsEmpty || slots[i].item != item || slots[i].count >= max) continue;
            int put = Mathf.Min(max - slots[i].count, amount);
            slots[i].count += put;
            amount -= put;
        }
        for (int i = 0; i < Capacity && amount > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int put = Mathf.Min(max, amount);
            slots[i] = new ItemStack(item, put);
            amount -= put;
        }
        return amount;
    }

    static void DropOverflow(Item item, int amount)
    {
        GameHud.Toast($"Inventory full! Dropped {amount} {Name(item).ToLower()}.");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null) FarmWorld.DropPickup(item, (Vector2)player.transform.position + Vector2.down * 0.6f, amount);
    }

    public static bool TryRemove(Item item, int amount = 1)
    {
        if (Get(item) < amount) return false;
        // take from the backpack end first so hotbar stacks (like your seeds) stay put as long as possible
        for (int i = Capacity - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].IsEmpty || slots[i].item != item) continue;
            int take = Mathf.Min(slots[i].count, amount);
            slots[i].count -= take;
            amount -= take;
            if (slots[i].count <= 0) slots[i] = default;
        }
        Changed?.Invoke();
        return true;
    }

    // ------------------------------------------------------------ hotbar

    public static ItemStack Held => slots[SelectedHotbar];

    public static void SelectHotbar(int index)
    {
        SelectedHotbar = Mathf.Clamp(index, 0, HotbarSize - 1);
        var held = slots[SelectedHotbar];
        if (!held.IsEmpty && Def(held.item).IsSeed) lastSeedCrop = (int)held.item - CropCount;
        Changed?.Invoke();
    }

    /// <summary>The crop that will be planted: the seed you are holding, or the last seed you held.</summary>
    public static int SelectedCrop
    {
        get
        {
            var held = slots[SelectedHotbar];
            if (!held.IsEmpty && Def(held.item).IsSeed) return (int)held.item - CropCount;
            return lastSeedCrop;
        }
    }

    // ------------------------------------------------------------ equipment

    public static int EquipmentAttack { get { int a = 0; for (int i = 1; i < equipment.Length; i++) if (!equipment[i].IsEmpty) a += Def(equipment[i].item).attack; return a; } }
    public static int EquipmentDefense { get { int d = 0; for (int i = 1; i < equipment.Length; i++) if (!equipment[i].IsEmpty) d += Def(equipment[i].item).defense; return d; } }

    /// <summary>Left-click on an inventory slot with the mouse cursor holding a stack (or nothing).</summary>
    public static void ClickSlot(int i, ref ItemStack cursor)
    {
        var s = slots[i];
        if (cursor.IsEmpty)
        {
            if (!s.IsEmpty) { cursor = s; slots[i] = default; }
        }
        else if (s.IsEmpty)
        {
            slots[i] = cursor; cursor = default;
        }
        else if (s.item == cursor.item)
        {
            int move = Mathf.Min(Def(s.item).stack - s.count, cursor.count);
            if (move > 0)
            {
                s.count += move; cursor.count -= move; slots[i] = s;
                if (cursor.count <= 0) cursor = default;
            }
        }
        else
        {
            slots[i] = cursor; cursor = s; // swap
        }
        Changed?.Invoke();
    }

    /// <summary>Right-click: with a stack on the cursor drop one into the slot; otherwise use/equip what is in the slot.</summary>
    public static void RightClickSlot(int i, ref ItemStack cursor)
    {
        var s = slots[i];
        if (!cursor.IsEmpty)
        {
            if (s.IsEmpty || (s.item == cursor.item && s.count < Def(s.item).stack))
            {
                slots[i] = new ItemStack(cursor.item, s.IsEmpty ? 1 : s.count + 1);
                cursor.count--;
                if (cursor.count <= 0) cursor = default;
                Changed?.Invoke();
            }
            return;
        }
        if (s.IsEmpty) return;

        var def = Def(s.item);
        if (def.IsEquipment) { EquipFromSlot(i); return; }
        if (def.IsEdible)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null) EatItem(s.item, player.Health);
            return;
        }
        if (def.IsSeed) { if (i < HotbarSize) SelectHotbar(i); else GameHud.Toast("Move seeds to the hotbar (top row) to plant them."); return; }
        GameHud.Toast($"{def.name}: nothing to do with it right now.");
    }

    public static void EquipFromSlot(int i)
    {
        var s = slots[i];
        if (s.IsEmpty) return;
        var def = Def(s.item);
        if (!def.IsEquipment) return;
        var old = equipment[(int)def.equip];
        equipment[(int)def.equip] = new ItemStack(s.item, 1);
        slots[i] = old; // swap the old gear back into the slot (or leave it empty)
        GameHud.Toast($"Equipped {def.name}");
        Changed?.Invoke();
    }

    /// <summary>Click on an equipment slot with the cursor.</summary>
    public static void ClickEquip(EquipSlot slot, ref ItemStack cursor)
    {
        var cur = equipment[(int)slot];
        if (cursor.IsEmpty)
        {
            if (!cur.IsEmpty) { cursor = cur; equipment[(int)slot] = default; }
        }
        else if (Def(cursor.item).equip == slot)
        {
            equipment[(int)slot] = new ItemStack(cursor.item, 1);
            cursor = cur;
        }
        else GameHud.Toast($"That doesn't go in the {slot.ToString().ToLower()} slot.");
        Changed?.Invoke();
    }

    // ------------------------------------------------------------ eating

    public static bool EatItem(Item item, Health hp)
    {
        if (hp == null || hp.Dead) return false;
        int h = Def(item).heal;
        if (h <= 0 || Get(item) <= 0) return false;
        if (hp.current >= hp.max) { GameHud.Toast("You're already at full health."); return false; }
        TryRemove(item);
        hp.Heal(h);
        GameHud.Toast($"Ate {Name(item).ToLower()}  +{h} HP");
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
        foreach (Item it in Enum.GetValues(typeof(Item)))
        {
            int h = Def(it).heal;
            if (h <= 0 || Get(it) <= 0) continue;
            bool covers = h >= missing, bestCovers = bestHeal >= missing;
            if (!found || (covers && !bestCovers) || (covers && bestCovers && h < bestHeal) || (!covers && !bestCovers && h > bestHeal))
            { found = true; best = it; bestHeal = h; }
        }
        if (!found) { GameHud.Toast("Nothing to eat! Harvest crops or craft food."); return false; }
        return EatItem(best, hp);
    }

    // ------------------------------------------------------------ sorting

    /// <summary>Merges stacks and sorts the backpack (everything below the hotbar row) by category, rarity, name.</summary>
    public static void Sort()
    {
        var totals = new Dictionary<Item, int>();
        for (int i = HotbarSize; i < Capacity; i++)
        {
            if (slots[i].IsEmpty) continue;
            totals.TryGetValue(slots[i].item, out int have);
            totals[slots[i].item] = have + slots[i].count;
            slots[i] = default;
        }
        var items = new List<Item>(totals.Keys);
        items.Sort((a, b) =>
        {
            var da = Def(a); var db = Def(b);
            int c = da.category.CompareTo(db.category);
            if (c != 0) return c;
            c = db.rarity.CompareTo(da.rarity);
            return c != 0 ? c : string.Compare(da.name, db.name, StringComparison.Ordinal);
        });

        int slot = HotbarSize;
        foreach (var item in items)
        {
            int left = totals[item], max = Def(item).stack;
            while (left > 0 && slot < Capacity)
            {
                int put = Mathf.Min(max, left);
                slots[slot++] = new ItemStack(item, put);
                left -= put;
            }
        }
        Changed?.Invoke();
    }

    // ------------------------------------------------------------ saving

    [Serializable] public class SlotData { public string item; public int count; }

    public static List<SlotData> CaptureSlots()
    {
        var list = new List<SlotData>();
        for (int i = 0; i < Capacity; i++) list.Add(ToData(slots[i]));
        return list;
    }

    public static List<SlotData> CaptureEquipment()
    {
        var list = new List<SlotData>();
        for (int i = 0; i < equipment.Length; i++) list.Add(ToData(equipment[i]));
        return list;
    }

    static SlotData ToData(ItemStack s) => s.IsEmpty ? new SlotData { item = "", count = 0 } : new SlotData { item = s.item.ToString(), count = s.count };

    static ItemStack FromData(SlotData d)
    {
        if (d == null || d.count <= 0 || !Enum.TryParse(d.item, out Item it) ||
            !Enum.IsDefined(typeof(Item), it))
            return default;

        // A damaged or edited save must not create impossible oversized stacks.
        int safeCount = Mathf.Min(d.count, Mathf.Max(1, Def(it).stack));
        return new ItemStack(it, safeCount);
    }

    public static void Restore(List<SlotData> slotData, List<SlotData> equipmentData, int hotbar)
    {
        Array.Clear(slots, 0, slots.Length);
        Array.Clear(equipment, 0, equipment.Length);

        for (int i = 0; i < Capacity && slotData != null && i < slotData.Count; i++)
            slots[i] = FromData(slotData[i]);

        // Equipment entries must match their destination slot. Ignore invalid or misplaced gear.
        for (int i = 1; i < equipment.Length && equipmentData != null && i < equipmentData.Count; i++)
        {
            var stack = FromData(equipmentData[i]);
            if (stack.IsEmpty) continue;

            var def = Def(stack.item);
            if (def.IsEquipment && def.equip == (EquipSlot)i)
                equipment[i] = new ItemStack(stack.item, 1);
        }

        SelectHotbar(Mathf.Clamp(hotbar, 0, HotbarSize - 1));
    }
}
