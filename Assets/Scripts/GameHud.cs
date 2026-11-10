using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Simple on-screen display: inventory, controls, interaction prompt and pop-up messages.</summary>
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { toasts.Clear(); instance = null; IsFading = false; }

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

    GUIStyle count, prompt, hint, toast;

    static readonly Item[] CropRow = { Item.Pumpkin, Item.Potato, Item.Carrot, Item.Tomato, Item.Peas };
    static readonly Item[] ExtraRow = { Item.Milk, Item.Herb, Item.Flower };

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
    }

    void OnGUI()
    {
        EnsureStyles();
        float s = Mathf.Max(1f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = Screen.height / s;
        var white = Texture2D.whiteTexture;

        // --- seeds (keys 1-5) ---
        float slot = 34f, gap = 4f, x0 = 8f, y0 = 8f;
        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(x0 - 4f, y0 - 4f, (slot + gap) * 5f + 4f, (slot + gap) * 2f + 4f), white);
        GUI.color = Color.white;
        for (int i = 0; i < 5; i++)
            DrawSlot(new Rect(x0 + i * (slot + gap), y0, slot, slot), Inventory.SeedFor(i), Inventory.SelectedCrop == i, (i + 1).ToString());
        // --- harvested items ---
        for (int i = 0; i < CropRow.Length; i++)
            DrawSlot(new Rect(x0 + i * (slot + gap), y0 + slot + gap, slot, slot), CropRow[i], false, null);
        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(x0 - 4f, y0 + (slot + gap) * 2f + 2f, (slot + gap) * 3f + 4f, slot + gap + 2f), white);
        GUI.color = Color.white;
        for (int i = 0; i < ExtraRow.Length; i++)
            DrawSlot(new Rect(x0 + i * (slot + gap), y0 + (slot + gap) * 2f + 4f, slot, slot), ExtraRow[i], false, null);

        // --- controls ---
        GUI.Label(new Rect(W - 420f, 6f, 412f, 40f),
            "WASD / arrows: move   Shift: run   Space / J: sword\nE: use   1-5: choose seed to plant", hint);

        // --- interaction prompt ---
        if (!string.IsNullOrEmpty(PlayerInteractor.CurrentPrompt))
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

        // --- fade to black (doors, sleeping) ---
        if (fadeAlpha > 0f)
        {
            GUI.depth = -100;
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
            GUI.color = Color.white;
        }
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
}
