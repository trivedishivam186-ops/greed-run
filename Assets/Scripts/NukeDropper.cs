using UnityEngine;

/// <summary>
/// Decides when a nuke drops. It's rare, never more than one on the floor,
/// and there's a minimum gap between nukes so they stay special.
/// It also helps when you're swamped: the more enemies alive, the higher the chance.
/// Put this on any object in the scene (e.g. the EnemySpawner).
/// </summary>
public class NukeDropper : MonoBehaviour
{
    [Tooltip("The nuke prefab.")]
    public NukePickup nukePrefab;

    [Tooltip("The Player object.")]
    public Transform player;

    [Header("Drop rules (designer knobs)")]
    [Tooltip("Base chance per kill (0.01 = 1%).")]
    [Range(0f, 0.2f)]
    public float baseChance = 0.01f;

    [Tooltip("Extra chance per kill when the horde is big. Added in full when this many enemies are alive.")]
    public int bigHordeSize = 80;

    [Tooltip("Extra chance at a full-size horde (0.03 = +3%).")]
    [Range(0f, 0.2f)]
    public float bigHordeBonusChance = 0.03f;

    [Tooltip("Seconds after a nuke drops before another can drop.")]
    public float minTimeBetweenNukes = 40f;

    [Tooltip("Seconds into the run before the first nuke can drop.")]
    public float earliestNuke = 30f;

    float lastDropTime = -999f;
    float runStart;

    void Start() { runStart = Time.time; }

    void OnEnable()  { Enemy.OnAnyEnemyKilled += HandleKill; }
    void OnDisable() { Enemy.OnAnyEnemyKilled -= HandleKill; }

    void HandleKill(Vector3 pos)
    {
        if (nukePrefab == null || Enemy.NukeInProgress || NukePickup.AnyOnFloor) return;
        if (Time.time - runStart < earliestNuke) return;
        if (Time.time - lastDropTime < minTimeBetweenNukes) return;

        float hordeFactor = Mathf.Clamp01(Enemy.All.Count / (float)Mathf.Max(1, bigHordeSize));
        float chance = baseChance + bigHordeBonusChance * hordeFactor;
        if (Random.value > chance) return;

        NukePickup n = Instantiate(nukePrefab, new Vector3(pos.x, 0.9f, pos.z), Quaternion.identity);
        n.Init(player);
        lastDropTime = Time.time;
        Debug.Log("A nuke dropped!");
    }
}
