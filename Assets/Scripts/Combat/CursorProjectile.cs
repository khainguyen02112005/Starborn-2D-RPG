using UnityEngine;

public class CursorProjectile : MonoBehaviour
{
    public float speed = 11f;
    private CorruptedKnightBoss target;
    private int damage;
    private Vector3 travelDirection = Vector3.up;
    private float remaining = 4f;

    public void Initialize(CorruptedKnightBoss boss, int amount, Vector2 direction = default)
    {
        target = boss;
        damage = amount;
        travelDirection = direction.sqrMagnitude > .0001f ? (Vector3)direction.normalized : Vector3.up;
        remaining = 4f;
    }

    private void Update()
    {
        if (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended) return;
        if (target == null) { Destroy(gameObject); return; }
        float step = speed * Time.unscaledDeltaTime;
        Vector3 from = transform.position;
        transform.position += travelDirection * step;
        remaining -= Time.unscaledDeltaTime;
        // Sweep the straight segment so a fast bullet cannot skip over the hero.
        Vector3 hitPoint = from + travelDirection * Mathf.Clamp(Vector3.Dot(target.transform.position - from, travelDirection), 0f, step);
        if (Vector2.Distance(hitPoint, target.transform.position) > .5f)
        {
            if (remaining <= 0f) Destroy(gameObject);
            return;
        }
        target.ReceiveCursorDamage(damage);
        GameAudioManager.Play(GameSound.CursorHit, transform.position, 0.62f, 1.08f);
        Destroy(gameObject);
    }

    public void BreakByKnight()
    {
        CameraShakeManager.instance?.ShakeLight();
        GameAudioManager.Play(GameSound.Block, transform.position, 0.75f, 1.18f);
        Destroy(gameObject);
    }
}
