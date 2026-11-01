using TMPro;
using UnityEngine;

/// <summary>
/// The Collector: a fast enemy sent by the AI Director when you get greedy.
/// It ignores you and goes for your loot tail. Every core it touches, it steals and carries.
///  - Kill it: it drops everything it stole.
///  - Let it steal its fill: it runs for the arena edge. If it escapes, that loot is gone.
///  - If your tail is empty, it comes for you instead.
/// Goes on the Collector prefab, next to an Enemy component.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class Collector : MonoBehaviour
{
    [Header("Stealing (designer knobs)")]
    [Tooltip("How close it must get to a core to steal it.")]
    public float stealRadius = 1.1f;

    [Tooltip("Seconds between steals.")]
    public float stealCooldown = 0.3f;

    [Tooltip("After stealing this many cores, it runs for the edge.")]
    public int coresBeforeFleeing = 8;

    [Tooltip("It escapes (and the loot is gone) once it's this far from the centre.")]
    public float escapeDistance = 31f;

    [Header("Label")]
    public float labelHeight = 2.4f;

    /// <summary>Fires when it escapes with loot (value lost).</summary>
    public static event System.Action<int> OnEscaped;

    /// <summary>Is a Collector alive right now? The Director only sends one at a time.</summary>
    public static bool AnyAlive { get; private set; }

    Enemy enemy;
    LootTrail trail;
    LootCore lootPrefab;
    Transform player;
    Transform fleePoint;
    int stolenValue, stolenCount;
    float nextSteal;
    bool fleeing;
    TextMeshPro label;
    Camera cam;

    public void Init(LootTrail lootTrail, LootCore corePrefab, Transform playerTransform, TMP_Text fontSource)
    {
        trail = lootTrail;
        lootPrefab = corePrefab;
        player = playerTransform;

        // A floating "COLLECTOR" label so the player knows what this is.
        GameObject lg = new GameObject("Label");
        lg.transform.SetParent(transform, false);
        label = lg.AddComponent<TextMeshPro>();
        if (fontSource != null) label.font = fontSource.font;
        label.fontSize = 5f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.4f, 1f);
        label.rectTransform.sizeDelta = new Vector2(10f, 2f);
    }

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemy.knocksLoot = false;
        cam = Camera.main;
        fleePoint = new GameObject("CollectorFleePoint").transform;
    }

    void OnEnable()
    {
        AnyAlive = true;
        GetComponent<Enemy>().OnDied += HandleDied;
    }

    void OnDisable()
    {
        AnyAlive = false;
        if (enemy != null) enemy.OnDied -= HandleDied;
    }

    void OnDestroy()
    {
        if (fleePoint != null) Destroy(fleePoint.gameObject);
    }

    void Update()
    {
        if (enemy.IsDead) return;

        if (fleeing) { Flee(); }
        else { Hunt(); }

        // Keep the label above it and facing the camera.
        if (label != null)
        {
            label.transform.position = transform.position + Vector3.up * labelHeight;
            if (cam != null) label.transform.rotation = cam.transform.rotation;
            label.text = stolenCount > 0 ? $"COLLECTOR  <size=80%>+{stolenValue}</size>" : "COLLECTOR";
        }
    }

    void Hunt()
    {
        LootCore target = trail != null ? trail.NearestCore(transform.position) : null;

        if (target == null)
        {
            // Nothing to steal: come for the player instead.
            enemy.chaseTarget = null;
            enemy.hurtsPlayer = true;
            return;
        }

        enemy.chaseTarget = target.transform;
        enemy.hurtsPlayer = false;

        Vector3 d = target.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude < stealRadius && Time.time >= nextSteal)
        {
            int v = trail.StealCore(target);
            if (v > 0)
            {
                stolenValue += v;
                stolenCount++;
                nextSteal = Time.time + stealCooldown;
                if (stolenCount >= coresBeforeFleeing) StartFleeing();
            }
        }
    }

    void StartFleeing()
    {
        fleeing = true;
        enemy.hurtsPlayer = false;

        // Run straight away from the player, out past the arena edge.
        Vector3 away = transform.position - (player != null ? player.position : Vector3.zero);
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
        fleePoint.position = transform.position + away.normalized * 60f;
        enemy.chaseTarget = fleePoint;
        Debug.Log($"Collector is escaping with {stolenValue}!");
    }

    void Flee()
    {
        Vector3 p = transform.position;
        if (Mathf.Abs(p.x) > escapeDistance || Mathf.Abs(p.z) > escapeDistance)
        {
            Debug.Log($"Collector escaped. {stolenValue} loot gone.");
            OnEscaped?.Invoke(stolenValue);
            Destroy(gameObject);
        }
    }

    void HandleDied(Enemy e)
    {
        // Killed: spill everything it stole.
        if (lootPrefab == null) return;
        for (int i = 0; i < stolenCount; i++)
        {
            LootCore c = Instantiate(lootPrefab);
            c.DropOnFloor(transform.position, 2.5f, 0.3f);
        }
        if (stolenCount > 0) Debug.Log($"Collector down! Dropped {stolenValue}.");
    }
}
