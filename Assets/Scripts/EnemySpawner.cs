using UnityEngine;

/// <summary>
/// Spawns enemies just outside the arena edge, in waves that grow over time.
/// Every wave: enemies spawn more often, a few more at once, and walk a little faster.
/// All the numbers are designer knobs in the Inspector.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The enemy prefab to spawn.")]
    public Enemy enemyPrefab;

    [Tooltip("The Player object (needs PlayerHealth).")]
    public PlayerHealth player;

    [Header("Where enemies appear")]
    [Tooltip("Enemies spawn on a square this far from the centre. The outer neon edge is at 28.")]
    public float spawnDistance = 31f;

    [Header("Waves (designer knobs)")]
    [Tooltip("How long each wave lasts, in seconds.")]
    public float waveDuration = 20f;

    [Tooltip("Seconds between spawns in wave 1.")]
    public float startSpawnInterval = 1.2f;

    [Tooltip("Each wave, the gap between spawns is multiplied by this (0.85 = 15% faster).")]
    [Range(0.5f, 1f)]
    public float intervalMultiplierPerWave = 0.85f;

    [Tooltip("The fastest spawning can ever get (seconds between spawns).")]
    public float minSpawnInterval = 0.2f;

    [Tooltip("Enemies spawned at once in wave 1.")]
    public int startGroupSize = 1;

    [Tooltip("Extra enemies added to each group every wave.")]
    public int groupSizeIncreasePerWave = 1;

    [Tooltip("Each wave, enemies walk this much faster (0.05 = +5%).")]
    public float speedIncreasePerWave = 0.05f;

    [Tooltip("Never more than this many enemies alive at once (keeps phones running smoothly).")]
    public int maxAlive = 150;

    public int CurrentWave { get; private set; } = 1;

    float waveTimer;
    float spawnTimer;

    void Update()
    {
        if (enemyPrefab == null || player == null || player.IsDead) return;

        // Move on to the next wave when this one's time is up.
        waveTimer += Time.deltaTime;
        if (waveTimer >= waveDuration)
        {
            waveTimer = 0f;
            CurrentWave++;
            Debug.Log($"Wave {CurrentWave}");
        }

        // Spawn a group whenever the timer runs out.
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            int groupSize = startGroupSize + groupSizeIncreasePerWave * (CurrentWave - 1);
            for (int i = 0; i < groupSize && Enemy.All.Count < maxAlive; i++)
                SpawnOne();

            float interval = startSpawnInterval * Mathf.Pow(intervalMultiplierPerWave, CurrentWave - 1);
            spawnTimer = Mathf.Max(minSpawnInterval, interval);
        }
    }

    void SpawnOne()
    {
        // Pick a random point on the square just outside the arena.
        float along = Random.Range(-spawnDistance, spawnDistance);
        Vector3 pos;
        switch (Random.Range(0, 4))
        {
            case 0:  pos = new Vector3(along, 0f,  spawnDistance); break; // top
            case 1:  pos = new Vector3(along, 0f, -spawnDistance); break; // bottom
            case 2:  pos = new Vector3( spawnDistance, 0f, along); break; // right
            default: pos = new Vector3(-spawnDistance, 0f, along); break; // left
        }

        Enemy e = Instantiate(enemyPrefab, pos, Quaternion.identity);
        float speedMultiplier = 1f + speedIncreasePerWave * (CurrentWave - 1);
        e.Init(player.transform, player, speedMultiplier);
    }
}
