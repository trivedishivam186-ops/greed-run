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

    // What the HUD reads.
    public bool IsOpen { get; private set; }
    public float TimeLeftOpen { get; private set; }
    public float TimeUntilOpen { get; private set; }
    public float ExtractProgress { get; private set; } // 0..1 while standing on the pad
    public float GreedMultiplier { get; private set; } = 1f;
    public int EvacsSkipped { get; private set; }
    public bool RunOver { get; private set; }

    /// <summary>The loot at risk right now, including the greed multiplier.</summary>
    public int AtRiskWithGreed => lootTrail != null ? Mathf.RoundToInt(lootTrail.AtRisk * GreedMultiplier) : 0;

    // What you'd have banked if you'd left at the last evac (for the death screen).
    int bankableAtLastEvac;

    void Start()
    {
        Time.timeScale = 1f; // in case the last run ended frozen
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

        if (endScreen != null)
            endScreen.Show("EXTRACTED",
                $"BANKED  +{banked}\n" +
                $"GREED  x{GreedMultiplier:0.0}   ·   EVACS SKIPPED  {EvacsSkipped}\n\n" +
                $"STASH  {stash}");
    }

    void EndRunDied()
    {
        RunOver = true;
        if (evacPad != null) evacPad.gameObject.SetActive(false);

        int lost = lootTrail != null
            ? Mathf.RoundToInt(Mathf.Max(lootTrail.AtRisk, lootTrail.LostOnDeath) * GreedMultiplier)
            : 0;

        string body = $"LOST  {lost}\n";
        if (EvacsSkipped > 0 && bankableAtLastEvac > 0)
            body += $"If you'd left at the last evac, you'd have banked {bankableAtLastEvac}.\n";
        body += $"\nSTASH  {Stash.Total}";

        if (endScreen != null) endScreen.Show("YOU WERE OVERRUN", body);
    }

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
