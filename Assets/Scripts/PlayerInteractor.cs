using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets the player use things in front of them with E (plant, harvest, open chests),
/// and pick the seed to plant with the number keys 1-5.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    /// <summary>What the HUD shows at the bottom of the screen (null = nothing to interact with).</summary>
    public static string CurrentPrompt;

    PlayerController player;

    void Awake() => player = GetComponent<PlayerController>();

    void OnDisable() => CurrentPrompt = null;

    void Update()
    {
        var kb = Keyboard.current;
        if (GameHud.UiOpen || GameHud.IsFading) { CurrentPrompt = null; return; }
        if (kb != null)
        {
            if (kb.fKey.wasPressedThisFrame) Inventory.EatBest(player.Health);
            if (kb.digit1Key.wasPressedThisFrame) Inventory.SelectedCrop = 0;
            if (kb.digit2Key.wasPressedThisFrame) Inventory.SelectedCrop = 1;
            if (kb.digit3Key.wasPressedThisFrame) Inventory.SelectedCrop = 2;
            if (kb.digit4Key.wasPressedThisFrame) Inventory.SelectedCrop = 3;
            if (kb.digit5Key.wasPressedThisFrame) Inventory.SelectedCrop = 4;
        }

        // Look for something usable just in front of the player's feet
        Vector2 point = (Vector2)transform.position + player.FacingDir * 0.7f + Vector2.up * 0.3f;
        Interactable best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(point, 0.8f))
        {
            var it = hit.GetComponentInParent<Interactable>();
            if (it == null || string.IsNullOrEmpty(it.Prompt)) continue;
            float d = Vector2.Distance(point, hit.ClosestPoint(point));
            if (d < bestDist) { bestDist = d; best = it; }
        }

        CurrentPrompt = best != null ? best.Prompt : null;
        if (best != null && kb != null && kb.eKey.wasPressedThisFrame) best.Interact();
    }
}
