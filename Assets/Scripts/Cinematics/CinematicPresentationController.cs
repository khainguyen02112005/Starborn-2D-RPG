using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CinematicPresentationController : MonoBehaviour
{
    private const string LibraryPath = "CinematicGenerated/CinematicGeneratedLibrary";

    private static CinematicPresentationController instance;
    public static CinematicPresentationController Instance => instance != null ? instance : FindAnyObjectByType<CinematicPresentationController>();
    public static bool IsCinematicActive => instance != null && instance.sequenceActive;
    public bool IsSuspended { get; private set; }

    [SerializeField] private CinematicGeneratedLibrary library;
    [SerializeField, Range(0.04f, 0.35f)] private float cameraSmoothTime = 0.16f;
    [SerializeField] private float normalCutsceneZoom = 4.25f;
    [SerializeField] private float closeCutsceneZoom = 3.1f;
    [SerializeField] private float wideCutsceneZoom = 5.7f;

    private Camera mainCamera;
    private CinemachineBrain brain;
    private bool brainWasEnabled;
    private bool cameraControlled;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;
    private Transform cameraTarget;
    private Vector3 cameraPoint;
    private bool useCameraPoint;
    private Vector3 cameraVelocity;
    private float desiredCameraSize;
    private ICinemachineCamera gameplayCamera;
    private bool cameraReturning;
    private float cameraReturnElapsed;
    private Vector3 cameraReturnPosition;
    private float cameraReturnSize;

    private Canvas overlayCanvas;
    private RectTransform overlayRoot;
    private RectTransform vfxMount;
    private Image tint;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private TextMeshProUGUI timeCaption;
    private RectTransform glitchMount;
    private Coroutine dayTransition;
    private Coroutine celebration;
    private bool sequenceActive;
    private readonly Dictionary<CanvasGroup, UiState> hiddenHud = new Dictionary<CanvasGroup, UiState>();
    private readonly Dictionary<Behaviour, bool> suspendedGameplay = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Rigidbody2D, RigidbodyInterpolation2D> cinematicInterpolation = new Dictionary<Rigidbody2D, RigidbodyInterpolation2D>();
    private readonly Dictionary<Animator, float> pauseAnimatorSpeeds = new Dictionary<Animator, float>();

    private struct UiState
    {
        public float alpha;
        public bool interactable;
        public bool blocksRaycasts;
        public bool added;
    }

    private Transform followedHero;
    private GeneratedCinematicActor heroProxy;
    private Vector3 heroProxyScale = Vector3.one;
    private readonly List<SpriteRenderer> hiddenHeroRenderers = new List<SpriteRenderer>();
    private readonly List<Animator> frozenAnimators = new List<Animator>();
    private readonly List<float> frozenAnimatorSpeeds = new List<float>();
    private readonly Dictionary<Rigidbody2D, bool> cinematicSimulation = new Dictionary<Rigidbody2D, bool>();
    private readonly List<ParticleSystem> frozenParticles = new List<ParticleSystem>();
    private readonly List<GameObject> transientObjects = new List<GameObject>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (library == null) library = Resources.Load<CinematicGeneratedLibrary>(LibraryPath);
        EnsureOverlay();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        RestoreWorldMotion();
        RestoreOriginalHero();
        RestoreCameraImmediate();
        RestoreGameplayUi();
        RestoreCinematicBodies();
    }

    public static CinematicPresentationController GetOrCreate()
    {
        CinematicPresentationController found = Instance;
        if (found != null) return found;
        GameObject go = new GameObject("CinematicPresentationController");
        return go.AddComponent<CinematicPresentationController>();
    }

    public static Canvas FindGameplayCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas candidate in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (candidate.renderMode == RenderMode.WorldSpace || candidate.GetComponentInParent<CinematicUiLayer>() != null) continue;
            if (candidate.name == "Canvas") return candidate;
            if (fallback == null) fallback = candidate;
        }
        return fallback;
    }

    public void BeginSequence(Transform hero, bool mage, bool useGeneratedHero)
    {
        if (cameraReturning) RestoreCameraImmediate();
        if (GameplayUiFocus.OpenBook != null) GameplayUiFocus.OpenBook.CloseAll();
        if (hero != null && hero.TryGetComponent(out Rigidbody2D heroBody))
        {
            if (!cinematicInterpolation.ContainsKey(heroBody)) cinematicInterpolation.Add(heroBody, heroBody.interpolation);
            if (!cinematicSimulation.ContainsKey(heroBody)) cinematicSimulation.Add(heroBody, heroBody.simulated);
            // Timeline writes transforms directly; gameplay interpolation would
            // otherwise pull the rendered actor back toward the last physics pose.
            heroBody.interpolation = RigidbodyInterpolation2D.None;
            heroBody.linearVelocity = Vector2.zero;
            heroBody.simulated = false;
        }
        EnsureOverlay();
        sequenceActive = true;
        IsSuspended = false;
        SuppressGameplayUi();
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude))
        {
            if (!suspendedGameplay.ContainsKey(enemy)) suspendedGameplay.Add(enemy, enemy.enabled);
            enemy.enabled = false;
            if (enemy.TryGetComponent(out Rigidbody2D body)) body.linearVelocity = Vector2.zero;
        }
        AcquireCamera();
        SetLetterbox(true);
        if (hero != null)
        {
            followedHero = hero;
            Focus(hero, normalCutsceneZoom);
            if (useGeneratedHero) ShowGeneratedHero(hero, mage);
        }
    }

    public void EndSequence(bool restoreHero = true)
    {
        if (!isActiveAndEnabled || !gameObject.scene.isLoaded) return;
        SetSuspended(false);
        sequenceActive = false;
        if (dayTransition != null) StopCoroutine(dayTransition);
        if (celebration != null) StopCoroutine(celebration);
        dayTransition = celebration = null;
        if (timeCaption != null) timeCaption.gameObject.SetActive(false);
        if (restoreHero) RestoreOriginalHero();
        RestoreWorldMotion();
        SetLetterbox(false);
        StartCoroutine(FadeTintTo(new Color(0f, 0f, 0f, 0f), 0.25f));
        BeginCameraReturn();
        ClearTransientObjects();
        RestoreGameplayUi();
        foreach (var pair in suspendedGameplay)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        suspendedGameplay.Clear();
        RestoreCinematicBodies();
    }

    private void RestoreCinematicBodies()
    {
        foreach (var pair in cinematicInterpolation)
            if (pair.Key != null)
            {
                pair.Key.position = pair.Key.transform.position;
                pair.Key.interpolation = pair.Value;
            }
        cinematicInterpolation.Clear();
        foreach (var pair in cinematicSimulation)
            if (pair.Key != null) pair.Key.simulated = pair.Value;
        cinematicSimulation.Clear();
    }

    public void SetSuspended(bool value)
    {
        IsSuspended = value;
        foreach (CinematicDigitalGlitch effect in GetComponentsInChildren<CinematicDigitalGlitch>(true)) effect.Suspend(value);
        if (value)
        {
            foreach (Animator animator in FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
            {
                if (pauseAnimatorSpeeds.ContainsKey(animator)) continue;
                pauseAnimatorSpeeds.Add(animator, animator.speed);
                animator.speed = 0f;
            }
        }
        else
        {
            foreach (var pair in pauseAnimatorSpeeds) if (pair.Key != null) pair.Key.speed = pair.Value;
            pauseAnimatorSpeeds.Clear();
        }
    }

    public void PlayDigitalGlitch(float duration, CinematicSignalStyle style = CinematicSignalStyle.SignalBreak)
    {
        EnsureOverlay();
        foreach (CinematicDigitalGlitch previous in glitchMount.GetComponentsInChildren<CinematicDigitalGlitch>(true))
        {
            previous.Suspend(true);
            Destroy(previous.gameObject);
        }
        if (library == null || library.digitalCorruptionMaterial == null) return;
        GameObject go = new GameObject("FullscreenDigitalCorruption", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(CinematicDigitalGlitch));
        go.transform.SetParent(glitchMount, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.GetComponent<CinematicDigitalGlitch>().Initialize(library.digitalCorruptionMaterial, duration, style);
        transientObjects.Add(go);
    }

    public void ClearScreenEffects()
    {
        if (glitchMount != null) foreach (Transform child in glitchMount) Destroy(child.gameObject);
        if (vfxMount != null) foreach (Transform child in vfxMount) Destroy(child.gameObject);
    }

    public void Focus(Transform target, float zoom = -1f)
    {
        if (target == null) return;
        AcquireCamera();
        cameraTarget = target;
        useCameraPoint = false;
        desiredCameraSize = zoom > 0f ? zoom : normalCutsceneZoom;
    }

    public void FocusHero(bool close = false)
    {
        if (followedHero != null) Focus(followedHero, close ? closeCutsceneZoom : normalCutsceneZoom);
    }

    public void FocusPoint(Vector3 point, float zoom = -1f)
    {
        AcquireCamera();
        cameraTarget = null;
        cameraPoint = point;
        useCameraPoint = true;
        desiredCameraSize = zoom > 0f ? zoom : normalCutsceneZoom;
    }

    public void WideShot(Transform a, Transform b)
    {
        if (a == null && b == null) return;
        Vector3 point = a != null && b != null ? (a.position + b.position) * 0.5f : (a != null ? a.position : b.position);
        FocusPoint(point, wideCutsceneZoom);
    }

    public void PlayHeroState(string stateName)
    {
        if (heroProxy != null) heroProxy.Play(stateName);
    }

    public void PlayHeroEmotion(int variant)
    {
        string state = variant switch
        {
            1 => "Run",
            2 => "Attack",
            3 => "Special",
            4 => "HurtDeath",
            5 => "Cinematic",
            _ => "Idle"
        };
        PlayHeroState(state);
    }

    public void ShowHeroEmotion(bool mage, bool despair)
    {
        if (followedHero == null || library == null) return;
        GameObject prefab = mage ? library.mageEmotionPrefab : library.knightEmotionPrefab;
        if (prefab == null) return;
        ShowGeneratedHero(followedHero, mage, prefab);
        heroProxy?.Play(despair ? "Despair" : "Tremble");
    }

    public GameObject PlayCombatVfx(string stateName, Vector3 position, float scale = 1f, float lifetime = 1.1f)
    {
        if (library == null || library.combatVfxPrefab == null) return null;
        GameObject effect = Instantiate(library.combatVfxPrefab, position, Quaternion.identity);
        effect.transform.localScale *= Mathf.Max(0.1f, scale);
        GeneratedCinematicVFX player = effect.GetComponent<GeneratedCinematicVFX>();
        if (player != null) player.Play(stateName, lifetime);
        transientObjects.Add(effect);
        return effect;
    }

    public void PlayScreenVfx(string stateName, float lifetime = 1.25f)
    {
        if (stateName == "TVStatic" || stateName == "Corruption")
        {
            PlayDigitalGlitch(lifetime);
            return;
        }
        // Old full-rect sprites magnified a tiny magenta orb over the whole hero.
        // Narrative VFX now stay in world space and leave the body/caption readable.
        PlayPixelFragments(followedHero != null ? followedHero.position + Vector3.up * .6f : Vector3.zero, Mathf.Min(lifetime, 1.1f));
    }

    public GameObject PlayActorCorruption(Transform target, bool mage, bool npc, string state, float seconds, bool replace = false)
    {
        if (target == null) return null;
        GameObject effect = new GameObject(target.name + "_SPUMSignalFragments", typeof(CinematicActorEffect));
        effect.GetComponent<CinematicActorEffect>().Initialize(target, state, seconds, replace);
        transientObjects.Add(effect);
        return effect;
    }

    public GameObject PlayPixelFragments(Vector3 position, float seconds = .85f, float size = 1.2f)
    {
        if (library == null || library.pixelFragmentsPrefab == null) return null;
        GameObject effect = Instantiate(library.pixelFragmentsPrefab, position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * Mathf.Clamp(size, .3f, 2.5f);
        effect.GetComponent<GeneratedCinematicVFX>()?.Play("Scatter", seconds);
        transientObjects.Add(effect);
        return effect;
    }

    public void PlayDayLaterTransition()
    {
        EnsureOverlay();
        if (dayTransition != null) StopCoroutine(dayTransition);
        dayTransition = StartCoroutine(DayLaterRoutine());
    }

    public void FadeInFromBlack(float duration)
    {
        EnsureOverlay();
        tint.color = Color.black;
        StartCoroutine(FadeTintTo(Color.clear, duration));
    }

    public void PlayRoyalHandoff(Transform messenger, Transform elder)
    {
        if (messenger == null || elder == null) return;
        StartCoroutine(RoyalLetterTravel(messenger, elder));
    }

    private IEnumerator RoyalLetterTravel(Transform messenger, Transform elder)
    {
        Vector3 start = messenger.position + new Vector3(.45f, .65f, 0f);
        Vector3 finish = elder.position + new Vector3(-.35f, .65f, 0f);
        GameObject prefab = Resources.Load<GameObject>("CinematicGenerated/RoyalDecreeHandoff");
        GameObject decree = prefab != null ? Instantiate(prefab, start, Quaternion.identity) : null;
        if (decree == null && library != null && library.royalDecreeSprite != null)
        {
            decree = new GameObject("RoyalDecreeProp", typeof(SpriteRenderer));
            SpriteRenderer renderer = decree.GetComponent<SpriteRenderer>();
            renderer.sprite = library.royalDecreeSprite;
            renderer.sortingOrder = 120;
            float height = Mathf.Max(0.01f, renderer.bounds.size.y);
            decree.transform.localScale = Vector3.one * (.38f / height);
        }
        if (decree == null) yield break;
        decree.name = "RoyalDecreeProp";
        transientObjects.Add(decree);
        Animator animator = decree.GetComponent<Animator>();
        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        float elapsed = 0f;
        while (elapsed < 1.35f && decree != null)
        {
            if (!IsSuspended) elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.05f));
            decree.transform.position = Vector3.Lerp(start, finish, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .12f;
            yield return null;
        }
        if (decree != null) Destroy(decree);
    }

    public void PlayHappyVillageMontage(Transform elder)
    {
        if (followedHero != null) WideShot(followedHero, elder);
        if (tint != null) tint.color = Color.clear;
        if (celebration == null) celebration = StartCoroutine(FireworksRoutine());
    }

    public void FreezeWorldMotion()
    {
        if (frozenAnimators.Count > 0 || frozenParticles.Count > 0) return;
        foreach (Animator animator in FindObjectsByType<Animator>(FindObjectsInactive.Exclude))
        {
            if (animator == null || animator.GetComponentInParent<GeneratedCinematicVFX>() != null
                || animator.GetComponentInParent<CinematicUiLayer>() != null
                || (followedHero != null && animator.transform.IsChildOf(followedHero))
                || (heroProxy != null && animator == heroProxy.Animator)) continue;
            frozenAnimators.Add(animator);
            frozenAnimatorSpeeds.Add(animator.speed);
            animator.speed = 0f;
        }
        foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude))
        {
            if (particles == null) continue;
            frozenParticles.Add(particles);
            particles.Pause(true);
        }
    }

    public void RestoreWorldMotion()
    {
        for (int i = 0; i < frozenAnimators.Count; i++)
            if (frozenAnimators[i] != null) frozenAnimators[i].speed = i < frozenAnimatorSpeeds.Count ? frozenAnimatorSpeeds[i] : 1f;
        foreach (ParticleSystem particles in frozenParticles)
            if (particles != null) particles.Play(true);
        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();
        frozenParticles.Clear();
    }

    public void SetLetterbox(bool visible)
    {
        EnsureOverlay();
        if (topBar != null) topBar.gameObject.SetActive(visible);
        if (bottomBar != null) bottomBar.gameObject.SetActive(visible);
    }

    private void LateUpdate()
    {
        if (sequenceActive) SuppressGameplayUi();
        if (heroProxy != null && followedHero != null)
        {
            heroProxy.transform.position = followedHero.position;
            float direction = -Mathf.Sign(followedHero.lossyScale.x);
            heroProxy.transform.localScale = new Vector3(Mathf.Abs(heroProxyScale.x), heroProxyScale.y, heroProxyScale.z);
            heroProxy.SetFacing(direction);
        }

        if (IsSuspended || !cameraControlled || mainCamera == null) return;
        if (cameraReturning)
        {
            Vector3 destination = originalCameraPosition;
            float size = originalCameraSize;
            if (gameplayCamera != null && gameplayCamera.IsValid)
            {
                gameplayCamera.UpdateCameraState(Vector3.up, Time.unscaledDeltaTime);
                destination = gameplayCamera.State.GetFinalPosition();
                size = gameplayCamera.State.Lens.OrthographicSize;
            }
            cameraReturnElapsed += Time.unscaledDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, cameraReturnElapsed / .4f);
            mainCamera.transform.position = Vector3.Lerp(cameraReturnPosition, destination, blend);
            mainCamera.orthographicSize = Mathf.Lerp(cameraReturnSize, size, blend);
            if (blend >= 1f)
            {
                if (brain != null) brain.enabled = brainWasEnabled;
                cameraReturning = cameraControlled = false;
            }
            return;
        }
        Vector3 focus = useCameraPoint ? cameraPoint : cameraTarget != null ? cameraTarget.position : mainCamera.transform.position;
        focus.z = originalCameraPosition.z;
        mainCamera.transform.position = Vector3.SmoothDamp(mainCamera.transform.position, focus, ref cameraVelocity, cameraSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, desiredCameraSize, 1f - Mathf.Exp(-6.5f * Time.unscaledDeltaTime));
    }

    private void ShowGeneratedHero(Transform hero, bool mage, GameObject overridePrefab = null)
    {
        RestoreOriginalHero();
        if (library == null) library = Resources.Load<CinematicGeneratedLibrary>(LibraryPath);
        GameObject prefab = overridePrefab != null ? overridePrefab : library != null ? (mage ? library.magePrefab : library.knightPrefab) : null;
        if (prefab == null || hero == null) return;

        Bounds sourceBounds = default;
        bool hasBounds = false;
        foreach (SpriteRenderer renderer in hero.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!hasBounds) { sourceBounds = renderer.bounds; hasBounds = true; }
            else sourceBounds.Encapsulate(renderer.bounds);
            hiddenHeroRenderers.Add(renderer);
            renderer.enabled = false;
        }

        GameObject proxyObject = Instantiate(prefab, hero.position, Quaternion.identity);
        proxyObject.name = mage ? "GeneratedMageCutsceneProxy" : "GeneratedKnightCutsceneProxy";
        heroProxy = proxyObject.GetComponent<GeneratedCinematicActor>();
        if (heroProxy == null) heroProxy = proxyObject.AddComponent<GeneratedCinematicActor>();
        float sourceHeight = hasBounds ? Mathf.Clamp(sourceBounds.size.y, 0.8f, 4f) : 1.65f;
        float proxyHeight = heroProxy.Renderer != null ? Mathf.Max(0.01f, heroProxy.Renderer.bounds.size.y) : 1f;
        float scale = sourceHeight / proxyHeight;
        heroProxyScale = Vector3.one * scale;
        proxyObject.transform.localScale = heroProxyScale;
        heroProxy.Play("Idle");
    }

    public void RestoreOriginalHero()
    {
        foreach (SpriteRenderer renderer in hiddenHeroRenderers)
            if (renderer != null) renderer.enabled = true;
        hiddenHeroRenderers.Clear();
        if (heroProxy != null) Destroy(heroProxy.gameObject);
        heroProxy = null;
    }

    private void AcquireCamera()
    {
        if (cameraControlled && mainCamera != null) return;
        mainCamera = Camera.main;
        if (mainCamera == null) return;
        brain = mainCamera.GetComponent<CinemachineBrain>();
        brainWasEnabled = brain != null && brain.enabled;
        gameplayCamera = brain != null ? brain.ActiveVirtualCamera : null;
        if (brain != null) brain.enabled = false;
        originalCameraPosition = mainCamera.transform.position;
        originalCameraSize = mainCamera.orthographicSize;
        desiredCameraSize = originalCameraSize;
        cameraControlled = true;
    }

    private void BeginCameraReturn()
    {
        if (!cameraControlled) return;
        if (mainCamera == null || !brainWasEnabled || gameplayCamera == null)
        {
            RestoreCameraImmediate();
            return;
        }
        cameraReturning = true;
        cameraReturnElapsed = 0f;
        cameraReturnPosition = mainCamera.transform.position;
        cameraReturnSize = mainCamera.orthographicSize;
    }

    private void RestoreCameraImmediate()
    {
        if (!cameraControlled) return;
        cameraTarget = null;
        useCameraPoint = false;
        cameraVelocity = Vector3.zero;
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.orthographicSize = originalCameraSize;
        }
        if (brain != null) brain.enabled = brainWasEnabled;
        cameraReturning = false;
        cameraControlled = false;
    }

    private IEnumerator DayLaterRoutine()
    {
        if (timeCaption != null) timeCaption.gameObject.SetActive(false);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (tint != null) tint.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, elapsed));
            yield return null;
        }
        if (tint != null) tint.color = Color.black;
        if (timeCaption != null)
        {
            timeCaption.text = "1 ngày sau";
            timeCaption.gameObject.SetActive(true);
            timeCaption.alpha = 0f;
        }
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (timeCaption != null) timeCaption.alpha = Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1.5f);
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (timeCaption != null) timeCaption.alpha = 1f - Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }
        if (timeCaption != null) timeCaption.gameObject.SetActive(false);
        yield return FadeTintTo(Color.clear, 1.1f);
        dayTransition = null;
    }

    private IEnumerator FireworksRoutine()
    {
        // A sequence of separate sprite bursts leaves the village's colors untouched.
        for (int i = 0; i < 18; i++)
        {
            if (!sequenceActive) break;
            if (library != null && library.fireworkPrefab != null && mainCamera != null)
            {
                Vector3 point = mainCamera.ViewportToWorldPoint(new Vector3(Random.Range(0.12f, 0.88f), Random.Range(0.56f, 0.82f), -mainCamera.transform.position.z));
                point.z = 0f;
                GameObject burst = Instantiate(library.fireworkPrefab, point, Quaternion.identity);
                burst.transform.localScale *= Random.Range(1.1f, 1.8f);
                burst.GetComponent<GeneratedCinematicVFX>()?.Play(i % 3 == 0 ? "Gold" : i % 3 == 1 ? "Cyan" : "Rose", 1.6f);
                transientObjects.Add(burst);
            }
            yield return new WaitForSecondsRealtime(0.38f);
        }
        celebration = null;
    }

    public static bool IsPresentationUi(Transform root)
    {
        if (root.GetComponentInParent<CinematicUiLayer>() != null) return true;
        string name = root.name;
        return name == "CorruptionStoryCard" || name == "GlitchPauseOverlay" || name == "ChoicePanel"
            || name == "EndingPanel" || name == "CursorBattleRoot" || name == "PauseMenuUI"
            || name == "SettingsMenuUI" || name == "CinematicBlurTint";
    }

    private void SuppressGameplayUi()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (canvas == overlayCanvas)
            {
                // A late gameplay script may have selected this canvas with FindAnyObjectByType.
                // Keep only our owned layers and explicitly registered cinematic content here.
                foreach (Transform child in canvas.transform)
                {
                    bool owned = child == tint.transform || child == topBar || child == bottomBar
                        || child == timeCaption.transform || child == vfxMount;
                    if (!owned && child.GetComponent<CinematicUiLayer>() == null) HideUiRoot(child.gameObject);
                }
                continue;
            }
            if (canvas.GetComponentInParent<CinematicUiLayer>() != null) continue;
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                HideUiRoot(canvas.gameObject);
                continue;
            }
            foreach (Transform child in canvas.transform)
                if (!IsPresentationUi(child)) HideUiRoot(child.gameObject);
        }
    }

    private void HideUiRoot(GameObject root)
    {
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        bool added = group == null;
        if (added) group = root.AddComponent<CanvasGroup>();
        if (!hiddenHud.ContainsKey(group))
            hiddenHud.Add(group, new UiState { alpha = group.alpha, interactable = group.interactable, blocksRaycasts = group.blocksRaycasts, added = added });
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void RestoreGameplayUi()
    {
        foreach (var pair in hiddenHud)
        {
            if (pair.Key == null) continue;
            pair.Key.alpha = pair.Value.alpha;
            pair.Key.interactable = pair.Value.interactable;
            pair.Key.blocksRaycasts = pair.Value.blocksRaycasts;
            if (pair.Value.added) Destroy(pair.Key);
        }
        hiddenHud.Clear();
    }

    private IEnumerator FadeTintTo(Color target, float duration)
    {
        EnsureOverlay();
        Color start = tint != null ? tint.color : Color.clear;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (tint != null) tint.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
            yield return null;
        }
        if (tint != null) tint.color = target;
    }

    private IEnumerator FloatAndFade(GameObject target, float duration)
    {
        if (target == null) yield break;
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        Vector3 start = target.transform.position;
        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.transform.position = start + Vector3.up * (0.28f * Mathf.Sin(t * Mathf.PI));
            if (renderer != null)
            {
                Color c = renderer.color;
                c.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1f, t));
                renderer.color = c;
            }
            yield return null;
        }
        if (target != null) Destroy(target);
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null) return;
        GameObject canvasObject = new GameObject("CinematicPresentationCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 28000;
        canvasObject.AddComponent<CinematicUiLayer>();
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        overlayRoot = canvasObject.GetComponent<RectTransform>();

        tint = CreateImage(overlayRoot, "CinematicTint", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        tint.raycastTarget = false;
        tint.transform.SetAsFirstSibling();

        topBar = CreateImage(overlayRoot, "LetterboxTop", new Vector2(0f, 0.875f), Vector2.one, new Color(0.01f, 0.012f, 0.022f, 0.98f)).rectTransform;
        bottomBar = CreateImage(overlayRoot, "LetterboxBottom", Vector2.zero, new Vector2(1f, 0.15f), new Color(0.01f, 0.012f, 0.022f, 0.98f)).rectTransform;
        topBar.gameObject.SetActive(false);
        bottomBar.gameObject.SetActive(false);

        GameObject captionObject = new GameObject("TimePassageCaption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        captionObject.transform.SetParent(overlayRoot, false);
        RectTransform captionRect = captionObject.GetComponent<RectTransform>();
        captionRect.anchorMin = new Vector2(0.18f, 0.42f);
        captionRect.anchorMax = new Vector2(0.82f, 0.6f);
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;
        timeCaption = captionObject.GetComponent<TextMeshProUGUI>();
        timeCaption.alignment = TextAlignmentOptions.Center;
        timeCaption.fontSize = 42f;
        timeCaption.fontStyle = FontStyles.Bold;
        timeCaption.color = new Color(0.96f, 0.96f, 1f, 1f);
        timeCaption.raycastTarget = false;
        captionObject.SetActive(false);

        GameObject mount = new GameObject("GeneratedOverlayVFX", typeof(RectTransform));
        mount.transform.SetParent(overlayRoot, false);
        vfxMount = mount.GetComponent<RectTransform>();
        vfxMount.anchorMin = Vector2.zero;
        vfxMount.anchorMax = Vector2.one;
        vfxMount.offsetMin = Vector2.zero;
        vfxMount.offsetMax = Vector2.zero;

        GameObject glitchObject = new GameObject("CinematicGlitchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CinematicUiLayer));
        glitchObject.transform.SetParent(transform, false);
        Canvas glitchCanvas = glitchObject.GetComponent<Canvas>();
        glitchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        glitchCanvas.sortingOrder = 31000;
        CanvasScaler glitchScaler = glitchObject.GetComponent<CanvasScaler>();
        glitchScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        glitchScaler.referenceResolution = new Vector2(1920f, 1080f);
        glitchMount = glitchObject.GetComponent<RectTransform>();
    }

    private static Image CreateImage(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void ClearTransientObjects()
    {
        foreach (GameObject item in transientObjects)
            if (item != null) Destroy(item);
        transientObjects.Clear();
    }
}
