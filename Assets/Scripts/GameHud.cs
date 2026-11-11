using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>One row of a list menu (crafting, trading).</summary>
public class MenuEntry
{
    public Item icon;
    public string title, detail, button;
    public bool enabled = true;
    public Action action;
}

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

    // generic list menu (crafting table, market stall)
    static bool menuOpen;
    static int menuSelected, menuScroll, menuOpenedFrame;
    static string menuTitle, menuSubtitle;
    static Func<List<MenuEntry>> menuProvider;
    static bool menuShowCoins;
    public static bool MenuOpen => menuOpen;
    /// <summary>True while any full-screen UI (list menu or inventory) is open: the player stands still.</summary>
    public static bool UiOpen => menuOpen || InventoryScreen.IsOpen;
    const int VisibleRows = 7;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { toasts.Clear(); instance = null; IsFading = false; menuOpen = false; }

    /// <summary>Opens a list menu. The provider is called every frame so counts and prices stay current.</summary>
    public static void OpenMenu(string title, string subtitle, Func<List<MenuEntry>> provider, string extra = null)
    {
        menuOpen = true;
        menuTitle = title;
        menuSubtitle = subtitle;
        menuProvider = provider;
        menuShowCoins = extra == "Coins";
        menuSelected = 0;
        menuScroll = 0;
        menuOpenedFrame = Time.frameCount;
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
        if (!menuOpen) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame ||
            (kb.eKey.wasPressedThisFrame && Time.frameCount != menuOpenedFrame))
        { menuOpen = false; return; }

        var entries = menuProvider();
        int n = entries.Count;
        if (n == 0) { menuSelected = 0; return; }
        menuSelected = Mathf.Clamp(menuSelected, 0, n - 1);

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) menuSelected = (menuSelected + n - 1) % n;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) menuSelected = (menuSelected + 1) % n;
        if (menuSelected < menuScroll) menuScroll = menuSelected;
        if (menuSelected >= menuScroll + VisibleRows) menuScroll = menuSelected - VisibleRows + 1;

        if ((kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && entries[menuSelected].enabled)
            entries[menuSelected].action();
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
        DrawHotbar(W, H, white);

        // --- controls ---
        GUI.Label(new Rect(W - 440f, 6f, 432f, 56f),
            "WASD / arrows: move   Shift: run   Space / J / click: sword\nE: use / water / plant / harvest / open   1-8 or wheel: hotbar   F: eat to heal\nI: inventory   F5: save   F9: load   Watch out for traps!", hint);

        // --- interaction prompt ---
        if (!UiOpen && !string.IsNullOrEmpty(PlayerInteractor.CurrentPrompt))
        {
            var rect = new Rect(W * 0.5f - 200f, H - 108f, 400f, 30f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, white);
            GUI.color = Color.white;
            GUI.Label(rect, PlayerInteractor.CurrentPrompt, prompt);
        }

        // --- pop-up messages ---
        float ty = H - 144f;
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            float left = toasts[i].until - Time.time;
            if (left <= 0f) { toasts.RemoveAt(i); continue; }
            GUI.color = new Color(1f, 0.95f, 0.5f, Mathf.Clamp01(left));
            GUI.Label(new Rect(W * 0.5f - 300f, ty, 600f, 24f), toasts[i].text, toast);
            ty -= 24f;
        }
        GUI.color = Color.white;

        if (menuOpen) DrawMenu(W, H, white);

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

        // coins
        var coin = Inventory.Icon(Item.Coin);
        if (coin != null)
        {
            var tex = coin.texture; var tr = coin.rect;
            GUI.DrawTextureWithTexCoords(new Rect(x, y + h + 6f, 20f, 20f), tex,
                new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
        }
        GUI.Label(new Rect(x + 24f, y + h + 6f, 80f, 20f), Inventory.Get(Item.Coin).ToString(), rowName);

        // active boosts
        float bx = x + 110f;
        if (player.SpeedBoostLeft > 0f) { GUI.Label(new Rect(bx, y + h + 6f, 150f, 20f), $"Speed {Mathf.CeilToInt(player.SpeedBoostLeft)}s", rowInfo); bx += 80f; }
        if (player.StrengthBoostLeft > 0f) GUI.Label(new Rect(bx, y + h + 6f, 150f, 20f), $"Strength {Mathf.CeilToInt(player.StrengthBoostLeft)}s", rowInfo);
    }

    /// <summary>The 8 hotbar slots along the bottom of the screen (keys 1-8 / mouse wheel).</summary>
    void DrawHotbar(float W, float H, Texture2D white)
    {
        float slot = 42f, gap = 4f;
        float total = Inventory.HotbarSize * (slot + gap) - gap;
        float x0 = W * 0.5f - total * 0.5f, y0 = H - slot - 10f;

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(x0 - 6f, y0 - 6f, total + 12f, slot + 12f), white);
        GUI.color = Color.white;
        for (int i = 0; i < Inventory.HotbarSize; i++)
            ItemUI.Slot(new Rect(x0 + i * (slot + gap), y0, slot, slot), Inventory.GetSlot(i), i == Inventory.SelectedHotbar, (i + 1).ToString());

        var held = Inventory.Held;
        if (!held.IsEmpty)
        {
            var d = Inventory.Def(held.item);
            rowInfo.normal.textColor = ItemDatabase.RarityColor(d.rarity);
            rowInfo.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x0, y0 - 24f, total, 18f), d.name, rowInfo);
            rowInfo.alignment = TextAnchor.MiddleLeft;
        }
    }

    void DrawMenu(float W, float H, Texture2D white)
    {
        var entries = menuProvider();

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, W, H), white);

        float w = 600f, rowH = 56f, h = 96f + rowH * VisibleRows + 34f;
        var win = new Rect(W * 0.5f - w * 0.5f, H * 0.5f - h * 0.5f, w, h);
        GUI.color = new Color(0.30f, 0.18f, 0.12f, 0.98f);
        GUI.DrawTexture(win, white);
        GUI.color = new Color(0.17f, 0.10f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(win.x + 4f, win.y + 4f, win.width - 8f, win.height - 8f), white);
        GUI.color = Color.white;
        GUI.Label(new Rect(win.x, win.y + 8f, win.width, 30f), menuTitle, title);
        GUI.Label(new Rect(win.x, win.y + 38f, win.width, 22f), menuSubtitle, hint2());
        if (menuShowCoins) GUI.Label(new Rect(win.xMax - 140f, win.y + 12f, 120f, 22f), $"Coins: {Inventory.Get(Item.Coin)}", rowName);

        if (entries.Count == 0)
            GUI.Label(new Rect(win.x, win.y + 120f, win.width, 30f), "Nothing here right now.", hint2());

        for (int v = 0; v < VisibleRows; v++)
        {
            int i = menuScroll + v;
            if (i >= entries.Count) break;
            var e = entries[i];
            var row = new Rect(win.x + 14f, win.y + 70f + v * rowH, win.width - 28f, rowH - 6f);

            GUI.color = i == menuSelected ? new Color(1f, 0.85f, 0.3f, 0.35f) : new Color(1f, 1f, 1f, 0.07f);
            GUI.DrawTexture(row, white);
            GUI.color = Color.white;

            var icon = Inventory.Icon(e.icon);
            if (icon != null)
            {
                var tex = icon.texture; var tr = icon.rect;
                GUI.DrawTextureWithTexCoords(new Rect(row.x + 6f, row.y + 6f, row.height - 12f, row.height - 12f), tex,
                    new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
            }

            rowName.normal.textColor = e.enabled ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(row.x + row.height + 4f, row.y + 3f, 380f, 22f), e.title, rowName);
            rowInfo.normal.textColor = e.enabled ? new Color(0.65f, 1f, 0.65f) : new Color(1f, 0.6f, 0.55f);
            GUI.Label(new Rect(row.x + row.height + 4f, row.y + 26f, 400f, 20f), e.detail, rowInfo);

            GUI.enabled = e.enabled;
            if (GUI.Button(new Rect(row.xMax - 90f, row.y + 8f, 80f, row.height - 16f), e.button))
            {
                menuSelected = i;
                e.action();
            }
            GUI.enabled = true;
        }

        if (entries.Count > VisibleRows)
            GUI.Label(new Rect(win.x, win.yMax - 52f, win.width, 20f), $"{menuScroll + 1}-{Mathf.Min(entries.Count, menuScroll + VisibleRows)} of {entries.Count}   (W/S to scroll)", hint2());
        GUI.Label(new Rect(win.x, win.yMax - 30f, win.width, 22f), "W/S or arrows: choose     Enter / Space / click: confirm     Esc / E: close", hint2());
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
