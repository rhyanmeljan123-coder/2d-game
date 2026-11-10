using System.Collections.Generic;
using UnityEngine;

/// <summary>A market stall. Sell crops, dungeon loot and food for coins, and buy seeds and flasks.</summary>
public class ShopStall : Interactable
{
    public override string Prompt => "Trade at the market stall [E]";

    public override void Interact() =>
        GameHud.OpenMenu("Market stall", "Sell what you grow and find, buy seeds and flasks", BuildMenu, "Coins");

    static readonly Item[] Buyable =
    {
        Item.PumpkinSeeds, Item.PotatoSeeds, Item.CarrotSeeds, Item.TomatoSeeds, Item.PeaSeeds, Item.Milk, Item.RedFlask,
    };

    static List<MenuEntry> BuildMenu()
    {
        var list = new List<MenuEntry>();

        // Selling: only things you actually have
        for (int i = 0; i < System.Enum.GetValues(typeof(Item)).Length; i++)
        {
            var item = (Item)i;
            int price = Inventory.SellPrice(item);
            int have = Inventory.Get(item);
            if (price <= 0 || have <= 0) continue;
            list.Add(new MenuEntry
            {
                icon = item,
                title = $"Sell {Inventory.Name(item).ToLower()}   +{price} coins",
                detail = $"you have {have}",
                button = "Sell",
                enabled = true,
                action = () =>
                {
                    if (Inventory.TryRemove(item)) { Inventory.Add(Item.Coin, price); GameHud.Toast($"Sold {Inventory.Name(item).ToLower()} for {price} coins"); }
                },
            });
        }

        // Buying
        foreach (var item in Buyable)
        {
            var thing = item;
            int price = Inventory.BuyPrice(thing);
            list.Add(new MenuEntry
            {
                icon = thing,
                title = $"Buy {Inventory.Name(thing).ToLower()}   -{price} coins",
                detail = $"you have {Inventory.Get(thing)}",
                button = "Buy",
                enabled = Inventory.Get(Item.Coin) >= price,
                action = () =>
                {
                    if (Inventory.TryRemove(Item.Coin, price)) { Inventory.Add(thing, 1); GameHud.Toast($"Bought {Inventory.Name(thing).ToLower()}"); }
                },
            });
        }
        return list;
    }
}
