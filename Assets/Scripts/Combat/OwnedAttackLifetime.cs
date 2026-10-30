using UnityEngine;

// Independently spawned spells must be removed even when the caster's coroutine stops.
public sealed class OwnedAttackLifetime : MonoBehaviour
{
    private Component owner;
    private float remaining = 8f;
    private bool initialized;
    private Material ownedMaterial;

    public void Initialize(Component caster, float seconds = 8f, Material material = null)
    {
        owner = caster;
        remaining = seconds;
        ownedMaterial = material;
        initialized = true;
    }
    private void Update()
    {
        if (!initialized) return;
        if (owner == null || !owner.gameObject.activeInHierarchy || (owner is BossAI boss && boss.IsDying)) { Destroy(gameObject); return; }
        if (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) Destroy(gameObject);
    }
    public static void Clear(Component caster)
    {
        foreach (OwnedAttackLifetime attack in FindObjectsByType<OwnedAttackLifetime>())
            if (attack.owner == caster) { attack.gameObject.SetActive(false); Destroy(attack.gameObject); }
    }
    private void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
}
