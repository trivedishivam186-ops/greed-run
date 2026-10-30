using TMPro;
using UnityEngine;

/// <summary>
/// The on-screen numbers during a run: health, wave, loot at risk, greed and the evac banner.
/// Goes on the HUD canvas. Any box left empty is simply skipped.
/// </summary>
public class RunHUD : MonoBehaviour
{
    [Header("What to read from")]
    public PlayerHealth playerHealth;
    public LootTrail lootTrail;
    public EnemySpawner spawner;
    public EvacManager evac;

    [Header("Text boxes to write into")]
    public TMP_Text healthText;
    public TMP_Text waveText;
    public TMP_Text atRiskText;
    public TMP_Text greedText;

    [Tooltip("The big banner text in the middle-top of the screen.")]
    public TMP_Text evacText;

    void Update()
    {
        if (healthText != null && playerHealth != null)
            healthText.text = $"HP {Mathf.CeilToInt(playerHealth.CurrentHealth)}";

        if (waveText != null && spawner != null)
        {
            if (spawner.State == EnemySpawner.WaveState.Breather)
            {
                int next = Mathf.CeilToInt(spawner.BreatherTimeLeft);
                waveText.text = spawner.CurrentWave == 0
                    ? $"GET READY  {next}"
                    : $"WAVE {spawner.CurrentWave} CLEARED\n<size=70%>NEXT IN {next}</size>";
            }
            else
            {
                // Once you pass your best wave, the HUD tells you - a reason to keep going.
                string best = Stash.BestWave > 0
                    ? (spawner.CurrentWave > Stash.BestWave ? "  <color=#00E5FF>NEW BEST</color>" : $"  ·  BEST {Stash.BestWave}")
                    : "";
                waveText.text = $"WAVE {spawner.CurrentWave}\n<size=70%>{spawner.EnemiesRemaining} LEFT{best}</size>";
            }
        }

        if (atRiskText != null && lootTrail != null)
        {
            int atRisk = evac != null ? evac.AtRiskWithGreed : lootTrail.AtRisk;
            atRiskText.text = $"AT RISK: {atRisk}";
        }

        if (greedText != null && evac != null)
        {
            bool greedy = evac.GreedMultiplier > 1.001f;
            greedText.gameObject.SetActive(greedy);
            if (greedy) greedText.text = $"GREED x{evac.GreedMultiplier:0.0}";
        }

        if (evacText != null && evac != null)
        {
            if (evac.RunOver)
            {
                evacText.text = "";
            }
            else if (evac.IsOpen)
            {
                int secs = Mathf.CeilToInt(evac.TimeLeftOpen);
                if (evac.ExtractProgress > 0f)
                    evacText.text = $"EXTRACTING... {Mathf.RoundToInt(evac.ExtractProgress * 100f)}%";
                else
                    evacText.text = $"<color=#FF3C50>EVAC OPEN  {secs}</color>\n<size=70%>EXTRACT OR STAY?</size>";
            }
            else
            {
                evacText.text = $"<size=70%>NEXT EVAC IN {Mathf.CeilToInt(evac.TimeUntilOpen)}</size>";
            }
        }
    }
}
