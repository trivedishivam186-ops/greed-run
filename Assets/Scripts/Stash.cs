using UnityEngine;

/// <summary>
/// The permanent bank of loot, saved between runs.
/// Saved in the browser on the web build, so it survives closing the tab.
/// </summary>
public static class Stash
{
    const string Key = "GreedRun_Stash";

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
