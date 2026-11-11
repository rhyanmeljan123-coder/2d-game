using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent item storage for a player-built crate.</summary>
public class StorageCrate : Interactable
{
    [Serializable] public class StoredItemData { public string item; public int count; }
    readonly Dictionary<Item, int> contents = new Dictionary<Item, int>();

    public override string Prompt => "Open storage crate [E]";

    public void Init(List<StoredItemData> saved)
    {
        contents.Clear();
        if (saved == null) return;
        foreach (var entry in saved)
        {
            if (entry == null || entry.count <= 0 || !Enum.TryParse(entry.item, out Item item) ||
                !Enum.IsDefined(typeof(Item), item)) continue;
            contents.TryGetValue(item, out int current);
            contents[item] = current + Mathf.Clamp(entry.count, 0, 999999);
        }
    }

    public List<StoredItemData> Capture()
    {
        var result = new List<StoredItemData>();
        foreach (var pair in contents)
            if (pair.Value > 0)
                result.Add(new StoredItemData { item = pair.Key.ToString(), count = pair.Value });
        return result;
    }

    public override void Interact()
    {
        GameHud.OpenMenu("Storage Crate", "Store items or take them back. Contents are saved with your world.", BuildEntries);
    }

    List<MenuEntry> BuildEntries()
    {
        var entries = new List<MenuEntry>();
        foreach (Item item in Enum.GetValues(typeof(Item)))
        {
            int carried = Inventory.Get(item);
            contents.TryGetValue(item, out int stored);

            if (carried > 0)
            {
                int amount = carried;
                entries.Add(new MenuEntry
                {
                    icon = item,
                    title = "Store " + Inventory.Name(item),
                    detail = $"Carrying: {carried}   In crate: {stored}",
                    button = "Store all",
                    enabled = true,
                    action = () => Deposit(item, amount)
                });
            }

            if (stored > 0)
            {
                int amount = stored;
                int take = amount;
                while (take > 0 && !Inventory.CanAdd(item, take)) take--;
                entries.Add(new MenuEntry
                {
                    icon = item,
                    title = "Take " + Inventory.Name(item),
                    detail = $"In crate: {stored}   Inventory space: {(take > 0 ? take : 0)}",
                    button = take > 0 ? "Take all" : "Full",
                    enabled = take > 0,
                    action = () => Withdraw(item, take)
                });
            }
        }
        return entries;
    }

    void Deposit(Item item, int amount)
    {
        if (amount <= 0 || !Inventory.TryRemove(item, amount)) return;
        contents.TryGetValue(item, out int current);
        contents[item] = current + amount;
        GameHud.Toast($"Stored {amount} {Inventory.Name(item).ToLower()}.");
    }

    void Withdraw(Item item, int amount)
    {
        if (amount <= 0 || !contents.TryGetValue(item, out int stored)) return;
        amount = Mathf.Min(amount, stored);
        if (!Inventory.CanAdd(item, amount))
        {
            GameHud.Toast("Not enough inventory space.");
            return;
        }
        contents[item] = stored - amount;
        if (contents[item] <= 0) contents.Remove(item);
        Inventory.Add(item, amount);
        GameHud.Toast($"Took {amount} {Inventory.Name(item).ToLower()}.");
    }
}
