using UnityEngine;

namespace CozyKitchen.Demo
{
    public class AnimatedObject : MonoBehaviour
    {
        public enum ReactMode
        {
            Trigger,
            Bool
        }

        [Header("Animation")]
        public ReactMode reactMode = ReactMode.Trigger;
        public string animationParameter = "Trigger";
        public float playIntervalSeconds = 0f;
        public bool playOnStart = true;

        [Header("Audio (Optional)")]
        public AudioSource audioSource;
        public AudioClip clipOverride;
        public bool playAudioWithAnimation = true;

        private Animator animator;
        private float timer;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        void Update()
        {
            if (playIntervalSeconds > 0f)
            {
                timer += Time.deltaTime;
                if (timer >= playIntervalSeconds)
                {
                    timer = 0f;
                    Play();
                }
            }
        }

        public void Play()
        {
            if (animator == null || string.IsNullOrEmpty(animationParameter))
                return;

            if (reactMode == ReactMode.Trigger)
            {
                if (HasParameter(animationParameter, AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger(animationParameter);
                }
            }
            else if (reactMode == ReactMode.Bool)
            {
                if (HasParameter(animationParameter, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(animationParameter, true);
                }
            }

            if (playAudioWithAnimation && audioSource != null)
            {
                if (clipOverride != null)
                {
                    audioSource.PlayOneShot(clipOverride);
                }
                else
                {
                    audioSource.Play();
                }
            }
        }

        private bool HasParameter(string paramName, AnimatorControllerParameterType type)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == type)
                    return true;
            }
            return false;
        }
    }
}
