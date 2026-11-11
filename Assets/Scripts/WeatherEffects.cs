using UnityEngine;

/// <summary>
/// Draws the look of the time of day, season and weather on top of the world (but under the HUD):
/// a day/night tint, a faint seasonal tint, rain, snow, storms with lightning.
/// Nothing is drawn indoors (farmhouse, dungeon).
/// </summary>
public class WeatherEffects : MonoBehaviour
{
    struct Key { public float hour; public Color color; public Key(float h, Color c) { hour = h; color = c; } }

    // time-of-day tint: hour (6-26), colour (alpha = strength)
    static readonly Key[] DayKeys =
    {
        new Key(6.0f,  new Color(0.10f, 0.15f, 0.38f, 0.45f)),  // before dawn
        new Key(7.5f,  new Color(1.00f, 0.70f, 0.40f, 0.12f)),  // sunrise
        new Key(9.0f,  new Color(1.00f, 1.00f, 1.00f, 0.00f)),  // day
        new Key(16.5f, new Color(1.00f, 1.00f, 1.00f, 0.00f)),
        new Key(18.0f, new Color(1.00f, 0.55f, 0.25f, 0.18f)),  // sunset
        new Key(19.5f, new Color(0.15f, 0.12f, 0.35f, 0.40f)),  // dusk
        new Key(21.0f, new Color(0.05f, 0.08f, 0.25f, 0.55f)),  // night
        new Key(26.0f, new Color(0.05f, 0.08f, 0.25f, 0.58f)),
    };

    float nextLightning = 8f, lightningUntil;
    Texture2D white;

    static Color TimeTint(float hour)
    {
        hour = Mathf.Clamp(hour, DayKeys[0].hour, DayKeys[DayKeys.Length - 1].hour);
        for (int i = 0; i < DayKeys.Length - 1; i++)
        {
            if (hour > DayKeys[i + 1].hour) continue;
            float t = Mathf.InverseLerp(DayKeys[i].hour, DayKeys[i + 1].hour, hour);
            return Color.Lerp(DayKeys[i].color, DayKeys[i + 1].color, t);
        }
        return DayKeys[DayKeys.Length - 1].color;
    }

    static Color SeasonTint(Season s)
    {
        switch (s)
        {
            case Season.Summer: return new Color(1.0f, 0.92f, 0.55f, 0.05f);
            case Season.Autumn: return new Color(1.0f, 0.60f, 0.25f, 0.08f);
            case Season.Winter: return new Color(0.80f, 0.90f, 1.00f, 0.12f);
            default: return new Color(1f, 1f, 1f, 0f);
        }
    }

    void Update()
    {
        if (!GameClock.Outdoors || GameClock.Today != Weather.Stormy) return;
        if (Time.time >= nextLightning)
        {
            lightningUntil = Time.time + 0.18f;
            nextLightning = Time.time + Random.Range(6f, 15f);
        }
    }

    void OnGUI()
    {
        if (!GameClock.Outdoors) return;
        if (white == null) white = Texture2D.whiteTexture;
        float W = Screen.width, H = Screen.height;
        GUI.depth = 100; // behind the HUD

        // day / night
        var tint = TimeTint(GameClock.HourFloat);
        Fill(W, H, tint);

        // season
        Fill(W, H, SeasonTint(GameClock.CurrentSeason));

        // weather
        switch (GameClock.Today)
        {
            case Weather.Rainy:
                Fill(W, H, new Color(0.25f, 0.30f, 0.40f, 0.14f));
                DrawRain(W, H, 200, 700f);
                break;
            case Weather.Stormy:
                Fill(W, H, new Color(0.12f, 0.14f, 0.24f, 0.28f));
                DrawRain(W, H, 360, 980f);
                if (Time.time < lightningUntil)
                {
                    float flick = (Mathf.Sin(Time.time * 90f) > 0f) ? 0.55f : 0.25f;
                    Fill(W, H, new Color(0.95f, 0.95f, 1f, flick));
                }
                break;
            case Weather.Snowy:
                Fill(W, H, new Color(0.85f, 0.92f, 1f, 0.06f));
                DrawSnow(W, H, 150);
                break;
        }
        GUI.color = Color.white;
    }

    void Fill(float W, float H, Color c)
    {
        if (c.a <= 0.001f) return;
        GUI.color = c;
        GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
    }

    // Cheap pseudo-random in [0,1) from an index, so particles don't need any per-particle state
    static float Hash(int i, int salt)
    {
        unchecked
        {
            uint x = (uint)(i * 73856093 ^ salt * 19349663);
            x ^= x >> 13; x *= 1274126177u; x ^= x >> 16;
            return (x & 0xFFFF) / 65536f;
        }
    }

    void DrawRain(float W, float H, int count, float speed)
    {
        GUI.color = new Color(0.78f, 0.86f, 1f, 0.45f);
        float t = Time.time;
        float scale = H / 540f;
        for (int i = 0; i < count; i++)
        {
            float x = Hash(i, 1) * W;
            float v = speed * (0.8f + 0.4f * Hash(i, 2)) * scale;
            float y = (Hash(i, 3) * H + t * v) % (H + 20f) - 20f;
            GUI.DrawTexture(new Rect(x, y, 1.5f * scale, (10f + 8f * Hash(i, 4)) * scale), white);
        }
    }

    void DrawSnow(float W, float H, int count)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        float t = Time.time;
        float scale = H / 540f;
        for (int i = 0; i < count; i++)
        {
            float v = (40f + 60f * Hash(i, 2)) * scale;
            float x = Hash(i, 1) * W + Mathf.Sin(t * (0.8f + Hash(i, 5)) + i) * 18f * scale;
            float y = (Hash(i, 3) * H + t * v) % (H + 10f) - 10f;
            float size = (2f + 2.5f * Hash(i, 4)) * scale;
            GUI.DrawTexture(new Rect(x, y, size, size), white);
        }
    }
}
