using System;
using UnityEngine;

public enum Season { Spring, Summer, Autumn, Winter }
public enum Weather { Sunny, Rainy, Stormy, Snowy }

/// <summary>
/// The shared game clock and calendar. Everything that depends on time (crops, animals, shops, events, NPCs) hooks in here.
///
/// A day runs from 6:00 AM until 2:00 AM (you pass out at 2:00 if you are still up). 1 real second = 2 game minutes,
/// so a full day lasts about 10 real minutes. 28 days per season, 4 seasons per year.
/// Weather is rolled per day from the season and the day number, so it is the same every time you play the same day
/// (which is also what lets the forecast tell you tomorrow's weather).
/// </summary>
public static class GameClock
{
    public const int DaysPerSeason = 28;
    public const int DayStartMinute = 6 * 60;       // wake up at 6:00 AM
    public const int DayEndMinute = 26 * 60;        // 2:00 AM: you pass out
    public const int StartMinute = 8 * 60;          // a new game begins at 8:00 AM
    public const float GameMinutesPerRealSecond = 2f;

    /// <summary>Days since the start of the game (0 = first day).</summary>
    public static int TotalDays { get; private set; }
    /// <summary>Game minutes since midnight of the current day. Goes past 1440 after midnight (up to 1560 = 2:00 AM).</summary>
    public static float Minutes { get; private set; } = StartMinute;
    public static Weather Today { get; private set; } = Weather.Sunny;

    public static event Action NewDay;
    public static event Action SeasonChanged;
    public static event Action WeatherChanged;
    /// <summary>Raised when the hour changes (argument: hour 0-23).</summary>
    public static event Action<int> HourChanged;

    static int lastHour;
    public static bool Sleeping { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        TotalDays = 0;
        Minutes = StartMinute;
        Today = Weather.Sunny;
        lastHour = Hour;
        Sleeping = false;
        NewDay = null; SeasonChanged = null; WeatherChanged = null; HourChanged = null;
    }

    // ------------------------------------------------------------ calendar

    public static Season CurrentSeason => SeasonOf(TotalDays);
    public static int DayOfSeason => TotalDays % DaysPerSeason + 1;
    public static int Year => TotalDays / (DaysPerSeason * 4) + 1;
    public static string WeekdayName => new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }[TotalDays % 7];
    public static Season SeasonOf(int day) => (Season)((day / DaysPerSeason) % 4);

    public static int Hour => (int)(Minutes / 60f) % 24;
    public static int MinuteOfHour => (int)Minutes % 60;
    public static float HourFloat => Minutes / 60f;

    /// <summary>True between 8:00 PM and 6:00 AM.</summary>
    public static bool IsNight => Minutes >= 20 * 60;

    public static string TimeText
    {
        get
        {
            int h = Hour, m = MinuteOfHour;
            string ampm = h >= 12 && h < 24 ? "PM" : "AM";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return $"{h12}:{m:00} {ampm}";
        }
    }

    public static string DateText => $"{CurrentSeason} {DayOfSeason}, Year {Year}";

    // ------------------------------------------------------------ gameplay effects

    /// <summary>Multiplies crop growth speed: winter is slow, rain helps.</summary>
    public static float GrowthMultiplier
    {
        get
        {
            float m = 1f;
            if (CurrentSeason == Season.Winter) m *= 0.4f;
            if (Today == Weather.Rainy || Today == Weather.Stormy) m *= 1.4f;
            return m;
        }
    }

    /// <summary>True while the player is standing in an interior (house, dungeon): no weather, no day/night tint.</summary>
    public static bool Outdoors => !CameraFollow.Indoors;

    // ------------------------------------------------------------ time passing

    public static void Tick(float realSeconds)
    {
        Minutes += realSeconds * GameMinutesPerRealSecond;
        int h = Hour;
        if (h != lastHour) { lastHour = h; HourChanged?.Invoke(h); }
    }

    public static bool IsPastBedtime => Minutes >= DayEndMinute;

    /// <summary>Skips to 6:00 AM the next day and rolls the weather. Must run while the screen is black.</summary>
    public static void StartNextDay()
    {
        var oldSeason = CurrentSeason;
        var oldWeather = Today;
        TotalDays++;
        Minutes = DayStartMinute;
        lastHour = Hour;
        Today = RollWeather(TotalDays);
        NewDay?.Invoke();
        if (CurrentSeason != oldSeason) SeasonChanged?.Invoke();
        if (Today != oldWeather) WeatherChanged?.Invoke();
    }

    /// <summary>
    /// Go to bed. A normal sleep (in a bed) heals fully; passing out at 2:00 AM heals half and wakes you at the farm.
    /// </summary>
    public static void Sleep(bool passedOut)
    {
        if (GameHud.IsFading || Sleeping) return;
        Sleeping = true;
        GameHud.Fade(() =>
        {
            StartNextDay();
            CropPlot.GrowAll(Bed.SleepSeconds);

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                if (passedOut)
                {
                    player.Health.Heal(player.Health.max / 2);
                    var home = GameSetup.PlayerStart;
                    var body = player.GetComponent<Rigidbody2D>();
                    body.linearVelocity = Vector2.zero;
                    body.position = home;
                    player.transform.position = home;
                }
                else player.Health.Heal(player.Health.max);
            }

            GameHud.Toast(passedOut ? "You stayed up too late and passed out..." : "You slept well.");
            GameHud.Toast($"{WeekdayName}, {DateText}.  Weather today: {Today}.  Tomorrow: {ForecastTomorrow}");
            SaveSystem.Save();
            Sleeping = false;
        });
    }

    // ------------------------------------------------------------ weather

    public static Weather ForecastTomorrow => RollWeather(TotalDays + 1);

    /// <summary>Deterministic: the same day number always gives the same weather.</summary>
    public static Weather RollWeather(int day)
    {
        if (day == 0) return Weather.Sunny; // a gentle first day
        int r = new System.Random(day * 7919 + 13).Next(100);
        switch (SeasonOf(day))
        {
            case Season.Spring: return r < 55 ? Weather.Sunny : r < 90 ? Weather.Rainy : Weather.Stormy;
            case Season.Summer: return r < 70 ? Weather.Sunny : r < 85 ? Weather.Rainy : Weather.Stormy;
            case Season.Autumn: return r < 45 ? Weather.Sunny : r < 85 ? Weather.Rainy : Weather.Stormy;
            default:            return r < 50 ? Weather.Sunny : Weather.Snowy; // winter
        }
    }

    // ------------------------------------------------------------ saving

    public static void Restore(int day, float minutes)
    {
        var oldSeason = CurrentSeason;
        var oldWeather = Today;
        TotalDays = Mathf.Max(0, day);
        Minutes = minutes < 1f ? StartMinute : Mathf.Clamp(minutes, DayStartMinute, DayEndMinute - 1);
        lastHour = Hour;
        Today = RollWeather(TotalDays);
        if (CurrentSeason != oldSeason) SeasonChanged?.Invoke();
        if (Today != oldWeather) WeatherChanged?.Invoke();
    }
}

/// <summary>Ticks the clock every frame (it pauses while menus or fades are open) and passes you out at 2:00 AM.</summary>
public class GameClockRunner : MonoBehaviour
{
    void Update()
    {
        if (GameHud.UiOpen || GameHud.IsFading || GameClock.Sleeping) return;
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null || player.Health.Dead) return;

        GameClock.Tick(Time.deltaTime);
        if (GameClock.IsPastBedtime) GameClock.Sleep(true);
    }
}
