using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Render an explicit frame instead of reading an unfinished Editor backbuffer.
// Used once per pause fracture, including every stacked error window and crack.
public static class CinematicFrameCapture
{
    private struct CanvasState
    {
        public Canvas canvas;
        public RenderMode mode;
        public Camera camera;
        public float distance;
    }

    public static Texture2D Capture(Camera camera)
    {
        if (camera == null) return null;
        var request = new UniversalRenderPipeline.SingleCameraRequest();
        if (GraphicsSettings.currentRenderPipeline != null && !RenderPipeline.SupportsRenderRequest(camera, request)) return null;
        int width = Mathf.Max(2, Screen.width);
        int height = Mathf.Max(2, Screen.height);
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        var canvases = new List<CanvasState>();
        Texture2D captured = null;
        try
        {
            camera.targetTexture = target;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvases.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera, distance = canvas.planeDistance });
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(0.5f, camera.nearClipPlane + 0.1f);
            }
            Canvas.ForceUpdateCanvases();
            if (GraphicsSettings.currentRenderPipeline == null) camera.Render();
            else
            {
                request.destination = target;
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            RenderTexture.active = target;
            captured = new Texture2D(width, height, TextureFormat.RGBA32, false);
            captured.name = "PauseGlassComposite";
            captured.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            captured.Apply(false, false);
            return captured;
        }
        catch (System.Exception exception)
        {
            if (captured != null) Object.Destroy(captured);
            Debug.LogWarning("Unable to capture the pause fracture picture: " + exception.Message);
            return null;
        }
        finally
        {
            foreach (CanvasState state in canvases)
            {
                if (state.canvas == null) continue;
                state.canvas.renderMode = state.mode;
                state.canvas.worldCamera = state.camera;
                state.canvas.planeDistance = state.distance;
            }
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Canvas.ForceUpdateCanvases();
        }
    }
}
