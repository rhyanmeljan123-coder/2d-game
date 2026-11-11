using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Recipe
{
    public string name;
    public Item result;
    public (Item item, int count)[] needs;

    public string NeedsText()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < needs.Length; i++)
        {
            if (i > 0) sb.Append("  +  ");
            sb.Append(needs[i].count).Append(' ').Append(Inventory.Name(needs[i].item).ToLower());
        }
        return sb.ToString();
    }
}

/// <summary>What can be crafted. Farm crops and dungeon drops are the ingredients; the results heal you.</summary>
public static class Recipes
{
    public static readonly Recipe[] All =
    {
        new Recipe { name = "Salad", result = Item.Salad, needs = new[] { (Item.Tomato, 1), (Item.Peas, 1), (Item.Herb, 1) } },
        new Recipe { name = "Vegetable stew", result = Item.VegStew, needs = new[] { (Item.Carrot, 1), (Item.Potato, 1), (Item.Tomato, 1) } },
        new Recipe { name = "Pumpkin soup", result = Item.PumpkinSoup, needs = new[] { (Item.Pumpkin, 1), (Item.Milk, 1) } },
        new Recipe { name = "Bone broth", result = Item.BoneBroth, needs = new[] { (Item.Bone, 2), (Item.Carrot, 1) } },
        new Recipe { name = "Healing potion", result = Item.HealingPotion, needs = new[] { (Item.Herb, 2), (Item.Flower, 1), (Item.SlimeGel, 1) } },
        new Recipe { name = "Hearty pie", result = Item.HeartyPie, needs = new[] { (Item.Pumpkin, 1), (Item.Potato, 2), (Item.Milk, 1) } },
    };

    public static List<MenuEntry> BuildMenu()
    {
        var list = new List<MenuEntry>();
        foreach (var r in All)
        {
            var recipe = r;
            list.Add(new MenuEntry
            {
                icon = recipe.result,
                title = $"{recipe.name}   (+{Inventory.HealAmount(recipe.result)} HP)",
                detail = $"{recipe.NeedsText()}      you have {Inventory.Get(recipe.result)}",
                button = "Craft",
                enabled = CanCraft(recipe),
                action = () => Craft(recipe),
            });
        }
        list.Add(new MenuEntry { icon = Item.Wood, title = "Wood fence kit", detail = "3 wood   |   added to inventory", button = "Craft", enabled = Inventory.Get(Item.Wood) >= 3 && Inventory.CanAdd(Item.FenceKit, 1), action = () => CraftBuild(Item.FenceKit, 3, 0) });
        list.Add(new MenuEntry { icon = Item.Wood, title = "Storage crate kit", detail = "5 wood + 2 stone   |   added to inventory", button = "Craft", enabled = Inventory.Get(Item.Wood) >= 5 && Inventory.Get(Item.Stone) >= 2 && Inventory.CanAdd(Item.StorageCrateKit, 1), action = () => CraftBuild(Item.StorageCrateKit, 5, 2) });
        return list;
    }

    public static bool CanCraft(Recipe r)
    {
        foreach (var n in r.needs) if (Inventory.Get(n.item) < n.count) return false;
        return true;
    }

    static bool CraftBuild(Item result, int wood, int stone)
    {
        if (!Inventory.CanAdd(result, 1)) { GameHud.Toast("Your inventory is full."); return false; }
        if (Inventory.Get(Item.Wood) < wood || Inventory.Get(Item.Stone) < stone)
        { GameHud.Toast("You are missing building materials."); return false; }
        if (!Inventory.TryRemove(Item.Wood, wood)) return false;
        if (stone > 0 && !Inventory.TryRemove(Item.Stone, stone))
        {
            Inventory.Add(Item.Wood, wood);
            return false;
        }
        Inventory.Add(result, 1);
        GameHud.Toast($"Crafted {Inventory.Name(result)}. Select it in your hotbar and press E to place it.");
        return true;
    }

    public static bool Craft(Recipe r)
    {
        if (!CanCraft(r)) { GameHud.Toast("You're missing some ingredients."); return false; }
        foreach (var n in r.needs) Inventory.TryRemove(n.item, n.count);
        Inventory.Add(r.result, 1);
        GameHud.Toast($"Crafted {r.name.ToLower()}!  (heals {Inventory.HealAmount(r.result)})");
        return true;
    }
}

/// <summary>A workbench: press E to open the crafting menu.</summary>
public class CraftingTable : Interactable
{
    public override string Prompt => "Craft food, potions & buildings [E]";
    public override void Interact() =>
        GameHud.OpenMenu("Crafting table", "Craft meals, potions, fences and storage crates from gathered materials", Recipes.BuildMenu);
}
