using UnityEngine;

/// <summary>
/// Makes the player shoot automatically at the nearest enemy in range.
/// The player only moves - aiming and firing happen by themselves (one-thumb mobile control).
/// </summary>
public class AutoShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The bullet prefab to fire.")]
    public Projectile projectilePrefab;

    [Header("Weapon (designer knobs)")]
    [Tooltip("Shots per second.")]
    public float fireRate = 4f;

    [Tooltip("Damage per bullet.")]
    public float damage = 10f;

    [Tooltip("Bullet speed in units per second.")]
    public float projectileSpeed = 25f;

    [Tooltip("Only enemies closer than this get targeted.")]
    public float range = 12f;

    [Tooltip("Bullets fired per shot. More than 1 = a spread, like a shotgun.")]
    public int projectilesPerShot = 1;

    [Tooltip("Total angle of the spread in degrees (only matters with more than 1 bullet).")]
    public float spreadAngle = 20f;

    [Tooltip("How many extra enemies each bullet passes through.")]
    public int pierce = 0;

    [Tooltip("Height bullets fire from (roughly the robot's chest).")]
    public float muzzleHeight = 1f;

    /// <summary>Fires every time the gun shoots, with the muzzle position.</summary>
    public static event System.Action<Vector3> OnFired;

    PlayerHealth health;
    float cooldown;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (projectilePrefab == null) return;
        if (health != null && health.IsDead) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindNearestEnemy();
        if (target == null) return; // nothing in range - wait, don't waste the shot

        Fire(target);
        cooldown = 1f / Mathf.Max(0.01f, fireRate);
    }

    Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float bestDistSq = range * range;
        Vector3 me = transform.position;

        for (int i = 0; i < Enemy.All.Count; i++)
        {
            Enemy e = Enemy.All[i];
            if (e == null || e.IsDead) continue;
            Vector3 diff = e.transform.position - me;
            diff.y = 0f;
            float dSq = diff.sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                nearest = e;
            }
        }
        return nearest;
    }

    void Fire(Enemy target)
    {
        Vector3 origin = transform.position + Vector3.up * muzzleHeight;
        Vector3 aim = target.transform.position - transform.position;
        aim.y = 0f;
        aim.Normalize();

        OnFired?.Invoke(origin);

        int count = Mathf.Max(1, projectilesPerShot);
        for (int i = 0; i < count; i++)
        {
            // Spread bullets evenly across the spread angle.
            float angle = count == 1 ? 0f : Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, i / (float)(count - 1));
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * aim;

            Projectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir, Vector3.up));
            p.Launch(dir, projectileSpeed, damage, pierce);
        }
    }
}
