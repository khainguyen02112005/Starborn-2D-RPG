using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameSound
{
    UIHover,
    UIClick,
    UIConfirm,
    UICancel,
    UIError,
    UIToggle,
    Dialogue,
    QuestComplete,
    SwordSwing,
    MageAttack,
    Dash,
    PlayerHit,
    PlayerDeath,
    Block,
    Revive,
    ItemPickup,
    Equip,
    Purchase,
    ChestOpen,
    ShopOpen,
    Blacksmith,
    Portal,
    EnemyAttack,
    EnemyHit,
    EnemyDeath,
    BossRoar,
    BossCast,
    BossLightning,
    BossFire,
    BossWater,
    BossHit,
    BossDeath,
    AOEWarning,
    AOEImpact,
    CursorShot,
    CursorHit
}

[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    private const string LibraryResourceName = "GameAudioLibrary";

    private static GameAudioManager instance;

    private GameAudioLibrary library;
    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private AudioSource uiSource;
    private AudioSource[] worldSources;
    private int nextWorldSource;
    private float lastFootstepTime = -10f;
    private float lastCreatureFootstepTime = -10f;

    public static bool IsReady => instance != null && instance.library != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static GameAudioManager EnsureInstance()
    {
        if (instance != null) return instance;
        instance = FindAnyObjectByType<GameAudioManager>(FindObjectsInactive.Include);
        if (instance != null) return instance;

        GameObject root = new GameObject("GameAudioManager");
        instance = root.AddComponent<GameAudioManager>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        library = Resources.Load<GameAudioLibrary>(LibraryResourceName);
        BuildSources();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void BuildSources()
    {
        var prefab = Resources.Load<GameObject>("AudioRig");
        if (prefab == null) { Debug.LogError("Missing native AudioRig prefab"); return; }
        var rig = Instantiate(prefab, transform);
        musicSource = rig.transform.Find("Music").GetComponent<AudioSource>();
        ambienceSource = rig.transform.Find("Ambience").GetComponent<AudioSource>();
        uiSource = rig.transform.Find("UI SFX").GetComponent<AudioSource>();
        worldSources = rig.GetComponentsInChildren<AudioSource>();
        worldSources = System.Array.FindAll(worldSources, source => source.name.StartsWith("World SFX "));
        ApplyVolumeSettings();
    }

    public static void RefreshVolumeSettings()
    {
        if (instance != null) instance.ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null) musicSource.volume = (library != null ? library.musicVolume : .07f) * AudioSettingsManager.MusicVolume;
        if (ambienceSource != null) ambienceSource.volume = library != null ? library.ambienceVolume : 0;
        if (uiSource != null) uiSource.volume = library != null ? library.uiVolume : .55f;
        // SFX now follows the master listener only; the removed effects slider
        // cannot leave combat/UI audio silently muted by a hidden legacy setting.
        if (worldSources != null) foreach (AudioSource source in worldSources) if (source != null) source.volume = library != null ? library.worldVolume : .55f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneAudio(scene.name);
        StopAllCoroutines();
        StartCoroutine(RefreshUIAudio());
    }

    private void ConfigureSceneAudio(string sceneName)
    {
        if (library == null) return;
        string normalized = sceneName.ToLowerInvariant();
        bool loading = normalized.Contains("loading");
        bool menu = normalized.Contains("main menu") || normalized.Contains("level_01") ||
                    normalized.Contains("tiến trình") || normalized.Contains("thành tựu");
        bool boss = normalized.Contains("level_04");

        AudioClip requestedMusic = loading ? library.loadingMusic
            : boss && library.bossMusic != null ? library.bossMusic
            : menu ? library.menuMusic
            : library.gameplayMusic;

        float requestedPitch = 1f;
        if (musicSource.clip != requestedMusic)
        {
            musicSource.Stop();
            musicSource.clip = requestedMusic;
            if (requestedMusic != null) musicSource.Play();
        }
        musicSource.pitch = requestedPitch;
        ApplyVolumeSettings();

        // Chỉ dùng một lớp âm nền. Chồng nhạc + gió làm nền bị ồn và mệt tai.
        bool useWind = requestedMusic == null && (normalized.Contains("level_02") ||
                       normalized.Contains("level_03") || normalized.Contains("level_04") || normalized.Contains("sample"));
        if (useWind && library.ambientWind != null)
        {
            if (ambienceSource.clip != library.ambientWind)
            {
                ambienceSource.clip = library.ambientWind;
                ambienceSource.Play();
            }
        }
        else
        {
            ambienceSource.Stop();
            ambienceSource.clip = null;
        }
    }

    private IEnumerator RefreshUIAudio()
    {
        // Native buttons already carry emitters; one pass covers legacy/runtime
        // controls created during scene Awake, without a permanent scene scan.
        yield return null;
        foreach (Selectable selectable in FindObjectsByType<Selectable>(FindObjectsInactive.Include))
        {
            if (selectable != null && selectable.GetComponent<UISoundEmitter>() == null)
                selectable.gameObject.AddComponent<UISoundEmitter>();
        }
    }

    public static void Play(GameSound sound, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        GameAudioManager manager = EnsureInstance();
        AudioClip clip = manager.GetClip(sound);
        if (clip == null || manager.worldSources == null || manager.worldSources.Length == 0) return;

        AudioSource source = manager.worldSources[manager.nextWorldSource++ % manager.worldSources.Length];
        source.transform.position = position;
        source.pitch = Mathf.Clamp(pitch, 0.65f, 1.45f);
        source.Stop();
        source.clip = clip;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayUI(GameSound sound, float volume = 1f, float pitch = 1f)
    {
        GameAudioManager manager = EnsureInstance();
        AudioClip clip = manager.GetClip(sound);
        if (clip == null || manager.uiSource == null) return;
        manager.uiSource.pitch = Mathf.Clamp(pitch, 0.7f, 1.4f);
        manager.uiSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayFootstep(Vector3 position, bool dashing = false, float volume = 1f, bool creature = false)
    {
        GameAudioManager manager = EnsureInstance();
        float minimumGap = creature ? .3f : dashing ? .12f : .18f;
        float lastTime = creature ? manager.lastCreatureFootstepTime : manager.lastFootstepTime;
        if (Time.unscaledTime - lastTime < minimumGap) return;
        if (manager.library == null || manager.library.footsteps == null || manager.library.footsteps.Length == 0) return;
        AudioClip clip = manager.library.footsteps[Random.Range(0, manager.library.footsteps.Length)];
        if (clip == null) return;
        AudioSource source = manager.worldSources[manager.nextWorldSource++ % manager.worldSources.Length];
        source.transform.position = position;
        source.pitch = Random.Range(dashing ? 1.08f : 0.92f, dashing ? 1.22f : 1.08f);
        source.Stop();
        source.clip = clip;
        source.PlayOneShot(clip, (dashing ? 0.62f : 0.46f) * Mathf.Clamp01(volume));
        if (creature) manager.lastCreatureFootstepTime = Time.unscaledTime;
        else manager.lastFootstepTime = Time.unscaledTime;
    }

    private AudioClip GetClip(GameSound sound)
    {
        if (library == null) return null;
        return sound switch
        {
            GameSound.UIHover => library.uiHover,
            GameSound.UIClick => library.uiClick,
            GameSound.UIConfirm => library.uiConfirm,
            GameSound.UICancel => library.uiCancel,
            GameSound.UIError => library.uiError,
            GameSound.UIToggle => library.uiToggle,
            GameSound.Dialogue => library.dialogue,
            GameSound.QuestComplete => library.questComplete,
            GameSound.SwordSwing => library.swordSwing,
            GameSound.MageAttack => library.mageAttack,
            GameSound.Dash => library.dash,
            GameSound.PlayerHit => library.playerHit,
            GameSound.PlayerDeath => library.playerDeath,
            GameSound.Block => library.block,
            GameSound.Revive => library.revive,
            GameSound.ItemPickup => library.itemPickup,
            GameSound.Equip => library.equip,
            GameSound.Purchase => library.purchase,
            GameSound.ChestOpen => library.chestOpen,
            GameSound.ShopOpen => library.shopOpen,
            GameSound.Blacksmith => library.blacksmith,
            GameSound.Portal => library.portal,
            GameSound.EnemyAttack => library.enemyAttack,
            GameSound.EnemyHit => library.enemyHit,
            GameSound.EnemyDeath => library.enemyDeath,
            GameSound.BossRoar => library.bossRoar,
            GameSound.BossCast => library.bossCast,
            GameSound.BossLightning => library.bossLightning,
            GameSound.BossFire => library.bossFire,
            GameSound.BossWater => library.bossWater,
            GameSound.BossHit => library.bossHit,
            GameSound.BossDeath => library.bossDeath,
            GameSound.AOEWarning => library.aoeWarning,
            GameSound.AOEImpact => library.aoeImpact,
            GameSound.CursorShot => library.cursorShot,
            GameSound.CursorHit => library.cursorHit,
            _ => null
        };
    }
}
