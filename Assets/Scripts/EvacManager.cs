using UnityEngine;

/// <summary>
/// The greed moment. Every so often an evac pad opens somewhere in the arena for a few seconds.
///  - Stand on it to extract: your tail is banked (times the greed multiplier) and the run ends.
///  - Let it close: the greed multiplier goes up (loot is worth more) but the horde jumps ahead a wave.
///  - Die: you lose everything you were carrying.
/// Put this on an empty "EvacManager" object.
/// </summary>
public class EvacManager : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth player;
    public LootTrail lootTrail;
    public EnemySpawner spawner;
    public RunEndScreen endScreen;

    [Tooltip("The evac pad object in the scene. It's hidden while closed.")]
    public Transform evacPad;

    [Header("Timing (designer knobs)")]
    [Tooltip("Seconds before the first evac opens.")]
    public float firstEvacAt = 45f;

    [Tooltip("Seconds between one evac closing and the next one opening.")]
    public float timeBetweenEvacs = 50f;

    [Tooltip("How long the evac stays open.")]
    public float openDuration = 10f;

    [Tooltip("Seconds you must stand on the pad to extract (makes leaving a commitment, not a free exit).")]
    public float extractHoldTime = 1f;

    [Tooltip("How close to the pad's centre counts as standing on it.")]
    public float padRadius = 2.5f;

    [Header("Where it opens")]
    [Tooltip("The pad opens at least this far from the player.")]
    public float minDistanceFromPlayer = 8f;

    [Tooltip("...and at most this far.")]
    public float maxDistanceFromPlayer = 18f;

    [Tooltip("Keep the pad this far inside the arena (the edge is at 28).")]
    public float arenaHalfSize = 22f;

    [Header("Greed (designer knobs)")]
    [Tooltip("Added to the greed multiplier every time you let an evac close.")]
    public float greedPerSkip = 0.5f;

    [Tooltip("Waves the horde jumps ahead every time you let an evac close.")]
    public int wavesPerSkip = 1;

    // Events for sounds and effects.
    public static event System.Action OnEvacOpened;
    public static event System.Action OnEvacMissed;
    public static event System.Action<Vector3> OnExtracted;
    public static event System.Action OnNewBest;

    // What the HUD reads.
    public bool IsOpen { get; private set; }
    public float TimeLeftOpen { get; private set; }
    public float TimeUntilOpen { get; private set; }
    public float ExtractProgress { get; private set; } // 0..1 while standing on the pad
    public float GreedMultiplier { get; private set; } = 1f;
    public int EvacsSkipped { get; private set; }
    public bool RunOver { get; private set; }

    /// <summary>The loot at risk right now, including the greed multiplier.</summary>
    public int AtRiskWithGreed => lootTrail != null ? Mathf.RoundToInt(lootTrail.AtRisk * GreedMultiplier * Threat.LootMultiplier) : 0;

    // What you'd have banked if you'd left at the last evac (for the death screen).
    int bankableAtLastEvac;

    void Start()
    {
        TimeUntilOpen = firstEvacAt;
        if (evacPad != null) evacPad.gameObject.SetActive(false);
    }

    void Update()
    {
        if (RunOver || player == null) return;

        if (player.IsDead)
        {
            EndRunDied();
            return;
        }

        if (!IsOpen)
        {
            TimeUntilOpen -= Time.deltaTime;
            if (TimeUntilOpen <= 0f) Open();
            return;
        }

        // The pad is open.
        TimeLeftOpen -= Time.deltaTime;

        Vector3 d = player.transform.position - evacPad.position;
        d.y = 0f;
        bool onPad = d.magnitude < padRadius;

        if (onPad)
        {
            ExtractProgress += Time.deltaTime / Mathf.Max(0.01f, extractHoldTime);
            if (ExtractProgress >= 1f)
            {
                Extract();
                return;
            }
        }
        else
        {
            ExtractProgress = 0f; // stepped off - start again
        }

        if (TimeLeftOpen <= 0f) CloseSkipped();
    }

    void Open()
    {
        IsOpen = true;
        TimeLeftOpen = openDuration;
        ExtractProgress = 0f;
        bankableAtLastEvac = AtRiskWithGreed;

        if (evacPad != null)
        {
            evacPad.position = PickPadPosition();
            evacPad.gameObject.SetActive(true);
        }
        Debug.Log("EVAC OPEN - extract or stay?");
        OnEvacOpened?.Invoke();
    }

    void CloseSkipped()
    {
        IsOpen = false;
        ExtractProgress = 0f;
        if (evacPad != null) evacPad.gameObject.SetActive(false);

        EvacsSkipped++;
        GreedMultiplier += greedPerSkip;
        if (spawner != null) spawner.AddWaves(wavesPerSkip);

        TimeUntilOpen = timeBetweenEvacs;
        Debug.Log($"Evac missed. Greed x{GreedMultiplier}");
        OnEvacMissed?.Invoke();
    }

    void Extract()
    {
        RunOver = true;
        IsOpen = false;

        int banked = AtRiskWithGreed;
        OnExtracted?.Invoke(player.transform.position);
        if (lootTrail != null) lootTrail.BankAll();

        int stash = Stash.Add(banked);
        Debug.Log($"Extracted! Banked {banked}. Stash now {stash}.");

        int wave = spawner != null ? spawner.CurrentWave : 0;
        bool bestWave = Stash.TryRecordWave(wave);
        bool bestBank = Stash.TryRecordBank(banked);
        int unlocked = Threat.TryUnlockNext(wave);

        string body =
            $"BANKED  +{banked}{NewBest(bestBank)}\n" +
            $"WAVE  {wave}{NewBest(bestWave)}   ·   THREAT  {Threat.Selected}\n" +
            $"GREED  x{GreedMultiplier:0.0}   ·   EVACS SKIPPED  {EvacsSkipped}\n" +
            (unlocked > 0 ? $"<color=#FF3C50>THREAT {unlocked} UNLOCKED</color>\n" : "") + "\n" +
            Records() + $"\nSTASH  {stash}";

        if (endScreen != null) endScreen.Show(bestBank || bestWave ? "NEW PERSONAL BEST" : "EXTRACTED", body);
        if (bestBank || bestWave || unlocked > 0) OnNewBest?.Invoke();
    }

    void EndRunDied()
    {
        RunOver = true;
        if (evacPad != null) evacPad.gameObject.SetActive(false);

        int lost = lootTrail != null
            ? Mathf.RoundToInt(Mathf.Max(lootTrail.AtRisk, lootTrail.LostOnDeath) * GreedMultiplier * Threat.LootMultiplier)
            : 0;

        int wave = spawner != null ? spawner.CurrentWave : 0;
        bool bestWave = Stash.TryRecordWave(wave);

        // Black Box upgrade: keep a share of what you were carrying.
        int kept = Mathf.RoundToInt(lost * StashShop.KeepOnDeath);
        if (kept > 0)
        {
            Stash.Add(kept);
            lost -= kept;
        }

        string body = $"LOST  {lost}\n";
        if (kept > 0) body += $"<color=#FFC21A>BLACK BOX RECOVERED  +{kept}</color>\n";
        body += $"WAVE  {wave}{NewBest(bestWave)}\n";
        if (EvacsSkipped > 0 && bankableAtLastEvac > 0)
            body += $"If you'd left at the last evac, you'd have banked {bankableAtLastEvac}.\n";
        body += "\n" + Records() + $"\nSTASH  {Stash.Total}";

        if (endScreen != null) endScreen.Show("YOU WERE OVERRUN", body);
        if (bestWave) OnNewBest?.Invoke();
    }

    static string NewBest(bool isNew) => isNew ? "  <color=#00E5FF>NEW BEST!</color>" : "";

    static string Records() =>
        $"<size=80%><color=#9AA4B2>BEST WAVE  {Stash.BestWave}   ·   BEST BANK  {Stash.BestBank}</color></size>";

    Vector3 PickPadPosition()
    {
        Vector3 p = player.transform.position;
        for (int i = 0; i < 30; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            Vector3 candidate = new Vector3(p.x + dir.x * dist, 0.03f, p.z + dir.y * dist);
            if (Mathf.Abs(candidate.x) <= arenaHalfSize && Mathf.Abs(candidate.z) <= arenaHalfSize)
                return candidate;
        }
        // Fallback: towards the centre.
        return new Vector3(-p.x * 0.5f, 0.03f, -p.z * 0.5f);
    }
}
