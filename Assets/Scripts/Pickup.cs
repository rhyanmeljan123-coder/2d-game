using UnityEngine;

/// <summary>An item lying on the ground. Walk over it to collect it.</summary>
public class Pickup : MonoBehaviour, ISaveable
{
    public Item item;
    public int amount = 1;
    Vector3 basePos;
    bool collected;

    public string CaptureState() => collected ? "gone" : null;
    public void RestoreState(string saved) { if (saved == "gone") { collected = true; Destroy(gameObject); } }
    public void Register() => WorldState.Register("pickup", this);

    public void Init(Item what, int count)
    {
        item = what;
        amount = count;
        basePos = transform.position;
    }

    void Update()
    {
        // Gentle bobbing so pickups are easy to spot
        transform.position = basePos + new Vector3(0f, Mathf.Sin(Time.time * 3f + basePos.x) * 0.06f, 0f);
    }

    float nextWarning;

    void OnTriggerStay2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        if (!Inventory.CanAdd(item, amount))
        {
            if (Time.time >= nextWarning) { GameHud.Toast("Inventory full! Make some room (press I)."); nextWarning = Time.time + 2f; }
            return; // stays on the ground until there is room
        }
        collected = true;
        Inventory.Add(item, amount);
        GameHud.Toast($"+{amount} {Inventory.Name(item)}");
        Destroy(gameObject);
    }
}
