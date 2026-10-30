using UnityEngine;

/// <summary>
/// The permanent bank of loot, saved between runs.
/// Saved in the browser on the web build, so it survives closing the tab.
/// </summary>
public static class Stash
{
    const string Key = "GreedRun_Stash";
    const string BestWaveKey = "GreedRun_BestWave";
    const string BestBankKey = "GreedRun_BestBank";

    /// <summary>Highest wave ever reached (extracted or not).</summary>
    public static int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);

    /// <summary>Most loot ever banked in a single run.</summary>
    public static int BestBank => PlayerPrefs.GetInt(BestBankKey, 0);

    /// <summary>Saves the wave if it's a new record. Returns true if it was.</summary>
    public static bool TryRecordWave(int wave)
    {
        if (wave <= BestWave) return false;
        PlayerPrefs.SetInt(BestWaveKey, wave);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Saves the banked amount if it's a new record. Returns true if it was.</summary>
    public static bool TryRecordBank(int amount)
    {
        if (amount <= BestBank) return false;
        PlayerPrefs.SetInt(BestBankKey, amount);
        PlayerPrefs.Save();
        return true;
    }

    public static int Total => PlayerPrefs.GetInt(Key, 0);

    /// <summary>Adds to the stash, saves it, and returns the new total.</summary>
    public static int Add(int amount)
    {
        int total = Total + Mathf.Max(0, amount);
        PlayerPrefs.SetInt(Key, total);
        PlayerPrefs.Save();
        return total;
    }

    /// <summary>Spends from the stash if there's enough. Returns true if it worked.</summary>
    public static bool Spend(int amount)
    {
        if (Total < amount) return false;
        PlayerPrefs.SetInt(Key, Total - amount);
        PlayerPrefs.Save();
        return true;
    }
}
