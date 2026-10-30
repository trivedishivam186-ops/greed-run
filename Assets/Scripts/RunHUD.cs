using TMPro;
using UnityEngine;

/// <summary>
/// The on-screen numbers during a run: health, wave, and how much loot is at risk.
/// Goes on the HUD canvas. Any box left empty is simply skipped.
/// </summary>
public class RunHUD : MonoBehaviour
{
    [Header("What to read from")]
    public PlayerHealth playerHealth;
    public LootTrail lootTrail;
    public EnemySpawner spawner;

    [Header("Text boxes to write into")]
    public TMP_Text healthText;
    public TMP_Text waveText;
    public TMP_Text atRiskText;

    void Update()
    {
        if (healthText != null && playerHealth != null)
            healthText.text = $"HP {Mathf.CeilToInt(playerHealth.CurrentHealth)}";

        if (waveText != null && spawner != null)
            waveText.text = $"WAVE {spawner.CurrentWave}";

        if (atRiskText != null && lootTrail != null)
            atRiskText.text = $"AT RISK: {lootTrail.AtRisk}";
    }
}
