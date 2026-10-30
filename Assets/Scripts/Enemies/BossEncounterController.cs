using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossEncounterController : MonoBehaviour
{
    public float triggerDistance = 8f;
    public float approachDistance = 6f;
    public bool playOnce = true;

    private Transform player;
    private BossAI boss;
    private bool started;
    private bool completed;
    private GameObject cinematicRoot;
    private TextMeshProUGUI speaker;
    private TextMeshProUGUI subtitle;
    private EnemyAI enemyAI;
    private Rigidbody2D bossBody;
    private CinematicPresentationController presentation;
    private readonly System.Collections.Generic.List<GameObject> hiddenHud = new System.Collections.Generic.List<GameObject>();

    private void Awake()
    {
        boss = GetComponent<BossAI>();
        enemyAI = GetComponent<EnemyAI>();
        bossBody = GetComponent<Rigidbody2D>();
        if (enemyAI != null) enemyAI.enabled = false;
        if (bossBody != null) bossBody.linearVelocity = Vector2.zero;
        if (GetComponent<ElectricBossController>() != null)
            GetComponent<ElectricBossController>().DeactivateEncounter();
    }

    private void Update()
    {
        if (completed || started || CinematicPresentationController.IsCinematicActive) return;
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (player != null && Vector2.Distance(player.position, transform.position) <= triggerDistance)
            RequestEncounter();
    }

    public void RestoreCompletedEncounter()
    {
        started = false;
        completed = true;
        if (enemyAI != null) enemyAI.enabled = true;
    }

    public void RequestEncounter()
    {
        if (started || completed || CinematicPresentationController.IsCinematicActive) return;
        StartCoroutine(EncounterSequence());
    }

    private IEnumerator EncounterSequence()
    {
        started = true;
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (player == null)
        {
            started = false;
            yield break;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        ElementalSpellController spells = player.GetComponent<ElementalSpellController>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;
        if (spells != null) spells.enabled = false;
        if (enemyAI != null) enemyAI.enabled = false;
        if (bossBody != null) bossBody.linearVelocity = Vector2.zero;
        HideGameplayHud();
        BuildCinematicUI();
        presentation = CinematicPresentationController.GetOrCreate();
        presentation.BeginSequence(player, PlayerClassController.IsMageSelected, false);
        presentation.WideShot(player, transform);
        GameAudioManager.Play(GameSound.BossRoar, transform.position, 0.86f, 0.78f);

        Vector3 away = (player.position - transform.position).normalized;
        if (away.sqrMagnitude < 0.1f) away = Vector3.left;
        Vector3 bossAnchor = transform.position;
        Vector3 destination = bossAnchor + away * approachDistance;
        Vector3 start = player.position;
        float elapsed = 0f;
        player.GetComponent<PlayerSPUMAnimation>()?.SetMoving(true);
        while (elapsed < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            player.position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, elapsed / 0.9f));
            transform.position = bossAnchor;
            FaceEachOther();
            yield return null;
        }
        player.GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
        FaceEachOther();

        string heroName = PlayerIdentity.Name;
        presentation.Focus(transform, 3.7f);
        yield return ShowLine("BOSS", "Một sinh vật nhỏ bé khác lại tự nguyện bước vào nơi này.");
        presentation.Focus(player, 3.7f);
        yield return ShowLine(heroName, "Ngươi chính là con quái vật mà sắc lệnh nhắc đến?");
        presentation.Focus(transform, 3.45f);
        yield return ShowLine("BOSS", "Sắc lệnh… điều ước… phần thưởng…");
        yield return ShowLine("BOSS", "Các ngươi luôn cần một lý do đẹp đẽ để che giấu lòng tham không đáy của mình. Đúng là loài người.");
        presentation.Focus(player, 3.55f);
        yield return ShowLine(heroName, "Ta không đến đây vì phần thưởng. Ta đến để ngăn ngươi tiến đến ngôi làng.");
        presentation.Focus(transform, 3.35f);
        yield return ShowLine("BOSS", "Vậy hãy chứng minh lời nói đó bằng mạng sống của ngươi.");
        GetComponent<EnemySPUMAnimation>()?.PlayMagicAttack();
        // The roar strikes the arena edges, leaving both actors and the subtitle clear.
        presentation.WideShot(player, transform);
        presentation.PlayCombatVfx("Lightning", transform.position + Vector3.right * 2.8f + Vector3.up, 0.7f, 0.8f);
        presentation.PlayCombatVfx("Lightning", transform.position + Vector3.left * 2.8f + Vector3.up * 1.5f, 0.6f, 0.8f);
        GameAudioManager.Play(GameSound.BossRoar, transform.position, 1f, 0.72f);
        CameraShakeManager.instance?.ShakeHeavy();
        yield return ShowLine("BOSS", "ĐẾN ĐÂY!");

        if (cinematicRoot != null) Destroy(cinematicRoot);
        if (movement != null) movement.enabled = true;
        if (combat != null) combat.enabled = true;
        if (spells != null) spells.enabled = true;
        RestoreGameplayHud();
        presentation.EndSequence();
        completed = true;
        started = false;
        boss?.ActivateFromCutscene();
        if (enemyAI != null) enemyAI.enabled = true;
        CameraShakeManager.instance?.ShakeHeavy();
    }

    private void FaceEachOther()
    {
        if (player == null) return;
        float playerMagnitude = Mathf.Max(0.01f, Mathf.Abs(player.localScale.y));
        float bossMagnitude = Mathf.Max(0.01f, Mathf.Abs(transform.localScale.y));
        player.localScale = new Vector3(transform.position.x > player.position.x ? -playerMagnitude : playerMagnitude, playerMagnitude, playerMagnitude);
        transform.localScale = new Vector3(player.position.x > transform.position.x ? -bossMagnitude : bossMagnitude, bossMagnitude, bossMagnitude);
    }

    private void HideGameplayHud()
    {
        hiddenHud.Clear();
        Canvas canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null) return;
        string[] names = { "HUD_StatusCard", "HUD_QuestCard", "HUD_HotkeyStrip", "MinimapUI", "ElementalSpellBar" };
        foreach (string objectName in names)
        {
            Transform found = canvas.transform.Find(objectName);
            if (found == null)
            {
                foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                    if (child.name == objectName) { found = child; break; }
            }
            if (found != null && found.gameObject.activeSelf)
            {
                hiddenHud.Add(found.gameObject);
                found.gameObject.SetActive(false);
            }
        }
    }

    private void RestoreGameplayHud()
    {
        foreach (GameObject item in hiddenHud) if (item != null) item.SetActive(true);
        hiddenHud.Clear();
    }

    private IEnumerator ShowLine(string who, string line)
    {
        if (speaker != null) speaker.text = PlayerIdentity.Literal(who);
        if (subtitle != null) subtitle.text = line;
        GameAudioManager.PlayUI(GameSound.Dialogue, 0.58f, who.Contains("CHÚA") ? 0.78f : 1f);
        float end = Time.unscaledTime + StoryReading.Duration(line);
        while (Time.unscaledTime < end) yield return null;
    }

    public IEnumerator PlayDefeatSequence()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        PlayerMovement movement = player != null ? player.GetComponent<PlayerMovement>() : null;
        PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : null;
        ElementalSpellController spells = player != null ? player.GetComponent<ElementalSpellController>() : null;
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;
        if (spells != null) spells.enabled = false;
        if (player != null && player.TryGetComponent(out Rigidbody2D playerBody)) playerBody.linearVelocity = Vector2.zero;
        HideGameplayHud();
        BuildCinematicUI();
        presentation = CinematicPresentationController.GetOrCreate();
        presentation.BeginSequence(player, PlayerClassController.IsMageSelected, false);
        presentation.WideShot(player, transform);

        string heroName = PlayerIdentity.Name;
        presentation.Focus(transform, 3.4f);
        presentation.PlayCombatVfx("Lightning", transform.position + Vector3.up * 0.7f, 0.35f, 0.7f);
        yield return ShowLine("BOSS", "Ngươi nghĩ… đây là chiến thắng sao?");
        presentation.Focus(player, 3.6f);
        yield return ShowLine(heroName, "Mọi chuyện kết thúc rồi.");
        presentation.Focus(transform, 3.2f);
        yield return ShowLine("BOSS", "Không… với ngươi, mọi chuyện chỉ đang bắt đầu.");
        yield return ShowLine("BOSS", "Trước khi chết… ta cho ngươi một lời khuyên cuối.");
        yield return ShowLine("BOSS", "Đừng tin bất cứ thứ gì…");
        if (cinematicRoot != null) Destroy(cinematicRoot);
        presentation.PlayCombatVfx("Glitch", transform.position + Vector3.up * 0.5f, 0.6f, 0.75f);
        presentation.EndSequence();
        RestoreGameplayHud();
        if (movement != null) movement.enabled = true;
        if (combat != null) combat.enabled = true;
        if (spells != null) spells.enabled = true;
    }

    private void BuildCinematicUI()
    {
        Canvas canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null) return;
        cinematicRoot = new GameObject("BossEncounterCinematic", typeof(RectTransform));
        cinematicRoot.transform.SetParent(canvas.transform, false);
        RectTransform root = cinematicRoot.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
        CinematicUiLayer.Configure(cinematicRoot, 30000);
        CreateBar(root, "TopBar", new Vector2(0f, 0.88f), Vector2.one);
        CreateBar(root, "BottomBar", Vector2.zero, new Vector2(1f, 0.2f));
        speaker = CreateText(root, "Speaker", string.Empty, 24, FontStyles.Bold,
            GameUITheme.Gold, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.18f));
        subtitle = CreateText(root, "Subtitle", string.Empty, 26, FontStyles.Normal,
            GameUITheme.Cream, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.125f));
    }

    private static void CreateBar(Transform parent, string name, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.01f, 0.015f, 0.025f, 0.92f);
        if(name=="BottomBar")GameUITheme.Surface(go.GetComponent<Image>(), "Dialogue");
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size,
        FontStyles style, Color color, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color;
        GameUITheme.Text(text, name == "Speaker");
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }
}
