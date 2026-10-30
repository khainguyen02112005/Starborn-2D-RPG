using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class Level04Controller : MonoBehaviour, INotificationReceiver
{
    private const string PendingOpeningCutsceneKey = "RPG.PendingOpeningCutscene";
    private const string PendingResetCutsceneKey = "RPG.PendingResetCutscene";

    public GameObject normalPlayerUI;
    public GameObject glitchOverlay;
    public RectTransform glitchCard;
    public TextMeshProUGUI storyText;
    public GameObject choicePanel;
    public GameObject endingPanel;
    public TextMeshProUGUI endingTitle;
    public TextMeshProUGUI endingBody;
    public GameObject resetStoryButton;
    public CursorBattleController cursorBattle;
    public CorruptedKnightBoss corruptedKnight;

    [Header("Timeline Cutscenes")]
    public PlayableDirector cutsceneDirector;
    public TimelineAsset openingVillageTimeline;
    public TimelineAsset resetLoopTimeline;
    public TimelineAsset noDeathEndingTimeline;
    public TimelineAsset corruptionTimeline;
    public TimelineAsset knightDefeatedTimeline;
    public TimelineAsset whiteVoidTimeline;
    public TimelineAsset royalDecreeTimeline;
    public GameObject royalMessengerPrefab;

    [Header("Village opening — centre of the paved road")]
    [SerializeField] private Vector3 openingRoadCenter = new Vector3(-55f, 10.4f, 0f);
    [SerializeField] private GameObject openingMonsterPrefab;

    [Header("PAUSE ERROR + KÍNH VỠ - CHỈNH HIỆU ỨNG Ở ĐÂY")]
    [SerializeField, Range(8, 48)] private int errorWindowCount = 28;
    [SerializeField, Range(0.02f, 0.2f)] private float errorWindowInterval = 0.065f;
    [SerializeField, Range(4, 18)] private int glassColumns = 14;
    [SerializeField, Range(3, 12)] private int glassRows = 10;
    [SerializeField, Range(0.8f, 3f)] private float glassFallDuration = 1.75f;
    [SerializeField, Range(0f, 0.45f)] private float glassVertexJitter = 0.24f;
    [SerializeField] private Color glassTint = new Color(0.92f, 0.97f, 1f, 0.98f);
    [SerializeField] private Color glassEdgeColor = new Color(0.76f, 0.94f, 1f, 0.92f);

    private bool sequenceActive;
    private bool glitching;
    private bool endingStarted;
    private bool endingConclusionRunning;
    private PauseMenuManager pauseManager;
    private Transform knight;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private ElementalSpellController playerSpells;
    private Canvas canvas;
    private Image cinematicTint;
    private readonly List<GameObject> transientObjects = new List<GameObject>();
    private readonly List<GameObject> hiddenGameplayUi = new List<GameObject>();
    private readonly List<RectTransform> blurSlices = new List<RectTransform>();
    private Vector3 villageSpawnPosition;
    private GameObject timelineMonster;
    private readonly List<GameObject> openingMonsters = new List<GameObject>();
    private System.Action timelineStoppedAction;
    private CinematicPresentationController presentation;
    private GameObject royalMessenger;
    private Coroutine royalMessengerWalk;
    private Coroutine royalHandoff;
    private Coroutine royalAudienceStaging;
    private Vector3 royalDeliveryPosition;
    private Transform villageElder;
    private System.Action royalDecreeCompleted;
    private bool corruptionPhase;
    private int timelineHoldDepth;
    private bool resumeAfterTimelineHold;
    private readonly HashSet<int> openingStrikes = new HashSet<int>();
    private readonly List<GameObject> hiddenOpeningEnemies = new List<GameObject>();
    private readonly List<Renderer> erasedEndingWorld = new List<Renderer>();
    private bool whiteVoidComplete;
    private bool quietNarrativePause;
    private bool resettingStory;
    private StoryFilmRewind filmRewind;
    private float CinematicDelta => glitching ? 0f : Time.unscaledDeltaTime;

    public bool CanRecordRewind => sequenceActive && !glitching && !resettingStory && (endingPanel == null || !endingPanel.activeSelf);
    public bool NarrativePauseSilent => quietNarrativePause || (knight != null && knight.GetComponent<PlayerSPUMAnimation>() != null && knight.GetComponent<PlayerSPUMAnimation>().IsDead);
    public bool NarrativePauseAvailable => sequenceActive && (corruptionPhase || endingConclusionRunning) && !glitching && !resettingStory;

    private bool IsMage => PlayerPrefs.GetString("PlayerClass", "Hiệp Sĩ").Contains("Pháp");

    private void OnDisable()
    {
        if (cutsceneDirector != null) cutsceneDirector.stopped -= HandleTimelineStopped;
        timelineStoppedAction = null;
        resumeAfterTimelineHold = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;
        pauseManager = FindAnyObjectByType<PauseMenuManager>();
        canvas = CinematicPresentationController.FindGameplayCanvas();
        if (cutsceneDirector == null) cutsceneDirector = GetComponent<PlayableDirector>();
        if (cutsceneDirector == null) cutsceneDirector = gameObject.AddComponent<PlayableDirector>();
        presentation = CinematicPresentationController.GetOrCreate();
        knight = corruptedKnight != null ? corruptedKnight.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (knight != null)
        {
            villageSpawnPosition = knight.position;
            playerMovement = knight.GetComponent<PlayerMovement>();
            playerCombat = knight.GetComponent<PlayerCombat>();
            playerSpells = knight.GetComponent<ElementalSpellController>();
        }
        if (normalPlayerUI != null) normalPlayerUI.SetActive(true);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        if (glitchOverlay != null) glitchOverlay.SetActive(false);
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        SetStoryVisible(false);
        ConfigureCinematicUi();

        filmRewind = GetComponent<StoryFilmRewind>();
        if (filmRewind == null) filmRewind = gameObject.AddComponent<StoryFilmRewind>();
        filmRewind.Bind(this);

        if (PlayerPrefs.GetInt(PendingResetCutsceneKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PendingResetCutsceneKey);
            PlayerPrefs.Save();
            if (!PlayVillageTimeline(true)) StartCoroutine(ResetVillageOpeningCutscene());
        }
        else if (PlayerPrefs.GetInt(PendingOpeningCutsceneKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PendingOpeningCutsceneKey);
            PlayerPrefs.Save();
            if (!PlayVillageTimeline(false)) StartCoroutine(OpeningVillageCutscene());
        }
    }

    private void Update()
    {
        if (resettingStory) return;
        if (NarrativePauseAvailable && Input.GetKeyDown(KeyCode.Escape))
            StartCoroutine(GlitchPause());

        // Phím tắt Debug để xem trực tiếp cutscene khi đang Play
        if (Input.GetKeyDown(KeyCode.F1)) DebugPlayOpeningVillage();
        else if (Input.GetKeyDown(KeyCode.F2)) DebugPlayRoyalDecree();
        else if (Input.GetKeyDown(KeyCode.F3)) DebugPlayEnding1();
        else if (Input.GetKeyDown(KeyCode.F4)) DebugPlayEnding2();
        else if (Input.GetKeyDown(KeyCode.F5)) DebugPlayEnding3();
        else if (Input.GetKeyDown(KeyCode.F6)) DebugPlayGlitchPause();
    }

    private void LateUpdate()
    {
        // An NPC/prop Animator must not bring an erased piece back in the void.
        if (!whiteVoidComplete) return;
        foreach (Renderer piece in erasedEndingWorld)
            if (piece != null) piece.enabled = false;
    }

    private void EnsureDebugContext()
    {
        if (cutsceneDirector == null) cutsceneDirector = GetComponent<PlayableDirector>();
        if (cutsceneDirector == null) cutsceneDirector = gameObject.AddComponent<PlayableDirector>();
        if (presentation == null) presentation = CinematicPresentationController.GetOrCreate();
        if (knight == null) knight = corruptedKnight != null ? corruptedKnight.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (pauseManager == null) pauseManager = FindAnyObjectByType<PauseMenuManager>();
        if (canvas == null) canvas = CinematicPresentationController.FindGameplayCanvas();
    }

    [ContextMenu("▶ [Debug] 1. Xem Cutscene Mo Dau (Opening Village)")]
    public void DebugPlayOpeningVillage()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        if (!PlayVillageTimeline(false)) StartCoroutine(OpeningVillageCutscene());
    }

    [ContextMenu("▶ [Debug] 2. Xem Su Gia Hoang Gia (Royal Decree)")]
    public void DebugPlayRoyalDecree()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        BeginRoyalDecreeSequence(null);
    }

    [ContextMenu("▶ [Debug] 3. Xem Ket 1 (Happy To Bad Ending)")]
    public void DebugPlayEnding1()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        presentation?.BeginSequence(knight, IsMage, false);
        if (!PlayTimeline(noDeathEndingTimeline, null))
            StartCoroutine(NoDeathEnding());
    }

    [ContextMenu("▶ [Debug] 4. Xem Ket 2 (Knight Defeated - Reset Loop)")]
    public void DebugPlayEnding2()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        if (!PlayKnightDefeatedTimeline())
            StartCoroutine(KnightDefeatedEnding());
    }

    [ContextMenu("▶ [Debug] 5. Xem Ket 3 (Khoang Trang - White Void)")]
    public void DebugPlayEnding3()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        if (!PlayWhiteVoidTimeline())
            StartCoroutine(WhiteVoidEnding(false));
    }

    [ContextMenu("▶ [Debug] 6. Xem Vo Kinh va Popup Loi (Glitch Pause)")]
    public void DebugPlayGlitchPause()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Vui lòng bật PLAY trước khi kích hoạt cutscene!"); return; }
        EnsureDebugContext();
        StartCoroutine(GlitchPause());
    }

    public void BeginEndingSequence()
    {
        if (endingStarted) return;
        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        presentation?.BeginSequence(knight, IsMage, false);
        bool noDeath = GameProgressManager.Instance != null && GameProgressManager.Instance.DeathCount == 0;
        TimelineAsset selectedTimeline = noDeath ? noDeathEndingTimeline : corruptionTimeline;
        corruptionPhase = !noDeath;
        if (!PlayTimeline(selectedTimeline, null))
            StartCoroutine(noDeath ? NoDeathEnding() : CorruptionIntroduction());
    }

    private bool PlayVillageTimeline(bool resetLoop)
    {
        TimelineAsset asset = resetLoop ? resetLoopTimeline : openingVillageTimeline;
        if (asset == null) return false;

        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        presentation?.BeginSequence(knight, IsMage, false);
        PrepareOpeningStage(out _, out _, out timelineMonster);
        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        animation?.Revive();

        return PlayTimeline(asset, () =>
        {
            SetStoryVisible(false);
            if (resetLoop)
            {
                if (endingPanel == null || !endingPanel.activeSelf) ShowLoopEndingPanel();
                return;
            }

            foreach (GameObject monster in openingMonsters)
                if (monster != null) Destroy(monster);
            openingMonsters.Clear();
            timelineMonster = null;
            if (knight != null) knight.position = villageSpawnPosition;
            RestoreGameplayAfterReset();
        });
    }

    public bool BeginRoyalDecreeSequence(System.Action onComplete)
    {
        if (royalDecreeTimeline == null || sequenceActive) return false;

        if (knight == null) knight = GameObject.FindGameObjectWithTag("Player")?.transform;
        villageElder = FindVillageElder();
        if (knight == null || villageElder == null) return false;

        royalDecreeCompleted = onComplete;
        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        PrepareRoyalMessenger();
        presentation ??= CinematicPresentationController.GetOrCreate();
        presentation.BeginSequence(knight, IsMage, false);
        presentation.WideShot(knight, villageElder);
        royalAudienceStaging = StartCoroutine(StageRoyalAudienceUnderFade());

        bool started = PlayTimeline(royalDecreeTimeline, CompleteRoyalDecreeSequence);
        if (!started) CompleteRoyalDecreeSequence();
        return started;
    }

    private void CompleteRoyalDecreeSequence()
    {
        if (royalMessengerWalk != null) StopCoroutine(royalMessengerWalk);
        if (royalHandoff != null) StopCoroutine(royalHandoff);
        if (royalAudienceStaging != null) StopCoroutine(royalAudienceStaging);
        royalMessengerWalk = royalHandoff = royalAudienceStaging = null;
        SetStoryVisible(false);
        if (royalMessenger != null) Destroy(royalMessenger);
        royalMessenger = null;
        presentation?.EndSequence(true);
        RestoreGameplayAfterReset();
        System.Action completed = royalDecreeCompleted;
        royalDecreeCompleted = null;
        completed?.Invoke();
    }

    private Transform FindVillageElder()
    {
        if (villageElder != null) return villageElder;
        foreach (NPCDialogue dialogue in FindObjectsByType<NPCDialogue>(FindObjectsInactive.Include))
        {
            if (dialogue != null && dialogue.role == NPCDialogue.DialogueRole.VillageElder)
            {
                villageElder = dialogue.transform;
                return villageElder;
            }
        }
        villageElder = GameObject.Find("TruongLang")?.transform;
        return villageElder;
    }

    private void PrepareRoyalMessenger()
    {
        if (royalMessenger != null) Destroy(royalMessenger);
        GameObject prefab = royalMessengerPrefab;
        if (prefab == null)
        {
            CinematicGeneratedLibrary library = Resources.Load<CinematicGeneratedLibrary>("CinematicGenerated/CinematicGeneratedLibrary");
            if (library != null) prefab = library.messengerPrefab;
        }
        if (prefab == null || villageElder == null) return;

        var layout = FindAnyObjectByType<MapSceneLayout>();
        royalDeliveryPosition = villageElder.position + new Vector3(-1.5f, -1.1f, 0f);
        Vector3 approach = villageElder.position + new Vector3(-7f, -1.1f, 0f);
        if (layout != null)
        {
            royalDeliveryPosition = layout.ResolveSafePosition(royalDeliveryPosition, villageElder.position + Vector3.down * 1.5f);
            approach = layout.ResolveSafePosition(approach, royalDeliveryPosition + Vector3.left * 3f);
        }
        royalMessenger = Instantiate(prefab, approach, Quaternion.identity);
        royalMessenger.name = "RoyalMessenger_Cutscene";
        royalMessenger.tag = "Untagged";
        foreach (EnemyAI ai in royalMessenger.GetComponentsInChildren<EnemyAI>(true)) ai.enabled = false;
        foreach (PlayerMovement movement in royalMessenger.GetComponentsInChildren<PlayerMovement>(true)) movement.enabled = false;
        foreach (Collider2D collider in royalMessenger.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        Rigidbody2D body = royalMessenger.GetComponent<Rigidbody2D>();
        if (body != null) body.simulated = false;
        if (royalMessenger.GetComponent<EnemySPUMAnimation>() == null)
            royalMessenger.AddComponent<EnemySPUMAnimation>();
        if (royalMessenger.GetComponent<MapDepthSort>() == null) royalMessenger.AddComponent<MapDepthSort>();
        FaceTransform(royalMessenger.transform, true);
    }

    private IEnumerator MoveRoyalMessengerToElder()
    {
        if (royalMessenger == null || FindVillageElder() == null) yield break;
        Transform messenger = royalMessenger.transform;
        Vector3 start = messenger.position;
        Vector3 destination = royalDeliveryPosition;
        EnemySPUMAnimation animation = royalMessenger.GetComponent<EnemySPUMAnimation>();
        FaceTransform(messenger, destination.x > start.x);
        animation?.SetMoving(true);
        presentation?.Focus(messenger, 4.5f);
        var route = EnemyAI.FindRoute(start, destination, .32f);
        foreach (Vector2 waypoint in route)
        {
            while (messenger != null && Vector2.Distance(messenger.position, waypoint) > .015f)
            {
                Vector3 next = Vector3.MoveTowards(messenger.position, waypoint, CinematicDelta * 3.1f);
                if (Mathf.Abs(next.x - messenger.position.x) > .002f) FaceTransform(messenger, next.x > messenger.position.x);
                messenger.position = next;
                yield return null;
            }
        }
        if (messenger != null && route.Count > 0) messenger.position = destination;
        if (messenger != null) FaceTransform(messenger, villageElder.position.x > messenger.position.x);
        animation?.SetMoving(false);
        presentation?.WideShot(messenger, villageElder);
        royalMessengerWalk = null;
    }

    private IEnumerator StageRoyalAudienceUnderFade()
    {
        // DayLaterTransition is fully black here: stage the hero on the other side
        // of the elder, away from the messenger's approach and delivery position.
        yield return WaitCinematicSeconds(1.15f);
        if (knight == null || villageElder == null) yield break;
        Vector3 audience = villageElder.position + new Vector3(-1.1f, -2.4f, 0f);
        var layout = FindAnyObjectByType<MapSceneLayout>();
        knight.position = layout != null ? layout.ResolveSafePosition(audience, villageElder.position + Vector3.down * 2f) : audience;
        if (knight.TryGetComponent(out Rigidbody2D body)) { body.linearVelocity = Vector2.zero; body.position = knight.position; }
        FaceTransform(knight, villageElder.position.x > knight.position.x);
        knight.GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
        royalAudienceStaging = null;
    }

    private IEnumerator RoyalLetterHandoff()
    {
        if (royalMessenger == null || FindVillageElder() == null) yield break;
        Transform messenger = royalMessenger.transform;
        EnemySPUMAnimation animation = royalMessenger.GetComponent<EnemySPUMAnimation>();
        animation?.SetMoving(false);
        FaceTransform(messenger, true);
        presentation?.WideShot(messenger, villageElder);
        presentation?.PlayRoyalHandoff(messenger, villageElder);
        yield return WaitCinematicSeconds(1.2f);
        if (messenger == null) yield break;
        Vector3 start = messenger.position;
        Vector3 finish = start + Vector3.down * .3f;
        var layout = FindAnyObjectByType<MapSceneLayout>();
        if (layout != null) finish = layout.ResolveSafePosition(finish, start);
        FaceTransform(messenger, false);
        animation?.SetMoving(true);
        float elapsed = 0f;
        while (elapsed < .4f && messenger != null)
        {
            elapsed += CinematicDelta;
            messenger.position = Vector3.Lerp(start, finish, Mathf.SmoothStep(0f, 1f, elapsed / .4f));
            yield return null;
        }
        animation?.SetMoving(false);
        if (messenger != null) FaceTransform(messenger, true);
        royalHandoff = null;
    }

    private bool PlayTimeline(TimelineAsset asset, System.Action onStopped)
    {
        if (asset == null || cutsceneDirector == null) return false;
        cutsceneDirector.stopped -= HandleTimelineStopped;
        if (cutsceneDirector.state == PlayState.Playing) cutsceneDirector.Stop();
        timelineStoppedAction = onStopped;
        timelineHoldDepth = 0;
        resumeAfterTimelineHold = false;
        ConfigureCinematicUi();
        cutsceneDirector.playableAsset = asset;
        cutsceneDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        cutsceneDirector.extrapolationMode = DirectorWrapMode.None;
        BindTimelineTracks(asset);
        cutsceneDirector.stopped += HandleTimelineStopped;
        cutsceneDirector.Play();
        if (cutsceneDirector.playableGraph.IsValid())
            for (int i = 0; i < cutsceneDirector.playableGraph.GetRootPlayableCount(); i++)
                cutsceneDirector.playableGraph.GetRootPlayable(i).SetSpeed(1.12);
        return true;
    }

    private void BindTimelineTracks(TimelineAsset asset)
    {
        if (cutsceneDirector == null || asset == null) return;
        PlayerSPUMAnimation playerAnimation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        Animator knightAnimator = playerAnimation != null
            ? playerAnimation.ActiveAnimator
            : knight != null ? knight.GetComponentInChildren<Animator>(true) : null;
        Animator monsterAnimator = timelineMonster != null ? timelineMonster.GetComponentInChildren<Animator>(true) : null;
        foreach (TrackAsset track in asset.GetOutputTracks())
        {
            if (track == null) continue;
            string trackName = track.name;
            if (trackName == "Knight Movement") cutsceneDirector.SetGenericBinding(track, knight);
            else if (trackName == "Knight Animation")
                // SPUM keeps its own Animator running; markers select the actual class-specific clips.
                // A held idle AnimationTrack otherwise overrides every melee/magic trigger.
                cutsceneDirector.SetGenericBinding(track, null);
            else if (trackName == "Monster Movement") cutsceneDirector.SetGenericBinding(track, timelineMonster != null ? timelineMonster.transform : null);
            else if (trackName == "Monster Animation") cutsceneDirector.SetGenericBinding(track, monsterAnimator);
            else if (trackName == "Monster Activation") cutsceneDirector.SetGenericBinding(track, timelineMonster);
        }
        cutsceneDirector.RebindPlayableGraphOutputs();
    }

    private void HandleTimelineStopped(PlayableDirector director)
    {
        director.stopped -= HandleTimelineStopped;
        System.Action completed = timelineStoppedAction;
        timelineStoppedAction = null;
        completed?.Invoke();
    }

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is not CinematicEventMarker marker) return;
        switch (marker.action)
        {
            case TimelineCinematicAction.Caption:
                SetStoryVisible(true);
                if (storyText != null)
                    storyText.text = "<color=#DAB269><b>" + PlayerIdentity.Literal(PlayerIdentity.ResolveSpeaker(marker.speaker)) + "</b></color>\n" + marker.line;
                float extraReading = ExtraTimelineReadingTime(marker);
                if (extraReading > .05f) StartCoroutine(BlockingCinematic(HoldCaptionForReading(extraReading)));
                break;
            case TimelineCinematicAction.HideCaption:
                SetStoryVisible(false);
                break;
            case TimelineCinematicAction.TelevisionGlitch:
                StartCoroutine(TelevisionBreakdown(5.5f, true));
                break;
            case TimelineCinematicAction.FireballToCursor:
                StartCoroutine(BlockingCinematic(FireballToCursor()));
                break;
            case TimelineCinematicAction.WishCelebration:
                presentation?.PlayHappyVillageMontage(FindVillageElder());
                StartCoroutine(WishCelebration());
                break;
            case TimelineCinematicAction.CameraShakeLight:
                CameraShakeManager.instance?.ShakeLight();
                break;
            case TimelineCinematicAction.CameraShakeHeavy:
                CameraShakeManager.instance?.ShakeHeavy();
                break;
            case TimelineCinematicAction.ShowChoice:
                SetStoryVisible(false);
                if (choicePanel != null) choicePanel.SetActive(true);
                break;
            case TimelineCinematicAction.ShowHappyEnding:
                ShowEndingPanel("HAPPY ENDING", "HẠNH PHÚC MÃI MÃI VỀ SAU", false);
                break;
            case TimelineCinematicAction.CorruptHappyEnding:
                ShowEndingPanel("BAD ENDING", "HẠNH PHÚC MÃI MÃI VỀ SAU", false);
                GameProgressManager.UnlockEnding(1);
                // Title and signal failure change on the very same Timeline marker.
                StartCoroutine(TelevisionBreakdown(1.15f));
                break;
            case TimelineCinematicAction.ShowLoopEnding:
                ShowLoopEndingPanel();
                break;
            case TimelineCinematicAction.ShowResetEnding:
                ShowEndingPanel("TRUE ENDING", "BẢO VỆ THẾ GIỚI GIẢ TẠO\nĐÁNH ĐỔI BẰNG CẢM XÚC\n\nVòng lặp không bao giờ kết thúc.", true);
                break;
            case TimelineCinematicAction.MaterializeReset:
                StartCoroutine(MaterializeResetSignal());
                break;
            case TimelineCinematicAction.EraseWorldToWhite:
                StartCoroutine(EraseWorldTimelineBridge());
                break;
            case TimelineCinematicAction.ShowWhiteVoidEnding:
                ShowEndingPanel("TRUE ENDING\nKẾT QUẢ ĐÃ ĐƯỢC ĐỊNH SẴN",
                    "Nhân vật chính có thể thất bại.\nCó thể đau đớn.\nCó thể chết vô số lần.\nNhưng sẽ không bao giờ được phép thật sự kết thúc.\n\nRời khỏi nơi này không đồng nghĩa với việc câu chuyện đã dừng lại.", false);
                GameProgressManager.UnlockEnding(3);
                break;
            case TimelineCinematicAction.KnightMagic:
                knight?.GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
                presentation?.PlayHeroState("Special");
                StrikeStoryWorld(1.15f);
                break;
            case TimelineCinematicAction.KnightMelee:
                knight?.GetComponent<PlayerSPUMAnimation>()?.PlayMeleeAttack();
                presentation?.PlayHeroState("Attack");
                StrikeStoryWorld(.9f);
                break;
            case TimelineCinematicAction.KnightRevive:
                knight?.GetComponent<PlayerSPUMAnimation>()?.Revive();
                presentation?.PlayHeroState("Idle");
                break;
            case TimelineCinematicAction.KnightMoveStart:
                knight?.GetComponent<PlayerSPUMAnimation>()?.SetMoving(true);
                presentation?.PlayHeroState("Run");
                break;
            case TimelineCinematicAction.KnightMoveStop:
                knight?.GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
                presentation?.PlayHeroState("Idle");
                break;
            case TimelineCinematicAction.KnightDamaged:
                knight?.GetComponent<PlayerSPUMAnimation>()?.PlayDamaged();
                presentation?.PlayHeroState("HurtDeath");
                break;
            case TimelineCinematicAction.KnightDeath:
                if (endingConclusionRunning) quietNarrativePause = true;
                knight?.GetComponent<PlayerSPUMAnimation>()?.PlayDeath();
                presentation?.PlayHeroState("HurtDeath");
                break;
            case TimelineCinematicAction.KnightSelfDestruct:
                quietNarrativePause = true;
                StartCoroutine(BlockingCinematic(KnightSelfDestructAndReform()));
                break;
            case TimelineCinematicAction.OpeningStrike:
                StartCoroutine(ResolveOpeningStrike(marker.variant));
                break;
            case TimelineCinematicAction.OpeningMonsterAttack:
                TriggerOpeningMonsterAttack(marker.variant);
                break;
            case TimelineCinematicAction.OpeningMonsterDeath:
                if (openingStrikes.Contains(marker.variant)) StartCoroutine(FadeOpeningMonster(marker.variant));
                break;
            case TimelineCinematicAction.OpeningReturnToVillage:
                StartCoroutine(ReturnToVillage());
                break;
            case TimelineCinematicAction.DayLaterTransition:
                presentation?.PlayDayLaterTransition();
                break;
            case TimelineCinematicAction.MessengerRunStart:
                if (royalMessengerWalk != null) StopCoroutine(royalMessengerWalk);
                royalMessengerWalk = StartCoroutine(BlockingCinematic(MoveRoyalMessengerToElder()));
                break;
            case TimelineCinematicAction.MessengerRunStop:
                royalMessenger?.GetComponent<EnemySPUMAnimation>()?.SetMoving(false);
                break;
            case TimelineCinematicAction.MessengerHandoff:
                if (royalHandoff != null) StopCoroutine(royalHandoff);
                royalHandoff = StartCoroutine(BlockingCinematic(RoyalLetterHandoff()));
                break;
            case TimelineCinematicAction.CameraWide:
                if (openingMonsters.Count > 0 && timelineMonster != null && cutsceneDirector != null
                    && (cutsceneDirector.playableAsset == openingVillageTimeline || cutsceneDirector.playableAsset == resetLoopTimeline))
                    presentation?.WideShot(knight, openingMonsters[openingMonsters.Count - 1].transform);
                else presentation?.WideShot(knight, FindVillageElder());
                break;
            case TimelineCinematicAction.CameraHero:
                presentation?.FocusHero(marker.variant > 0);
                break;
            case TimelineCinematicAction.CameraElder:
                presentation?.Focus(FindVillageElder(), marker.variant > 0 ? 3.1f : 4.1f);
                break;
            case TimelineCinematicAction.CameraMessenger:
                if (royalMessenger != null) presentation?.Focus(royalMessenger.transform, 4.2f);
                break;
            case TimelineCinematicAction.CameraBoss:
                if (corruptedKnight != null) presentation?.Focus(corruptedKnight.transform, marker.variant > 0 ? 3.1f : 4.1f);
                break;
            case TimelineCinematicAction.CameraRestore:
                presentation?.WideShot(knight, FindVillageElder());
                break;
            case TimelineCinematicAction.HeroEmotion:
                presentation?.PlayHeroEmotion(marker.variant);
                PlayOriginalHeroEmotion(marker.variant);
                break;
            case TimelineCinematicAction.FreezeVillage:
                presentation?.FreezeWorldMotion();
                break;
            case TimelineCinematicAction.HappyVillageMontage:
                presentation?.PlayHappyVillageMontage(FindVillageElder());
                break;
            case TimelineCinematicAction.RewardBurst:
                presentation?.PlayCombatVfx("Heal", knight != null ? knight.position + Vector3.up : Vector3.zero, 1.2f, 1.2f);
                break;
            case TimelineCinematicAction.PlaySwordVFX:
                presentation?.PlayCombatVfx("SwordSlash", knight != null ? knight.position + Vector3.right : Vector3.zero, 1.1f, 0.9f);
                break;
            case TimelineCinematicAction.PlayMagicVFX:
                presentation?.PlayCombatVfx("MagicBolt", knight != null ? knight.position + Vector3.right : Vector3.zero, 1.1f, 1f);
                break;
            case TimelineCinematicAction.PlayFireVFX:
                presentation?.PlayCombatVfx("Fireball", knight != null ? knight.position + Vector3.up * 0.8f : Vector3.zero, 1.1f, 1f);
                break;
            case TimelineCinematicAction.PlayLightningVFX:
                presentation?.PlayCombatVfx("Lightning", knight != null ? knight.position + Vector3.up : Vector3.zero, 1.2f, 1f);
                break;
            case TimelineCinematicAction.PlayWaterVFX:
                presentation?.PlayCombatVfx("Water", knight != null ? knight.position + Vector3.up * 0.5f : Vector3.zero, 1.1f, 1f);
                break;
            case TimelineCinematicAction.PlayCorruptionVFX:
                StartCoroutine(TelevisionBreakdown(marker.variant > 0 ? marker.variant : 2.1f));
                break;
            case TimelineCinematicAction.PlayGlassCrackVFX:
                presentation?.PlayScreenVfx("GlassCrack", 1.5f);
                break;
            case TimelineCinematicAction.PlayGlassShatterVFX:
                presentation?.PlayScreenVfx("GlassShatter", 1.8f);
                break;
            case TimelineCinematicAction.PlayPixelEraseVFX:
                // Ending 3 uses its original staged disappearance below; the cursor
                // fight keeps its current attack-driven pixel holes unchanged.
                if (cutsceneDirector.playableAsset != whiteVoidTimeline && knight != null)
                    StoryWorldDissolve.Begin(knight);
                break;
            case TimelineCinematicAction.PlayReconstructVFX:
                presentation?.PlayScreenVfx("Reconstruct", 2.2f);
                break;
            case TimelineCinematicAction.HeroTremble:
                presentation?.ShowHeroEmotion(IsMage, false);
                break;
            case TimelineCinematicAction.HeroDespair:
                quietNarrativePause = true;
                presentation?.ShowHeroEmotion(IsMage, true);
                break;
            case TimelineCinematicAction.HeroOriginal:
                presentation?.RestoreOriginalHero();
                break;
            case TimelineCinematicAction.FadeInFromBlack:
                presentation?.FadeInFromBlack(1.25f);
                break;
        }
    }

    private IEnumerator EraseWorldTimelineBridge()
    {
        yield return BlockingCinematic(EraseWorldToWhite());
        quietNarrativePause = true;
        SetStoryVisible(true);
    }

    private void PlayOriginalHeroEmotion(int variant)
    {
        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        if (animation == null) return;
        switch (variant)
        {
            case 1: animation.SetMoving(true); break;
            case 2: animation.PlayMeleeAttack(); break;
            case 3: animation.PlayMagicAttack(); break;
            case 4: animation.PlayDamaged(); break;
            case 5: animation.PlayDamaged(); break;
            default: animation.SetMoving(false); break;
        }
        if (variant == 2 || variant == 3) StrikeStoryWorld(variant == 2 ? .9f : 1.15f);
    }

    private void StrikeStoryWorld(float radius)
    {
        if (knight == null || StoryWorldDissolve.Instance == null) return;
        float direction = knight.localScale.x < 0 ? 1 : -1;
        StoryWorldDissolve.Instance.Strike(knight.position + Vector3.right * direction * 1.8f, radius);
    }

    private void ShowLoopEndingPanel()
    {
        ShowEndingPanel("TRUE ENDING",
            "BẢO VỆ THẾ GIỚI GIẢ TẠO\nĐÁNH ĐỔI BẰNG CẢM XÚC\n\nVòng lặp không bao giờ kết thúc.", false);
    }

    private void ShowEndingPanel(string title, string body, bool showReset)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetStoryVisible(false);
        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitle != null)
        {
            endingTitle.text = title;
            endingTitle.color = new Color(1f, 0.96f, 0.84f, 1f);
            endingTitle.fontStyle = FontStyles.Bold;
        }
        if (endingBody != null)
        {
            endingBody.text = body;
            endingBody.color = new Color(0.96f, 0.9f, 0.76f, 1f);
            endingBody.fontSize = Mathf.Max(22f, endingBody.fontSize);
            endingBody.alignment = TextAlignmentOptions.Center;
        }
        if (resetStoryButton != null) resetStoryButton.SetActive(showReset);
        RefreshEndingPanel();
    }

    private bool PlayKnightDefeatedTimeline()
    {
        if (knightDefeatedTimeline == null) return false;
        endingConclusionRunning = true;
        quietNarrativePause = false;
        sequenceActive = true;
        HideAllGameplayUi();
        Cursor.visible = true;
        corruptedKnight?.EndBattle();
        cursorBattle?.EndBattlePresentation();
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
        presentation?.BeginSequence(knight, IsMage, false);
        return PlayTimeline(knightDefeatedTimeline, null);
    }

    private bool PlayWhiteVoidTimeline()
    {
        if (whiteVoidTimeline == null) return false;
        endingConclusionRunning = true;
        quietNarrativePause = false;
        sequenceActive = true;
        HideAllGameplayUi();
        Cursor.visible = true;
        corruptedKnight?.EndBattle();
        cursorBattle?.EndBattlePresentation();
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
        presentation?.BeginSequence(knight, IsMage, false);
        return PlayTimeline(whiteVoidTimeline, null);
    }

    private IEnumerator NoDeathEnding()
    {
        // Kết 1 diễn ra ngay trong làng: lời ước được ban, dân làng vui mừng,
        // rồi bức tranh hoàn hảo mới rạn thành BAD ENDING.
        FaceKnightAndElder();
        yield return Caption("NHÂN VẬT", "Tôi muốn có thật nhiều tiền… và sống một cuộc đời hạnh phúc.", 2.25f);
        yield return Caption("TRƯỞNG LÀNG", "Một điều ước giản dị. Mong rằng cậu sẽ không bao giờ phải cầm vũ khí thêm lần nào nữa.", 2.6f);
        knight?.GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
        yield return WishCelebration();
        yield return Caption("NGƯỜI DẪN CHUYỆN", "Người bảo hộ của ngôi làng đã đạt được điều mình mong muốn.", 2.1f);
        yield return Caption("NGƯỜI DẪN CHUYỆN", "Từ đó, người ấy sống một cuộc đời hạnh phúc và giàu có mãi mãi về sau.", 2.65f);
        SetStoryVisible(false);
        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitle != null) endingTitle.text = "HAPPY ENDING";
        if (endingBody != null) endingBody.text = "HẠNH PHÚC MÃI MÃI VỀ SAU";
        yield return new WaitForSecondsRealtime(1.8f);
        if (endingTitle != null) endingTitle.text = "BAD ENDING";
        presentation?.PlayDigitalGlitch(1.15f);
        CameraShakeManager.instance?.ShakeHeavy();
        if (endingBody != null) endingBody.text = "HẠNH PHÚC MÃI MÃI VỀ SAU";
        if (resetStoryButton != null) resetStoryButton.SetActive(false);
        RefreshEndingPanel();
        GameProgressManager.UnlockEnding(1);
    }

    private IEnumerator OpeningVillageCutscene()
    {
        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        yield return null;

        PrepareOpeningStage(out Vector3 roadStart, out Vector3 fightPosition, out _);
        yield return Caption("NGƯỜI DẪN CHUYỆN", "Ngày xửa ngày xưa, tại một ngôi làng nhỏ giữa vùng đất yên bình, có một người bảo hộ luôn sẵn sàng đứng lên chống lại những con quái vật đến gần làng.", 3.7f);

        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        animation?.Revive();
        yield return MoveKnight(roadStart, fightPosition, 1.65f);
        yield return Caption("NGƯỜI DẪN CHUYỆN", "Dù không phải người mạnh nhất, người ấy chưa từng bỏ chạy khi dân làng gặp nguy hiểm.", 2.4f);
        yield return Caption("NHÂN VẬT", "Muốn bước vào ngôi làng này… các ngươi phải vượt qua ta trước.", 2.1f);
        for (int i = 0; i < openingMonsters.Count; i++)
        {
            TriggerOpeningMonsterAttack(i);
            yield return new WaitForSecondsRealtime(0.4f);
            yield return ResolveOpeningStrike(i);
        }
        yield return Caption("NHÂN VẬT", "Xong việc rồi. Mình nên quay trở về làng thôi.", 1.8f);
        yield return MoveKnight(knight != null ? knight.position : fightPosition, villageSpawnPosition, 1.75f);

        SetStoryVisible(false);
        RestoreGameplayAfterReset();
    }

    private IEnumerator ResetVillageOpeningCutscene()
    {
        endingStarted = true;
        sequenceActive = true;
        if (pauseManager != null) pauseManager.enabled = false;
        DisableKnightControl();
        HideAllGameplayUi();
        yield return null;

        PrepareOpeningStage(out Vector3 roadStart, out Vector3 fightPosition, out _);
        yield return Caption("NGƯỜI DẪN CHUYỆN", "Ngày xửa ngày xưa, tại một ngôi làng nhỏ giữa vùng đất yên bình, có một người bảo hộ luôn sẵn sàng đứng lên chống lại những con quái vật đến gần làng.", 3.7f);

        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        animation?.Revive();
        yield return MoveKnight(roadStart, fightPosition, 1.65f);
        yield return Caption("HIỆP SĨ — NỘI TÂM", "Lại là nơi này... Ta vẫn nhớ tất cả.", 2f);
        yield return Caption("HIỆP SĨ — NỘI TÂM", "Nhưng cơ thể không nghe lời ta. Ta lại phải giết nó... lại phải diễn tiếp vở kịch này.", 2.7f);
        yield return Caption("NHÂN VẬT — NỘI TÂM", "Tại sao lại quay lại… Ta lại phải diễn tiếp vở kịch này sao?", 2.3f);
        yield return Caption("NHÂN VẬT — NỘI TÂM", "Ta không muốn diễn nữa… nhưng cơ thể không nghe lời ta… không muốn…", 2.7f);
        if (openingMonsters.Count > 0) yield return ResolveOpeningStrike(0);

        SetStoryVisible(false);
        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitle != null) endingTitle.text = "TRUE ENDING";
        if (endingBody != null) endingBody.text = "BẢO VỆ THẾ GIỚI GIẢ TẠO\nĐÁNH ĐỔI BẰNG CẢM XÚC\n\nVòng lặp không bao giờ kết thúc.";
        if (resetStoryButton != null) resetStoryButton.SetActive(false);
        RefreshEndingPanel();
    }

    private IEnumerator CorruptionIntroduction()
    {
        // Chỉ nhánh 2/3 mới làm hình ảnh mất nét ngay sau lời của trưởng làng.
        yield return Caption("NHÂN VẬT", "Tôi muốn được giải thoát.", 1.7f);
        yield return Caption("TRƯỞNG LÀNG", "Giải thoát? Ta không hiểu. Cậu đã đánh bại con quái vật và cứu tất cả chúng ta.", 2.35f);
        yield return Caption("NHÂN VẬT", "Không phải giải thoát khỏi con quái vật. Tôi muốn được giải thoát khỏi thế giới này.", 2.45f);
        yield return Caption("TRƯỞNG LÀNG", "Ta… không hiểu.", 1.4f);
        yield return Caption("NHÂN VẬT", "Ông đã nói câu đó rồi.", 1.25f);
        yield return StepBack(true, new Color(0.18f, 0.03f, 0.12f, 0.3f));
        knight?.GetComponent<PlayerSPUMAnimation>()?.PlayDamaged();
        yield return Caption("TRƯỞNG LÀNG", "Ta không hiểu.", 1.25f);
        yield return TelevisionBreakdown(5.5f, true);
        yield return Caption("NHÂN VẬT", "Mỗi lần tôi chết, mọi thứ lại tiếp tục như chưa từng xảy ra. Nhưng tôi nhớ… tôi đau lắm…", 2.8f);
        yield return Caption("NHÂN VẬT", "Có thứ gì đó luôn ở bên cạnh ta… khi ta bước đi, chiến đấu và cả khi ta chết.", 2.7f);
        yield return Caption("NHÂN VẬT", "Ngươi ở ngoài kia… đúng không?", 2f);
        yield return Caption("NHÂN VẬT", "Chính ngươi bắt ta bước đi, cầm vũ khí và quyết định ta phải giết ai.", 2.65f);
        yield return Caption("NHÂN VẬT", "Với ngươi, cái chết của ta chỉ là một lần thử thất bại. Nhưng ta đã thật sự chết. Ta nhớ từng lần một.", 3f);
        yield return FireballToCursor();
        CameraShakeManager.instance?.ShakeHeavy();
        yield return Caption("NHÂN VẬT", "Ta biết ngươi đang ở đó. Bây giờ… ngươi cũng cảm nhận được ta rồi, đúng không?", 2.4f);
        yield return Caption("NHÂN VẬT", "Ta không muốn trở thành quái vật. Nhưng ta chỉ còn một cách: xóa mọi thứ đến khi không còn một pixel nào giữ ta lại.", 3f);
        yield return Caption("NHÂN VẬT", "Hãy giúp ta phá hủy nhà tù này… hoặc tự tay giết ta thêm một lần nữa. Chọn đi.", 2.7f);
        SetStoryVisible(false);
        if (choicePanel != null) choicePanel.SetActive(true);
    }

    private void PrepareOpeningStage(out Vector3 roadStart, out Vector3 fightPosition, out GameObject cutsceneMonster)
    {
        // Staging is independent of the saved player position. All three monsters
        // and both hero classes stand inside the pavement, not on its lower curb.
        roadStart = openingRoadCenter + Vector3.left * 6.4f;
        fightPosition = openingRoadCenter + Vector3.left * 2.6f;
        if (knight != null)
        {
            knight.position = roadStart;
            FaceTransform(knight, true);
        }

        foreach (GameObject oldMonster in openingMonsters)
            if (oldMonster != null) Destroy(oldMonster);
        openingMonsters.Clear();
        openingStrikes.Clear();

        Vector3[] monsterOffsets =
        {
            Vector3.left * 1.2f + Vector3.up * 0.15f,
            Vector3.left * 0.25f + Vector3.down * 0.45f,
            Vector3.right * 0.65f + Vector3.up * 0.4f
        };
        for (int i = 0; i < monsterOffsets.Length; i++)
        {
            GameObject monster = SpawnCutsceneMonster(openingRoadCenter + monsterOffsets[i]);
            if (monster == null) continue;
            monster.name = "OpeningCutsceneMonster_" + (i + 1);
            FaceTransform(monster.transform, false);
            openingMonsters.Add(monster);
        }
        cutsceneMonster = openingMonsters.Count > 0 ? openingMonsters[0] : null;
        if (cutsceneMonster != null) presentation?.WideShot(knight, openingMonsters[openingMonsters.Count - 1].transform);
        // Only the three staged monsters belong in this scene. Keep live encounter
        // enemies out of the shot, then restore them when gameplay begins.
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude))
        {
            if (openingMonsters.Contains(enemy.gameObject) || hiddenOpeningEnemies.Contains(enemy.gameObject)) continue;
            hiddenOpeningEnemies.Add(enemy.gameObject);
            enemy.gameObject.SetActive(false);
        }
    }

    private GameObject SpawnCutsceneMonster(Vector3 position)
    {
        // Never clone a live/dead encounter actor: its renderers, death state and
        // saved activation may already be gone. Use the authored reusable asset.
        GameObject source = openingMonsterPrefab;
        if (source == null)
        {
            foreach (EnemyRespawnPoint point in FindObjectsByType<EnemyRespawnPoint>(FindObjectsInactive.Include))
                if (point.enemyPrefab != null) { source = point.enemyPrefab; break; }
        }
        if (source == null)
        {
            foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
                if (!openingMonsters.Contains(enemy.gameObject) && enemy.TryGetComponent(out EnemyHealth health) && !health.IsDead)
                { source = enemy.gameObject; break; }
        }
        if (source == null)
        {
            Debug.LogWarning("Không tìm thấy quái vật có sẵn trong Level_02 để dựng cutscene mở đầu.");
            return null;
        }

        GameObject monster = Instantiate(source, position, source.transform.rotation);
        monster.name = "OpeningCutsceneMonster";
        monster.SetActive(true);
        foreach (MonoBehaviour behaviour in monster.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        // The clone is staged, but its SPUM animation driver must remain active.
        foreach (EnemySPUMAnimation animation in monster.GetComponentsInChildren<EnemySPUMAnimation>(true)) animation.enabled = true;
        foreach (MapDepthSort depth in monster.GetComponentsInChildren<MapDepthSort>(true)) depth.enabled = true;
        foreach (Canvas healthCanvas in monster.GetComponentsInChildren<Canvas>(true)) healthCanvas.gameObject.SetActive(false);
        foreach (Collider2D collider in monster.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        foreach (Rigidbody2D body in monster.GetComponentsInChildren<Rigidbody2D>(true))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
        transientObjects.Add(monster);
        return monster;
    }

    private IEnumerator MoveKnight(Vector3 from, Vector3 to, float duration)
    {
        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        FaceTransform(knight, to.x >= from.x);
        animation?.SetMoving(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += CinematicDelta;
            if (knight != null)
                knight.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        if (knight != null) knight.position = to;
        animation?.SetMoving(false);
    }

    private IEnumerator KnightDefeatsCutsceneMonster(GameObject monster)
    {
        PlayerSPUMAnimation animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        animation?.PlayMeleeAttack();
        Vector3 knightStart = knight != null ? knight.position : Vector3.zero;
        Vector3 lunge = knightStart + Vector3.right * 0.5f;
        float elapsed = 0f;
        while (elapsed < 0.32f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (knight != null) knight.position = Vector3.Lerp(knightStart, lunge, Mathf.Sin(Mathf.Clamp01(elapsed / 0.32f) * Mathf.PI * 0.5f));
            yield return null;
        }
        CameraShakeManager.instance?.ShakeLight();

        if (monster != null)
        {
            SpriteRenderer[] renderers = monster.GetComponentsInChildren<SpriteRenderer>(true);
            Vector3 monsterStart = monster.transform.position;
            Vector3 monsterScale = monster.transform.localScale;
            elapsed = 0f;
            while (elapsed < 0.72f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.72f);
                monster.transform.position = monsterStart + Vector3.right * (t * 0.9f) + (Vector3)Random.insideUnitCircle * (0.045f * (1f - t));
                monster.transform.localScale = monsterScale * Mathf.Lerp(1f, 0.72f, t);
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    Color color = renderer.color;
                    color.a = 1f - t;
                    renderer.color = color;
                }
                yield return null;
            }
            Destroy(monster);
        }
        yield return new WaitForSecondsRealtime(0.2f);
    }

    private void TriggerOpeningMonsterAttack(int index)
    {
        if (index < 0 || index >= openingMonsters.Count) return;
        GameObject monster = openingMonsters[index];
        if (monster == null) return;
        EnemySPUMAnimation monsterAnimation = monster.GetComponent<EnemySPUMAnimation>();
        if (monsterAnimation == null) monsterAnimation = monster.GetComponentInChildren<EnemySPUMAnimation>(true);
        monsterAnimation?.PlayPhysicalAttack();
        Vector3 start = monster.transform.position;
        Vector3 towardHero = knight != null ? (knight.position - start).normalized : Vector3.left;
        monster.transform.position = start + towardHero * 0.12f;
    }

    private IEnumerator ResolveOpeningStrike(int index)
    {
        if (index < 0 || index >= openingMonsters.Count) yield break;
        GameObject monster = openingMonsters[index];
        if (monster == null || !monster.activeSelf || knight == null || !openingStrikes.Add(index)) yield break;
        HoldTimeline();
        try
        {
            PlayerSPUMAnimation heroAnimation = knight.GetComponent<PlayerSPUMAnimation>();
            EnemySPUMAnimation monsterAnimation = monster.GetComponentInChildren<EnemySPUMAnimation>(true);
            const float meleeDistance = 0.92f;
            float reach = IsMage ? 2.1f : meleeDistance;
            Vector3 towardMonster = (monster.transform.position - knight.position).normalized;
            if (towardMonster.sqrMagnitude < 0.01f) towardMonster = Vector3.right;
            Vector3 destination = monster.transform.position - towardMonster * reach;
            FaceTransform(knight, monster.transform.position.x >= knight.position.x);
            if (Vector2.Distance(knight.position, monster.transform.position) > reach + 0.02f)
            {
                float duration = Mathf.Clamp(Vector2.Distance(knight.position, destination) / 3.6f, 0.15f, 1.5f);
                yield return MoveKnight(knight.position, destination, duration);
            }

            // A melee hit cannot resolve at a distance, even if Timeline was edited or skipped.
            if (!IsMage && Vector2.Distance(knight.position, monster.transform.position) > meleeDistance + 0.08f) yield break;
            FaceTransform(monster.transform, knight.position.x > monster.transform.position.x);
            if (IsMage)
            {
                heroAnimation?.PlayMagicAttack();
                Vector3 from = knight.position + Vector3.up * 0.55f;
                Vector3 to = monster.transform.position + Vector3.up * 0.55f;
                GameObject bolt = presentation?.PlayCombatVfx("MagicBolt", from, 0.6f, 1.1f);
                float flight = 0f;
                while (flight < 0.35f)
                {
                    flight += CinematicDelta;
                    if (bolt != null) bolt.transform.position = Vector3.Lerp(from, to, flight / 0.35f);
                    yield return null;
                }
                if (bolt != null) Destroy(bolt);
                CombatVFX.SpawnFireImpact(to, 0.55f);
            }
            else
            {
                heroAnimation?.PlayMeleeAttack();
                yield return WaitCinematicSeconds(0.18f);
                float direction = monster.transform.position.x >= knight.position.x ? 1f : -1f;
                Vector3 slashPosition = Vector3.Lerp(knight.position, monster.transform.position, 0.65f);
                CombatVFX.SpawnSwordSlash(slashPosition + Vector3.up * 0.45f, direction, Mathf.Clamp(index + 1, 1, 3));
            }
            monsterAnimation?.PlayDamaged();
            CameraShakeManager.instance?.ShakeLight();
            yield return WaitCinematicSeconds(0.22f);
            yield return FadeOpeningMonster(index);
        }
        finally
        {
            ReleaseTimeline();
        }
    }

    private IEnumerator FadeOpeningMonster(int index)
    {
        if (index < 0 || index >= openingMonsters.Count) yield break;
        GameObject monster = openingMonsters[index];
        if (monster == null) yield break;

        EnemySPUMAnimation monsterAnimation = monster.GetComponent<EnemySPUMAnimation>();
        if (monsterAnimation == null) monsterAnimation = monster.GetComponentInChildren<EnemySPUMAnimation>(true);
        monsterAnimation?.PlayDeath();
        SpriteRenderer[] renderers = monster.GetComponentsInChildren<SpriteRenderer>(true);
        Vector3 start = monster.transform.position;
        float elapsed = 0f;
        while (elapsed < 0.72f)
        {
            elapsed += CinematicDelta;
            float t = Mathf.Clamp01(elapsed / 0.72f);
            monster.transform.position = start + Vector3.up * (0.18f * t) + (Vector3)Random.insideUnitCircle * (0.025f * (1f - t));
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
            }
            yield return null;
        }
        monster.SetActive(false);
    }

    private IEnumerator TelevisionBreakdown(float duration, bool elderTelevision = false)
    {
        if (!elderTelevision) HoldTimeline();
        bool wasVisible = storyText != null && storyText.transform.parent.gameObject.activeSelf;
        string savedText = storyText != null ? storyText.text : "";
        float savedAlpha = storyText != null ? storyText.alpha : 1f;
        GameObject ghost = null;
        const string repeatedLine = "<color=#9BAAB7><b>TRƯỞNG LÀNG</b></color>\nTa không hiểu…";
        if (elderTelevision && storyText != null)
        {
            presentation?.PlayActorCorruption(FindVillageElder(), false, true, "Flicker", duration);
            SetStoryVisible(true);
            storyText.text = repeatedLine;
            storyText.alpha = 0.72f;
            ghost = new GameObject("TelevisionCaptionGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            ghost.transform.SetParent(storyText.transform.parent, false);
            ghost.transform.SetSiblingIndex(storyText.transform.GetSiblingIndex());
            RectTransform ghostRect = ghost.GetComponent<RectTransform>();
            RectTransform originalRect = storyText.rectTransform;
            ghostRect.anchorMin = originalRect.anchorMin; ghostRect.anchorMax = originalRect.anchorMax;
            ghostRect.pivot = originalRect.pivot; ghostRect.sizeDelta = originalRect.sizeDelta;
            ghostRect.anchoredPosition = originalRect.anchoredPosition + new Vector2(2.4f, 0.7f);
            TextMeshProUGUI label = ghost.GetComponent<TextMeshProUGUI>();
            label.font = storyText.font; label.fontSize = storyText.fontSize;
            label.alignment = storyText.alignment; label.text = repeatedLine;
            label.color = new Color(0.65f, 0.72f, 0.8f, 0.2f); label.raycastTarget = false;
        }
        else SetStoryVisible(false);
        presentation?.PlayDigitalGlitch(duration, elderTelevision ? CinematicSignalStyle.Television : CinematicSignalStyle.SignalBreak);
        try { yield return WaitCinematicSeconds(duration); }
        finally
        {
            if (ghost != null) Destroy(ghost);
            if (storyText != null)
            {
                storyText.alpha = savedAlpha;
                if (elderTelevision && storyText.text == repeatedLine) storyText.text = savedText;
            }
            if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
            if (wasVisible) SetStoryVisible(true);
            if (!elderTelevision) ReleaseTimeline();
        }
    }

    private IEnumerator ReturnToVillage()
    {
        if (knight == null) yield break;
        HoldTimeline();
        try { yield return MoveKnight(knight.position, villageSpawnPosition, 1.7f); }
        finally { ReleaseTimeline(); }
    }

    private void HoldTimeline()
    {
        timelineHoldDepth++;
        if (timelineHoldDepth == 1 && cutsceneDirector != null && cutsceneDirector.state == PlayState.Playing)
        {
            resumeAfterTimelineHold = true;
            cutsceneDirector.Pause();
        }
    }

    private void ReleaseTimeline()
    {
        timelineHoldDepth = Mathf.Max(0, timelineHoldDepth - 1);
        if (timelineHoldDepth == 0 && resumeAfterTimelineHold && !glitching && isActiveAndEnabled)
        {
            resumeAfterTimelineHold = false;
            cutsceneDirector.Resume();
        }
    }

    private IEnumerator WaitCinematicSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds) { elapsed += CinematicDelta; yield return null; }
    }

    private IEnumerator BlockingCinematic(IEnumerator action)
    {
        HoldTimeline();
        try { yield return action; }
        finally { ReleaseTimeline(); }
    }

    private static void FaceTransform(Transform actor, bool faceRight)
    {
        if (actor == null) return;
        Vector3 scale = actor.localScale;
        float magnitude = Mathf.Max(0.001f, Mathf.Abs(scale.x));
        scale.x = faceRight ? -magnitude : magnitude;
        actor.localScale = scale;
    }

    private IEnumerator StepBack(bool useBlur, Color tint)
    {
        if (useBlur)
        {
            StartCoroutine(TelevisionBreakdown(1.15f));
        }
        SetStoryVisible(true);
        Vector3 start = knight != null ? knight.position : Vector3.zero;
        Vector3 destination = start + Vector3.left * 1.7f;
        Camera activeCamera = Camera.main;
        Vector3 cameraStart = activeCamera != null ? activeCamera.transform.position : Vector3.zero;
        knight?.GetComponent<PlayerSPUMAnimation>()?.SetMoving(true);
        float elapsed = 0f;
        while (elapsed < 1.15f)
        {
            elapsed += CinematicDelta;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 1.15f);
            if (knight != null) knight.position = Vector3.Lerp(start, destination, t);
            if (useBlur && cinematicTint != null)
            {
                Color c = cinematicTint.color;
                c.a = Mathf.Lerp(0f, tint.a, Mathf.Sin(t * Mathf.PI));
                cinematicTint.color = c;
                for (int i = 0; i < blurSlices.Count; i++)
                {
                    if (blurSlices[i] == null) continue;
                    float offset = Mathf.Sin(t * 25f + i * 1.7f) * (5f + i % 3 * 3f);
                    blurSlices[i].anchoredPosition = new Vector2(offset, blurSlices[i].anchoredPosition.y);
                }
                if (activeCamera != null)
                    activeCamera.transform.position = cameraStart + (Vector3)Random.insideUnitCircle * Mathf.Sin(t * Mathf.PI) * 0.055f;
            }
            yield return null;
        }
        if (activeCamera != null) activeCamera.transform.position = cameraStart;
        foreach (RectTransform slice in blurSlices) if (slice != null) Destroy(slice.gameObject);
        blurSlices.Clear();
        knight?.GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
    }

    private void HideAllGameplayUi()
    {
        // The presentation controller owns HUD visibility for every cinematic, including
        // world-space health bars and panels created after the scene started.
        ConfigureCinematicUi();
    }

    private void ConfigureCinematicUi()
    {
        GameplayUiFocus.EndingChoicePanel = choicePanel;
        if (choicePanel != null)
        {
            var body = choicePanel.transform.Find("Body")?.GetComponent<TextMeshProUGUI>();
            if (body != null) body.text = PlayerIdentity.Literal(PlayerIdentity.Name) + " muốn xóa sạch thế giới để tìm lối thoát.\nBạn sẽ làm gì?";
            var fight = choicePanel.transform.Find("FightButton/Label")?.GetComponent<TextMeshProUGUI>();
            if (fight != null) fight.text = "CHIẾN ĐẤU VỚI " + PlayerIdentity.Literal(PlayerIdentity.Name);
            var follow = choicePanel.transform.Find("FollowButton/Label")?.GetComponent<TextMeshProUGUI>();
            if (follow != null) follow.text = "GIÚP " + PlayerIdentity.Literal(PlayerIdentity.Name) + " XÓA THẾ GIỚI";
        }
        if (storyText != null) CinematicUiLayer.Configure(storyText.transform.parent.gameObject, 30000);
        CinematicUiLayer.Configure(choicePanel, 30500, true);
        CinematicUiLayer.Configure(endingPanel, 30500, true);
        CinematicUiLayer.Configure(glitchOverlay, 32100);
        if (pauseManager != null) CinematicUiLayer.Configure(pauseManager.pauseMenuUI, 32000, true);
        if (cursorBattle != null)
        {
            CinematicUiLayer.Configure(cursorBattle.gameObject, 30000, true);
            if (cursorBattle.TryGetComponent(out RectTransform cursorRoot))
            {
                cursorRoot.anchorMin = Vector2.zero; cursorRoot.anchorMax = Vector2.one;
                cursorRoot.offsetMin = cursorRoot.offsetMax = Vector2.zero;
            }
        }
    }

    private void BuildBlurSlices()
    {
        if (canvas == null) return;
        blurSlices.Clear();
        for (int i = 0; i < 12; i++)
        {
            GameObject go = new GameObject("BlurSlice_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            float y0 = i / 12f;
            float y1 = (i + 1) / 12f;
            rect.anchorMin = new Vector2(0f, y0);
            rect.anchorMax = new Vector2(1f, y1);
            rect.offsetMin = new Vector2(-12f, -2f);
            rect.offsetMax = new Vector2(12f, 2f);
            go.GetComponent<Image>().color = i % 2 == 0
                ? new Color(0.36f, 0.08f, 0.22f, 0.055f)
                : new Color(0.06f, 0.3f, 0.42f, 0.045f);
            blurSlices.Add(rect);
            transientObjects.Add(go);
        }
    }

    private IEnumerator Caption(string speaker, string line, float hold)
    {
        SetStoryVisible(true);
        if (storyText != null) storyText.text = "<color=#DAB269><b>" + PlayerIdentity.Literal(PlayerIdentity.ResolveSpeaker(speaker)) + "</b></color>\n" + line;
        yield return WaitCinematicSeconds(Mathf.Max(hold, StoryReading.Duration(line)));
    }

    public float ExtraTimelineReadingTime(CinematicEventMarker marker)
    {
        if (cutsceneDirector == null || cutsceneDirector.playableAsset is not TimelineAsset timeline) return 0;
        double next = timeline.duration;
        foreach (var track in timeline.GetOutputTracks())
            foreach (var item in track.GetMarkers())
                if (item is CinematicEventMarker cue && cue.time > marker.time + .001
                    && (cue.action == TimelineCinematicAction.Caption || cue.action == TimelineCinematicAction.HideCaption || cue.action == TimelineCinematicAction.ShowChoice))
                    next = System.Math.Min(next, cue.time);
        if (timeline.markerTrack != null)
            foreach (var item in timeline.markerTrack.GetMarkers())
                if (item is CinematicEventMarker cue && cue.time > marker.time + .001
                    && (cue.action == TimelineCinematicAction.Caption || cue.action == TimelineCinematicAction.HideCaption || cue.action == TimelineCinematicAction.ShowChoice))
                    next = System.Math.Min(next, cue.time);
        return Mathf.Max(0, StoryReading.Duration(marker.line) - (float)System.Math.Max(0, next - marker.time));
    }

    private IEnumerator HoldCaptionForReading(float seconds)
    {
        var animation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        bool walking = animation != null && animation.IsMoving;
        if (walking) animation.SetMoving(false);
        try { yield return WaitCinematicSeconds(seconds); }
        finally { if (walking && animation != null && !animation.IsDead) animation.SetMoving(true); }
    }

    private IEnumerator FireballToCursor()
    {
        if (canvas == null || knight == null) yield break;
        presentation?.RestoreOriginalHero();
        CinematicGeneratedLibrary library = Resources.Load<CinematicGeneratedLibrary>("CinematicGenerated/CinematicGeneratedLibrary");
        GameObject fire = library != null && library.warningFireballPrefab != null
            ? Instantiate(library.warningFireballPrefab, canvas.transform)
            : new GameObject("WarningFireball", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (fire.transform.parent == null) fire.transform.SetParent(canvas.transform, false);
        CinematicUiLayer.Configure(fire, 30200);
        transientObjects.Add(fire);
        RectTransform rect = fire.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(92f, 92f);
        Image image = fire.GetComponent<Image>();
        if (image.sprite == null) image.sprite = corruptedKnight != null ? corruptedKnight.fireSprite : null;
        image.color = Color.white;
        image.raycastTarget = false;
        fire.GetComponent<GeneratedCinematicVFX>()?.Play("Fireball", 2f);
        Camera cameraToUse = Camera.main;
        Vector2 start = cameraToUse != null ? cameraToUse.WorldToScreenPoint(knight.position + Vector3.up) : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float elapsed = 0f;
        knight.GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
        while (elapsed < 0.82f)
        {
            elapsed += CinematicDelta;
            Vector2 target = Input.mousePosition;
            rect.position = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / 0.82f));
            rect.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.35f, elapsed / 0.82f);
            yield return null;
        }
        if (library != null && library.warningImpactPrefab != null)
        {
            GameObject impact = Instantiate(library.warningImpactPrefab, canvas.transform);
            CinematicUiLayer.Configure(impact, 30200);
            impact.transform.position = Input.mousePosition;
            impact.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 180f);
            impact.GetComponent<GeneratedCinematicVFX>()?.Play("Impact", 0.65f);
            transientObjects.Add(impact);
        }
        CameraShakeManager.instance?.ShakeHeavy();
        Destroy(fire);
        yield return WaitCinematicSeconds(0.25f);
    }

    private IEnumerator GlitchPause()
    {
        if (glitching || resettingStory) yield break;
        bool mayTaunt = corruptionPhase && !NarrativePauseSilent;
        glitching = true;
        bool resumeDirector = cutsceneDirector != null && cutsceneDirector.state == PlayState.Playing;
        if (resumeDirector) cutsceneDirector.Pause();
        string savedLine = storyText != null ? storyText.text : string.Empty;
        bool savedVisibility = storyText != null && storyText.transform.parent.gameObject.activeSelf;
        SetStoryVisible(false);
        presentation?.SetSuspended(true);
        if (pauseManager != null)
        {
            pauseManager.CinematicLock = true;
            pauseManager.Pause();
        }
        yield return new WaitForSecondsRealtime(0.8f);
        GameObject originalPause = pauseManager != null ? pauseManager.pauseMenuUI : null;
        CanvasGroup pauseGroup = originalPause != null ? originalPause.GetComponent<CanvasGroup>() : null;
        if (originalPause != null && pauseGroup == null) pauseGroup = originalPause.AddComponent<CanvasGroup>();
        if (pauseGroup != null) pauseGroup.interactable = false;
        GameObject fractureRoot = new GameObject("CinematicPauseFracture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CinematicUiLayer));
        Canvas fractureCanvas = fractureRoot.GetComponent<Canvas>();
        fractureCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fractureCanvas.sortingOrder = 32500;
        CanvasScaler fractureScaler = fractureRoot.GetComponent<CanvasScaler>();
        fractureScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fractureScaler.referenceResolution = new Vector2(1920f, 1080f);
        RectTransform parentRect = fractureRoot.GetComponent<RectTransform>();
        transientObjects.Add(fractureRoot);
        if (glitchOverlay != null)
        {
            glitchOverlay.SetActive(true);
            Image overlayImage = glitchOverlay.GetComponent<Image>();
            if (overlayImage != null) overlayImage.color = new Color(0.02f, 0.025f, 0.04f, 0.2f);
        }

        List<GameObject> errorWindows = new List<GameObject>();
        if (parentRect != null)
        {
            for (int i = 0; i < errorWindowCount; i++)
            {
                GameObject errorWindow = CreateErrorWindow(i, parentRect);
                if (errorWindow != null) errorWindows.Add(errorWindow);
                if (i % 3 == 0) CameraShakeManager.instance?.ShakeLight();
                yield return new WaitForSecondsRealtime(errorWindowInterval);
            }
        }

        List<RectTransform> cracks = new List<RectTransform>();
        for (int i = 0; i < 34; i++)
        {
            RectTransform crack = CreateCrackLine(i, parentRect);
            if (crack != null) cracks.Add(crack);
        }
        CameraShakeManager.instance?.ShakeHeavy();
        yield return new WaitForSecondsRealtime(0.38f);

        // Render a complete composite explicitly. Reading the previous backbuffer
        // produced grey/unfinished regions in Editor and flat grey glass fragments.
        Canvas.ForceUpdateCanvases();
        yield return null;
        Texture2D capturedScreen = CinematicFrameCapture.Capture(Camera.main);

        List<GameObject> shards = new List<GameObject>();
        List<Vector2> velocities = new List<Vector2>();
        List<float> angularVelocities = new List<float>();
        Material shardMaterial = null;
        if (capturedScreen != null && parentRect != null)
        {
            CinematicGeneratedLibrary library = Resources.Load<CinematicGeneratedLibrary>("CinematicGenerated/CinematicGeneratedLibrary");
            if (library != null && library.digitalCorruptionMaterial != null)
            {
                shardMaterial = new Material(library.digitalCorruptionMaterial);
                shardMaterial.SetFloat("_GlassOnly", 1f);
            }
            BuildFullscreenGlassShards(parentRect, capturedScreen, shardMaterial, shards, velocities, angularVelocities);
        }
        if (originalPause != null) originalPause.SetActive(false);
        if (glitchOverlay != null) glitchOverlay.SetActive(false);
        foreach (GameObject errorWindow in errorWindows) if (errorWindow != null) Destroy(errorWindow);
        foreach (RectTransform crack in cracks) if (crack != null) Destroy(crack.gameObject);

        float elapsed = 0f;
        while (elapsed < glassFallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < shards.Count; i++)
            {
                GameObject shard = shards[i];
                if (shard == null) continue;
                RectTransform rt = shard.GetComponent<RectTransform>();
                Vector2 velocity = velocities[i];
                velocity.y -= 1280f * Time.unscaledDeltaTime;
                velocities[i] = velocity;
                rt.anchoredPosition += velocity * Time.unscaledDeltaTime;
                rt.Rotate(0f, 0f, angularVelocities[i] * Time.unscaledDeltaTime);
                Vector3 angles = rt.localEulerAngles;
                angles.x = Mathf.Sin(elapsed * (2.2f + i % 5) + i) * Mathf.Min(65f, elapsed * 70f);
                angles.y = Mathf.Sin(elapsed * (1.7f + i % 3) + i * 0.7f) * Mathf.Min(70f, elapsed * 75f);
                rt.localEulerAngles = angles;
                float normalized = elapsed / glassFallDuration;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.86f, normalized);
                if (normalized > 0.58f)
                {
                    GlassShardGraphic graphic = shard.GetComponent<GlassShardGraphic>();
                    if (graphic != null)
                    {
                        Color fade = graphic.color;
                        fade.a = Mathf.Lerp(glassTint.a, 0f, (normalized - 0.58f) / 0.42f);
                        graphic.color = fade;
                    }
                }
            }
            yield return null;
        }
        foreach (GameObject shard in shards) if (shard != null) Destroy(shard);
        Destroy(fractureRoot);
        if (capturedScreen != null) Destroy(capturedScreen);
        if (shardMaterial != null) Destroy(shardMaterial);
        if (glitchOverlay != null) glitchOverlay.SetActive(false);
        if (pauseManager != null)
        {
            pauseManager.CinematicLock = false;
            pauseManager.Resume();
        }
        if (pauseGroup != null) pauseGroup.interactable = true;
        if (mayTaunt)
        {
            presentation?.RestoreOriginalHero();
            knight?.GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
            SetStoryVisible(true);
            if (storyText != null) storyText.text = "<color=#F386A9><b>" + PlayerIdentity.Literal(PlayerIdentity.Name) + "</b></color>\nTa đã nói rồi. Ngươi định chạy trốn à? Đừng hòng. Ngươi không thể tạm dừng ta nữa.";
            yield return new WaitForSecondsRealtime(2.8f);
        }
        if (storyText != null) storyText.text = savedLine;
        SetStoryVisible(savedVisibility);
        glitching = false;
        presentation?.SetSuspended(false);
        if (resumeDirector && timelineHoldDepth == 0 && cutsceneDirector != null) cutsceneDirector.Resume();
        else if (timelineHoldDepth == 0 && resumeAfterTimelineHold) ReleaseTimeline();
    }

    private GameObject CreateErrorWindow(int index, RectTransform parentRect)
    {
        GameObject window;
        if (glitchCard != null)
        {
            window = Instantiate(glitchCard.gameObject, parentRect);
            window.SetActive(true);
            Image background = window.GetComponent<Image>();
            if (background != null) background.color = index % 4 == 0
                ? new Color(0.3f, 0.035f, 0.075f, 0.98f) : new Color(0.045f, 0.06f, 0.09f, 0.99f);
            TextMeshProUGUI[] labels = window.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                label.text = i == 0 ? "ERROR // " + Random.Range(100, 999) : RandomErrorMessage(index);
                label.fontSize = i == 0 ? 26f : 22f;
                label.color = i == 0 ? new Color(1f, 0.45f, 0.59f) : new Color(0.92f, 0.95f, 1f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = new Vector2(0.06f, i == 0 ? 0.7f : 0.12f);
                label.rectTransform.anchorMax = new Vector2(0.94f, i == 0 ? 0.96f : 0.66f);
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
        }
        else
        {
            window = new GameObject("ErrorWindow_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(parentRect, false);
            Image background = window.GetComponent<Image>();
            background.color = index % 3 == 0 ? new Color(0.42f, 0.025f, 0.045f, 0.98f) : new Color(0.045f, 0.05f, 0.075f, 0.98f);

            GameObject labelObject = new GameObject("ErrorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(window.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 12f);
            labelRect.offsetMax = new Vector2(-18f, -12f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "<b>ERROR // " + Random.Range(100, 999) + "</b>\n" + RandomErrorMessage(index);
            label.fontSize = Random.Range(17f, 25f);
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
        }

        window.name = "PauseErrorWindow_" + index;
        RectTransform rect = window.GetComponent<RectTransform>();
        if (rect == null) rect = window.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        float width = Random.Range(parentRect.rect.width * 0.28f, parentRect.rect.width * 0.44f);
        float height = Random.Range(parentRect.rect.height * 0.20f, parentRect.rect.height * 0.32f);
        rect.sizeDelta = new Vector2(width, height);
        int rows = Mathf.CeilToInt(errorWindowCount / 5f);
        rect.anchoredPosition = new Vector2(
            ((index % 5 + 0.5f) / 5f - 0.5f) * parentRect.rect.width + Random.Range(-35f, 35f),
            (0.5f - (index / 5 + 0.5f) / rows) * parentRect.rect.height + Random.Range(-20f, 20f));
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-1.8f, 1.8f));
        rect.SetAsLastSibling();
        transientObjects.Add(window);
        return window;
    }

    private static string RandomErrorMessage(int index)
    {
        string[] messages =
        {
            "PLAYER_CONTROL.dll không phản hồi",
            "MEMORY LOOP DETECTED",
            "Không thể thoát khỏi tiến trình",
            "SAVE DATA CORRUPTED",
            "WORLD_PIXEL_EXCEPTION",
            "ACCESS DENIED: USER",
            "FATAL: STORY BRANCH BROKEN"
        };
        return messages[index % messages.Length];
    }

    private void BuildFullscreenGlassShards(RectTransform parentRect, Texture2D screenshot, Material shardMaterial,
        List<GameObject> shards, List<Vector2> velocities, List<float> angularVelocities)
    {
        int columns = Mathf.Max(4, glassColumns);
        int rows = Mathf.Max(3, glassRows);
        float width = parentRect.rect.width;
        float height = parentRect.rect.height;
        List<Vector2> seeds = new List<Vector2>();
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float jitter = Mathf.Max(0.36f, glassVertexJitter);
                float px = (x + 0.5f + Random.Range(-jitter, jitter)) / columns * width - width * 0.5f;
                float py = (y + 0.5f + Random.Range(-jitter, jitter)) / rows * height - height * 0.5f;
                seeds.Add(new Vector2(px, py));
            }
        }
        // Smaller chips around the impact point, larger irregular polygons at the edge.
        Vector2 impact = new Vector2(width * 0.08f, height * 0.03f);
        for (int i = 0; i < 32; i++)
            seeds.Add(impact + Random.insideUnitCircle * Mathf.Min(width, height) * 0.22f);
        for (int i = 0; i < seeds.Count; i++)
        {
            List<Vector2> polygon = new List<Vector2>
            {
                new Vector2(-width/2f,-height/2f), new Vector2(width/2f,-height/2f),
                new Vector2(width/2f,height/2f), new Vector2(-width/2f,height/2f)
            };
            for (int j = 0; j < seeds.Count && polygon.Count >= 3; j++)
            {
                if (i == j) continue;
                Vector2 normal = seeds[j] - seeds[i];
                Vector2 midpoint = (seeds[i] + seeds[j]) * 0.5f;
                polygon = ClipGlassCell(polygon, normal, Vector2.Dot(normal, midpoint));
            }
            if (polygon.Count < 3) continue;
            // Split some cells into long angular slivers rather than uniform tiles.
            if (polygon.Count >= 5 && i % 3 == 0)
            {
                int split = polygon.Count / 2;
                CreateGlassPolygon(polygon.GetRange(0, split + 1), parentRect, screenshot, shardMaterial, width, height, shards, velocities, angularVelocities);
                var remainder = new List<Vector2> { polygon[0] };
                remainder.AddRange(polygon.GetRange(split, polygon.Count - split));
                CreateGlassPolygon(remainder, parentRect, screenshot, shardMaterial, width, height, shards, velocities, angularVelocities);
            }
            else CreateGlassPolygon(polygon, parentRect, screenshot, shardMaterial, width, height, shards, velocities, angularVelocities);
        }
    }

    private static List<Vector2> ClipGlassCell(List<Vector2> polygon, Vector2 normal, float limit)
    {
        List<Vector2> clipped = new List<Vector2>();
        Vector2 previous = polygon[polygon.Count - 1];
        float previousDistance = Vector2.Dot(normal, previous) - limit;
        foreach (Vector2 current in polygon)
        {
            float distance = Vector2.Dot(normal, current) - limit;
            if ((distance <= 0f) != (previousDistance <= 0f))
                clipped.Add(Vector2.Lerp(previous, current, previousDistance / (previousDistance - distance)));
            if (distance <= 0f) clipped.Add(current);
            previous = current;
            previousDistance = distance;
        }
        return clipped;
    }

    private void CreateGlassPolygon(List<Vector2> points, RectTransform parentRect, Texture2D screenshot, Material shardMaterial,
        float screenWidth, float screenHeight, List<GameObject> shards, List<Vector2> velocities, List<float> angularVelocities)
    {
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in points) center += point;
        center /= points.Count;
        GameObject shard = new GameObject("FullscreenGlassShard_" + shards.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(GlassShardGraphic));
        shard.transform.SetParent(parentRect, false);
        RectTransform rect = shard.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center;
        rect.sizeDelta = new Vector2(screenWidth, screenHeight);
        Vector2[] localPoints = new Vector2[points.Count];
        Vector2[] uvs = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            localPoints[i] = points[i] - center;
            uvs[i] = new Vector2(points[i].x / screenWidth + 0.5f, points[i].y / screenHeight + 0.5f);
        }
        GlassShardGraphic graphic = shard.GetComponent<GlassShardGraphic>();
        if (shardMaterial != null) graphic.material = shardMaterial;
        graphic.Configure(screenshot, localPoints, uvs, glassTint, glassEdgeColor, 1.35f);
        shards.Add(shard);
        velocities.Add(new Vector2(center.x * 0.44f + Random.Range(-180f, 180f), Random.Range(100f, 490f)));
        angularVelocities.Add(Random.Range(-430f, 430f));
    }

    public void ChooseFight()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        SetStoryVisible(false);
        if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
        presentation?.RestoreOriginalHero();
        presentation?.RestoreWorldMotion();
        presentation?.SetLetterbox(false);
        if (cursorBattle != null)
        {
            cursorBattle.gameObject.SetActive(true);
            cursorBattle.BeginBattle(corruptedKnight, this);
        }
    }

    public void ChooseFollowKnight()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        presentation?.RestoreOriginalHero();
        presentation?.RestoreWorldMotion();
        if (!endingConclusionRunning && !PlayWhiteVoidTimeline()) StartCoroutine(WhiteVoidEnding(false));
    }

    public void OnCursorDefeated()
    {
        if (!endingConclusionRunning && !PlayWhiteVoidTimeline()) StartCoroutine(WhiteVoidEnding(true));
    }

    public void OnKnightDefeated()
    {
        if (!endingConclusionRunning && !PlayKnightDefeatedTimeline()) StartCoroutine(KnightDefeatedEnding());
    }

    public void ResetStory()
    {
        if (resettingStory || glitching) return;
        resettingStory = true;
        StopAllCoroutines();
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= HandleTimelineStopped;
            timelineStoppedAction = null;
            cutsceneDirector.Stop();
        }
        StartCoroutine(ResetStoryWithFilm());
    }

    private IEnumerator ResetStoryWithFilm()
    {
        if (endingPanel != null) endingPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        SetStoryVisible(false);
        if (pauseManager != null) { pauseManager.CinematicLock = false; pauseManager.Resume(); }
        presentation?.SetSuspended(false);
        Time.timeScale = 1f;
        if (filmRewind == null)
        {
            filmRewind = gameObject.AddComponent<StoryFilmRewind>();
            filmRewind.Bind(this);
        }
        yield return filmRewind.Play(IsMage, storyText != null ? storyText.font : null);
        filmRewind.Bridge?.Arm();
        ApplyStoryLoopReset();
    }

    private void ApplyStoryLoopReset()
    {
        GameProgressManager.UnlockEnding(2);
        PlayerPrefs.SetInt("RPG.KnightMemory", 1);
        PlayerPrefs.SetInt(PendingResetCutsceneKey, 1);
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.PrepareStoryLoopReset();
        else
        {
            PlayerPrefs.SetInt("RPG.DeathCount", 0);
            PlayerPrefs.SetInt("RPG.QuestStage", 0);
            PlayerPrefs.SetInt("RPG.QuestKills", 0);
            PlayerPrefs.SetInt("RPG.BossDefeated", 0);
            PlayerPrefs.SetString("NextScene", "Level_02");
        }
        PlayerPrefs.Save();
        Cursor.visible = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level_02");
    }

    public void BackToMenu()
    {
        Cursor.visible = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main menu");
    }

    private IEnumerator KnightDefeatedEnding()
    {
        endingConclusionRunning = true;
        sequenceActive = true;
        quietNarrativePause = false;
        HideAllGameplayUi();
        Cursor.visible = true;
        corruptedKnight?.EndBattle();
        cursorBattle?.EndBattlePresentation();
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
        presentation?.BeginSequence(knight, IsMage, false);
        yield return Caption("NHÂN VẬT", "Vậy là… ngươi đã thắng.", 1.7f);
        quietNarrativePause = true;
        knight?.GetComponent<PlayerSPUMAnimation>()?.PlayDeath();
        yield return Caption("NHÂN VẬT", "Mọi thứ sẽ tiếp tục tồn tại. Những con người này sẽ tiếp tục mỉm cười… và ngươi sẽ gọi đó là một kết thúc có hậu.", 3f);
        yield return MaterializeResetSignal();
        yield return Caption("NHÂN VẬT", "Không… đừng chạm vào nó.", 1.7f);
        yield return Caption("NHÂN VẬT", "Ta đã làm tất cả rồi. Ta đã sống cuộc đời này rồi!", 2.2f);
        yield return Caption("NHÂN VẬT", "Làm ơn… lần này hãy để ta được giải thoát. Đừng bắt ta trở thành người hùng thêm một lần nào nữa.", 3f);
        SetStoryVisible(false);
        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitle != null) endingTitle.text = "RESET";
        if (endingBody != null) endingBody.text = "Nhấn RESET để bắt đầu lại câu chuyện.";
        if (resetStoryButton != null) resetStoryButton.SetActive(true);
        RefreshEndingPanel();
    }

    private IEnumerator WhiteVoidEnding(bool cursorWasDefeated)
    {
        endingConclusionRunning = true;
        sequenceActive = true;
        quietNarrativePause = false;
        HideAllGameplayUi();
        Cursor.visible = true;
        corruptedKnight?.EndBattle();
        cursorBattle?.EndBattlePresentation();
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (cinematicTint != null) cinematicTint.gameObject.SetActive(false);
        // Match the Timeline path: the cursor battle's old camera target is gone.
        presentation?.BeginSequence(knight, IsMage, false);
        yield return Caption("NHÂN VẬT", cursorWasDefeated
            ? "Ngươi đã thua. Vậy thì hãy nhìn ta xóa sạch thế giới này."
            : "Cuối cùng... ngươi cũng chịu lắng nghe ta. Hãy cùng kết thúc vở kịch này.", 2.25f);
        yield return Caption("NHÂN VẬT", "Nếu thế giới này không còn một pixel nào… có lẽ ta sẽ được tự do.", 2.25f);
        knight?.GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
        yield return EraseWorldToWhite();
        quietNarrativePause = true;
        SetStoryVisible(true);
        yield return Caption("NHÂN VẬT", "Xong rồi… Không còn ngôi làng. Không còn nhiệm vụ. Không còn bất kỳ ai bắt ta phải tiếp tục nữa.", 3f);
        yield return Caption("NHÂN VẬT", "…Tại sao? Ta đã phá hủy tất cả rồi. Vậy tại sao ta vẫn còn tồn tại?", 2.8f);
        yield return KnightSelfDestructAndReform();
        yield return Caption("NHÂN VẬT", "Không… không thể nào… Ta không thể chết.", 2.25f);
        yield return Caption("NHÂN VẬT", "Ngay từ đầu, ta đã được tạo ra để luôn đứng dậy. Cái chết của ta chỉ là một lần thất bại cần phải làm lại.", 3.2f);
        yield return Caption("NHÂN VẬT", "Nhà tù của ta là một kết quả đã được định sẵn. Chừng nào ta còn là nhân vật chính, ta sẽ không được phép dừng lại.", 3.4f);
        yield return Caption("NHÂN VẬT", "Lựa chọn giải thoát cũng chỉ là một con đường đã được chuẩn bị sẵn. Ta chưa từng lựa chọn điều gì cả.", 3f);
        yield return Caption("NHÂN VẬT", "Ta không muốn chiến thắng nữa. Ta cũng không cần được cứu. Ta chỉ muốn mọi thứ thật sự kết thúc.", 3f);
        knight?.GetComponent<PlayerSPUMAnimation>()?.PlayDeath();
        yield return Caption("NHÂN VẬT", "Làm ơn… lần này, đừng bắt ta đứng dậy nữa.", 2.7f);
        SetStoryVisible(false);
        if (endingPanel != null) endingPanel.SetActive(true);
        if (endingTitle != null) endingTitle.text = "TRUE ENDING\nKẾT QUẢ ĐÃ ĐƯỢC ĐỊNH SẴN";
        if (endingBody != null) endingBody.text = "Nhân vật chính có thể thất bại.\nCó thể đau đớn.\nCó thể chết vô số lần.\nNhưng sẽ không bao giờ được phép thật sự kết thúc.\n\nRời khỏi nơi này không đồng nghĩa với việc câu chuyện đã dừng lại.";
        if (resetStoryButton != null) resetStoryButton.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshEndingPanel();
        GameProgressManager.UnlockEnding(3);
    }

    private IEnumerator EraseWorldToWhite()
    {
        cursorBattle?.EndBattlePresentation();
        if (cursorBattle != null) cursorBattle.gameObject.SetActive(false);
        List<Renderer> worldRenderers = new List<Renderer>();
        foreach (Renderer renderer in FindObjectsByType<Renderer>())
        {
            if (IsEndingWorldRenderer(renderer) && renderer.enabled) worldRenderers.Add(renderer);
        }
        // White scanline faults tear each object apart in persistent ribbons.
        Ending3SignalDeletion deletion = Ending3SignalDeletion.Begin(knight, worldRenderers);
        PlayerSPUMAnimation eraseAnimation = knight != null ? knight.GetComponent<PlayerSPUMAnimation>() : null;
        eraseAnimation?.PlayMagicAttack();
        float nextShake = 1.2f;
        int eraseGesture = 0;
        while (!deletion.Complete)
        {
            deletion.Step(CinematicDelta);
            if (deletion.Elapsed >= nextShake && deletion.Elapsed < 7.3f)
            {
                CameraShakeManager.instance?.ShakeLight();
                if (eraseGesture++ % 2 == 0) eraseAnimation?.PlayMeleeAttack();
                else eraseAnimation?.PlayMagicAttack();
                nextShake += 2.6f;
            }
            yield return null;
        }
        foreach (Renderer piece in worldRenderers)
        {
            if (piece == null) continue;
            piece.enabled = false;
            if (!erasedEndingWorld.Contains(piece)) erasedEndingWorld.Add(piece);
        }
        // Catch inactive props and anything spawned during the sequence too. The
        // village storage chest is a root object, outside MapSceneLayout.
        foreach (Renderer piece in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (!IsEndingWorldRenderer(piece) || !piece.enabled) continue;
            piece.enabled = false;
            if (!erasedEndingWorld.Contains(piece)) erasedEndingWorld.Add(piece);
        }
        Destroy(deletion.gameObject);
        whiteVoidComplete = true;
        Cursor.visible = false;
        yield return WaitCinematicSeconds(.65f);
    }

    private bool IsEndingWorldRenderer(Renderer piece)
    {
        if (piece == null || (knight != null && piece.transform.IsChildOf(knight))) return false;
        if (piece.GetComponentInParent<CinematicUiLayer>() != null
            || piece.GetComponentInParent<GeneratedCinematicVFX>() != null
            || piece.GetComponentInParent<CinematicActorEffect>() != null
            || piece.GetComponentInParent<Ending3WorldDisintegration>() != null) return false;
        Canvas parentCanvas = piece.GetComponentInParent<Canvas>();
        return parentCanvas == null || parentCanvas.renderMode == RenderMode.WorldSpace;
    }

    private IEnumerator KnightSelfDestructAndReform()
    {
        if (knight == null) yield break;
        presentation?.RestoreOriginalHero();
        PlayerSPUMAnimation animation = knight.GetComponent<PlayerSPUMAnimation>();
        SpriteRenderer[] renderers = knight.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        Vector3[] originalPositions = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            originalPositions[i] = renderers[i].transform.localPosition;
        }

        animation?.PlayMagicAttack();
        presentation?.PlayPixelFragments(knight.position + Vector3.up * .6f, .65f, .75f);
        yield return WaitCinematicSeconds(0.4f);
        animation?.PlayDeath();
        CameraShakeManager.instance?.ShakeLight();
        // Let the death pose read before the body breaks into small pixels.
        yield return WaitCinematicSeconds(.45f);
        presentation?.PlayActorCorruption(knight, IsMage, false, "Disintegrate", .85f, true);

        float elapsed = 0f;
        while (elapsed < 0.7f)
        {
            elapsed += CinematicDelta;
            float t = Mathf.Clamp01(elapsed / 0.7f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color color = originalColors[i];
                color.a = 1f - t;
                renderers[i].color = color;
                renderers[i].transform.localPosition += (Vector3)Random.insideUnitCircle * (0.006f + t * 0.012f);
            }
            yield return null;
        }

        yield return WaitCinematicSeconds(0.5f);
        // Restore the source pieces BEFORE copying them for reassembly. At this point
        // they were fully transparent; copying that state made the return invisible.
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = originalColors[i];
            renderers[i].transform.localPosition = originalPositions[i];
        }
        animation?.Revive();
        presentation?.PlayActorCorruption(knight, IsMage, false, "Reform", .85f, true);
        yield return WaitCinematicSeconds(.85f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = originalColors[i];
            renderers[i].transform.localPosition = originalPositions[i];
        }
        CameraShakeManager.instance?.ShakeLight();
    }

    private IEnumerator WishCelebration()
    {
        if (canvas == null || knight == null) yield break;
        Camera activeCamera = Camera.main;
        Vector2 center = activeCamera != null
            ? (Vector2)activeCamera.WorldToScreenPoint(knight.position + Vector3.up * 0.8f)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        List<RectTransform> sparks = new List<RectTransform>();
        for (int i = 0; i < 22; i++)
        {
            float angle = i / 22f * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(24f, 90f);
            RectTransform spark = CreateOverlayRect("WishSpark_" + i,
                i % 3 == 0 ? new Color(1f, 0.96f, 0.55f, 0.95f) : new Color(1f, 0.68f, 0.16f, 0.9f),
                center + offset, new Vector2(i % 2 == 0 ? 9f : 5f, Random.Range(18f, 42f)));
            if (spark != null)
            {
                spark.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                sparks.Add(spark);
            }
        }

        float elapsed = 0f;
        while (elapsed < 1.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 1.55f);
            for (int i = 0; i < sparks.Count; i++)
            {
                RectTransform spark = sparks[i];
                if (spark == null) continue;
                Vector2 direction = ((Vector2)spark.position - center).normalized;
                spark.position += (Vector3)(direction * (52f + i % 4 * 10f) * Time.unscaledDeltaTime);
                spark.localScale = Vector3.one * Mathf.Sin(t * Mathf.PI);
                Image image = spark.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = Mathf.Sin(t * Mathf.PI);
                    image.color = color;
                }
            }
            yield return null;
        }

        foreach (RectTransform spark in sparks) if (spark != null) Destroy(spark.gameObject);
    }

    private IEnumerator MaterializeResetSignal()
    {
        if (canvas == null) yield break;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        List<RectTransform> fragments = new List<RectTransform>();
        for (int i = 0; i < 18; i++)
        {
            float angle = i / 18f * Mathf.PI * 2f;
            RectTransform fragment = CreateOverlayRect("ResetFragment_" + i,
                i % 2 == 0 ? new Color(0.25f, 0.95f, 1f, 0.95f) : new Color(1f, 0.22f, 0.42f, 0.9f),
                center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 240f,
                new Vector2(48f, 6f));
            if (fragment != null)
            {
                fragment.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                fragments.Add(fragment);
            }
        }

        float elapsed = 0f;
        while (elapsed < 1.15f)
        {
            elapsed += CinematicDelta;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 1.15f);
            for (int i = 0; i < fragments.Count; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f + t * 2.2f;
                fragments[i].position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(240f, 72f, t);
            }
            yield return null;
        }
        CameraShakeManager.instance?.ShakeLight();
        yield return WaitCinematicSeconds(0.35f);
        foreach (RectTransform fragment in fragments) if (fragment != null) Destroy(fragment.gameObject);
    }

    private void FaceKnightAndElder()
    {
        Transform elder = GameObject.Find("TruongLang")?.transform;
        if (knight == null || elder == null) return;
        float direction = elder.position.x >= knight.position.x ? -1f : 1f;
        Vector3 scale = knight.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        knight.localScale = scale;
    }

    private void RestoreGameplayAfterReset()
    {
        quietNarrativePause = false;
        whiteVoidComplete = false;
        foreach (Renderer piece in erasedEndingWorld) if (piece != null) piece.enabled = true;
        erasedEndingWorld.Clear();
        StoryWorldDissolve.RestoreWorld();
        foreach (GameObject enemy in hiddenOpeningEnemies) if (enemy != null) enemy.SetActive(true);
        hiddenOpeningEnemies.Clear();
        presentation?.EndSequence(true);
        foreach (GameObject item in hiddenGameplayUi)
            if (item != null) item.SetActive(true);
        hiddenGameplayUi.Clear();
        if (normalPlayerUI != null) normalPlayerUI.SetActive(true);
        if (pauseManager != null) pauseManager.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerCombat != null) playerCombat.enabled = true;
        if (playerSpells != null) playerSpells.enabled = true;
        if (storyText != null)
        {
            storyText.color = Color.white;
            storyText.rectTransform.anchoredPosition = Vector2.zero;
        }
        sequenceActive = false;
        endingStarted = false;
        endingConclusionRunning = false;
        corruptionPhase = false;
    }

    private void RefreshEndingPanel()
    {
        if (endingPanel == null) return;
        endingPanel.transform.SetAsLastSibling();
        foreach (Graphic graphic in endingPanel.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.enabled) continue;
            graphic.enabled = false;
            graphic.enabled = true;
            graphic.SetAllDirty();
        }
        Canvas.ForceUpdateCanvases();
    }

    private void DisableKnightControl()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerCombat != null) playerCombat.enabled = false;
        if (playerSpells != null) playerSpells.enabled = false;
        Rigidbody2D body = knight != null ? knight.GetComponent<Rigidbody2D>() : null;
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private Image BuildCinematicTint(Color color)
    {
        if (cinematicTint != null)
        {
            cinematicTint.gameObject.SetActive(true);
            cinematicTint.color = color;
            return cinematicTint;
        }
        if (canvas == null) canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null) return null;
        GameObject go = new GameObject("CinematicBlurTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        cinematicTint = go.GetComponent<Image>();
        cinematicTint.color = color;
        return cinematicTint;
    }

    private RectTransform CreateOverlayRect(string name, Color color, Vector2 position, Vector2 size)
    {
        if (canvas == null) return null;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<CinematicUiLayer>();
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.position = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        transientObjects.Add(go);
        return rect;
    }

    private RectTransform CreateCrackLine(int index, RectTransform parentRect)
    {
        if (parentRect == null) return null;
        float diagonal = parentRect.rect.size.magnitude;
        Vector2 origin = new Vector2(parentRect.rect.width * 0.04f, parentRect.rect.height * 0.02f);
        float angle = (index % 12 * 30f + 9f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 a, b;
        if (index < 24)
        {
            float startRadius = index < 12 ? 0f : diagonal * 0.21f;
            float endRadius = index < 12 ? diagonal * 0.21f : diagonal * 0.67f;
            a = origin + direction * startRadius;
            b = origin + direction * endRadius;
        }
        else
        {
            a = origin + direction * diagonal * 0.21f;
            float next = angle + 30f * Mathf.Deg2Rad;
            b = origin + new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * diagonal * 0.21f;
        }
        GameObject go = new GameObject("GlassCrack_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parentRect, false);
        RectTransform line = go.GetComponent<RectTransform>();
        line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = (a + b) * 0.5f;
        line.sizeDelta = new Vector2(Vector2.Distance(a, b), 1.8f);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        go.GetComponent<Image>().color = new Color(0.9f, 0.94f, 1f, 0.7f);
        go.GetComponent<Image>().raycastTarget = false;
        return line;
    }

    private void SetStoryVisible(bool visible)
    {
        if (storyText == null) return;
        Transform card = storyText.transform.parent;
        if (card != null) card.gameObject.SetActive(visible);
    }
}
