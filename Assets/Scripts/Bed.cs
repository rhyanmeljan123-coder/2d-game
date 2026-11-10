using UnityEngine;

/// <summary>A bed. Sleeping fast-forwards time, so crops on the farm grow.</summary>
public class Bed : Interactable
{
    public const float SleepSeconds = 15f;

    public override string Prompt => GameHud.IsFading ? null : "Sleep (crops grow) [E]";

    public override void Interact()
    {
        if (GameHud.IsFading) return;
        GameHud.Fade(() =>
        {
            CropPlot.GrowAll(SleepSeconds);
            GameHud.Toast("You slept well. Your crops grew a little!");
        });
    }
}
