using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public Slider loadingBar;
    public string sceneToLoad = "Level_02";
    public RectTransform animatedCharacter;
    public RectTransform trackStart;
    public RectTransform trackEnd;
    public TextMeshProUGUI progressText;
    public float minimumDisplayTime = 1.8f;
    public Transform worldCharacter;
    public Vector3 worldTrackStart = new Vector3(-5f, -2.2f, 0f);
    public Vector3 worldTrackEnd = new Vector3(5f, -2.2f, 0f);
    [Header("Nhân vật chạy trên thanh loading")]
    [SerializeField, Range(48f, 140f)] private float runnerPixelHeight = 88f;
    private Camera runnerCamera;
    private Camera mainCamera;
    private RenderTexture runnerTexture;
    private RectTransform runnerRect;
    private int previousCameraMask;
    private const int PreviewLayer = 31;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        string requestedScene = PlayerPrefs.GetString("NextScene", sceneToLoad);
        if (!string.IsNullOrWhiteSpace(requestedScene)) sceneToLoad = requestedScene;
        if (worldCharacter == null) worldCharacter = new GameObject("LoadingClassAvatar").transform;
        if (worldCharacter.GetComponent<PlayerClassController>() == null) worldCharacter.gameObject.AddComponent<PlayerClassController>();
        // ClassController.Start rebinds the selected rig; start running after that.
        yield return null;
        ConfigureRunner();
        yield return LoadSceneAsync();
    }

    private void ConfigureRunner()
    {
        if (loadingBar == null || worldCharacter == null) return;
        Canvas canvas = loadingBar.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        PlayerSPUMAnimation animation = worldCharacter.GetComponent<PlayerSPUMAnimation>();
        if (animation == null) animation = worldCharacter.gameObject.AddComponent<PlayerSPUMAnimation>();
        var selected = worldCharacter.GetComponent<PlayerClassController>().ActiveAvatarObject;
        if (selected != null) animation.SetAvatar(selected.GetComponent<SPUM_Prefabs>());
        worldCharacter.localScale = new Vector3(-1f, 1f, 1f);
        worldCharacter.position = new Vector3(0f, 10000f, 0f);
        animation.SetMoving(true);
        foreach (Transform child in worldCharacter.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            previousCameraMask = mainCamera.cullingMask;
            mainCamera.cullingMask &= ~(1 << PreviewLayer);
        }
        GameObject cameraObject = new GameObject("LoadingAvatarCamera", typeof(Camera));
        runnerCamera = cameraObject.GetComponent<Camera>();
        runnerCamera.orthographic = true;
        runnerCamera.clearFlags = CameraClearFlags.SolidColor;
        runnerCamera.backgroundColor = Color.clear;
        runnerCamera.cullingMask = 1 << PreviewLayer;
        runnerCamera.depth = -50f;
        runnerTexture = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32) { name = "LoadingAvatarPreview", filterMode = FilterMode.Point };
        runnerTexture.Create();
        runnerCamera.targetTexture = runnerTexture;
        Bounds bounds = new Bounds(worldCharacter.position + Vector3.up * .7f, Vector3.one);
        bool found = false;
        foreach (SpriteRenderer renderer in worldCharacter.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        runnerCamera.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x) * .64f;
        runnerCamera.transform.position = bounds.center + Vector3.back * 10f;
        GameObject icon = new GameObject("SelectedClassLoadingRunner", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        icon.transform.SetParent(canvas.transform, false);
        runnerRect = icon.GetComponent<RectTransform>();
        runnerRect.sizeDelta = Vector2.one * runnerPixelHeight;
        runnerRect.pivot = new Vector2(.5f, .12f);
        RawImage image = icon.GetComponent<RawImage>();
        image.texture = runnerTexture; image.raycastTarget = false;
        loadingBar.interactable = false;
        loadingBar.navigation = new Navigation { mode = Navigation.Mode.None };
        if (progressText != null)
        {
            progressText.color = GameUITheme.Cream;
            progressText.fontStyle = FontStyles.Bold;
        }
        loadingBar.minValue = 0f; loadingBar.maxValue = 1f; loadingBar.wholeNumbers = false;
        // A progress indicator is not a draggable slider. Keep its empty track
        // dark and crop the fill rect, including when its Image has no sprite.
        if (loadingBar.TryGetComponent(out Image trackImage)) GameUITheme.Surface(trackImage, "Dialogue", 2.8f);
        if (loadingBar.handleRect != null)
        {
            loadingBar.handleRect.parent.gameObject.SetActive(false);
            loadingBar.handleRect = null;
        }
        if (loadingBar.fillRect != null)
        {
            loadingBar.fillRect.offsetMin = loadingBar.fillRect.offsetMax = Vector2.zero;
            Image fill = loadingBar.fillRect.GetComponent<Image>();
            if (fill != null) { fill.type = Image.Type.Simple; fill.color = GameUITheme.Gold; }
        }
        UpdatePresentation(0f);
    }

    private IEnumerator LoadSceneAsync()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad) || !Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError("LoadingManager cannot load scene: " + sceneToLoad, this);
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;
        float displayed = 0f;
        float elapsed = 0f;

        while (!operation.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float loaded = Mathf.Clamp01(operation.progress / 0.9f);
            float timed = Mathf.Clamp01(elapsed / minimumDisplayTime);
            displayed = Mathf.MoveTowards(displayed, Mathf.Min(loaded, timed), 1.6f * Time.unscaledDeltaTime);
            UpdatePresentation(displayed);

            if (displayed >= 0.999f && loaded >= 0.999f)
                operation.allowSceneActivation = true;

            yield return null;
        }
    }

    public void UpdatePresentation(float progress)
    {
        if (loadingBar != null) loadingBar.value = progress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
        if (animatedCharacter != null && trackStart != null && trackEnd != null)
        {
            Vector3 position = Vector3.Lerp(trackStart.position, trackEnd.position, progress);
            position.y += Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) * 9f;
            animatedCharacter.position = position;
        }
        if (runnerRect != null && loadingBar != null)
        {
            RectTransform track = loadingBar.fillRect != null ? loadingBar.fillRect.parent as RectTransform : loadingBar.GetComponent<RectTransform>();
            Vector3 foot = track.TransformPoint(new Vector3(Mathf.Lerp(track.rect.xMin + 5f, track.rect.xMax - 5f, progress), track.rect.yMax + 2f, 0f));
            runnerRect.position = foot;
        }
    }

    private void OnDestroy()
    {
        if (mainCamera != null && runnerCamera != null) mainCamera.cullingMask = previousCameraMask;
        if (runnerCamera != null) { runnerCamera.targetTexture = null; Destroy(runnerCamera.gameObject); }
        if (runnerTexture != null) { runnerTexture.Release(); Destroy(runnerTexture); }
        if (runnerRect != null) Destroy(runnerRect.gameObject);
    }
}
