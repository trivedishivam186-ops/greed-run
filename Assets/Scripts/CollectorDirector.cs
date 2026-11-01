using TMPro;
using UnityEngine;

/// <summary>
/// The AI Director. Once a second it looks at how greedy the player is being and decides
/// whether to send a Collector after their loot.
///
/// Greed score (0 = careful, 1 = very greedy) is built from:
///   - how much loot is at risk right now (including the greed multiplier)
///   - how many evacs they've skipped this run
///   - how long their tail is
/// The higher the score, the higher the chance each second. Only one Collector at a time,
/// with a cooldown, so it's a "moment" and not spam.
/// It's rule-based on purpose: cheap, predictable, works offline and needs no API key.
/// Put this on an empty "CollectorDirector" object.
/// </summary>
public class CollectorDirector : MonoBehaviour
{
    [Header("References")]
    public Collector collectorPrefab;
    public LootCore lootPrefab;
    public LootTrail lootTrail;
    public PlayerHealth player;
    public EvacManager evac;
    public EnemySpawner spawner;
    public Canvas canvas;
    [Tooltip("Optional: any HUD text, so messages use the same font.")]
    public TMP_Text fontSource;

    [Header("Greed score (designer knobs)")]
    [Tooltip("Loot at risk that counts as 'fully greedy' on its own.")]
    public int atRiskForFullScore = 400;
    [Tooltip("How much each skipped evac adds to the score.")]
    public float scorePerSkippedEvac = 0.25f;
    [Tooltip("Tail length that counts as 'fully greedy' on its own.")]
    public int tailForFullScore = 30;

    [Header("Sending a Collector (designer knobs)")]
    [Tooltip("Never sends one unless the tail has at least this many cores.")]
    public int minTailCores = 8;
    [Tooltip("Chance per second at a greed score of 1 (0.25 = 25%).")]
    [Range(0f, 1f)] public float chancePerSecondAtFullGreed = 0.25f;
    [Tooltip("Seconds into the run before the first Collector can come.")]
    public float earliest = 40f;
    [Tooltip("Seconds after one dies or escapes before another can come.")]
    public float cooldown = 30f;
    [Tooltip("Each wave makes Collectors this much faster (0.03 = +3%).")]
    public float speedPerWave = 0.03f;

    /// <summary>The latest greed score, 0..1 (handy for showing on screen or logging).</summary>
    public float GreedScore { get; private set; }

    public static event System.Action OnCollectorSent;

    float runTime, nextCheck, readyAt;
    bool wasAlive;
    TMP_Text message;
    float messageTimer;

    void Awake() { BuildMessage(); }

    void Update()
    {
        if (player == null || player.IsDead || (evac != null && evac.RunOver)) return;
        runTime += Time.deltaTime;

        // When a Collector dies or escapes, start the cooldown.
        if (wasAlive && !Collector.AnyAlive) readyAt = runTime + cooldown;
        wasAlive = Collector.AnyAlive;

        UpdateMessage();

        if (runTime < nextCheck) return;
        nextCheck = runTime + 1f;

        GreedScore = CalculateGreed();

        if (Collector.AnyAlive || runTime < earliest || runTime < readyAt) return;
        if (lootTrail == null || lootTrail.Cores.Count < minTailCores) return;

        if (Random.value < GreedScore * chancePerSecondAtFullGreed)
            Send();
    }

    float CalculateGreed()
    {
        if (lootTrail == null) return 0f;
        int atRisk = evac != null ? evac.AtRiskWithGreed : lootTrail.AtRisk;
        float fromLoot = atRisk / (float)Mathf.Max(1, atRiskForFullScore);
        float fromSkips = evac != null ? evac.EvacsSkipped * scorePerSkippedEvac : 0f;
        float fromTail = lootTrail.Cores.Count / (float)Mathf.Max(1, tailForFullScore);
        // Average of loot and tail, plus skipped evacs on top.
        return Mathf.Clamp01((fromLoot + fromTail) * 0.5f + fromSkips);
    }

    void Send()
    {
        if (collectorPrefab == null) return;

        // Come in from the edge furthest from the player, so you see it coming.
        Vector3 p = player.transform.position;
        Vector3 spawn = new Vector3(p.x > 0 ? -30f : 30f, 0f, Random.Range(-20f, 20f));
        if (Random.value < 0.5f) spawn = new Vector3(Random.Range(-20f, 20f), 0f, p.z > 0 ? -30f : 30f);

        Collector c = Instantiate(collectorPrefab, spawn, Quaternion.identity);
        int wave = spawner != null ? spawner.CurrentWave : 1;
        c.GetComponent<Enemy>().Init(player.transform, player, 1f + speedPerWave * (wave - 1));
        c.Init(lootTrail, lootPrefab, player.transform, fontSource);

        Debug.Log($"Director: greed score {GreedScore:0.00} - sending a Collector.");
        ShowMessage("THE DIRECTOR NOTICED YOUR GREED");
        OnCollectorSent?.Invoke();
    }

    // ---------- On-screen message ----------

    void BuildMessage()
    {
        if (canvas == null) return;
        GameObject go = new GameObject("DirectorMessage", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 60f);
        rt.anchoredPosition = new Vector2(0f, 210f);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fontSource != null) t.font = fontSource.font;
        t.fontSize = 26f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.85f, 0.4f, 1f, 0f);
        t.raycastTarget = false;
        message = t;
    }

    void ShowMessage(string text)
    {
        if (message == null) return;
        message.text = text;
        messageTimer = 3f;
    }

    void UpdateMessage()
    {
        if (message == null || messageTimer <= 0f) return;
        messageTimer -= Time.deltaTime;
        Color c = message.color;
        c.a = Mathf.Clamp01(messageTimer); // fades out over the last second
        message.color = c;
    }
}
