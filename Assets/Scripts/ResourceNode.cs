using UnityEngine;

public enum ResourceKind { Tree, Rock }

/// <summary>Interactive farm resource node. Chop trees for wood and mine rocks for stone.</summary>
public class ResourceNode : Interactable, ISaveable
{
    [SerializeField] ResourceKind kind;
    [SerializeField] int hits;
    SpriteRenderer spriteRenderer;
    Collider2D solidCollider;
    bool depleted;

    int RequiredHits => kind == ResourceKind.Tree ? 3 : 2;
    Item DropItem => kind == ResourceKind.Tree ? Item.Wood : Item.Stone;

    public void Init(ResourceKind resourceKind)
    {
        kind = resourceKind;
        spriteRenderer = GetComponent<SpriteRenderer>();
        solidCollider = GetComponent<Collider2D>();
        WorldState.Register("resource", this, transform.position);
    }

    public override string Prompt
    {
        get
        {
            if (depleted) return null;
            string action = kind == ResourceKind.Tree ? "Chop tree" : "Mine rock";
            return $"{action} [E] ({hits}/{RequiredHits})";
        }
    }

    public override void Interact()
    {
        if (depleted) return;
        hits++;
        if (hits < RequiredHits)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.82f, 0.65f);
            GameHud.Toast(kind == ResourceKind.Tree
                ? $"Chopping tree: {hits}/{RequiredHits}"
                : $"Mining rock: {hits}/{RequiredHits}");
            return;
        }

        depleted = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (solidCollider != null) solidCollider.enabled = false;

        int amount = kind == ResourceKind.Tree ? Random.Range(3, 6) : Random.Range(2, 5);
        ProgressionSystem.AddXP(ProgressionSystem.Skill.Gathering, 20);
        if (Random.value < ProgressionSystem.BonusGatherChance) amount++;
        FarmWorld.DropPickup(transform.parent, DropItem, transform.position + Vector3.up * 0.35f, amount);
        GameHud.Toast($"+{amount} {Inventory.Name(DropItem)}");
    }

    public string CaptureState() => hits <= 0 ? null : (depleted ? "depleted" : hits.ToString());

    public void RestoreState(string saved)
    {
        hits = 0;
        depleted = false;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (solidCollider == null) solidCollider = GetComponent<Collider2D>();
        if (string.IsNullOrEmpty(saved)) return;

        if (saved == "depleted")
        {
            hits = RequiredHits;
            depleted = true;
        }
        else if (int.TryParse(saved, out int restoredHits) && restoredHits > 0 && restoredHits < RequiredHits)
        {
            hits = restoredHits;
        }
        else
        {
            Debug.LogWarning($"Ignored invalid resource save data for {name}.");
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !depleted;
            spriteRenderer.color = Color.white;
        }
        if (solidCollider != null) solidCollider.enabled = !depleted;
    }
}