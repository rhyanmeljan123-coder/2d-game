using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent XP, levels and simple passive bonuses for the six player skills.</summary>
public static class ProgressionSystem
{
    public enum Skill { Farming, Gathering, Combat, Crafting, AnimalCare, Exploration }
    public const int MaxSkillLevel = 10;
    public const int SkillCount = 6;

    [Serializable]
    public class SkillSave
    {
        public int skill;
        public int totalXP;
    }

    static readonly int[] totalXP = new int[SkillCount];
    public static bool PanelOpen { get; private set; }
    public static int TotalXP { get { int total = 0; for (int i = 0; i < totalXP.Length; i++) total += totalXP[i]; return total; } }
    public static int PlayerLevel
    {
        get
        {
            int level = 1, xp = TotalXP;
            while (level < 99 && xp >= XPForPlayerLevel(level)) { xp -= XPForPlayerLevel(level); level++; }
            return level;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Array.Clear(totalXP, 0, totalXP.Length);
        PanelOpen = false;
    }

    public static void TogglePanel() => PanelOpen = !PanelOpen;

    public static string SkillName(Skill skill)
    {
        return skill == Skill.AnimalCare ? "Animal Care" : skill.ToString();
    }

    public static int Level(Skill skill)
    {
        int level = 1, xp = totalXP[(int)skill];
        while (level < MaxSkillLevel && xp >= XPForNext(level)) { xp -= XPForNext(level); level++; }
        return level;
    }

    public static int XPIntoLevel(Skill skill)
    {
        int xp = totalXP[(int)skill];
        for (int level = 1; level < Level(skill); level++) xp -= XPForNext(level);
        return xp;
    }

    public static int XPForNextLevel(Skill skill) =>
        Level(skill) >= MaxSkillLevel ? 0 : XPForNext(Level(skill));

    static int XPForNext(int level) => 100 + (level - 1) * 75;
    static int XPForPlayerLevel(int level) => 150 + (level - 1) * 100;

    public static int CombatDamageBonus => Mathf.Min(3, (Level(Skill.Combat) - 1) / 3);
    public static float BonusHarvestChance => Mathf.Min(0.25f, (Level(Skill.Farming) - 1) * 0.03f);
    public static float BonusGatherChance => Mathf.Min(0.30f, (Level(Skill.Gathering) - 1) * 0.035f);

    public static void AddXP(Skill skill, int amount)
    {
        if (amount <= 0) return;
        int index = (int)skill;
        if (index < 0 || index >= SkillCount || Level(skill) >= MaxSkillLevel) return;
        int oldLevel = Level(skill);
        totalXP[index] = Mathf.Min(10000000, totalXP[index] + amount);
        int newLevel = Level(skill);
        if (newLevel > oldLevel)
            GameHud.Toast(SkillName(skill) + " skill reached level " + newLevel + "!");
        GameHud.Toast("+" + amount + " " + SkillName(skill) + " XP");
    }

    public static List<SkillSave> Capture()
    {
        var data = new List<SkillSave>(SkillCount);
        for (int i = 0; i < SkillCount; i++)
            data.Add(new SkillSave { skill = i, totalXP = totalXP[i] });
        return data;
    }

    public static void Restore(List<SkillSave> data)
    {
        Array.Clear(totalXP, 0, totalXP.Length);
        if (data == null) return;
        foreach (var entry in data)
        {
            if (entry == null || entry.skill < 0 || entry.skill >= SkillCount) continue;
            totalXP[entry.skill] = Mathf.Clamp(entry.totalXP, 0, 10000000);
        }
    }
}
