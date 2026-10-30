using System.Collections.Generic;
using UnityEngine;

public class LightningSkill : MonoBehaviour
{
    public KeyCode castKey = KeyCode.Q;
    public bool listenForInput;
    public bool requireLearnedSkill = true;
    public LayerMask enemyLayers;
    public Sprite[] lightningFrames;
    public int manaCost = 2;
    public int damage = 12;
    public float range = 8f;
    public float impactRadius = 1.35f;
    public float cooldown = 1.8f;
    private float nextCastTime;

    private void Update()
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (!listenForInput || (health != null && health.IsDead) || !Input.GetKeyDown(castKey)) return;
        Cast();
    }

    public bool Cast()
    {
        if (GameplayUiFocus.BlocksControls) return false;
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat == null || !combat.RequireUsableWeapon()) return false;
        SkillTree tree = GetComponent<SkillTree>();
        if (requireLearnedSkill && (tree == null || !tree.hasSkill1)) return false;
        if (Time.time < nextCastTime) return false;
        PlayerMana mana = GetComponent<PlayerMana>();
        if (mana == null || !mana.UseMana(manaCost)) return false;
        nextCastTime = Time.time + cooldown;
        Vector3 targetPosition = FindTarget();
        DamageAt(targetPosition);
        Vector3 origin = combat != null && combat.attackPoint != null ? combat.attackPoint.position : transform.position + Vector3.up * .7f;
        CombatVFX.SpawnLightningCast(origin, .8f);
        CombatVFX.SpawnLightningStrike(targetPosition, 1.35f);
        CombatVFX.SpawnLightningImpact(targetPosition, 1.1f);
        LightningArcVFX.Spawn(origin, targetPosition);
        GetComponent<PlayerSPUMAnimation>()?.PlayMagicAttack();
        return true;
    }

    private Vector3 FindTarget()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, range, enemyLayers.value == 0 ? ~0 : enemyLayers.value);
        Collider2D closest = null;
        float closestDistance = float.MaxValue;
        foreach (Collider2D target in targets)
        {
            if (target.GetComponentInParent<EnemyHealth>() == null && target.GetComponentInParent<BossAI>() == null) continue;
            float distance = Vector2.SqrMagnitude(target.transform.position - transform.position);
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closest = target;
        }
        if (closest != null) return closest.transform.position;
        float direction = transform.localScale.x < 0f ? 1f : -1f;
        return transform.position + Vector3.right * direction * Mathf.Min(4f, range);
    }

    private void DamageAt(Vector3 position)
    {
        int dealt = GetComponent<PlayerCombat>()?.SpellDamage(damage) ?? damage;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, impactRadius, enemyLayers.value == 0 ? ~0 : enemyLayers.value);
        HashSet<GameObject> damaged = new HashSet<GameObject>();
        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            BossAI boss = hit.GetComponentInParent<BossAI>();
            GameObject owner = enemy != null ? enemy.gameObject : boss != null ? boss.gameObject : null;
            if (owner == null || !damaged.Add(owner)) continue;
            if (enemy != null) enemy.TakeDamage(dealt, false);
            else if (boss != null) boss.TakeDamage(dealt, false);
        }
    }

}
