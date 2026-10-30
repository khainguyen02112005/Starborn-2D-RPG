using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CursorBattleController : MonoBehaviour
{
    public RectTransform cursorVisual;
    public Image healthFill;
    public Image knightHealthFill;
    public TextMeshProUGUI cursorHealthText;
    public TextMeshProUGUI knightHealthText;
    public Camera worldCamera;
    public Sprite projectileSprite;
    public int maxHealth = 100;
    public int projectileDamage = 8;
    public float fireCooldown = 0.16f;
    [Header("Kết 2 — nhấp phải tỏa đạn, giữ kéo để chém")]
    [Min(.1f)] public float radialShotCooldown = .6f;
    [Min(1)] public int slashDamage = 12;
    [Min(.05f)] public float slashCooldown = .28f;
    [Min(.1f)] public float slashHitRadius = .55f;

    private int currentHealth;
    private float nextShot;
    private CorruptedKnightBoss knight;
    private Level04Controller director;
    private bool drawing;
    private Vector3 previousSlashPosition;
    private float nextSlash;
    private bool slashHitLatched;
    private TrailRenderer slashTrail;
    private bool rightPressed;
    private Vector3 rightPressPosition;
    private float rightPressTime;
    private float rightDragPixels;
    private float nextRadialShot;
    private const float RightDragThreshold = 12f, RightHoldDuration = .1f, RightTapDuration = .3f;
    private bool battleActive;
    private GameObject battleHudRoot;
    private Image topCursorHealthFill;
    private Animator hitFeedbackAnimator;
    private Transform cursorAnchor;
    private CursorLockMode previousLock;
    private MapSceneLayout battleLayout;
    [Min(.01f)] public float cursorSensitivity = .18f;

    public int CurrentHealth => currentHealth;
    public bool IsBattleActive => battleActive;

    public Vector3 CursorWorldPosition => cursorAnchor != null ? cursorAnchor.position : ScreenToWorld(Input.mousePosition);

    public void BeginBattle(CorruptedKnightBoss target, Level04Controller owner)
    {
        enabled = true;
        battleActive = true;
        knight = target;
        director = owner;
        currentHealth = maxHealth;
        nextShot = nextSlash = nextRadialShot = 0f;
        CancelRightGesture();
        BuildBattleHUD();
        if (battleHudRoot != null) battleHudRoot.SetActive(true);
        UpdateHealth();
        Cursor.visible = false;
        if (worldCamera == null) worldCamera = Camera.main;
        previousLock = Cursor.lockState;
        battleLayout = FindAnyObjectByType<MapSceneLayout>();
        var anchor = new GameObject("VirtualCursorCameraAnchor");
        cursorAnchor = anchor.transform;
        Vector3 start = target != null ? target.transform.position + Vector3.down * 4f : ScreenToWorld(Input.mousePosition);
        if (battleLayout != null) start = battleLayout.ResolveSafePosition(start, target != null ? target.transform.position + Vector3.left * 4f : start);
        cursorAnchor.position = start;
        Cursor.lockState = CursorLockMode.Locked;
        if (target != null)
        {
            var presentation = CinematicPresentationController.GetOrCreate();
            if (!CinematicPresentationController.IsCinematicActive)
                presentation.BeginSequence(target.transform, PlayerClassController.IsMageSelected, false);
            // Integrate relative mouse input in world space before following it.
            // ScreenToWorld each frame would feed camera motion back into the aim.
            presentation.Focus(cursorAnchor, 5.5f);
            presentation.SetLetterbox(false);
            StoryWorldDissolve.Begin(target.transform);
        }
        knight?.BeginBattle(this);
    }

    private void Update()
    {
        if (!battleActive || (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended))
        {
            CancelRightGesture();
            return;
        }
        Vector3 frameStartPosition = CursorWorldPosition;
        if (cursorAnchor != null)
        {
            Vector3 next = cursorAnchor.position + new Vector3(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"), 0f) * cursorSensitivity;
            MapSceneLayout layout = battleLayout;
            if (layout != null)
            {
                Rect bounds = layout.walkableBounds;
                next.x = Mathf.Clamp(next.x, bounds.xMin + .6f, bounds.xMax - .6f);
                next.y = Mathf.Clamp(next.y, bounds.yMin + .6f, bounds.yMax - .6f);
            }
            // Keep a small non-overlapping cursor centre; swept slashes can still
            // cross the hero, but parking inside the sprite cannot farm hit locks.
            if (knight != null && Vector2.Distance(next, knight.transform.position) < 1.35f)
            {
                Vector3 away = next - knight.transform.position;
                next = knight.transform.position + (away.sqrMagnitude > .001f ? away.normalized : Vector3.down) * 1.35f;
            }
            cursorAnchor.position = next;
        }
        if (cursorVisual != null && worldCamera != null) cursorVisual.position = worldCamera.WorldToScreenPoint(CursorWorldPosition);
        if (knight == null) return;
        if (Input.GetMouseButton(0) && Time.unscaledTime >= nextShot)
        {
            nextShot = Time.unscaledTime + fireCooldown;
            SpawnProjectileAtWorld(CursorWorldPosition, projectileDamage);
        }
        UpdateRightGesture(Input.GetMouseButtonDown(1), Input.GetMouseButton(1),
            Input.GetMouseButtonUp(1), frameStartPosition, CursorWorldPosition);
    }

    private void UpdateRightGesture(bool pressed, bool held, bool released, Vector3 frameStart, Vector3 position)
    {
        if (!battleActive || knight == null ||
            (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended))
        {
            CancelRightGesture();
            return;
        }
        if (pressed)
        {
            CancelRightGesture();
            rightPressed = true;
            rightPressPosition = frameStart;
            rightPressTime = Time.unscaledTime;
            rightDragPixels = 0f;
        }
        if (!rightPressed) return;
        float pixels = worldCamera != null
            ? Vector2.Distance(worldCamera.WorldToScreenPoint(rightPressPosition), worldCamera.WorldToScreenPoint(position))
            : Vector2.Distance(rightPressPosition, position) * 100f;
        rightDragPixels = Mathf.Max(rightDragPixels, pixels);
        float duration = Time.unscaledTime - rightPressTime;
        if (held && !drawing && duration >= RightHoldDuration && rightDragPixels >= RightDragThreshold)
            BeginSlashStroke(rightPressPosition);
        if (held && drawing)
        {
            TrySlashSegment(previousSlashPosition, position);
            previousSlashPosition = position;
            if (slashTrail != null) slashTrail.transform.position = position;
        }
        if (!released && held) return;
        bool tap = !drawing && duration <= RightTapDuration && rightDragPixels < RightDragThreshold;
        CancelRightGesture();
        if (tap) SpawnRadialProjectiles(position);
    }

    private void CancelRightGesture()
    {
        rightPressed = false;
        EndSlashStroke();
    }

    private void SpawnRadialProjectiles(Vector3 position)
    {
        if (!battleActive || knight == null || Time.unscaledTime < nextRadialShot ||
            (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended)) return;
        nextRadialShot = Time.unscaledTime + radialShotCooldown;
        GameAudioManager.PlayUI(GameSound.CursorShot, .58f, 1.04f);
        for (int i = 0; i < 9; i++)
        {
            float angle = (90f + i * 40f) * Mathf.Deg2Rad;
            CreateProjectile(position, projectileDamage, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }
    }

    public void TakeDamage(int amount)
    {
        if (!battleActive) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, amount));
        GameAudioManager.PlayUI(GameSound.CursorHit, 0.78f, Random.Range(0.92f, 1.08f));
        ShowDamageNumber(CursorWorldPosition, amount, new Color(1f, 0.25f, 0.25f));
        if (hitFeedbackAnimator != null) hitFeedbackAnimator.Play("CursorHit", 0, 0f);
        UpdateHealth();
        if (currentHealth > 0) return;
        battleActive = false;
        CancelRightGesture();
        enabled = false;
        Cursor.visible = true;
        Cursor.lockState = previousLock;
        director?.OnCursorDefeated();
    }

    private void BeginSlashStroke(Vector3 position)
    {
        if (!battleActive) return;
        drawing = true;
        previousSlashPosition = position;
        slashHitLatched = false;
        if (slashTrail == null)
        {
            var prefab = Resources.Load<GameObject>("CombatVFX/Ending2/SlashTrail");
            if (prefab != null) slashTrail = Instantiate(prefab).GetComponent<TrailRenderer>();
        }
        if (slashTrail != null)
        {
            slashTrail.transform.position = position;
            slashTrail.Clear();
            slashTrail.AddPosition(position);
            slashTrail.emitting = true;
        }
    }

    private void EndSlashStroke()
    {
        drawing = false;
        slashHitLatched = false;
        if (slashTrail != null) slashTrail.emitting = false;
    }

    private bool TrySlashSegment(Vector3 from, Vector3 to)
    {
        if (!drawing || !battleActive || knight == null ||
            (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended)) return false;
        Vector3 center = knight.transform.position + Vector3.up * .45f;
        bool endsInside = Vector2.Distance(to, center) <= slashHitRadius + .08f;
        if (!endsInside) slashHitLatched = false;
        Vector3 segment = to - from;
        if (segment.sqrMagnitude < .000225f || slashHitLatched || Time.unscaledTime < nextSlash) return false;
        Vector3 nearest = from + segment * Mathf.Clamp01(Vector3.Dot(center - from, segment) / segment.sqrMagnitude);
        if (Vector2.Distance(nearest, center) > slashHitRadius) return false;
        nextSlash = Time.unscaledTime + slashCooldown;
        slashHitLatched = endsInside;
        knight.ReceiveCursorDamage(slashDamage);
        var impact = Resources.Load<GameObject>("CombatVFX/Ending2/SlashImpact");
        if (impact != null) Instantiate(impact, nearest, Quaternion.Euler(0, 0, Mathf.Atan2(segment.y, segment.x) * Mathf.Rad2Deg));
        GameAudioManager.PlayUI(GameSound.SwordSwing, .55f, 1.12f);
        CameraShakeManager.instance?.ShakeLight();
        return true;
    }

    private void SpawnProjectileAtWorld(Vector3 position, int damage)
    {
        if (!battleActive || knight == null) return;
        GameAudioManager.PlayUI(GameSound.CursorShot, 0.52f, Random.Range(0.94f, 1.1f));
        CreateProjectile(position, damage, Vector2.up);
    }

    private void CreateProjectile(Vector3 position, int damage, Vector2 direction)
    {
        GameObject shot = new GameObject("CursorProjectile");
        shot.transform.position = position;
        SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.color = new Color(0.3f, 0.95f, 1f, 1f);
        renderer.sortingOrder = 1120;
        renderer.sharedMaterial = Resources.Load<Material>("CombatVFX/StormArcUnlit");
        shot.transform.localScale = Vector3.one * 0.35f;
        CursorProjectile projectile = shot.AddComponent<CursorProjectile>();
        projectile.Initialize(knight, damage, direction);
    }

    private Vector3 ScreenToWorld(Vector3 screenPosition)
    {
        Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;
        if (cameraToUse == null) return Vector3.zero;
        Vector3 world = cameraToUse.ScreenToWorldPoint(screenPosition);
        world.z = 0f;
        return world;
    }

    private void UpdateHealth()
    {
        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01(currentHealth / (float)Mathf.Max(1,maxHealth));
        if (topCursorHealthFill != null) topCursorHealthFill.fillAmount = (float)currentHealth / maxHealth;
        if (cursorHealthText != null) cursorHealthText.text = "CON TRỎ  " + currentHealth + " / " + maxHealth;
    }

    public void UpdateKnightHealth(int current, int maximum)
    {
        if (knightHealthFill != null) knightHealthFill.fillAmount = maximum > 0 ? (float)current / maximum : 0f;
        if (knightHealthText != null) knightHealthText.text = PlayerIdentity.Literal(PlayerIdentity.Name) + " — HẮC HÓA  " + current + " / " + maximum;
    }

    public void EndBattlePresentation()
    {
        battleActive = false;
        enabled = false;
        CancelRightGesture();
        if (slashTrail != null) Destroy(slashTrail.gameObject);
        StopAllCoroutines();
        knight?.EndBattle();
        ClearTransientBattleObjects();
        Cursor.visible = true;
        Cursor.lockState = previousLock;
        if (cursorAnchor != null) Destroy(cursorAnchor.gameObject);
        if (battleHudRoot != null) battleHudRoot.SetActive(false);
    }

    public void ShowDamageNumber(Vector3 worldPosition, int amount, Color color)
    {
        if (!battleActive) return;
        DamagePopup.Show(worldPosition + Vector3.up * .6f, amount, false, false, true)?.SetColor(color);
    }

    private static void ClearTransientBattleObjects()
    {
        foreach (CursorProjectile projectile in FindObjectsByType<CursorProjectile>(FindObjectsInactive.Include))
            if (projectile != null) Destroy(projectile.gameObject);

        foreach (TextMeshPro label in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include))
            if (label != null && label.gameObject.name.StartsWith("BattleDamage_")) Destroy(label.gameObject);

        string[] effectNames = { "CorruptedLightning", "CorruptedFire", "CorruptedWaterWave" };
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item == null) continue;
            foreach (string effectName in effectNames)
            {
                if (item.name == effectName)
                {
                    Destroy(item.gameObject);
                    break;
                }
            }
        }
    }

    public void ShowHeroHitEffect(Transform actor)
    {
        var prefab = Resources.Load<GameObject>("CombatVFX/Ending2HeroHit");
        if (actor != null && prefab != null) Instantiate(prefab, actor.position + Vector3.up * .55f, Quaternion.identity);
    }

    private void BuildBattleHUD()
    {
        if (battleHudRoot != null) return;
        var prefab = Resources.Load<GameObject>("GameUI/Ending2/BattleHUD");
        if (prefab == null) { Debug.LogError("Missing native Ending 2 HUD prefab"); return; }
        battleHudRoot = Instantiate(prefab);
        knightHealthFill = battleHudRoot.transform.Find("HeroHealth/Fill").GetComponent<Image>();
        knightHealthText = battleHudRoot.transform.Find("HeroHealth/Label").GetComponent<TextMeshProUGUI>();
        topCursorHealthFill = battleHudRoot.transform.Find("CursorHealth/Fill").GetComponent<Image>();
        cursorHealthText = battleHudRoot.transform.Find("CursorHealth/Label").GetComponent<TextMeshProUGUI>();
        hitFeedbackAnimator = battleHudRoot.GetComponent<Animator>();
        if (healthFill != null)
        {
            healthFill.sprite = topCursorHealthFill.sprite;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
        }
    }

    private void OnDestroy()
    {
        if (battleActive) { Cursor.lockState = previousLock; Cursor.visible = true; }
        if (cursorAnchor != null) Destroy(cursorAnchor.gameObject);
        if (battleHudRoot != null) Destroy(battleHudRoot);
        if (slashTrail != null) Destroy(slashTrail.gameObject);
    }

}
