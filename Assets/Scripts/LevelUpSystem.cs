using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kills give XP. When the XP bar fills, the game pauses and offers 3 random upgrade cards.
/// Pick one and the run continues, a little stronger.
/// The upgrade list is editable in the Inspector - every card is a designer knob.
/// The XP bar and the card screen are built by this script, so there's no UI to make by hand.
/// Put this on an empty "LevelUpSystem" object.
/// </summary>
public class LevelUpSystem : MonoBehaviour
{
    public enum UpgradeType
    {
        FireRatePercent,
        DamagePercent,
        ExtraProjectile,
        RangePercent,
        MoveSpeedPercent,
        PickupRangePercent,
        MaxHealth,
        Pierce
    }

    [Serializable]
    public class Upgrade
    {
        public string title;
        [TextArea] public string description;
        public UpgradeType type;
        [Tooltip("How much it adds. Percent types: 20 = +20%. Extra Projectile: number of bullets. Max Health: health points.")]
        public float amount;
        [Tooltip("How many times this card can be taken in one run.")]
        public int maxTimes = 5;
        [HideInInspector] public int timesTaken;
    }

    [Header("References")]
    public Canvas canvas;
    public AutoShooter shooter;
    public PlayerController movement;
    public LootTrail lootTrail;
    public PlayerHealth playerHealth;
    public EvacManager evac;

    [Tooltip("Optional: the end screen, so the level-up cards are drawn underneath it.")]
    public RunEndScreen endScreen;

    [Tooltip("Optional: any HUD text, so the cards use the same font.")]
    public TMP_Text fontSource;

    [Header("XP (designer knobs)")]
    [Tooltip("XP for each enemy killed.")]
    public int xpPerKill = 1;

    [Tooltip("XP needed for the first level-up.")]
    public int firstLevelXp = 5;

    [Tooltip("Extra XP needed for each level after that.")]
    public int extraXpPerLevel = 4;

    [Tooltip("Do enemies killed by a nuke give XP? Off, so one nuke doesn't trigger 10 level-ups in a row.")]
    public bool xpFromNukeKills = false;

    [Tooltip("How many cards to offer.")]
    public int choices = 3;

    [Header("Upgrade cards (designer knobs)")]
    public List<Upgrade> upgrades = new List<Upgrade>
    {
        new Upgrade { title = "RAPID FIRE",   description = "+20% fire rate",          type = UpgradeType.FireRatePercent,    amount = 20, maxTimes = 6 },
        new Upgrade { title = "HEAVY ROUNDS", description = "+50% damage",             type = UpgradeType.DamagePercent,      amount = 50, maxTimes = 6 },
        new Upgrade { title = "SPREAD SHOT",  description = "+1 bullet per shot",      type = UpgradeType.ExtraProjectile,    amount = 1,  maxTimes = 4 },
        new Upgrade { title = "LONG BARREL",  description = "+15% range",              type = UpgradeType.RangePercent,       amount = 15, maxTimes = 4 },
        new Upgrade { title = "SERVO LEGS",   description = "+10% move speed",         type = UpgradeType.MoveSpeedPercent,   amount = 10, maxTimes = 4 },
        new Upgrade { title = "MAGNET",       description = "+60% loot pull range",    type = UpgradeType.PickupRangePercent, amount = 60, maxTimes = 4 },
        new Upgrade { title = "PIERCING ROUNDS", description = "Bullets go through +1 enemy", type = UpgradeType.Pierce, amount = 1, maxTimes = 3 },
        new Upgrade { title = "PLATING",      description = "+25 max health, heal 25", type = UpgradeType.MaxHealth,          amount = 25, maxTimes = 5 },
    };

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => firstLevelXp + extraXpPerLevel * (Level - 1);

    /// <summary>Fires when the card screen opens (for sounds).</summary>
    public static event Action OnLevelUp;

    int pendingLevelUps;
    bool choosing;

    // Built UI
    RectTransform xpFill;
    GameObject cardScreen;
    TMP_Text headerText;
    readonly List<Button> cardButtons = new List<Button>();
    readonly List<TMP_Text> cardTitles = new List<TMP_Text>();
    readonly List<TMP_Text> cardDescs = new List<TMP_Text>();
    readonly List<Upgrade> offered = new List<Upgrade>();

    static readonly Color Gold = new Color(1f, 0.76f, 0.1f);
    static readonly Color Cyan = new Color(0f, 0.9f, 1f);
    static readonly Color CardBg = new Color(0.04f, 0.07f, 0.12f, 0.95f);

    void Awake()
    {
        foreach (Upgrade u in upgrades) u.timesTaken = 0;
        BuildUI();
    }

    void OnEnable()  { Enemy.OnAnyEnemyKilled += HandleKill; }
    void OnDisable() { Enemy.OnAnyEnemyKilled -= HandleKill; }

