using UnityEngine;

[DisallowMultipleComponent]
public sealed class GeneratedCinematicActor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public Animator Animator => animator;
    public SpriteRenderer Renderer => spriteRenderer;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Play(string stateName, bool restart = true)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) return;
        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash)) return;
        animator.Play(hash, 0, restart ? 0f : animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        animator.Update(0f);
    }

    public void SetFacing(float direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction) < 0.01f) return;
        spriteRenderer.flipX = direction < 0f;
    }
}
