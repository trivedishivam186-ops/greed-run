using UnityEngine;

/// <summary>
/// Threat Levels (like Helldivers' difficulty tiers): once your upgrades are maxed,
/// progression moves on to mastery. Higher threat = tougher enemies, richer loot.
/// You unlock the next level by extracting after a set wave on your highest level.
/// The numbers are set from the StashShop's Inspector (designer knobs).
/// </summary>
public static class Threat
{
    const string UnlockedKey = "GreedRun_ThreatUnlocked";
    const string SelectedKey = "GreedRun_ThreatSelected";

    // Set by the StashShop from its Inspector values.
    public static float EnemyBonusPerLevel = 0.3f;
    public static float LootBonusPerLevel = 0.5f;
    public static int UnlockWave = 5;
    public static int MaxLevel = 10;

    /// <summary>Highest level the player has unlocked (starts at 1).</summary>
    public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, MaxLevel);

    /// <summary>The level chosen for this run.</summary>
    public static int Selected
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 1), 1, Unlocked);
        set { PlayerPrefs.SetInt(SelectedKey, Mathf.Clamp(value, 1, Unlocked)); PlayerPrefs.Save(); }
    }

    /// <summary>Enemy health and damage multiplier for the chosen level (Threat 2 = x1.3).</summary>
    public static float EnemyMultiplier => 1f + EnemyBonusPerLevel * (Selected - 1);

    /// <summary>Loot value multiplier for the chosen level (Threat 2 = x1.5).</summary>
    public static float LootMultiplier => 1f + LootBonusPerLevel * (Selected - 1);

    /// <summary>
    /// Called on a successful extraction. Unlocks the next level if the player was on their
    /// highest level and survived long enough. Returns the newly unlocked level, or 0.
    /// </summary>
    public static int TryUnlockNext(int waveReached)
    {
        if (Selected != Unlocked || Unlocked >= MaxLevel || waveReached < UnlockWave) return 0;
        int next = Unlocked + 1;
        PlayerPrefs.SetInt(UnlockedKey, next);
        PlayerPrefs.Save();
        return next;
    }
}
