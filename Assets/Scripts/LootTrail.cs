using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The heart of Greed Run's risk: every core you pick up joins a glowing tail behind you.
/// The longer the tail, the more you're carrying - and the harder it is to protect.
/// Enemies that touch the tail knock cores off. If you die, the whole tail is lost.
/// Goes on the Player.
/// </summary>
public class LootTrail : MonoBehaviour
{
    [Header("Pickup (designer knobs)")]
    [Tooltip("How close you need to be to grab a core from the floor.")]
    public float pickupRange = 2.5f;

    [Header("Trail shape")]
    [Tooltip("Gap between cores in the tail.")]
    public float spacing = 0.7f;

    [Tooltip("How far behind the robot the tail starts.")]
    public float startGap = 1.0f;

    [Tooltip("How quickly cores slide into place. Higher = stiffer tail.")]
    public float followSharpness = 18f;

    [Header("Enemies stealing from the tail (designer knobs)")]
    [Tooltip("How close an enemy must get to a core to knock it off.")]
    public float knockRadius = 0.9f;

    [Tooltip("Minimum seconds between knock-offs, so one swarm doesn't strip the whole tail instantly.")]
    public float knockCooldown = 0.35f;

    [Tooltip("How far knocked-off cores fly.")]
    public float knockScatter = 2.5f;

    [Tooltip("Seconds before a knocked-off core can be grabbed again.")]
    public float repickDelay = 0.8f;

    /// <summary>The cores currently in the tail, nearest the robot first.</summary>
    public readonly List<LootCore> Cores = new List<LootCore>();

    /// <summary>Total value you're carrying - what you'd lose if you died now.</summary>
    public int AtRisk
    {
        get
        {
            int total = 0;
            for (int i = 0; i < Cores.Count; i++) total += Cores[i].value;
            return total;
        }
    }

    PlayerHealth health;
    float nextKnockTime;
    bool lostOnDeath;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (health != null && health.IsDead)
        {
            if (!lostOnDeath) LoseEverything();
            return;
        }

        PickUpNearby();
        FollowPlayer();
        CheckEnemiesStealing();
    }

    void PickUpNearby()
    {
        float rangeSq = pickupRange * pickupRange;
        for (int i = LootCore.OnFloor.Count - 1; i >= 0; i--)
        {
            LootCore c = LootCore.OnFloor[i];
            if (c == null || Time.time < c.PickupAllowedAt) continue;
            Vector3 d = c.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < rangeSq)
            {
                c.JoinTrail();
                Cores.Add(c); // newest cores go on the end of the tail
            }
        }
    }

    void FollowPlayer()
    {
        // Each core follows the one in front of it, like a snake.
        // The first one follows a point just behind the robot.
        Vector3 leader = transform.position - transform.forward * startGap;
        float gap = 0f;

        for (int i = 0; i < Cores.Count; i++)
        {
            LootCore c = Cores[i];
            Vector3 pos = c.transform.position;
            float y = c.floatHeight;
            Vector3 flatPos = new Vector3(pos.x, 0f, pos.z);
            Vector3 flatLeader = new Vector3(leader.x, 0f, leader.z);

            Vector3 offset = flatPos - flatLeader;
            Vector3 target = offset.sqrMagnitude > 0.0001f
                ? flatLeader + offset.normalized * gap
                : flatLeader;

            Vector3 newPos = Vector3.Lerp(flatPos, target, followSharpness * Time.deltaTime);
            c.transform.position = new Vector3(newPos.x, y, newPos.z);

            leader = c.transform.position;
            gap = spacing;
        }
    }

    void CheckEnemiesStealing()
    {
        if (Time.time < nextKnockTime || Cores.Count == 0) return;

        float rSq = knockRadius * knockRadius;
        for (int i = 0; i < Cores.Count; i++)
        {
            Vector3 cp = Cores[i].transform.position;
            for (int j = 0; j < Enemy.All.Count; j++)
            {
                Enemy e = Enemy.All[j];
                if (e == null || e.IsDead) continue;
                Vector3 d = e.transform.position - cp;
                d.y = 0f;
                if (d.sqrMagnitude < rSq)
                {
                    KnockOff(i);
                    nextKnockTime = Time.time + knockCooldown;
                    return; // one knock per cooldown
                }
            }
        }
    }

    void KnockOff(int index)
    {
        LootCore c = Cores[index];
        Cores.RemoveAt(index);
        c.DropOnFloor(c.transform.position, knockScatter, repickDelay);
    }

    /// <summary>Called by the evac pad: removes the whole tail and returns what it was worth.</summary>
    public int BankAll()
    {
        int total = AtRisk;
        for (int i = 0; i < Cores.Count; i++)
            if (Cores[i] != null) Destroy(Cores[i].gameObject);
        Cores.Clear();
        return total;
    }

    void LoseEverything()
    {
        lostOnDeath = true;
        int lost = AtRisk;
        for (int i = 0; i < Cores.Count; i++)
            if (Cores[i] != null) Destroy(Cores[i].gameObject);
        Cores.Clear();
        if (lost > 0) Debug.Log($"Died carrying {lost}. All lost.");
    }
}
