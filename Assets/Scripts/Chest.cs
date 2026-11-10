using System.Collections.Generic;
using UnityEngine;

/// <summary>A chest that can be opened once for random loot.</summary>
public class Chest : Interactable
{
    SpriteRenderer sr;
    Sprite openSprite;
    bool opened;

    public void Init(SpriteRenderer renderer, Sprite open)
    {
        sr = renderer;
        openSprite = open;
    }

    public override string Prompt => opened ? null : "Open chest [E]";

    public override void Interact()
    {
        if (opened) return;
        opened = true;
        if (sr != null && openSprite != null) sr.sprite = openSprite;

        var summary = new List<string>();
        // 2 seed packets + 1 extra
        for (int i = 0; i < 2; i++)
        {
            var seed = Inventory.SeedFor(Random.Range(0, Inventory.CropCount));
            int n = Random.Range(1, 4);
            Inventory.Add(seed, n);
            summary.Add($"+{n} {Inventory.Name(seed)}");
        }
        var extras = new[] { Item.Milk, Item.Herb, Item.Flower, Item.Pumpkin, Item.Carrot };
        var extra = extras[Random.Range(0, extras.Length)];
        Inventory.Add(extra, 1);
        summary.Add($"+1 {Inventory.Name(extra)}");
        GameHud.Toast("Chest: " + string.Join(", ", summary));
    }
}
