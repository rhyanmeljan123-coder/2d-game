using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A patch of soil. Plant the selected seed (keys 1-5) with E, wait for it to grow, then press E to harvest.
/// </summary>
public class CropPlot : Interactable, ISaveable
{
    public const float StageSeconds = 5f; // 4 growth steps, so a crop is ripe after ~20 seconds

    enum State { Empty, Growing, Ripe }
    State state = State.Empty;
    int crop;
    float timer;
    int stage;
    SpriteRenderer cropRenderer;

    static readonly List<CropPlot> all = new List<CropPlot>();
    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    /// <summary>Fast-forwards every growing crop (used by the bed).</summary>
    public static void GrowAll(float seconds)
    {
        foreach (var plot in all)
            if (plot.state == State.Growing) plot.timer += seconds;
    }

    // plants.png rows for each crop (Pumpkin, Potato, Carrot, Tomato); peas use the tall 2-row sprites
    static readonly int[] PlantRow = { 0, 2, 1, 3, -1 };

    public void Init(SpriteRenderer cropSr)
    {
        cropRenderer = cropSr;
        WorldState.Register("plot", this, transform.position);
    }

    // ---- saving: "state|crop|timer" for anything that is not an empty plot
    public string CaptureState() => state == State.Empty ? null : $"{(int)state}|{crop}|{timer.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public void RestoreState(string saved)
    {
        // Always begin from a safe empty state. Invalid save data must not leave
        // a partially restored crop or throw an exception during world loading.
        state = State.Empty;
        crop = 0;
        timer = 0f;
        stage = 0;
        if (cropRenderer != null) cropRenderer.sprite = null;

        if (string.IsNullOrEmpty(saved)) return;

        var parts = saved.Split('|');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int savedState) ||
            !int.TryParse(parts[1], out int savedCrop) ||
            !float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float savedTimer) ||
            !System.Enum.IsDefined(typeof(State), savedState) ||
            savedState == (int)State.Empty ||
            savedCrop < 0 || savedCrop >= PlantRow.Length ||
            float.IsNaN(savedTimer) || float.IsInfinity(savedTimer) || savedTimer < 0f)
        {
            Debug.LogWarning($"Ignored invalid crop save data for {name}.");
            return;
        }

        crop = savedCrop;
        timer = savedTimer;
        state = (State)savedState;

        if (state == State.Ripe || timer >= StageSeconds * 4f)
        {
            state = State.Ripe;
            stage = 4;
        }
        else
        {
            stage = Mathf.Clamp(Mathf.FloorToInt(timer / StageSeconds), 0, 3);
        }

        ShowStage();
    }

    public override string Prompt
    {
        get
        {
            switch (state)
            {
                case State.Empty:
                    int c = Inventory.SelectedCrop;
                    return Inventory.Get(Inventory.SeedFor(c)) > 0
                        ? $"Plant {Inventory.CropName(c)} [E]"
                        : $"No {Inventory.Name(Inventory.SeedFor(c)).ToLower()} (hold seeds on the hotbar)";
                case State.Growing:
                    return $"{Inventory.CropName(crop)} growing...";
                default:
                    return $"Harvest {Inventory.CropName(crop)} [E]";
            }
        }
    }

    public override void Interact()
    {
        if (state == State.Empty)
        {
            int c = Inventory.SelectedCrop;
            if (!Inventory.TryRemove(Inventory.SeedFor(c))) return;
            crop = c;
            timer = 0f;
            stage = 0;
            state = State.Growing;
            ShowStage();
        }
        else if (state == State.Ripe)
        {
            Inventory.Add(Inventory.CropItem(crop), 1);
            string msg = $"+1 {Inventory.CropName(crop)}";
            if (Random.value < 0.6f)
            {
                int seeds = Random.Range(1, 3);
                Inventory.Add(Inventory.SeedFor(crop), seeds);
                msg += $", +{seeds} {Inventory.Name(Inventory.SeedFor(crop)).ToLower()}";
            }
            GameHud.Toast(msg);
            state = State.Empty;
            cropRenderer.sprite = null;
        }
    }

    void Update()
    {
        if (state != State.Growing) return;
        timer += Time.deltaTime;
        int newStage = Mathf.Min(4, Mathf.FloorToInt(timer / StageSeconds));
        if (newStage != stage) { stage = newStage; ShowStage(); }
        if (stage >= 4) state = State.Ripe;
    }

    void ShowStage()
    {
        if (cropRenderer != null)
            cropRenderer.sprite = GetSprite(crop, stage);
    }

    public static Sprite GetSprite(int crop, int stage)
    {
        if (crop < 0 || crop >= PlantRow.Length || stage < 0 || stage > 4)
            return null;

        if (crop == 4) return SpriteSheet.Cut("Items/plants", stage * 16, 64, 16, 32, new Vector2(8, 3)); // peas: tall
        var sheet = SpriteSheet.Slice("Items/plants", 16, new Vector2(8, 3));
        return sheet.Length == 0 ? null : sheet[PlantRow[crop], stage];
    }
}
