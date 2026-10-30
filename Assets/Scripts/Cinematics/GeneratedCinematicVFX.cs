using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GeneratedCinematicVFX : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void Play(string stateName, float lifetime)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(stateName))
        {
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                animator.Update(0f);
            }
        }
        StartCoroutine(DestroyAfter(Mathf.Max(0.25f, lifetime)));
    }

    private IEnumerator DestroyAfter(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            CinematicPresentationController presentation = CinematicPresentationController.Instance;
            if (presentation == null || !presentation.IsSuspended) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}
