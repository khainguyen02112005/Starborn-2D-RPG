using UnityEngine;

[DefaultExecutionOrder(1000)] // Chạy sau cùng để đè lên Cinemachine
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager instance;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Camera mainCam;
    private Vector3 appliedOffset;
    private Vector3 lastShakenPosition;
    private float shakeLength;
    private float shakeSeed;

    void Awake()
    {
        instance = this;
        mainCam = Camera.main;
    }

    public void ShakeLight()
    {
        ShakeSignal(0.15f, 0.1f);
    }

    public void ShakeHeavy()
    {
        ShakeSignal(0.3f, 0.32f);
    }

    public void ShakeSignal(float seconds, float magnitude)
    {
        if (shakeDuration > 0f && magnitude < shakeMagnitude) return;
        shakeDuration = shakeLength = Mathf.Max(0.01f, seconds);
        shakeMagnitude = magnitude;
        shakeSeed = Random.Range(1f, 100f);
    }

    void LateUpdate()
    {
        if (mainCam == null) return;
        // Remove our old offset only if a camera controller has not already replaced it.
        if ((mainCam.transform.position - lastShakenPosition).sqrMagnitude < 0.000001f)
            mainCam.transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
        if (shakeDuration > 0 && mainCam != null)
        {
            float age = shakeLength - shakeDuration;
            float decay = Mathf.Clamp01(shakeDuration / shakeLength);
            float x = Mathf.Sin(age * 88f + shakeSeed) * decay;
            float y = (Mathf.PerlinNoise(shakeSeed, age * 31f) * 2f - 1f) * decay;
            Vector3 randomOffset = new Vector3(x, y * 0.65f, 0f) * shakeMagnitude;
            appliedOffset = randomOffset;
            mainCam.transform.position += appliedOffset;
            lastShakenPosition = mainCam.transform.position;
            
            // The broken pause cinematic runs at timeScale zero. Scaled deltaTime
            // made the shake run forever and walk the camera away from the actors.
            shakeDuration -= Time.unscaledDeltaTime;
        }
    }
}
