using UnityEngine;

/// <summary>A doorway: walk into it and the player fades out and appears at the destination.</summary>
public class Teleporter : MonoBehaviour
{
    public Vector2 destination;

    public void Init(Vector2 dest) => destination = dest;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GameHud.IsFading) return;
        var body = other.attachedRigidbody;
        var target = destination;
        GameHud.Fade(() =>
        {
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = target; }
            other.transform.position = target;
        });
    }
}
