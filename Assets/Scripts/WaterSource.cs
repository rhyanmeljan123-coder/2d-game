using UnityEngine;

/// <summary>A refill point placed on the walkable edge of a pond.</summary>
public class WaterSource : Interactable
{
    public override string Prompt
    {
        get
        {
            if (Inventory.Get(Item.WateringCan) <= 0) return "Pond";
            if (Inventory.Held.IsEmpty || Inventory.Held.item != Item.WateringCan)
                return "Select watering can to refill";
            return WateringCanSystem.Charges >= WateringCanSystem.MaxCharges
                ? "Watering can is full"
                : $"Refill watering can ({WateringCanSystem.Charges}/{WateringCanSystem.MaxCharges}) [E]";
        }
    }

    public override void Interact()
    {
        if (Inventory.Held.IsEmpty || Inventory.Held.item != Item.WateringCan)
        {
            GameHud.Toast("Select your watering can first.");
            return;
        }
        if (WateringCanSystem.Charges >= WateringCanSystem.MaxCharges)
        {
            GameHud.Toast("Your watering can is already full.");
            return;
        }
        WateringCanSystem.Refill();
    }
}
