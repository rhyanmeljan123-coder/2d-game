using UnityEngine;

/// <summary>An item lying on the ground. Walk over it to collect it.</summary>
public class Pickup : MonoBehaviour
{
    public Item item;
    public int amount = 1;
    Vector3 basePos;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Inventory.Add(item, amount);
        GameHud.Toast($"+{amount} {Inventory.Name(item)}");
        Destroy(gameObject);
    }
}
