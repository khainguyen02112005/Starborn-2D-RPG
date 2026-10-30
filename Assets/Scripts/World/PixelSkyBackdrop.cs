using UnityEngine;
using UnityEngine.Rendering;

// A visual-only sky layer. It never expands the walkable map or changes the camera.
// Fit per rendering camera so the same sky also works in Scene view and the minimap.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class PixelSkyBackdrop : MonoBehaviour
{
    private SpriteRenderer sky;

    private void OnEnable()
    {
        sky = GetComponent<SpriteRenderer>();
        RenderPipelineManager.beginCameraRendering += BeforeSrpCamera;
        Camera.onPreCull += BeforeCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeSrpCamera;
        Camera.onPreCull -= BeforeCamera;
    }

    private void BeforeSrpCamera(ScriptableRenderContext context, Camera camera) => BeforeCamera(camera);

    private void BeforeCamera(Camera camera)
    {
        if (sky == null || sky.sprite == null || !sky.enabled || camera == null || !camera.orthographic) return;
        if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection) return;

        Vector2 spriteSize = sky.sprite.bounds.size;
        float height = camera.orthographicSize * 2f;
        float width = height * camera.aspect;
        float scale = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * 1.02f;
        float distance = Mathf.Min(50f, camera.farClipPlane * .5f);
        transform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * distance, camera.transform.rotation);
        transform.localScale = new Vector3(scale, scale, 1f);
        sky.sortingLayerID = 0;
        sky.sortingOrder = -30000;
    }
}
