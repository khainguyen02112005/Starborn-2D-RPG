using System.Collections.Generic;
using UnityEngine;

public class ElementalProjectile : MonoBehaviour
{
    public enum Element { Fire, Water, Arcane, Lightning }

    private Element element;
    private Vector2 direction;
    private float speed;
    private int damage;
    private LayerMask targetLayers;
    private float lifeRemaining = 2.6f;
    private bool finished;
    private Transform caster;
    private CursorBattleController cursorTarget;
    private bool cursorAttack;
    private float orbitRemaining;
    private float orbitRadius;
    private Vector2 orbitDirection;
    private Vector3 previousPosition;

    public void SetCaster(Transform owner, CursorBattleController cursor = null)
    {
        caster = owner;
        cursorTarget = cursor;
        cursorAttack = cursor != null;
        if (cursorAttack) lifeRemaining = 5f;
    }
    public static void ClearCaster(Transform owner)
    {
        foreach (ElementalProjectile bolt in FindObjectsByType<ElementalProjectile>())
            if (bolt.caster == owner) { bolt.gameObject.SetActive(false); Destroy(bolt.gameObject); }
    }
    public void OrbitBeforeLaunch(Transform owner, Vector2 radialDirection, float radius, float delay)
    {
        caster = owner;
        orbitDirection = radialDirection;
        orbitRadius = radius;
        orbitRemaining = delay;
        transform.position = caster.position + Vector3.up * .45f + (Vector3)(orbitDirection * radius);
    }

    public void Initialize(Element projectileElement, Vector2 travelDirection, float travelSpeed, int amount, LayerMask layers)
    {
        element = projectileElement;
        direction = travelDirection.normalized;
        speed = travelSpeed;
        damage = amount;
        targetLayers = layers;
        CombatVFX.AttachProjectileAura(transform, element);
        OrientVisual();
    }

    private void Update()
    {
        if (cursorAttack && (caster == null || cursorTarget == null || !cursorTarget.IsBattleActive)) { Destroy(gameObject); return; }
        if (!cursorAttack && Time.timeScale <= 0f) return;
        if (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended) return;
        if (!cursorAttack && CinematicPresentationController.IsCinematicActive) { Destroy(gameObject); return; }
        float delta = cursorAttack ? Time.unscaledDeltaTime : Time.deltaTime;
        if (orbitRemaining > 0f)
        {
            if (caster == null) { Destroy(gameObject); return; }
            orbitRemaining -= delta;
            orbitDirection = Quaternion.Euler(0f, 0f, 90f * delta) * orbitDirection;
            transform.position = caster.position + Vector3.up * .45f + (Vector3)(orbitDirection * orbitRadius);
            direction = orbitDirection.normalized;
            OrientVisual();
            return;
        }
        previousPosition = transform.position;
        if (cursorAttack)
        {
            Vector2 aim = cursorTarget.CursorWorldPosition - transform.position;
            if (aim.sqrMagnitude > .00001f) direction = aim.normalized;
        }
        float step = speed * delta;
        transform.position += (Vector3)(direction * step);
        if (element == Element.Lightning) OrientVisual();
        else transform.Rotate(0f, 0f, 180f * delta);
        lifeRemaining -= delta;
        if (HitTarget()) Finish(true);
        else if (lifeRemaining <= 0f) Finish(false);
    }

    private void OrientVisual()
    {
        // The electric sprite faces left; keep it aligned with flight, not spinning.
        if (element == Element.Lightning && direction.sqrMagnitude > .0001f)
            transform.rotation = Quaternion.FromToRotation(Vector3.left, direction);
    }

    private bool HitTarget()
    {
        if (cursorAttack)
        {
            Vector2 from = previousPosition;
            Vector2 segment = (Vector2)transform.position - from;
            float t = segment.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot((Vector2)cursorTarget.CursorWorldPosition - from, segment) / segment.sqrMagnitude) : 0f;
            if (Vector2.Distance(from + segment * t, cursorTarget.CursorWorldPosition) > .35f) return false;
            cursorTarget.TakeDamage(damage);
            StoryWorldDissolve.Instance?.Strike(cursorTarget.CursorWorldPosition, .65f, false);
            return true;
        }
        // Sweep the whole frame's travel, so a fast bolt cannot jump over a
        // nearby enemy during a slow frame (overlap-at-end alone missed hits).
        Vector2 travel = (Vector2)(transform.position - previousPosition);
        int mask = targetLayers.value == 0 ? ~0 : targetLayers.value;
        Collider2D[] hits;
        if (travel.sqrMagnitude > .000001f)
        {
            RaycastHit2D[] swept = Physics2D.CircleCastAll(previousPosition, .32f, travel.normalized, travel.magnitude, mask);
            hits = new Collider2D[swept.Length];
            for (int i = 0; i < swept.Length; i++) hits[i] = swept[i].collider;
        }
        else hits = Physics2D.OverlapCircleAll(transform.position, .32f, mask);
        HashSet<Component> checkedTargets = new HashSet<Component>();
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform.IsChildOf(transform) || hit.CompareTag("Player")) continue;
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            BossAI boss = hit.GetComponentInParent<BossAI>();
            if (enemy == null && boss == null) continue;
            Component target = enemy != null ? enemy : boss;
            if (!checkedTargets.Add(target)) continue;
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                if (element == Element.Water) enemy.ApplyFreeze(.7f);
            }
            else boss.TakeDamage(damage);
            return true;
        }
        return false;
    }

    private void Finish(bool hit)
    {
        if (finished) return;
        finished = true;
        if (element == Element.Fire) CombatVFX.SpawnFireImpact(transform.position, hit ? 1f : 0.65f);
        else if (element == Element.Water) CombatVFX.SpawnWaterImpact(transform.position, hit ? 1f : 0.65f);
        else CombatVFX.SpawnLightningImpact(transform.position, hit ? 0.8f : 0.5f);
        if (hit)
        {
            GameSound impact = element == Element.Fire ? GameSound.BossFire
                : element == Element.Water ? GameSound.BossWater
                : GameSound.CursorHit;
            GameAudioManager.Play(impact, transform.position, 0.52f, Random.Range(0.9f, 1.12f));
        }
        Destroy(gameObject);
    }
}
