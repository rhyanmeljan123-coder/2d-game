using UnityEngine;
using System;

/// <summary>Daily care and collectible farm products for a single animal.</summary>
public class AnimalCare : Interactable, ISaveable
{
    static AnimalCare[] all = Array.Empty<AnimalCare>();

    string species;
    string stableId;
    Item feedItem;
    Item productItem;
    bool fedToday;
    bool pettedToday;
    bool productReady;

    public void Init(string animalSpecies, string id)
    {
        species = animalSpecies.ToLowerInvariant();
        stableId = id;
        switch (species)
        {
            case "chicken": feedItem = Item.Peas; productItem = Item.Egg; break;
            case "cow":     feedItem = Item.Carrot; productItem = Item.Milk; break;
            case "pig":     feedItem = Item.Potato; productItem = Item.Truffle; break;
            case "sheep":   feedItem = Item.Pumpkin; productItem = Item.Wool; break;
            default:        feedItem = Item.Herb; productItem = Item.Egg; break;
        }
        RegisterSaveable();
    }

    public void RegisterSaveable()
    {
        if (!string.IsNullOrEmpty(stableId))
            WorldState.RegisterFixed("animal-care:" + stableId, this);
    }

    void OnEnable() => AddToList();
    void OnDisable() => RemoveFromList();

    void AddToList()
    {
        var list = new System.Collections.Generic.List<AnimalCare>(all);
        if (!list.Contains(this)) list.Add(this);
        all = list.ToArray();
    }

    void RemoveFromList()
    {
        var list = new System.Collections.Generic.List<AnimalCare>(all);
        list.Remove(this);
        all = list.ToArray();
    }

    /// <summary>Called once whenever the player sleeps and advances to the next day.</summary>
    public static void AdvanceDay()
    {
        foreach (var animal in all)
        {
            if (animal == null) continue;
            if (animal.fedToday && !animal.productReady)
                animal.productReady = true;
            animal.fedToday = false;
            animal.pettedToday = false;
        }
    }

    public override string Prompt
    {
        get
        {
            if (productReady) return "Collect " + Inventory.Name(productItem) + " [E]";
            if (fedToday && pettedToday) return species + " is happy and cared for today";
            if (fedToday) return "Pet " + species + " [E]";
            if (pettedToday) return "Select " + Inventory.Name(feedItem).ToLower() + " to feed " + species;
            if (!Inventory.Held.IsEmpty && Inventory.Held.item == feedItem)
                return "Feed " + species + " [E]";
            return "Pet " + species + " [E] (feed with " + Inventory.Name(feedItem).ToLower() + ")";
        }
    }

    public override void Interact()
    {
        if (productReady)
        {
            Inventory.Add(productItem, 1);
            productReady = false;
            GameHud.Toast("Collected 1 " + Inventory.Name(productItem).ToLower() + " from your " + species + "!");
            return;
        }

        var held = Inventory.Held;
        if (!fedToday && !held.IsEmpty && held.item == feedItem)
        {
            if (!Inventory.TryRemove(feedItem, 1)) return;
            fedToday = true;
            GameHud.Toast("You fed your " + species + ". It may produce something after you sleep.");
            return;
        }

        if (!pettedToday)
        {
            pettedToday = true;
            GameHud.Toast("You petted your " + species + ". It looks happy!");
            return;
        }

        if (!fedToday)
            GameHud.Toast("Select " + Inventory.Name(feedItem).ToLower() + " to feed this animal.");
        else
            GameHud.Toast("This animal has already been cared for today.");
    }

    public string CaptureState() =>
        (fedToday ? "1" : "0") + "|" + (productReady ? "1" : "0") + "|" + (pettedToday ? "1" : "0");

    public void RestoreState(string saved)
    {
        fedToday = false;
        productReady = false;
        pettedToday = false;
        if (string.IsNullOrEmpty(saved)) return;

        var parts = saved.Split('|');
        if (parts.Length != 3 ||
            (parts[0] != "0" && parts[0] != "1") ||
            (parts[1] != "0" && parts[1] != "1") ||
            (parts[2] != "0" && parts[2] != "1"))
        {
            Debug.LogWarning("AnimalCare: ignored invalid saved state for " + stableId);
            return;
        }

        fedToday = parts[0] == "1";
        productReady = parts[1] == "1";
        pettedToday = parts[2] == "1";
    }
}