using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>On-screen display: health, inventory, controls, interaction prompt, pop-up messages and the crafting menu.</summary>
public class GameHud : MonoBehaviour
{
    struct Message { public string text; public float until; }
    static readonly List<Message> toasts = new List<Message>();

    public static void Toast(string text)
    {
        toasts.Add(new Message { text = text, until = Time.time + 3.5f });
        if (toasts.Count > 4) toasts.RemoveAt(0);
    }

    static GameHud instance;
    float fadeAlpha;
    public static bool IsFading { get; private set; }

    // crafting menu
    static bool craftingOpen;
    static int craftSelected;
    static int craftOpenedFrame;
    public static bool UiOpen => craftingOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { toasts.Clear(); instance = null; IsFading = false; craftingOpen = false; }

    public static void OpenCrafting()
    {
        craftingOpen = true;
        craftSelected = 0;
        craftOpenedFrame = Time.frameCount;
    }

    void Awake() => instance = this;

    /// <summary>Fades to black, runs the action while the screen is black, then fades back in.</summary>
    public static void Fade(Action atBlack)
    {
        if (IsFading) return;
        if (instance == null) { atBlack?.Invoke(); return; }
        instance.StartCoroutine(instance.FadeRoutine(atBlack));
    }

    IEnumerator FadeRoutine(Action atBlack)
    {
        IsFading = true;
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { fadeAlpha = t / 0.25f; yield return null; }
        fadeAlpha = 1f;
        atBlack?.Invoke();
        yield return new WaitForSecondsRealtime(0.1f);
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { fadeAlpha = 1f - t / 0.25f; yield return null; }
        fadeAlpha = 0f;
        IsFading = false;
    }

    // ------------------------------------------------------------ input for the crafting menu

