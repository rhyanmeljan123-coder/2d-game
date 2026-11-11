using System.Collections.Generic;
using UnityEngine;

public enum LootKind { Common, SilverLocked, GoldLocked }

/// <summary>
/// An animated chest. Press E to open it for loot (coins, seeds, items).
/// Locked chests need a key found in the dungeon: silver key -> east chest (holds the gold key), gold key -> north chest.
/// </summary>
public class Chest : Interactable, ISaveable
{
    SimpleAnimator anim;
    Sprite[] closedFrames, openFrames;
    LootKind kind;
    bool opened;

    public void Init(SimpleAnimator animator, Sprite[] closed, Sprite[] open, LootKind lootKind)
    {
        anim = animator;
        closedFrames = closed;
        openFrames = open;
        kind = lootKind;
        anim.Play(closedFrames, 3f);
        WorldState.Register("chest", this, transform.position);
    }

    public string CaptureState() => opened ? "open" : null;

    public void RestoreState(string saved)
    {
        opened = saved == "open";
        if (opened) anim.ShowLast(openFrames);
        else anim.Play(closedFrames, 3f, true, true);
    }

    Item? RequiredKey => kind == LootKind.SilverLocked ? Item.SilverKey : kind == LootKind.GoldLocked ? Item.GoldKey : (Item?)null;

    public override string Prompt
    {
        get
        {
            if (opened) return null;
            var key = RequiredKey;
            if (key.HasValue && Inventory.Get(key.Value) == 0) return $"Locked: needs the {Inventory.Name(key.Value).ToLower()}";
            return key.HasValue ? $"Unlock chest with {Inventory.Name(key.Value).ToLower()} [E]" : "Open chest [E]";
        }
    }

    public override void Interact()
    {
        if (opened) return;
        var key = RequiredKey;
        if (key.HasValue && !Inventory.TryRemove(key.Value))
        {
            GameHud.Toast($"The chest is locked. You need the {Inventory.Name(key.Value).ToLower()}.");
            return;
        }

        opened = true;
        anim.Play(openFrames, 7f, false, true);

        var summary = new List<string>();
        void Give(Item item, int n) { Inventory.Add(item, n); summary.Add($"+{n} {Inventory.Name(item).ToLower()}"); }

        switch (kind)
        {
            case LootKind.Common:
                for (int i = 0; i < 2; i++) Give(Inventory.SeedFor(Random.Range(0, Inventory.CropCount)), Random.Range(1, 4));
                var extras = new[] { Item.Milk, Item.Herb, Item.Flower, Item.Pumpkin, Item.Carrot };
                Give(extras[Random.Range(0, extras.Length)], 1);
                Give(Item.Coin, Random.Range(3, 9));
                break;
            case LootKind.SilverLocked:
                Give(Item.GoldKey, 1);
                Give(Item.IronSword, 1);
                Give(Item.HealingPotion, 1);
                Give(Item.RedFlask, 1);
                Give(Item.Coin, Random.Range(10, 21));
                break;
            default: // GoldLocked: the dungeon's treasure
                Give(Item.KnightArmor, 1);
                Give(Item.HeartyPie, 1);
                Give(Item.HealingPotion, 2);
                Give(Item.BigRedFlask, 1);
                Give(Item.Coin, Random.Range(30, 51));
                break;
        }
        GameHud.Toast("Chest: " + string.Join(", ", summary));
    }
}
