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
            // F eats the held food, or the best food in your bag
            if (kb.fKey.wasPressedThisFrame)
            {
                var held = Inventory.Held;
                if (!held.IsEmpty && (Inventory.Def(held.item).IsEdible || held.item == Item.SwiftTonic || held.item == Item.StrengthTonic)) Inventory.UseItem(held.item, player);
                else Inventory.EatBest(player.Health);
            }

            // hotbar: keys 1-8
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
            for (int i = 0; i < digits.Length; i++)
                if (kb[digits[i]].wasPressedThisFrame) Inventory.SelectHotbar(i);
        }

        // hotbar: mouse wheel
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel > 0.1f) Inventory.SelectHotbar((Inventory.SelectedHotbar + Inventory.HotbarSize - 1) % Inventory.HotbarSize);
            else if (wheel < -0.1f) Inventory.SelectHotbar((Inventory.SelectedHotbar + 1) % Inventory.HotbarSize);
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

        var heldItem = Inventory.Held;
        bool holdingBuildKit = !heldItem.IsEmpty && (heldItem.item == Item.FenceKit || heldItem.item == Item.StorageCrateKit);
        CurrentPrompt = best != null ? best.Prompt : holdingBuildKit
            ? (heldItem.item == Item.FenceKit ? "Place wood fence [E]" : "Place storage crate [E]")
            : null;

        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            if (best != null) best.Interact();
            else if (holdingBuildKit) BuildableSystem.PlaceFromKit(heldItem.item);
        }
    }
}