    void Update()
    {
        if (!craftingOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame ||
            (kb.eKey.wasPressedThisFrame && Time.frameCount != craftOpenedFrame))
        { craftingOpen = false; return; }

        int n = Recipes.All.Length;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) craftSelected = (craftSelected + n - 1) % n;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) craftSelected = (craftSelected + 1) % n;
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Recipes.Craft(Recipes.All[craftSelected]);
    }

    // ------------------------------------------------------------ drawing

    GUIStyle count, prompt, hint, toast, title, rowName, rowInfo, bar;

    void EnsureStyles()
    {
        if (count != null) return;
        count = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
        count.normal.textColor = Color.white;
        prompt = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        prompt.normal.textColor = Color.white;
        toast = new GUIStyle(prompt) { fontSize = 14 };
        hint = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.UpperRight };
        hint.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
        title = new GUIStyle(prompt) { fontSize = 20 };
        rowName = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        rowName.normal.textColor = Color.white;
        rowInfo = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
        bar = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bar.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        EnsureStyles();
        float s = Mathf.Max(1f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = Screen.height / s;
        var white = Texture2D.whiteTexture;

        DrawHealth(W, white);
        DrawInventory(white);

        // --- controls ---
        GUI.Label(new Rect(W - 440f, 6f, 432f, 56f),
            "WASD / arrows: move   Shift: run   Space / J / click: sword\nE: use / plant / harvest / open   1-5: choose seed\nF: eat food to heal   Crafting table: inside the farmhouse", hint);

        // --- interaction prompt ---
        if (!craftingOpen && !string.IsNullOrEmpty(PlayerInteractor.CurrentPrompt))
        {
            var rect = new Rect(W * 0.5f - 200f, H - 64f, 400f, 30f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, white);
            GUI.color = Color.white;
            GUI.Label(rect, PlayerInteractor.CurrentPrompt, prompt);
        }

        // --- pop-up messages ---
        float ty = H - 100f;
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            float left = toasts[i].until - Time.time;
            if (left <= 0f) { toasts.RemoveAt(i); continue; }
            GUI.color = new Color(1f, 0.95f, 0.5f, Mathf.Clamp01(left));
            GUI.Label(new Rect(W * 0.5f - 300f, ty, 600f, 24f), toasts[i].text, toast);
            ty -= 24f;
        }
        GUI.color = Color.white;

        if (craftingOpen) DrawCrafting(W, H, white);

        // --- fade to black (doors, sleeping) ---
        if (fadeAlpha > 0f)
        {
            GUI.depth = -100;
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
            GUI.color = Color.white;
        }
    }

    void DrawHealth(float W, Texture2D white)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        var hp = player.Health;
        float w = 220f, h = 18f, x = W * 0.5f - w * 0.5f, y = 8f;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), white);
        GUI.color = new Color(0.25f, 0.05f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(x, y, w, h), white);
        float frac = Mathf.Clamp01(hp.current / (float)hp.max);
        GUI.color = Color.Lerp(new Color(0.85f, 0.15f, 0.2f), new Color(0.3f, 0.8f, 0.35f), frac);
        GUI.DrawTexture(new Rect(x, y, w * frac, h), white);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, h), $"HP  {hp.current} / {hp.max}", bar);
    }

    static readonly Item[] BackpackOrder =
    {
        Item.Pumpkin, Item.Potato, Item.Carrot, Item.Tomato, Item.Peas, Item.Milk, Item.Herb, Item.Flower, Item.SlimeGel, Item.Bone,
        Item.Salad, Item.VegStew, Item.PumpkinSoup, Item.BoneBroth, Item.HealingPotion, Item.HeartyPie,
    };

    void DrawInventory(Texture2D white)
    {
        float slot = 34f, gap = 4f, x0 = 8f, y0 = 8f;

        // items you actually have (everything except seeds), 6 per row
        var have = new List<Item>();
        foreach (var it in BackpackOrder) if (Inventory.Get(it) > 0) have.Add(it);
        int rows = 1 + Mathf.CeilToInt(have.Count / 6f);

        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(x0 - 4f, y0 - 4f, (slot + gap) * 6f + 4f, (slot + gap) * rows + 4f), white);
        GUI.color = Color.white;

        for (int i = 0; i < 5; i++)
            DrawSlot(new Rect(x0 + i * (slot + gap), y0, slot, slot), Inventory.SeedFor(i), Inventory.SelectedCrop == i, (i + 1).ToString());
        for (int i = 0; i < have.Count; i++)
            DrawSlot(new Rect(x0 + (i % 6) * (slot + gap), y0 + (1 + i / 6) * (slot + gap), slot, slot), have[i], false, null);
    }

    void DrawSlot(Rect r, Item item, bool selected, string key)
    {
        var white = Texture2D.whiteTexture;
        GUI.color = selected ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(r, white);
        GUI.color = new Color(0.12f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, r.height - 4f), white);

        int n = Inventory.Get(item);
        var icon = Inventory.Icon(item);
        if (icon != null)
        {
            var tex = icon.texture; var tr = icon.rect;
            GUI.color = n > 0 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTextureWithTexCoords(new Rect(r.x + 3f, r.y + 3f, r.width - 6f, r.height - 6f), tex,
                new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x, r.y, r.width - 3f, r.height - 1f), n.ToString(), count);
        if (key != null)
        {
            var keyStyle = new GUIStyle(count) { alignment = TextAnchor.UpperLeft, fontSize = 10 };
            GUI.Label(new Rect(r.x + 3f, r.y + 1f, 14f, 14f), key, keyStyle);
        }
    }

    void DrawCrafting(float W, float H, Texture2D white)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, W, H), white);

        float w = 560f, rowH = 56f, h = 90f + rowH * Recipes.All.Length + 34f;
        var win = new Rect(W * 0.5f - w * 0.5f, H * 0.5f - h * 0.5f, w, h);
        GUI.color = new Color(0.30f, 0.18f, 0.12f, 0.98f);
        GUI.DrawTexture(win, white);
        GUI.color = new Color(0.17f, 0.10f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(win.x + 4f, win.y + 4f, win.width - 8f, win.height - 8f), white);
        GUI.color = Color.white;
        GUI.Label(new Rect(win.x, win.y + 8f, win.width, 30f), "Crafting table", title);
        GUI.Label(new Rect(win.x, win.y + 38f, win.width, 22f), "Turn crops and dungeon drops into food that heals you", hint2());

        for (int i = 0; i < Recipes.All.Length; i++)
        {
            var r = Recipes.All[i];
            bool can = Recipes.CanCraft(r);
            var row = new Rect(win.x + 14f, win.y + 70f + i * rowH, win.width - 28f, rowH - 6f);

            GUI.color = i == craftSelected ? new Color(1f, 0.85f, 0.3f, 0.35f) : new Color(1f, 1f, 1f, 0.07f);
            GUI.DrawTexture(row, white);
            GUI.color = Color.white;

            var icon = Inventory.Icon(r.result);
            if (icon != null)
            {
                var tex = icon.texture; var tr = icon.rect;
                GUI.DrawTextureWithTexCoords(new Rect(row.x + 6f, row.y + 6f, row.height - 12f, row.height - 12f), tex,
                    new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
            }

            rowName.normal.textColor = can ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(row.x + row.height + 4f, row.y + 3f, 300f, 22f), $"{r.name}   (+{Inventory.HealAmount(r.result)} HP)", rowName);
            rowInfo.normal.textColor = can ? new Color(0.65f, 1f, 0.65f) : new Color(1f, 0.6f, 0.55f);
            GUI.Label(new Rect(row.x + row.height + 4f, row.y + 26f, 330f, 20f), r.NeedsText(), rowInfo);
            GUI.Label(new Rect(row.xMax - 200f, row.y + 26f, 100f, 20f), $"you have {Inventory.Get(r.result)}", rowInfo);

            GUI.enabled = can;
            if (GUI.Button(new Rect(row.xMax - 90f, row.y + 8f, 80f, row.height - 16f), "Craft"))
            {
                craftSelected = i;
                Recipes.Craft(r);
            }
            GUI.enabled = true;
        }

        GUI.Label(new Rect(win.x, win.yMax - 30f, win.width, 22f), "W/S or arrows: choose     Enter / Space / click: craft     Esc / E: close", hint2());
    }

    GUIStyle hintStyle;
    GUIStyle hint2()
    {
        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        }
        return hintStyle;
    }
}