    void HandleKill(Vector3 _)
    {
        if (RunIsOver()) return;
        if (Enemy.NukeInProgress && !xpFromNukeKills) return;

        Xp += xpPerKill;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            pendingLevelUps++;
        }
    }

    void Update()
    {
        if (xpFill != null)
            xpFill.anchorMax = new Vector2(Mathf.Clamp01(Xp / (float)XpToNext), 1f);

        if (!choosing && pendingLevelUps > 0 && !RunIsOver())
            OpenCards();

        // If the run ends while cards are up (shouldn't happen, but just in case), hide them.
        if (choosing && RunIsOver()) CloseCards(false);
    }

    bool RunIsOver() =>
        (evac != null && evac.RunOver) || (playerHealth != null && playerHealth.IsDead);

    // ---------- Cards ----------

    void OpenCards()
    {
        // Pick random cards that haven't been maxed out.
        List<Upgrade> pool = upgrades.FindAll(u => u.timesTaken < u.maxTimes);
        if (pool.Count == 0) { pendingLevelUps = 0; return; }

        offered.Clear();
        int n = Mathf.Min(choices, pool.Count);
        for (int i = 0; i < n; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            offered.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        headerText.text = $"LEVEL {Level - pendingLevelUps + 1}\n<size=60%>CHOOSE AN UPGRADE</size>";
        for (int i = 0; i < cardButtons.Count; i++)
        {
            bool used = i < offered.Count;
            cardButtons[i].gameObject.SetActive(used);
            if (!used) continue;
            Upgrade u = offered[i];
            string level = u.timesTaken > 0 ? $"  <size=70%><color=#00E5FF>LV {u.timesTaken + 1}</color></size>" : "";
            cardTitles[i].text = u.title + level;
            cardDescs[i].text = u.description;
        }

        choosing = true;
        cardScreen.SetActive(true);
        Time.timeScale = 0f;
        OnLevelUp?.Invoke();
    }

    void Pick(int index)
    {
        if (!choosing || index >= offered.Count) return;
        Apply(offered[index]);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        CloseCards(true);
    }

    void CloseCards(bool resume)
    {
        choosing = false;
        cardScreen.SetActive(false);
        if (resume) Time.timeScale = 1f;
    }

    void Apply(Upgrade u)
    {
        u.timesTaken++;
        float pct = 1f + u.amount / 100f;

        switch (u.type)
        {
            case UpgradeType.FireRatePercent:
                if (shooter != null) shooter.fireRate *= pct; break;
            case UpgradeType.DamagePercent:
                if (shooter != null) shooter.damage *= pct; break;
            case UpgradeType.ExtraProjectile:
                if (shooter != null)
                {
                    shooter.projectilesPerShot += Mathf.RoundToInt(u.amount);
                    shooter.spreadAngle += 8f * u.amount; // widen the fan as bullets are added
                }
                break;
            case UpgradeType.RangePercent:
                if (shooter != null) shooter.range *= pct; break;
            case UpgradeType.MoveSpeedPercent:
                if (movement != null) movement.moveSpeed *= pct; break;
            case UpgradeType.PickupRangePercent:
                if (lootTrail != null) lootTrail.pickupRange *= pct; break;
            case UpgradeType.Pierce:
                if (shooter != null) shooter.pierce += Mathf.RoundToInt(u.amount); break;
            case UpgradeType.MaxHealth:
                if (playerHealth != null) playerHealth.AddMaxHealth(u.amount); break;
        }
        Debug.Log($"Upgrade: {u.title} (x{u.timesTaken})");
    }

    // ---------- Building the UI ----------

    void BuildUI()
    {
        if (canvas == null) return;
        Transform root = canvas.transform;

        // XP bar: a thin strip along the very top of the screen.
        RectTransform bar = NewRect("XpBar", root);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, 8f);
        bar.anchoredPosition = Vector2.zero;
        AddImage(bar, new Color(0f, 0f, 0f, 0.5f));

        xpFill = NewRect("XpFill", bar);
        xpFill.anchorMin = Vector2.zero;
        xpFill.anchorMax = new Vector2(0f, 1f);
        xpFill.offsetMin = xpFill.offsetMax = Vector2.zero;
        AddImage(xpFill, Cyan);

        // Card screen: dark full-screen panel with a header and 3 cards.
        RectTransform screen = NewRect("LevelUpScreen", root);
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = screen.offsetMax = Vector2.zero;
        AddImage(screen, new Color(0f, 0f, 0f, 0.75f)).raycastTarget = true; // blocks taps on the game
        cardScreen = screen.gameObject;

        headerText = NewText("Header", screen, 46, Gold, FontStyles.Bold);
        RectTransform hr = headerText.rectTransform;
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(500f, 130f);
        hr.anchoredPosition = new Vector2(0f, 300f);

        for (int i = 0; i < 3; i++)
        {
            int index = i; // captured for the click handler

            RectTransform card = NewRect($"Card{i + 1}", screen);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(440f, 140f);
            card.anchoredPosition = new Vector2(0f, 140f - i * 165f);

            Image bg = AddImage(card, CardBg);
            bg.raycastTarget = true; // the card must catch taps, or the button can't be pressed
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);

            Button b = card.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            b.colors = cb;
            b.onClick.AddListener(() => Pick(index));
            cardButtons.Add(b);

            TMP_Text title = NewText("Title", card, 34, Gold, FontStyles.Bold);
            RectTransform tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 0.5f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(20f, 0f);
            tr.offsetMax = new Vector2(-20f, -10f);
            cardTitles.Add(title);

            TMP_Text desc = NewText("Desc", card, 26, Color.white, FontStyles.Normal);
            RectTransform dr = desc.rectTransform;
            dr.anchorMin = new Vector2(0f, 0f);
            dr.anchorMax = new Vector2(1f, 0.5f);
            dr.offsetMin = new Vector2(20f, 10f);
            dr.offsetMax = new Vector2(-20f, 0f);
            cardDescs.Add(desc);
        }

        // Keep the end screen on top of the cards.
        if (endScreen != null && endScreen.panel != null)
            endScreen.panel.transform.SetAsLastSibling();

        cardScreen.SetActive(false);
    }

    RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    Image AddImage(RectTransform rt, Color c)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    TMP_Text NewText(string name, Transform parent, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (fontSource != null) t.font = fontSource.font;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
