using UnityEngine;

/// <summary>
/// A bullet. Flies in a straight line and damages the first enemy it touches.
/// Uses simple distance checks instead of physics, same as the enemies (cheaper on phones).
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("How close (units) to an enemy's centre counts as a hit.")]
    public float hitRadius = 0.8f;

    [Tooltip("Seconds before the bullet disappears if it hits nothing.")]
    public float lifetime = 1.2f;

    Vector3 direction;
    float speed;
    float damage;
    float age;

    /// <summary>Called by the AutoShooter right after creating the bullet.</summary>
    public void Launch(Vector3 dir, float projectileSpeed, float projectileDamage)
    {
        direction = dir;
        speed = projectileSpeed;
        damage = projectileDamage;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Check every enemy for a hit (ignoring height, since the bullet flies at chest level).
        float hitRadiusSq = hitRadius * hitRadius;
        for (int i = 0; i < Enemy.All.Count; i++)
        {
            Enemy e = Enemy.All[i];
            if (e == null || e.IsDead) continue;
            Vector3 diff = e.transform.position - transform.position;
            diff.y = 0f;
            if (diff.sqrMagnitude < hitRadiusSq)
            {
                e.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }
    }
}
