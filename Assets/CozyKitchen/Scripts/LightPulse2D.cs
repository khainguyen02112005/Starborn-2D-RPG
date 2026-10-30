using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Timbeaux.CozyKitchen
{
    public class LightPulse2D : MonoBehaviour
    {
        public Light2D targetLight;
        public float minIntensity = 0.8f;
        public float maxIntensity = 1.2f;
        public float pulseSpeed = 1f;

        private float t;

        private void Reset()
        {
            targetLight = GetComponent<Light2D>();
        }

        private void Update()
        {
            if (targetLight == null)
                return;

            t += Time.deltaTime * pulseSpeed;
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PingPong(t, 1f));
        }
    }
}
