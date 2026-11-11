using UnityEngine;

/// <summary>A bed. Sleeping skips to the next morning (heals you fully, saves, rolls the weather, grows crops a little).</summary>
public class Bed : Interactable
{
    public const float SleepSeconds = 15f;

    public override string Prompt => GameHud.IsFading ? null : "Sleep until morning [E]";

    public override void Interact() => GameClock.Sleep(false);
}
