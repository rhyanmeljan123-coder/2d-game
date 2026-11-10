using UnityEngine;

/// <summary>Anything the player can use by pressing E while facing it.</summary>
public abstract class Interactable : MonoBehaviour
{
    /// <summary>Text shown on screen while the player is next to it. Empty/null = not usable right now.</summary>
    public abstract string Prompt { get; }
    public abstract void Interact();
}
