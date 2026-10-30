using UnityEngine;

/// <summary>
/// A rare power-up on the floor. Walk over it and every enemy alive is wiped out.
/// It blinks and disappears if you don't grab it in time.
/// Goes on the Nuke prefab.
/// </summary>
public class NukePickup : MonoBehaviour
{
    [Tooltip("How close you need to be to grab it.")]
    public float pickupRadius = 1.8f;

    [Tooltip("Seconds before it disappears.")]
    public float lifetime = 12f;

    [Tooltip("It starts blinking this many seconds before disappearing.")]
    public float blinkWhenSecondsLeft = 3f;

    public float spinSpeed = 90f;
    public float floatHeight = 0.9f;

    /// <summary>Fires when a nuke goes off, with how many enemies it killed.</summary>
    public static event System.Action<Vector3, int> OnNuke;

    /// <summary>Is there a nuke lying on the floor right now?</summary>
    public static bool AnyOnFloor { get; private set; }

    Transform player;
    float age;
    Renderer[] renderers;

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
    }

    void OnEnable()  { AnyOnFloor = true; }
    void OnDisable() { AnyOnFloor = false; }

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 p = transform.position;
        p.y = floatHeight + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = p;

        // Blink near the end, then vanish.
        float left = lifetime - age;
        if (left <= 0f) { Destroy(gameObject); return; }
        if (left < blinkWhenSecondsLeft)
        {
            bool visible = Mathf.Repeat(Time.time * 8f, 1f) > 0.4f;
            foreach (Renderer r in renderers) r.enabled = visible;
        }

        if (player == null) return;
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.magnitude < pickupRadius)
        {
            int killed = Enemy.KillAll();
            Debug.Log($"NUKE! {killed} enemies wiped out.");
            OnNuke?.Invoke(transform.position, killed);
            Destroy(gameObject);
        }
    }
}
