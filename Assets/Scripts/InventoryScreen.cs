using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Full inventory screen (press I). Left-click picks up / places a stack, right-click uses / equips an item
/// (or places a single item from the stack you're holding). The top row of slots is the hotbar.
/// </summary>
public class InventoryScreen : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    static ItemStack cursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { IsOpen = false; cursor = default; }

    static readonly EquipSlot[] EquipSlots = { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Tool, EquipSlot.Charm };

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!IsOpen)
        {
            if (kb.iKey.wasPressedThisFrame && !GameHud.MenuOpen && !GameHud.IsFading) IsOpen = true;
        }
        else if (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    void OnDisable() { if (IsOpen) Close(); }

    static void Close()
    {
        IsOpen = false;
        if (!cursor.IsEmpty) { Inventory.Add(cursor.item, cursor.count); cursor = default; } // put the held stack back
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        float s = Mathf.Max(1f, Screen.height / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = Screen.height / s;
        var white = Texture2D.whiteTexture;
        var e = Event.current;
        bool down = e.type == EventType.MouseDown;

        GUI.depth = -50;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0f, 0f, W, H), white);

        float slot = 46f, gap = 5f;
        float gridW = Inventory.Columns * (slot + gap) - gap;
        float rows = Inventory.Capacity / Inventory.Columns;
        float gridH = rows * (slot + gap) - gap;
        float panelW = 170f;
        var win = new Rect(0f, 0f, gridW + panelW + 70f, gridH + 150f);
        win.x = W * 0.5f - win.width * 0.5f;
        win.y = H * 0.5f - win.height * 0.5f;

        GUI.color = new Color(0.30f, 0.18f, 0.12f, 0.98f);
        GUI.DrawTexture(win, white);
        GUI.color = new Color(0.17f, 0.10f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(win.x + 4f, win.y + 4f, win.width - 8f, win.height - 8f), white);
        GUI.color = Color.white;

        var title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        var small = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
        GUI.Label(new Rect(win.x + 20f, win.y + 10f, 300f, 26f), "Inventory", title);
        GUI.Label(new Rect(win.x + 20f, win.y + 36f, 400f, 18f), "Top row = hotbar (keys 1-8)", small);

        int hover = -1;
        EquipSlot hoverEquip = EquipSlot.None;

        // --- backpack grid ---
        float gx = win.x + 20f, gy = win.y + 58f;
        for (int i = 0; i < Inventory.Capacity; i++)
        {
            var r = new Rect(gx + (i % Inventory.Columns) * (slot + gap), gy + (i / Inventory.Columns) * (slot + gap), slot, slot);
            ItemUI.Slot(r, Inventory.GetSlot(i), i == Inventory.SelectedHotbar, i < Inventory.HotbarSize ? (i + 1).ToString() : null);
            if (!r.Contains(e.mousePosition)) continue;
            hover = i;
            if (down)
            {
                if (e.button == 0) Inventory.ClickSlot(i, ref cursor);
                else if (e.button == 1) Inventory.RightClickSlot(i, ref cursor);
                e.Use();
            }
        }

        // --- equipment & stats ---
        float ex = gx + gridW + 30f;
        GUI.Label(new Rect(ex, win.y + 36f, panelW, 18f), "Equipment", small);
        for (int k = 0; k < EquipSlots.Length; k++)
        {
            var es = EquipSlots[k];
            var r = new Rect(ex, gy + k * (slot + gap), slot, slot);
            ItemUI.Slot(r, Inventory.GetEquipped(es), false, null);
            GUI.Label(new Rect(r.xMax + 8f, r.y + 12f, 110f, 22f), es.ToString(), small);
            if (!r.Contains(e.mousePosition)) continue;
            hoverEquip = es;
            if (down && e.button == 0) { Inventory.ClickEquip(es, ref cursor); e.Use(); }
            else if (down && e.button == 1 && !Inventory.GetEquipped(es).IsEmpty && cursor.IsEmpty)
            { Inventory.ClickEquip(es, ref cursor); e.Use(); }
        }

        var player = FindFirstObjectByType<PlayerController>();
        float sy = gy + 4f * (slot + gap) + 6f;
        GUI.Label(new Rect(ex, sy, panelW, 18f), $"Attack:  {(player != null ? player.swordDamage : 0)} + {Inventory.EquipmentAttack}", small);
        GUI.Label(new Rect(ex, sy + 18f, panelW, 18f), $"Defense:  {Inventory.EquipmentDefense}", small);
        if (player != null) GUI.Label(new Rect(ex, sy + 36f, panelW, 18f), $"Health:  {player.Health.current} / {player.Health.max}", small);

        // --- buttons ---
        if (GUI.Button(new Rect(win.x + 20f, win.yMax - 52f, 90f, 28f), "Sort")) Inventory.Sort();
        if (GUI.Button(new Rect(win.x + 120f, win.yMax - 52f, 90f, 28f), "Close")) Close();
        GUI.Label(new Rect(win.x + 225f, win.yMax - 54f, win.width - 240f, 34f),
            "Left-click: pick up / place    Right-click: use / equip / place one\nI or Esc: close    F5: save    F9: load", small);

        // --- tooltip & the stack on the cursor ---
        if (cursor.IsEmpty)
        {
            if (hover >= 0 && !Inventory.GetSlot(hover).IsEmpty) ItemUI.Tooltip(e.mousePosition, Inventory.GetSlot(hover).item, W, H);
            else if (hoverEquip != EquipSlot.None && !Inventory.GetEquipped(hoverEquip).IsEmpty) ItemUI.Tooltip(e.mousePosition, Inventory.GetEquipped(hoverEquip).item, W, H);
        }
        else
        {
            ItemUI.Slot(new Rect(e.mousePosition.x - 20f, e.mousePosition.y - 20f, 40f, 40f), cursor, false, null);
        }
        GUI.color = Color.white;
    }
}
