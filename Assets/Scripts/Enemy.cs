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

    // Every enemy alive, so they can avoid each other and the spawner can count them.
    public static readonly List<Enemy> All = new List<Enemy>();

    Transform player;
    PlayerHealth playerHealth;
    float nextHitTime;

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
        if (player == null) return;

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
