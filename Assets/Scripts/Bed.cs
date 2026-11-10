using UnityEngine;

/// <summary>A bed. Sleeping fast-forwards time, so crops on the farm grow.</summary>
public class Bed : Interactable
{
    public const float SleepSeconds = 15f;

    public override string Prompt => GameHud.IsFading ? null : "Sleep: heal & grow crops [E]";

    public override void Interact()
    {
        if (GameHud.IsFading) return;
        GameHud.Fade(() =>
        {
            CropPlot.GrowAll(SleepSeconds);
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.Health.Heal(player.Health.max);
            GameHud.Toast("You slept well: full health, and your crops grew a little!");
        });
    }
}
