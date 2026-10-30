using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A basic enemy: walks straight at the player, pushes away from other enemies so they
/// don't all stack into one blob, and hurts the player on contact.
/// It doesn't use Unity physics on purpose - simple maths is much cheaper when there
/// are 100+ enemies on a phone browser.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Enemy (designer knobs)")]
    [Tooltip("Walking speed in units per second. The player moves at 8, so keep this lower.")]
    public float moveSpeed = 3.5f;

    [Tooltip("Damage dealt each time it hits the player.")]
    public float contactDamage = 10f;

    [Tooltip("Seconds between hits while touching the player.")]
    public float hitCooldown = 0.6f;

    [Tooltip("How close (units) it must be to the player to hit.")]
    public float contactRadius = 1.3f;

    [Tooltip("Enemies closer than this push each other apart.")]
    public float separationRadius = 1.2f;

    [Tooltip("How strongly enemies push apart. 0 = they all overlap.")]
    public float separationStrength = 3f;

    [Header("Health (designer knobs)")]
    [Tooltip("How much damage it takes to kill. The player's bullets do 10, so 20 = two hits.")]
    public float maxHealth = 20f;

    [Tooltip("How much it swells for a moment when hit (0.3 = 30% bigger). Makes hits feel punchy.")]
    public float hitPunch = 0.3f;

    // Every enemy alive, so they can avoid each other and the spawner can count them.
    public static readonly List<Enemy> All = new List<Enemy>();

    /// <summary>Fires whenever any enemy dies, with the spot where it died. Loot will listen to this.</summary>
    public static event System.Action<Vector3> OnAnyEnemyKilled;

    /// <summary>Fires whenever any enemy is hit (for sparks and sounds).</summary>
    public static event System.Action<Vector3> OnAnyEnemyHit;

    public bool IsDead { get; private set; }

    /// <summary>True while a nuke is wiping the horde (so XP and per-kill sounds can be skipped).</summary>
    public static bool NukeInProgress { get; private set; }

    /// <summary>Kill every enemy alive right now.</summary>
    public static int KillAll()
    {
        NukeInProgress = true;
        List<Enemy> copy = new List<Enemy>(All);
        int killed = 0;
        foreach (Enemy e in copy)
        {
            if (e == null || e.IsDead) continue;
            e.TakeDamage(float.MaxValue);
            killed++;
        }
        NukeInProgress = false;
        return killed;
    }

    Transform player;
    PlayerHealth playerHealth;
    float nextHitTime;
    float health;
    Vector3 baseScale;
    float punchTimer;

    void Awake()
    {
        health = maxHealth;
        baseScale = transform.localScale;
    }

    /// <summary>Called by bullets when they hit this enemy.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        health -= amount;
        punchTimer = 0.1f; // swell briefly
        if (health > 0f) OnAnyEnemyHit?.Invoke(transform.position);

        if (health <= 0f)
        {
            IsDead = true;
            OnAnyEnemyKilled?.Invoke(transform.position);
            Destroy(gameObject);
        }
    }

    /// <summary>Called by the spawner right after creating this enemy.</summary>
    public void Init(Transform playerTransform, PlayerHealth health, float speedMultiplier)
    {
        player = playerTransform;
        playerHealth = health;
        moveSpeed *= speedMultiplier;
    }

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        // Hit "punch": swell for a split second after being hit, then shrink back.
        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(punchTimer / 0.1f);
            transform.localScale = baseScale * (1f + hitPunch * t);
        }

        if (player == null || IsDead) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // 1. Walk towards the player.
        Vector3 move = distance > 0.01f ? toPlayer / distance : Vector3.zero;

        // 2. Push away from nearby enemies.
        Vector3 push = Vector3.zero;
        for (int i = 0; i < All.Count; i++)
        {
            Enemy other = All[i];
            if (other == this) continue;
            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float d = away.magnitude;
            if (d > 0.001f && d < separationRadius)
                push += away / d * (1f - d / separationRadius);
        }
        move += push * separationStrength;

        // Don't let the push make it faster than its normal speed.
        move = Vector3.ClampMagnitude(move, 1f);
        transform.position += move * moveSpeed * Time.deltaTime;

        // Face the player.
        if (distance > 0.01f)
            transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);

        // 3. Hurt the player when touching.
        if (distance < contactRadius && Time.time >= nextHitTime && playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);
            nextHitTime = Time.time + hitCooldown;
        }
    }
}
