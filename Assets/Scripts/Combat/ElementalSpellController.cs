using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ElementalSpellController : MonoBehaviour
{
    [Header("Phím phép có thể đổi trong Inspector")]
    public KeyCode lightningKey = KeyCode.Q;
    public KeyCode fireKey = KeyCode.F;
    public KeyCode waterKey = KeyCode.R;

    [Header("Thiết lập phép")]
    public int fireManaCost = 1;
    public int waterManaCost = 1;
    public int fireDamage = 10;
    public int waterDamage = 8;
    public float projectileSpeed = 9f;
    public float spellCooldown = 0.35f;
    public Sprite waterSprite;
    public TextMeshProUGUI feedbackText;
    [Header("Kiểu tung phép — Tab đổi kiểu")]
    public SpellCastPattern castPattern = SpellCastPattern.Fan;
    public KeyCode changePatternKey = KeyCode.Tab;
    private TextMeshProUGUI patternLabel;

    private SkillTree tree;
    private PlayerMana mana;
    private PlayerCombat combat;
    private LightningSkill lightning;
    private PlayerHealth health;
    private float nextCastTime;
    private float feedbackUntil;
    private Button lightningButton;
    private Button fireButton;
    private Button waterButton;

    private void Awake()
    {
        tree = GetComponent<SkillTree>();
        mana = GetComponent<PlayerMana>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        lightning = GetComponent<LightningSkill>();
        if (lightning != null) lightning.listenForInput = false;
    }

    private void Start()
    {
        BuildSpellBar();
        foreach (TextMeshProUGUI label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
        {
            if (label.gameObject.name == "HotkeyText")
                GameplayControlHints.Bind(label);
        }
    }

    private void Update()
    {
        if (feedbackText != null && feedbackText.gameObject.activeSelf && Time.unscaledTime >= feedbackUntil)
            feedbackText.gameObject.SetActive(false);
        if (GameplayUiFocus.BlocksControls || (health != null && health.IsDead)) return;
        if (Input.GetKeyDown(changePatternKey)) CyclePattern();
        if (Input.GetKeyDown(lightningKey)) CastLightning();
        if (Input.GetKeyDown(fireKey)) CastFire();
        if (Input.GetKeyDown(waterKey)) CastWater();
        RefreshButtons();
    }

    public void CastLightning()
    {
        if (!CanUse(tree != null && tree.HasLightning, "SẤM SÉT")) return;
        if (castPattern != SpellCastPattern.Focused)
        {
            if (!SpendMana((lightning != null ? lightning.manaCost : 2) + PatternManaBonus())) return;
            CastVolley(ElementalProjectile.Element.Lightning, lightning != null ? lightning.damage : 12);
            nextCastTime = Time.time + (lightning != null ? lightning.cooldown : 1.8f);
            return;
        }
        if (lightning != null && lightning.Cast())
        {
            nextCastTime = Time.time + spellCooldown;
            GameAudioManager.Play(GameSound.BossLightning, transform.position, 0.86f, 1.12f);
        }
    }

    public void CastFire()
    {
        if (!CanUse(tree != null && tree.HasFire, "LỬA") || !SpendMana(fireManaCost + PatternManaBonus())) return;
        CastVolley(ElementalProjectile.Element.Fire, fireDamage);
        nextCastTime = Time.time + spellCooldown;
    }

    private void CastVolley(ElementalProjectile.Element element, int baseDamage)
    {
        Transform origin = combat != null && combat.attackPoint != null ? combat.attackPoint : transform;
        PlayerStats stats = GetComponent<PlayerStats>();
        int damage = combat != null ? combat.SpellDamage(baseDamage) : baseDamage;
        SpellVolley.Cast(transform, origin.position, new Vector2(FacingDirection(), 0f), element,
            castPattern, projectileSpeed, damage, combat != null ? combat.enemyLayers : default);
        GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
        GameAudioManager.Play(element == ElementalProjectile.Element.Fire ? GameSound.BossFire : element == ElementalProjectile.Element.Water ? GameSound.BossWater : GameSound.BossLightning, origin.position, .68f, 1.08f);
    }

    public void CyclePattern()
    {
        castPattern = (SpellCastPattern)(((int)castPattern + 1) % 3);
        RefreshButtons();
        GameAudioManager.PlayUI(GameSound.UIClick, .5f);
    }

    private int PatternManaBonus() => 0;

    public void CastWater()
    {
        if (!CanUse(tree != null && tree.HasWater, "NƯỚC") || !SpendMana(waterManaCost + PatternManaBonus())) return;
        CastVolley(ElementalProjectile.Element.Water, waterDamage);
        nextCastTime = Time.time + spellCooldown;
    }

    private bool CanUse(bool learned, string displayName)
    {
        if (!enabled || GameplayUiFocus.BlocksControls || (health != null && health.IsDead)) return false;
        if (combat == null || !combat.RequireUsableWeapon()) return false;
        if (!learned)
        {
            ShowFeedback("Chưa học phép " + displayName + " trong Cây Kỹ Năng (V)");
            GameAudioManager.PlayUI(GameSound.UIError, 0.7f);
            return false;
        }
        if (Time.time < nextCastTime) return false;
        return true;
    }

    private bool SpendMana(int cost)
    {
        if (mana != null && mana.UseMana(cost)) return true;
        ShowFeedback("Không đủ mana");
        GameAudioManager.PlayUI(GameSound.UIError, 0.7f);
        return false;
    }

    private float FacingDirection()
    {
        return transform.localScale.x < 0f ? 1f : -1f;
    }

    public void ShowFeedback(string message)
    {
        Debug.Log(message, this);
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        feedbackUntil = Time.unscaledTime + 1.6f;
    }

    private void BuildSpellBar()
    {
        Canvas canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null || GameObject.Find("ElementalSpellBar") != null) return;
        GameObject root = new GameObject("ElementalSpellBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.72f, 0.14f); rect.anchorMax = new Vector2(0.985f, 0.235f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        GameUITheme.Surface(root.GetComponent<Image>(), "Dialogue", 2f);
        root.AddComponent<CombatHudVisibility>();
        lightningButton = CreateSpellButton(root.transform, "LightningButton", "Q\nSẤM", new Vector2(0.02f, 0.08f), new Vector2(0.32f, 0.92f));
        fireButton = CreateSpellButton(root.transform, "FireButton", "F\nLỬA", new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.92f));
        waterButton = CreateSpellButton(root.transform, "WaterButton", "R\nNƯỚC", new Vector2(0.68f, 0.08f), new Vector2(0.98f, 0.92f));
        lightningButton.onClick.AddListener(CastLightning);
        fireButton.onClick.AddListener(CastFire);
        waterButton.onClick.AddListener(CastWater);
        Button patternButton = CreateSpellButton(root.transform, "PatternButton", "", new Vector2(.02f, -.33f), new Vector2(.98f, -.02f));
        patternLabel = patternButton.GetComponentInChildren<TextMeshProUGUI>();
        if (patternLabel != null) patternLabel.fontSize = 15f;
        patternButton.onClick.AddListener(CyclePattern);
        if (feedbackText == null)
        {
            var message = new GameObject("SpellFeedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            message.transform.SetParent(root.transform, false);
            feedbackText = message.GetComponent<TextMeshProUGUI>();
            var messageRect = feedbackText.rectTransform;
            messageRect.anchorMin = new Vector2(-1.5f, 1.08f); messageRect.anchorMax = new Vector2(1, 1.95f);
            messageRect.offsetMin = messageRect.offsetMax = Vector2.zero;
            feedbackText.fontSize = 20; feedbackText.alignment = TextAlignmentOptions.Right; GameUITheme.Text(feedbackText);
            message.SetActive(false);
        }
        RefreshButtons();
    }

    private Button CreateSpellButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = new Color(0.1f, 0.14f, 0.2f, 1f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label; text.fontSize = 18f; text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        GameUITheme.Button(button);
        GameUITheme.Surface(image, "Button", 3f);
        return button;
    }

    private void RefreshButtons()
    {
        if (patternLabel != null) patternLabel.text = "TAB • " + (castPattern == SpellCastPattern.Focused ? "ĐƠN TIA" : castPattern == SpellCastPattern.Fan ? "QUẠT 5 TIA" : "VÒNG 8 TIA") + " • +" + PatternManaBonus() + " mana";
        SetButtonState(lightningButton, tree != null && tree.HasLightning, new Color(0.2f, 0.72f, 1f));
        SetButtonState(fireButton, tree != null && tree.HasFire, new Color(1f, 0.3f, 0.12f));
        SetButtonState(waterButton, tree != null && tree.HasWater, new Color(0.12f, 0.55f, 0.95f));
    }

    private static void SetButtonState(Button button, bool learned, Color learnedColor)
    {
        if (button == null) return;
        button.image.color = learned ? Color.Lerp(Color.white, learnedColor, .18f) : new Color(.48f, .48f, .42f, .85f);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.color = learned ? GameUITheme.Cream : GameUITheme.Muted;
    }
}
