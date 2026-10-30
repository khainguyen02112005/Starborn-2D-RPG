using UnityEngine;

// The Animator drives only the canopy pivot. Trunk footprints remain static.
[DisallowMultipleComponent]
public sealed class NaturalTreeMotion : MonoBehaviour
{
    public Animator canopyAnimator;
    [Range(0, 1)] public float phase;
    private void Start()
    {
        if (canopyAnimator != null && canopyAnimator.runtimeAnimatorController != null)
            canopyAnimator.Play("Breeze", 0, phase);
    }
}
