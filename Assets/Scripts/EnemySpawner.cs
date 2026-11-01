using UnityEngine;

/// <summary>
/// Spawns enemies in proper waves:
///   1. A wave has a set number of enemies, which arrive in groups from outside the arena.
///   2. The wave is cleared when every one of them is dead.
///   3. Then there's a short breather before the next, bigger wave starts.
/// Skipping an evac (greed) jumps the wave number up and adds enemies to the current wave.
/// All the numbers are designer knobs in the Inspector.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum WaveState { Breather, Spawning, Clearing }

    [Header("References")]
    [Tooltip("The enemy prefab to spawn.")]
    public Enemy enemyPrefab;

    [Tooltip("The Player object (needs PlayerHealth).")]
    public PlayerHealth player;

    [Header("Extra enemy types (designer knobs)")]
    [Tooltip("Fast, fragile enemy. Leave empty to turn off.")]
    public Enemy runnerPrefab;
    [Tooltip("First wave Runners can appear in.")]
    public int runnerFromWave = 3;
    [Tooltip("Share of enemies that are Runners in their first wave (0.2 = 20%).")]
    [Range(0f, 1f)] public float runnerShareStart = 0.2f;
    [Tooltip("Extra share each wave after that.")]
    [Range(0f, 0.2f)] public float runnerSharePerWave = 0.03f;
    [Range(0f, 1f)] public float runnerShareMax = 0.4f;

    [Tooltip("Slow, tough enemy. Leave empty to turn off.")]
    public Enemy brutePrefab;
    [Tooltip("First wave Brutes can appear in.")]
    public int bruteFromWave = 5;
    [Range(0f, 1f)] public float bruteShareStart = 0.08f;
    [Range(0f, 0.2f)] public float bruteSharePerWave = 0.02f;
    [Range(0f, 1f)] public float bruteShareMax = 0.2f;

    [Header("Where enemies appear")]
    [Tooltip("Enemies spawn on a square this far from the centre. The outer neon edge is at 28.")]
    public float spawnDistance = 31f;

    [Header("Wave size (designer knobs)")]
    [Tooltip("Enemies in wave 1.")]
    public int firstWaveEnemies = 10;

    [Tooltip("Extra enemies added to each wave after that.")]
    public int extraEnemiesPerWave = 6;

    [Header("Breathing space (designer knobs)")]
    [Tooltip("Seconds before wave 1 starts.")]
    public float firstWaveDelay = 2f;

    [Tooltip("Seconds of calm after a wave is cleared, before the next one starts.")]
    public float breatherTime = 5f;

    [Header("Spawning within a wave (designer knobs)")]
    [Tooltip("Seconds between groups in wave 1.")]
    public float startSpawnInterval = 1.2f;

    [Tooltip("Each wave, the gap between groups is multiplied by this (0.85 = 15% faster).")]
    [Range(0.5f, 1f)]
    public float intervalMultiplierPerWave = 0.85f;

    [Tooltip("The fastest groups can ever arrive (seconds between groups).")]
    public float minSpawnInterval = 0.2f;

    [Tooltip("Enemies per group in wave 1.")]
    public int startGroupSize = 1;

    [Tooltip("Extra enemies added to each group every wave.")]
    public int groupSizeIncreasePerWave = 1;

    [Tooltip("Each wave, enemies walk this much faster (0.05 = +5%).")]
    public float speedIncreasePerWave = 0.05f;

    [Tooltip("Never more than this many enemies alive at once (keeps phones running smoothly).")]
    public int maxAlive = 150;

    // Sounds and the HUD listen to these.
    public static event System.Action<int> OnWaveStarted;
    public static event System.Action<int> OnWaveCleared;

    public int CurrentWave { get; private set; }
    public WaveState State { get; private set; } = WaveState.Breather;
    public float BreatherTimeLeft { get; private set; }

    /// <summary>Enemies still to come in this wave plus those alive now.</summary>
    public int EnemiesRemaining => toSpawn + AliveCount();

    int toSpawn;
    float spawnTimer;

    void Start()
    {
        CurrentWave = 0;
        State = WaveState.Breather;
        BreatherTimeLeft = firstWaveDelay;
    }

    void Update()
    {
        if (enemyPrefab == null || player == null || player.IsDead) return;

        switch (State)
        {
            case WaveState.Breather:
                BreatherTimeLeft -= Time.deltaTime;
                if (BreatherTimeLeft <= 0f) StartWave(CurrentWave + 1);
                break;

            case WaveState.Spawning:
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    int groupSize = startGroupSize + groupSizeIncreasePerWave * (CurrentWave - 1);
                    for (int i = 0; i < groupSize && toSpawn > 0 && AliveCount() < maxAlive; i++)
                    {
                        SpawnOne();
                        toSpawn--;
                    }

                    float interval = startSpawnInterval * Mathf.Pow(intervalMultiplierPerWave, CurrentWave - 1);
                    spawnTimer = Mathf.Max(minSpawnInterval, interval);
                }
                if (toSpawn <= 0) State = WaveState.Clearing;
                break;

            case WaveState.Clearing:
                if (AliveCount() == 0)
                {
                    Debug.Log($"Wave {CurrentWave} cleared");
                    OnWaveCleared?.Invoke(CurrentWave);
                    State = WaveState.Breather;
                    BreatherTimeLeft = breatherTime;
                }
                break;
        }
    }

    void StartWave(int wave)
    {
        CurrentWave = wave;
        toSpawn = firstWaveEnemies + extraEnemiesPerWave * (wave - 1);
        spawnTimer = 0f;
        State = WaveState.Spawning;
        Debug.Log($"Wave {CurrentWave}: {toSpawn} enemies");
        OnWaveStarted?.Invoke(CurrentWave);
    }

    /// <summary>
    /// Greed: jump the difficulty forward (used when the player skips an evac).
    /// The wave number goes up, and the current wave gets extra enemies straight away.
    /// </summary>
    public void AddWaves(int count)
    {
        CurrentWave += count;
        if (State != WaveState.Breather)
        {
            toSpawn += extraEnemiesPerWave * count;
            State = WaveState.Spawning;
        }
        Debug.Log($"Greed! Jumped to wave {CurrentWave}");
    }

    /// <summary>Choose which enemy to spawn, based on the wave number.</summary>
    Enemy PickType()
    {
        float roll = Random.value;

        if (brutePrefab != null && CurrentWave >= bruteFromWave)
        {
            float share = Mathf.Min(bruteShareMax, bruteShareStart + bruteSharePerWave * (CurrentWave - bruteFromWave));
            if (roll < share) return brutePrefab;
            roll -= share;
        }
        if (runnerPrefab != null && CurrentWave >= runnerFromWave)
        {
            float share = Mathf.Min(runnerShareMax, runnerShareStart + runnerSharePerWave * (CurrentWave - runnerFromWave));
            if (roll < share) return runnerPrefab;
        }
        return enemyPrefab;
    }

    int AliveCount()
    {
        int n = 0;
        for (int i = 0; i < Enemy.All.Count; i++)
            if (Enemy.All[i] != null && !Enemy.All[i].IsDead) n++;
        return n;
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

        Enemy e = Instantiate(PickType(), pos, Quaternion.identity);
        float speedMultiplier = 1f + speedIncreasePerWave * (CurrentWave - 1);
        e.Init(player.transform, player, speedMultiplier);
    }
}
