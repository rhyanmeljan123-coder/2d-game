using UnityEngine;

/// <summary>
/// A doorway: walk into its trigger to fade out and appear at the destination.
/// Supports the Player tag on either the collider or its parent player object.
/// </summary>
public class Teleporter : MonoBehaviour
{
    public Vector2 destination;

    bool teleportStarted;

    public void Init(Vector2 dest) => destination = dest;

    void OnTriggerEnter2D(Collider2D other) => TryTeleport(other);

    // Stay is a safety net for cases where the player begins overlapping the
    // doorway trigger or the first physics enter event is missed.
    void OnTriggerStay2D(Collider2D other) => TryTeleport(other);

    void TryTeleport(Collider2D other)
    {
        if (teleportStarted || GameHud.IsFading) return;

        // The collider may be on the player root or on a child object.
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null && other.attachedRigidbody != null)
            player = other.attachedRigidbody.GetComponent<PlayerController>();

        if (player == null) return;
        if (!other.CompareTag("Player") && !player.CompareTag("Player")) return;

        teleportStarted = true;
        var body = player.GetComponent<Rigidbody2D>();
        var target = destination;

        GameHud.Fade(() =>
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = target;
            }

            player.transform.position = target;
        });

        // Keep this doorway locked until the player has left its trigger.
        // This prevents an immediate second teleport while the fade is running.
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null && other.attachedRigidbody != null)
            player = other.attachedRigidbody.GetComponent<PlayerController>();

        if (player != null)
            teleportStarted = false;
    }
}
