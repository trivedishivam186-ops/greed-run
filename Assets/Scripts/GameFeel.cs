using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// All the "juice" in one place: sounds, debris bursts, screen shake and screen flashes.
/// It listens to events from the other scripts, so nothing else needs to know it exists.
/// Sounds are loaded automatically from Assets/Resources/Audio - swap any .wav there to change a sound.
/// Put this on an empty "GameFeel" object.
/// </summary>
public class GameFeel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera follow script (for screen shake).")]
    public CameraFollow cameraFollow;

    [Tooltip("The HUD canvas (a full-screen flash is added to it).")]
    public Canvas canvas;

    [Header("Debris materials (use the glowing ones)")]
    public Material redMaterial;   // M_EnemyRed
    public Material goldMaterial;  // M_LootGold
    public Material cyanMaterial;  // M_Bullet

    [Header("Your own sounds (optional - these replace the built-in ones)")]
    [Tooltip("Looping background music.")]
    public AudioClip backgroundMusic;
    [Tooltip("Plays when you extract. Replaces the built-in 'extract' jingle.")]
    public AudioClip successSound;
    [Tooltip("Plays when you die. Replaces the built-in 'death' sound.")]
    public AudioClip failureSound;
    [Tooltip("Plays when the evac opens. Replaces the built-in alarm.")]
    public AudioClip evacSound;

    [Header("Volume (designer knobs)")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float shootVolume = 0.25f;
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    [Tooltip("Volume of the evac-open sound.")]
    [Range(0f, 1f)] public float evacVolume = 0.4f;

    [Tooltip("Play a sound when an enemy knocks a core off your tail. Off by default (it got noisy in big hordes).")]
    public bool knockOffSound = false;

    [Tooltip("Music volume while the end screen is showing (so the success/failure sound stands out).")]
    [Range(0f, 1f)] public float musicVolumeOnEndScreen = 0.12f;

    [Header("Shake strength (designer knobs)")]
    public float shakeOnEnemyDeath = 0.04f;
    public float shakeOnHurt = 0.35f;
    public float shakeOnEvacOpen = 0.15f;
    public float shakeOnExtract = 0.5f;
    public float shakeOnDeath = 0.8f;

    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly List<AudioSource> voices = new List<AudioSource>();
    AudioSource music;
    float musicTarget;
    int nextVoice;
    float lastShootSound;

    ParticleSystem redBurst, goldBurst, cyanBurst;
    RawImage flash;
    float flashAlpha;
    Color flashColor;

    // ---------- Setup ----------

    void Awake()
    {
        // Sounds
        foreach (AudioClip c in Resources.LoadAll<AudioClip>("Audio"))
            clips[c.name] = c;
        for (int i = 0; i < 12; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D sound
            voices.Add(s);
        }

        // Your clips replace the built-in ones with the same job.
        if (successSound != null) clips["extract"] = successSound;
        if (failureSound != null) clips["death"] = failureSound;
        if (evacSound != null) clips["evac_open"] = evacSound;

        // Background music on its own looping source.
        if (backgroundMusic != null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.clip = backgroundMusic;
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            musicTarget = musicVolume;
            music.volume = musicVolume * masterVolume;
            music.Play();
        }

        // Debris
        Mesh cube = GetCubeMesh();
        redBurst  = MakeBurstSystem("RedBurst",  redMaterial,  cube);
        goldBurst = MakeBurstSystem("GoldBurst", goldMaterial, cube);
        cyanBurst = MakeBurstSystem("CyanBurst", cyanMaterial, cube);

        // Screen flash
        if (canvas != null)
        {
            GameObject go = new GameObject("ScreenFlash", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsFirstSibling(); // behind the HUD text
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            flash = go.GetComponent<RawImage>();
            flash.texture = Texture2D.whiteTexture;
            flash.raycastTarget = false;
            flash.enabled = false;
        }
    }

    void OnEnable()
    {
        AutoShooter.OnFired += HandleFired;
        Enemy.OnAnyEnemyHit += HandleEnemyHit;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        LootTrail.OnPickedUp += HandlePickedUp;
        LootTrail.OnKnockedOff += HandleKnockedOff;
        PlayerHealth.OnPlayerDamaged += HandleHurt;
        PlayerHealth.OnPlayerDied += HandlePlayerDied;
        EvacManager.OnEvacOpened += HandleEvacOpened;
        EvacManager.OnEvacMissed += HandleEvacMissed;
        EvacManager.OnExtracted += HandleExtracted;
        LevelUpSystem.OnLevelUp += HandleLevelUp;
        NukePickup.OnNuke += HandleNuke;
        EvacManager.OnNewBest += HandleNewBest;
        StashShop.OnPurchased += HandlePurchased;
        StashShop.OnDeploy += HandleDeploy;
        CollectorDirector.OnCollectorSent += HandleCollectorSent;
        Boss.OnBossSpawned += HandleBossSpawned;
        Boss.OnBossCharge += HandleBossCharge;
        Boss.OnBossKilled += HandleBossKilled;
        Collector.OnEscaped += HandleCollectorEscaped;
        EnemySpawner.OnWaveStarted += HandleWaveStarted;
        EnemySpawner.OnWaveCleared += HandleWaveCleared;
    }

    void OnDisable()
    {
        AutoShooter.OnFired -= HandleFired;
        Enemy.OnAnyEnemyHit -= HandleEnemyHit;
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
        LootTrail.OnPickedUp -= HandlePickedUp;
        LootTrail.OnKnockedOff -= HandleKnockedOff;
        PlayerHealth.OnPlayerDamaged -= HandleHurt;
        PlayerHealth.OnPlayerDied -= HandlePlayerDied;
        EvacManager.OnEvacOpened -= HandleEvacOpened;
        EvacManager.OnEvacMissed -= HandleEvacMissed;
        EvacManager.OnExtracted -= HandleExtracted;
        LevelUpSystem.OnLevelUp -= HandleLevelUp;
        NukePickup.OnNuke -= HandleNuke;
        EvacManager.OnNewBest -= HandleNewBest;
        StashShop.OnPurchased -= HandlePurchased;
        StashShop.OnDeploy -= HandleDeploy;
        CollectorDirector.OnCollectorSent -= HandleCollectorSent;
        Boss.OnBossSpawned -= HandleBossSpawned;
        Boss.OnBossCharge -= HandleBossCharge;
        Boss.OnBossKilled -= HandleBossKilled;
        Collector.OnEscaped -= HandleCollectorEscaped;
        EnemySpawner.OnWaveStarted -= HandleWaveStarted;
        EnemySpawner.OnWaveCleared -= HandleWaveCleared;
    }

    void HandleBossSpawned()
    {
        Play("greed", 1f, 0f, 0.8f);
        Play("wave_start", 1f, 0f, 0.7f);
        Flash(new Color(1f, 0.1f, 0.15f), 0.35f);
        Shake(0.5f);
    }

    void HandleBossCharge()
    {
        Play("hurt", 0.9f, 0f, 0.6f);
        Shake(0.3f);
    }

    void HandleBossKilled(Vector3 pos)
    {
        Play("nuke", 1f, 0f, 1.2f);
        Burst(redBurst, pos + Vector3.up, 40, 12f, 0.35f, 1.2f);
        Burst(goldBurst, pos + Vector3.up, 30, 9f, 0.25f, 1.2f);
        Flash(new Color(1f, 0.8f, 0.2f), 0.5f);
        Shake(0.9f);
    }

    void HandleCollectorSent()
    {
        Play("greed", 1f, 0f, 1.3f);
        Flash(new Color(0.7f, 0.2f, 1f), 0.3f);
        Shake(0.2f);
    }

    void HandleCollectorEscaped(int lost)
    {
        Play("knock", 0.9f, 0f, 0.7f);
        Flash(new Color(0.7f, 0.2f, 1f), 0.4f);
    }

    void HandlePurchased()
    {
        Play("pickup", 0.8f, 0f, 1.4f);
    }

    void HandleDeploy()
    {
        Play("wave_start", 0.7f, 0f);
    }

    void HandleNewBest()
    {
        Play("levelup", 0.8f, 0f, 1.25f);
    }

    void HandleNuke(Vector3 pos, int killed)
    {
        Play("nuke", 1f, 0f);
        Flash(Color.white, 0.9f);
        Shake(1f);
    }

    void HandleWaveStarted(int wave)
    {
        if (wave > 1) Play("wave_start", 0.6f, 0f);
    }

    void HandleWaveCleared(int wave)
    {
        Play("wave_clear", 0.7f, 0f);
    }

    void HandleLevelUp()
    {
        Play("levelup", 0.9f, 0f);
        Flash(new Color(0f, 0.9f, 1f), 0.25f);
    }

    // ---------- What happens on each event ----------

    void HandleFired(Vector3 muzzle)
    {
        // Shooting is constant, so keep it quiet and don't stack sounds.
        if (Time.time - lastShootSound > 0.06f)
        {
            Play("shoot", shootVolume, 0.12f);
            lastShootSound = Time.time;
        }
        Burst(cyanBurst, muzzle, 2, 3f, 0.12f, 0.15f);
    }

    void HandleEnemyHit(Vector3 pos)
    {
        Play("hit", 0.35f, 0.15f);
        Burst(cyanBurst, pos + Vector3.up * 0.5f, 3, 5f, 0.12f, 0.25f);
    }

    void HandleEnemyKilled(Vector3 pos)
    {
        if (Enemy.NukeInProgress)
        {
            // A nuke kills everything at once: debris only, the nuke has its own sound and shake.
            Burst(redBurst, pos + Vector3.up * 0.4f, 4, 7f, 0.22f, 0.6f);
            return;
        }
        Play("enemy_die", 0.6f, 0.15f);
        Burst(redBurst, pos + Vector3.up * 0.4f, 10, 7f, 0.22f, 0.6f);
        Shake(shakeOnEnemyDeath);
    }

    void HandlePickedUp(Vector3 pos, int trailLength)
    {
        // The pitch climbs as your tail grows - greed you can hear.
        float pitch = 1f + Mathf.Min(trailLength, 30) * 0.02f;
        Play("pickup", 0.5f, 0f, pitch);
        Burst(goldBurst, pos, 4, 3f, 0.12f, 0.35f);
    }

    void HandleKnockedOff(Vector3 pos)
    {
        if (knockOffSound) Play("knock", 0.7f, 0.1f);
        Burst(goldBurst, pos, 8, 6f, 0.15f, 0.5f);
    }

    void HandleHurt(float amount)
    {
        Play("hurt", 0.8f, 0.1f);
        Flash(new Color(1f, 0.1f, 0.15f), 0.35f);
        Shake(shakeOnHurt);
    }

    void HandlePlayerDied()
    {
        musicTarget = musicVolumeOnEndScreen;
        Play("death", 1f, 0f);
        if (cameraFollow != null && cameraFollow.target != null)
            Burst(redBurst, cameraFollow.target.position + Vector3.up, 30, 10f, 0.3f, 1f);
        Flash(new Color(1f, 0.1f, 0.15f), 0.6f);
        Shake(shakeOnDeath);
    }

    void HandleEvacOpened()
    {
        Play("evac_open", evacVolume, 0f);
        Flash(new Color(1f, 0.7f, 0f), 0.15f);
        Shake(shakeOnEvacOpen);
    }

    void HandleEvacMissed()
    {
        Play("greed", 1f, 0f);
        Flash(new Color(0.6f, 0f, 0.1f), 0.3f);
        Shake(0.2f);
    }

    void HandleExtracted(Vector3 pos)
    {
        musicTarget = musicVolumeOnEndScreen;
        Play("extract", 1f, 0f);
        Burst(goldBurst, pos + Vector3.up, 40, 9f, 0.25f, 1.2f);
        Flash(new Color(1f, 0.8f, 0.2f), 0.45f);
        Shake(shakeOnExtract);
    }

    // ---------- Helpers ----------

    void Update()
    {
        // Smoothly move the music volume towards its target (real time, works when frozen).
        if (music != null)
            music.volume = Mathf.MoveTowards(music.volume, musicTarget * masterVolume, Time.unscaledDeltaTime);

        // Fade the screen flash (uses real time so it still fades on the frozen end screen).
        if (flash != null && flashAlpha > 0f)
        {
            flashAlpha = Mathf.Max(0f, flashAlpha - 2.5f * Time.unscaledDeltaTime);
            Color c = flashColor;
            c.a = flashAlpha;
            flash.color = c;
            flash.enabled = flashAlpha > 0.001f;
        }
    }

    void Play(string name, float volume, float pitchVariation, float basePitch = 1f)
    {
        if (!clips.TryGetValue(name, out AudioClip clip)) return;
        AudioSource s = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Count;
        s.pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
        s.PlayOneShot(clip, volume * masterVolume);
    }

    void Shake(float amount)
    {
        if (cameraFollow != null) cameraFollow.Shake(amount);
    }

    void Flash(Color color, float strength)
    {
        if (flash == null) return;
        flashColor = color;
        flashAlpha = Mathf.Max(flashAlpha, strength);
    }

    /// <summary>Throw out a handful of little glowing cubes.</summary>
    void Burst(ParticleSystem ps, Vector3 pos, int count, float speed, float size, float life)
    {
        if (ps == null) return;
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f; // mostly outwards and up
            p.position = pos;
            p.velocity = dir.normalized * speed * Random.Range(0.5f, 1f);
            p.startSize = size * Random.Range(0.7f, 1.3f);
            p.startLifetime = life * Random.Range(0.7f, 1.2f);
            p.rotation3D = Random.insideUnitSphere * 180f;
            ps.Emit(p, 1);
        }
    }

    ParticleSystem MakeBurstSystem(string name, Material mat, Mesh mesh)
    {
        if (mat == null) return null;

        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f;
        main.maxParticles = 600;
        main.startSpeed = 0f;

        var emission = ps.emission;
        emission.enabled = false; // we only emit by hand

        var shape = ps.shape;
        shape.enabled = false;

        // Shrink to nothing over their life.
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Slow down as they fly.
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.dampen = 0.15f;
        lim.limit = 0f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = mesh;
        r.material = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ps.Play();
        return ps;
    }

    static Mesh GetCubeMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh m = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return m;
    }
}
