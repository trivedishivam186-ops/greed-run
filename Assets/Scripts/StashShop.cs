using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The start screen and stash shop, shown before every run.
/// Spend banked loot on permanent upgrades, then press DEPLOY to start.
/// Upgrades are saved in the browser and applied at the start of every run.
/// The whole screen is built by this script. Put it on an empty "StashShop" object.
/// </summary>
public class StashShop : MonoBehaviour
{
    public enum ShopUpgradeType { MaxHealth, DamagePercent, PickupRangePercent, KeepOnDeathPercent }

    [Serializable]
    public class ShopUpgrade
    {
        [Tooltip("Save name. Don't change it after release, or players lose this upgrade.")]
        public string id;
        public string title;
        public string description;
        public ShopUpgradeType type;
        [Tooltip("What each level adds. Percent types: 10 = +10%. Max Health: health points.")]
        public float amountPerLevel;
        public int maxLevel = 5;
        [Tooltip("Cost of level 1.")]
        public int baseCost = 100;
        [Tooltip("Each level costs this many times the last (1.6 = 60% more).")]
        public float costGrowth = 1.6f;

        public int Level => PlayerPrefs.GetInt("GreedRun_Up_" + id, 0);
        public int NextCost => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, Level));
        public bool IsMaxed => Level >= maxLevel;
        public float Total => amountPerLevel * Level;
    }

    [Header("References")]
    public Canvas canvas;
    public PlayerHealth playerHealth;
    public AutoShooter shooter;
    public LootTrail lootTrail;

    [Tooltip("Optional: any HUD text, so the screen uses the same font.")]
    public TMP_Text fontSource;

    [Header("Upgrades for sale (designer knobs)")]
    public List<ShopUpgrade> upgrades = new List<ShopUpgrade>
    {
        new ShopUpgrade { id = "health",  title = "MAX HEALTH",      description = "+20 health",                       type = ShopUpgradeType.MaxHealth,          amountPerLevel = 20, maxLevel = 5, baseCost = 100, costGrowth = 1.6f },
        new ShopUpgrade { id = "damage",  title = "STARTING DAMAGE", description = "+15% bullet damage",               type = ShopUpgradeType.DamagePercent,      amountPerLevel = 15, maxLevel = 5, baseCost = 120, costGrowth = 1.6f },
        new ShopUpgrade { id = "magnet",  title = "PICKUP RANGE",    description = "+15% loot pull range",             type = ShopUpgradeType.PickupRangePercent, amountPerLevel = 15, maxLevel = 5, baseCost = 80,  costGrowth = 1.6f },
        new ShopUpgrade { id = "blackbox",title = "BLACK BOX",       description = "Keep 10% of your loot if you die", type = ShopUpgradeType.KeepOnDeathPercent, amountPerLevel = 10, maxLevel = 3, baseCost = 250, costGrowth = 2f },
    };

    [Header("Threat Levels (designer knobs)")]
    [Tooltip("Each Threat Level adds this much enemy health and damage (0.3 = +30%).")]
    public float threatEnemyBonusPerLevel = 0.3f;
    [Tooltip("Each Threat Level adds this much loot value (0.5 = +50%).")]
    public float threatLootBonusPerLevel = 0.5f;
    [Tooltip("Extract after reaching this wave on your highest Threat Level to unlock the next one.")]
    public int threatUnlockWave = 5;
    [Tooltip("Highest Threat Level.")]
    public int threatMaxLevel = 10;

    /// <summary>Share of carried loot kept when you die (from the Black Box upgrade). 0.2 = 20%.</summary>
    public static float KeepOnDeath { get; private set; }

    /// <summary>True while the start screen is up (the game is frozen).</summary>
    public static bool InMenu { get; private set; }

    public static event Action OnPurchased;
    public static event Action OnDeploy;

    static readonly Color Gold = new Color(1f, 0.76f, 0.1f);
    static readonly Color Cyan = new Color(0f, 0.9f, 1f);
    static readonly Color Grey = new Color(0.6f, 0.65f, 0.72f);
    static readonly Color Red = new Color(1f, 0.3f, 0.35f);
    static readonly Color CardBg = new Color(0.04f, 0.07f, 0.12f, 0.95f);

    GameObject screen;
    TMP_Text stashText, recordsText, threatText;
    GameObject threatDown, threatUp;
    readonly List<TMP_Text> rowTitles = new List<TMP_Text>();
    readonly List<TMP_Text> rowDescs = new List<TMP_Text>();
    readonly List<TMP_Text> rowCosts = new List<TMP_Text>();

    void Awake()
    {
        KeepOnDeath = 0f;
        Threat.EnemyBonusPerLevel = threatEnemyBonusPerLevel;
        Threat.LootBonusPerLevel = threatLootBonusPerLevel;
        Threat.UnlockWave = threatUnlockWave;
        Threat.MaxLevel = threatMaxLevel;
        BuildUI();
    }

    void Start()
    {
        // Freeze the game until the player presses DEPLOY.
        InMenu = true;
        Time.timeScale = 0f;
        screen.SetActive(true);
        Refresh();
    }

    // ---------- Buying and deploying ----------

    void TryBuy(int index)
    {
        ShopUpgrade u = upgrades[index];
        if (u.IsMaxed) return;
        if (!Stash.Spend(u.NextCost)) return; // can't afford

        PlayerPrefs.SetInt("GreedRun_Up_" + u.id, u.Level + 1);
        PlayerPrefs.Save();
        OnPurchased?.Invoke();
        Refresh();
    }

    void Deploy()
    {
        // Apply every permanent upgrade to this run.
        foreach (ShopUpgrade u in upgrades)
        {
            if (u.Level <= 0) continue;
            float pct = 1f + u.Total / 100f;
            switch (u.type)
            {
                case ShopUpgradeType.MaxHealth:
                    if (playerHealth != null) playerHealth.AddMaxHealth(u.Total); break;
                case ShopUpgradeType.DamagePercent:
                    if (shooter != null) shooter.damage *= pct; break;
                case ShopUpgradeType.PickupRangePercent:
                    if (lootTrail != null) lootTrail.pickupRange *= pct; break;
                case ShopUpgradeType.KeepOnDeathPercent:
                    KeepOnDeath = Mathf.Clamp01(u.Total / 100f); break;
            }
        }

        screen.SetActive(false);
        InMenu = false;
        Time.timeScale = 1f;
        OnDeploy?.Invoke();
    }

    void ChangeThreat(int delta)
    {
        Threat.Selected = Threat.Selected + delta;
        OnPurchased?.Invoke(); // reuse the click sound
        Refresh();
    }

    void Refresh()
    {
        int t = Threat.Selected;
        string effect = t == 1
            ? "Standard enemies and loot"
            : $"Enemies +{Mathf.RoundToInt(threatEnemyBonusPerLevel * (t - 1) * 100)}%   ·   Loot x{Threat.LootMultiplier:0.0}";
        string unlock = Threat.Unlocked < Threat.MaxLevel
            ? $"\n<size=75%><color=#9AA4B2>Unlock Threat {Threat.Unlocked + 1}: extract after wave {Threat.UnlockWave} on Threat {Threat.Unlocked}</color></size>"
            : "";
        threatText.text = $"<color=#FF3C50>THREAT LEVEL {t}</color>\n<size=70%>{effect}</size>{unlock}";
        threatDown.SetActive(t > 1);
        threatUp.SetActive(t < Threat.Unlocked);

        stashText.text = $"STASH  {Stash.Total:N0}";
        recordsText.text = $"BEST WAVE  {Stash.BestWave}   ·   BEST BANK  {Stash.BestBank:N0}";

        for (int i = 0; i < upgrades.Count && i < rowTitles.Count; i++)
        {
            ShopUpgrade u = upgrades[i];
            rowTitles[i].text = $"{u.title}  <size=70%><color=#00E5FF>LV {u.Level}/{u.maxLevel}</color></size>";
            rowDescs[i].text = u.description;

            if (u.IsMaxed)
            {
                rowCosts[i].text = "MAX";
                rowCosts[i].color = Cyan;
            }
            else
            {
                rowCosts[i].text = $"{u.NextCost:N0}";
                rowCosts[i].color = Stash.Total >= u.NextCost ? Gold : Red;
            }
        }
    }

    /// <summary>Right-click the component in the Inspector to wipe stash, upgrades and records (for testing).</summary>
    [ContextMenu("Reset all save data")]
    void ResetSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Save data wiped.");
        if (screen != null) Refresh();
    }

    // ---------- Building the UI ----------

    void BuildUI()
    {
        RectTransform s = NewRect("StartScreen", canvas.transform);
        Stretch(s);
        AddImage(s, new Color(0.01f, 0.02f, 0.04f, 0.92f)).raycastTarget = true;
        screen = s.gameObject;

        Text("Title", s, "GREED RUN", 66, Gold, FontStyles.Bold, new Vector2(0, 380), new Vector2(520, 90));
        Text("Tagline", s, "BETTER LOOT. HIGHER RISK.", 22, Grey, FontStyles.Normal, new Vector2(0, 325), new Vector2(520, 40));
        stashText = Text("Stash", s, "", 40, Gold, FontStyles.Bold, new Vector2(0, 255), new Vector2(520, 60));
        recordsText = Text("Records", s, "", 20, Grey, FontStyles.Normal, new Vector2(0, 210), new Vector2(520, 36));

        for (int i = 0; i < upgrades.Count; i++)
        {
            int index = i;
            RectTransform row = NewRect($"Upgrade{i + 1}", s);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(470f, 105f);
            row.anchoredPosition = new Vector2(0f, 140f - i * 112f); // tighter, to fit the Threat selector below

            Image bg = AddImage(row, CardBg);
            bg.raycastTarget = true;
            Outline o = row.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0.6f, 0.75f);
            o.effectDistance = new Vector2(2f, -2f);
            Button b = row.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.onClick.AddListener(() => TryBuy(index));

            TMP_Text title = Text("Title", row, "", 28, Color.white, FontStyles.Bold, Vector2.zero, Vector2.zero);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.72f, 1f), new Vector2(18f, 0f), new Vector2(0f, -8f));
            title.alignment = TextAlignmentOptions.Left;
            rowTitles.Add(title);

            TMP_Text desc = Text("Desc", row, "", 21, Grey, FontStyles.Normal, Vector2.zero, Vector2.zero);
            Place(desc.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 0.5f), new Vector2(18f, 8f), new Vector2(0f, 0f));
            desc.alignment = TextAlignmentOptions.Left;
            rowDescs.Add(desc);

            TMP_Text cost = Text("Cost", row, "", 30, Gold, FontStyles.Bold, Vector2.zero, Vector2.zero);
            Place(cost.rectTransform, new Vector2(0.72f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-14f, 0f));
            cost.alignment = TextAlignmentOptions.Right;
            rowCosts.Add(cost);
        }

        // Threat Level selector:  <  THREAT LEVEL 2  >
        threatText = Text("Threat", s, "", 28, Color.white, FontStyles.Bold, new Vector2(0, -305), new Vector2(380, 100));
        threatDown = ArrowButton(s, "<", new Vector2(-215, -305), () => ChangeThreat(-1));
        threatUp = ArrowButton(s, ">", new Vector2(215, -305), () => ChangeThreat(+1));

        RectTransform deploy = NewRect("Deploy", s);
        deploy.anchorMin = deploy.anchorMax = new Vector2(0.5f, 0.5f);
        deploy.sizeDelta = new Vector2(360f, 90f);
        deploy.anchoredPosition = new Vector2(0f, -405f);
        Image dbg = AddImage(deploy, Gold);
        dbg.raycastTarget = true;
        Button db = deploy.gameObject.AddComponent<Button>();
        db.targetGraphic = dbg;
        db.onClick.AddListener(Deploy);
        TMP_Text dt = Text("Label", deploy, "DEPLOY", 44, new Color(0.05f, 0.05f, 0.08f), FontStyles.Bold, Vector2.zero, Vector2.zero);
        Stretch(dt.rectTransform);

        s.SetAsLastSibling(); // on top of everything
        screen.SetActive(false);
    }

    GameObject ArrowButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform r = NewRect("Arrow" + label, parent);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(70f, 70f);
        r.anchoredPosition = pos;
        Image bg = AddImage(r, CardBg);
        bg.raycastTarget = true;
        Outline o = r.gameObject.AddComponent<Outline>();
        o.effectColor = Red;
        o.effectDistance = new Vector2(2f, -2f);
        Button b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        b.onClick.AddListener(onClick);
        TMP_Text t = Text("Label", r, label, 40, Red, FontStyles.Bold, Vector2.zero, Vector2.zero);
        Stretch(t.rectTransform);
        return r.gameObject;
    }

    RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }

    Image AddImage(RectTransform rt, Color c)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    TMP_Text Text(string name, Transform parent, string text, float size, Color color, FontStyles style, Vector2 pos, Vector2 box)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontSource != null) t.font = fontSource.font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
