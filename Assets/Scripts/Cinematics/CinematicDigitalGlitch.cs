using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public enum CinematicSignalStyle { Television, SignalBreak }

// Processes the live camera feed, not a tiled picture of coloured corruption.
[RequireComponent(typeof(RawImage))]
[DefaultExecutionOrder(1200)]
public sealed class CinematicDigitalGlitch : MonoBehaviour
{
    private RawImage noiseImage;
    private RawImage sceneImage;
    private Material sceneMaterial;
    private Material noiseMaterial;
    private Camera sourceCamera;
    private Camera pictureCamera;
    private RenderTexture livePicture;
    private float elapsed;
    private float lifetime;
    private bool suspended;
    private CinematicSignalStyle style;
    private float nextShake = 0.3f;
    private readonly List<CanvasState> capturedCanvases = new List<CanvasState>();
    private bool canvasesCaptured;

    private struct CanvasState
    {
        public Canvas canvas;
        public RenderMode mode;
        public Camera camera;
        public float distance;
    }

    public float Elapsed => elapsed;

    public void Initialize(Material template, float seconds, CinematicSignalStyle signalStyle)
    {
        noiseImage = GetComponent<RawImage>();
        noiseImage.raycastTarget = false;
        noiseImage.texture = Texture2D.whiteTexture;
        lifetime = Mathf.Max(0.3f, seconds);
        style = signalStyle;
        if (template == null) { Destroy(gameObject); return; }
        sceneMaterial = new Material(template);
        noiseMaterial = new Material(template);
        noiseMaterial.SetFloat("_NoiseOnly", 1f);
        noiseImage.material = noiseMaterial;

        // Composite the world *and* cinematic UI into one signal. A separate,
        // overlay-only canvas displays the processed picture, avoiding feedback.
        GameObject backdrop = new GameObject("LiveTelevisionPicture", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(RawImage));
        backdrop.transform.SetParent(transform, false);
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        CinematicUiLayer.Configure(backdrop, 30900);
        sceneImage = backdrop.GetComponent<RawImage>();
        sceneImage.raycastTarget = false;
        sceneImage.material = sceneMaterial;
        sourceCamera = Camera.main;
        if (sourceCamera != null)
        {
            GameObject cameraObject = new GameObject("CinematicSignalCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            pictureCamera = cameraObject.GetComponent<Camera>();
            pictureCamera.CopyFrom(sourceCamera);
            var data = pictureCamera.GetUniversalAdditionalCameraData();
            var sourceData = sourceCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = sourceData.renderPostProcessing;
            data.volumeLayerMask = sourceData.volumeLayerMask;
            data.volumeTrigger = sourceData.volumeTrigger;
            data.renderShadows = sourceData.renderShadows;
            data.antialiasing = sourceData.antialiasing;
            data.SetRenderer(-1);
        }
        CollectCanvasStates();
        EnsurePicture();
        UpdateSignal();
    }

    private void EnsurePicture()
    {
        if (sourceCamera == null || pictureCamera == null || suspended) return;
        float scale = Mathf.Min(1f, 1600f / Mathf.Max(1, Screen.width));
        int width = Mathf.Max(2, Mathf.RoundToInt(Screen.width * scale));
        int height = Mathf.Max(2, Mathf.RoundToInt(Screen.height * scale));
        if (livePicture != null && livePicture.width == width && livePicture.height == height)
        {
            pictureCamera.targetTexture = livePicture;
            pictureCamera.enabled = true;
            CaptureCanvases(true);
            return;
        }
        DetachPicture();
        ReleasePicture();
        livePicture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "CinematicLiveSignal",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        livePicture.Create();
        sceneImage.texture = livePicture;
        pictureCamera.targetTexture = livePicture;
        pictureCamera.enabled = true;
        CaptureCanvases(true);
    }

    private void CollectCanvasStates()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder >= 31000) continue;
            if (canvas.GetComponentInParent<CinematicDigitalGlitch>() != null) continue;
            capturedCanvases.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera, distance = canvas.planeDistance });
        }
    }

    private void CaptureCanvases(bool capture)
    {
        if (canvasesCaptured == capture) return;
        canvasesCaptured = capture;
        foreach (CanvasState state in capturedCanvases)
        {
            if (state.canvas == null) continue;
            if (capture)
            {
                state.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                state.canvas.worldCamera = pictureCamera;
                state.canvas.planeDistance = Mathf.Max(0.5f, sourceCamera.nearClipPlane + 0.1f);
            }
            else
            {
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.distance;
            }
        }
    }

    public void Suspend(bool value)
    {
        suspended = value;
        if (noiseImage != null) noiseImage.enabled = !value;
        if (sceneImage != null) sceneImage.enabled = !value;
        if (value) DetachPicture();
        else EnsurePicture();
    }

    private void Update()
    {
        if (suspended || sceneMaterial == null) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= lifetime) { DetachPicture(); Destroy(gameObject); return; }
        if (elapsed >= nextShake)
        {
            nextShake = elapsed + 0.95f;
            CameraShakeManager.instance?.ShakeSignal(0.22f, style == CinematicSignalStyle.Television ? 0.055f : 0.13f);
        }
        EnsurePicture();
        UpdateSignal();
    }

    private void UpdateSignal()
    {
        float attack = Mathf.SmoothStep(0f, 1f, elapsed / 0.16f);
        float release = Mathf.SmoothStep(0f, 1f, (lifetime - elapsed) / 0.3f);
        float envelope = attack * release;
        bool hard = style == CinematicSignalStyle.SignalBreak;
        float swell = 0.76f + 0.24f * Mathf.Sin(elapsed * 4.1f);
        float strength = envelope * swell * (hard ? 0.95f : 0.47f);
        SetSignal(sceneMaterial, strength, envelope, hard);
        SetSignal(noiseMaterial, strength, envelope, hard);
        noiseImage.color = new Color(1f, 1f, 1f, envelope);
    }

    private void LateUpdate()
    {
        if (suspended || sourceCamera == null || pictureCamera == null) return;
        // Keep Main Camera rendering to Display 1. Redirecting it to a texture
        // made Unity show "Display 1 — No cameras rendering" during some endings.
        pictureCamera.CopyFrom(sourceCamera);
        pictureCamera.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        pictureCamera.targetTexture = livePicture;
        pictureCamera.depth = sourceCamera.depth - 1f;
        pictureCamera.rect = new Rect(0f, 0f, 1f, 1f);
        pictureCamera.enabled = true;
    }

    private void SetSignal(Material material, float strength, float envelope, bool hard)
    {
        material.SetFloat("_SignalTime", elapsed);
        material.SetFloat("_Strength", strength);
        material.SetFloat("_Blur", envelope * (hard ? 1.3f : 1.05f));
        material.SetFloat("_SignalLoss", hard ? 1f : 0f);
    }

    private void DetachPicture()
    {
        CaptureCanvases(false);
        if (pictureCamera != null)
        {
            pictureCamera.enabled = false;
            pictureCamera.targetTexture = null;
        }
    }

    private void ReleasePicture()
    {
        if (livePicture == null) return;
        livePicture.Release();
        Destroy(livePicture);
        livePicture = null;
    }

    private void OnDisable() => DetachPicture();

    private void OnDestroy()
    {
        DetachPicture();
        ReleasePicture();
        if (sceneMaterial != null) Destroy(sceneMaterial);
        if (noiseMaterial != null) Destroy(noiseMaterial);
    }
}
