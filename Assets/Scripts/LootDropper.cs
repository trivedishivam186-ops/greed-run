using UnityEngine;

/// <summary>
/// Listens for enemy deaths and drops data cores where they died.
/// Put this on any object in the scene (e.g. the EnemySpawner).
/// </summary>
public class LootDropper : MonoBehaviour
{
    [Tooltip("The data core prefab.")]
    public LootCore lootPrefab;

    [Header("Drops (designer knobs)")]
    [Tooltip("Chance an enemy drops a core when it dies (0.7 = 70%).")]
    [Range(0f, 1f)]
    public float dropChance = 0.7f;

    [Tooltip("How far the core slides away from where the enemy died.")]
    public float scatterDistance = 0.8f;

    void OnEnable()  { Enemy.OnAnyEnemyKilled += HandleEnemyKilled; }
    void OnDisable() { Enemy.OnAnyEnemyKilled -= HandleEnemyKilled; }

    void HandleEnemyKilled(Vector3 position)
    {
        if (lootPrefab == null) return;
        if (Random.value > dropChance) return;

        LootCore core = Instantiate(lootPrefab);
        core.DropOnFloor(position, scatterDistance, 0f);
    }
}
