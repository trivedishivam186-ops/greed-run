using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Titan: a boss that arrives every 5th wave.
///  - Slowly stomps towards you, and hits very hard.
///  - Every few seconds it stops, a red warning line shows where it's about to charge,
///    then it charges along that line at high speed. Get out of the way.
///  - It has a big health bar at the top of the screen. Nukes only take a chunk off it.
///  - When it dies it bursts into a big pile of loot (and you have to decide: grab it, or evac?).
/// Goes on the Boss prefab, next to an Enemy component.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class Boss : MonoBehaviour
{
    [Header("Charge attack (designer knobs)")]
    [Tooltip("Seconds between charges.")]
    public float chargeInterval = 5f;

    [Tooltip("Seconds of warning before it charges (the red line shows where).")]
    public float windupTime = 0.9f;

    [Tooltip("Charge speed (the player runs at 8).")]
    public float chargeSpeed = 22f;

    [Tooltip("How far it charges.")]
    public float chargeDistance = 18f;

    [Tooltip("Width of the warning line.")]
    public float warningWidth = 2.2f;

    [Tooltip("Material for the warning line (use a red glowing one, e.g. M_EnemyRed).")]
    public Material warningMaterial;

    [Header("Reward (designer knobs)")]
    [Tooltip("Loot cores dropped when it dies.")]
    public int lootOnDeath = 25;

    [Tooltip("The loot core prefab.")]
    public LootCore lootPrefab;

    [Header("Health bar")]
    public string bossName = "TITAN";

    public static event System.Action OnBossSpawned;
    public static event System.Action OnBossCharge;
    public static event System.Action<Vector3> OnBossKilled;

    enum State { Walking, WindUp, Charging }

    Enemy enemy;
    float baseSpeed;
    State state = State.Walking;
    float timer;
    Transform chargePoint;
    LineRenderer warning;
    Vector3 chargeStart;

    // Health bar UI (built in code)
    GameObject barRoot;
    RectTransform barFill;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemy.knocksLoot = false;
        chargePoint = new GameObject("BossChargePoint").transform;

        warning = new GameObject("BossWarning").AddComponent<LineRenderer>();
        warning.positionCount = 2;
        warning.startWidth = warning.endWidth = warningWidth;
        warning.material = warningMaterial;
        warning.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        warning.enabled = false;
    }

    void Start()
    {
        baseSpeed = enemy.moveSpeed;
        timer = chargeInterval;
        enemy.OnDied += HandleDied;
        BuildHealthBar();
        OnBossSpawned?.Invoke();
    }

    void OnDestroy()
    {
        if (chargePoint != null) Destroy(chargePoint.gameObject);
        if (warning != null) Destroy(warning.gameObject);
        if (barRoot != null) Destroy(barRoot);
    }

    void Update()
    {
        if (enemy.IsDead) return;

        if (barFill != null) barFill.anchorMax = new Vector2(enemy.Health01, 1f);

        timer -= Time.deltaTime;
        switch (state)
        {
            case State.Walking:
                if (timer <= 0f) StartWindUp();
                break;

            case State.WindUp:
                // Flicker the warning line so it reads as "danger".
                warning.enabled = Mathf.Repeat(Time.time * 10f, 1f) > 0.3f;
                if (timer <= 0f) StartCharge();
                break;

            case State.Charging:
                Vector3 travelled = transform.position - chargeStart;
                travelled.y = 0f;
                if (timer <= 0f || travelled.magnitude >= chargeDistance) EndCharge();
                break;
        }
    }

    void StartWindUp()
    {
        Transform player = FindPlayer();
        if (player == null) { timer = chargeInterval; return; }

        state = State.WindUp;
        timer = windupTime;
        enemy.moveSpeed = 0f; // plant its feet

        // Lock in the charge direction now - the player gets the wind-up to dodge.
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();
        chargeStart = transform.position;
        chargePoint.position = transform.position + dir * chargeDistance;

        Vector3 a = transform.position + Vector3.up * 0.05f;
        Vector3 b = chargePoint.position + Vector3.up * 0.05f;
        warning.SetPosition(0, a);
        warning.SetPosition(1, b);
        warning.enabled = true;
    }

    void StartCharge()
    {
        state = State.Charging;
        timer = chargeDistance / chargeSpeed + 0.2f;
        warning.enabled = false;
        enemy.chaseTarget = chargePoint;
        enemy.moveSpeed = chargeSpeed;
        OnBossCharge?.Invoke();
    }

    void EndCharge()
    {
        state = State.Walking;
        timer = chargeInterval;
        enemy.chaseTarget = null;
        enemy.moveSpeed = baseSpeed;
    }

    Transform FindPlayer()
    {
        PlayerHealth p = FindFirstObjectByType<PlayerHealth>();
        return p != null && !p.IsDead ? p.transform : null;
    }

    void HandleDied(Enemy e)
    {
        if (warning != null) warning.enabled = false;
        if (barRoot != null) barRoot.SetActive(false);

        if (lootPrefab != null)
        {
            for (int i = 0; i < lootOnDeath; i++)
            {
                LootCore c = Instantiate(lootPrefab);
                c.DropOnFloor(transform.position, Random.Range(1f, 4f), 0.2f);
            }
        }
        OnBossKilled?.Invoke(transform.position);
        Debug.Log("Boss down!");
    }

    // ---------- Health bar at the top of the screen ----------

    void BuildHealthBar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        barRoot = new GameObject("BossBar", typeof(RectTransform));
        barRoot.transform.SetParent(canvas.transform, false);
        barRoot.transform.SetSiblingIndex(1); // under the end/level-up screens
        RectTransform root = barRoot.GetComponent<RectTransform>();
        // Along the bottom of the screen, clear of the HUD and evac banner at the top.
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(440f, 22f);
        root.anchoredPosition = new Vector2(0f, 60f);
        Image bg = barRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(barRoot.transform, false);
        barFill = fill.GetComponent<RectTransform>();
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = Vector2.one;
        barFill.offsetMin = new Vector2(3f, 3f);
        barFill.offsetMax = new Vector2(-3f, -3f);
        Image fi = fill.AddComponent<Image>();
        fi.color = new Color(1f, 0.15f, 0.25f);
        fi.raycastTarget = false;

        GameObject label = new GameObject("Name", typeof(RectTransform));
        label.transform.SetParent(barRoot.transform, false);
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0f, 1f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 0f);
        lr.sizeDelta = new Vector2(0f, 34f);
        lr.anchoredPosition = new Vector2(0f, 2f);
        TextMeshProUGUI t = label.AddComponent<TextMeshProUGUI>();
        TMP_Text any = canvas.GetComponentInChildren<TMP_Text>();
        if (any != null) t.font = any.font;
        t.text = bossName;
        t.fontSize = 26f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.3f, 0.35f);
        t.raycastTarget = false;
    }
}
