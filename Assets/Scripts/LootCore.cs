using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A gold data core. It's either lying on the floor (waiting to be picked up)
/// or part of the player's trail (controlled by LootTrail).
/// </summary>
public class LootCore : MonoBehaviour
{
    [Tooltip("How much this core is worth.")]
    public int value = 10;

    [Tooltip("How high it floats above the floor.")]
    public float floatHeight = 0.6f;

    [Tooltip("Spin speed in degrees per second.")]
    public float spinSpeed = 120f;

    // Every core lying on the floor, so the trail can find ones to pick up.
    public static readonly List<LootCore> OnFloor = new List<LootCore>();

    public bool InTrail { get; private set; }

    /// <summary>Can't be picked up before this time (stops knocked-off cores snapping straight back).</summary>
    public float PickupAllowedAt { get; private set; }

    Vector3 scatterTarget;
    float scatterTime;

    /// <summary>Put the core on the floor, sliding a little in a random direction.</summary>
    public void DropOnFloor(Vector3 from, float scatterDistance, float pickupDelay)
    {
        InTrail = false;
        Vector2 r = Random.insideUnitCircle.normalized * scatterDistance;
        transform.position = new Vector3(from.x, floatHeight, from.z);
        scatterTarget = new Vector3(from.x + r.x, floatHeight, from.z + r.y);
        scatterTime = 0.25f;
        PickupAllowedAt = Time.time + pickupDelay;
        if (!OnFloor.Contains(this)) OnFloor.Add(this);
    }

    /// <summary>Called by LootTrail when the player picks this core up.</summary>
    public void JoinTrail()
    {
        InTrail = true;
        OnFloor.Remove(this);
    }

    void OnDisable() { OnFloor.Remove(this); }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (InTrail) return; // the trail moves it

        // Slide out to where it was knocked/dropped.
        if (scatterTime > 0f)
        {
            scatterTime -= Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, scatterTarget, 12f * Time.deltaTime);
        }

        // Gentle bob so loot on the floor catches the eye.
        Vector3 p = transform.position;
        p.y = floatHeight + Mathf.Sin(Time.time * 4f + p.x) * 0.1f;
        transform.position = p;
    }
}
