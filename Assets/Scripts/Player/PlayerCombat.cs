#pragma warning disable CS0618

using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;
    public float attackRange = 0.8f;
    public LayerMask enemyLayers;
    public GameObject fireballPrefab;
    public GameObject damagePopupPrefab;

    public float critChance = 0.2f;
    public int critMultiplier = 2;
    public float freezeChance = 0.15f;
    public float poisonChance = 0.15f;
    public bool isBlocking;

    [Header("Đòn đánh thường theo lớp")]
    [Min(1)] public int knightBaseDamage = 3;
    [Min(1)] public int mageBaseDamage = 4;
    [Min(0f)] public float knightAttackStaminaCost = 12f;
    [Min(0f)] public float mageAttackStaminaCost = 10f;
    [Min(0.1f)] public float mageProjectileSpeed = 11f;
    [Min(0f)] public float basicAttackCooldown = 0.35f;

    [Header("Combo kiếm")]
    public float comboResetTime = 1f;

    private int comboStep;
    private float lastAttackTime;
    private float nextAttackTime;
    private PlayerHealth playerHealth;
    private PlayerSPUMAnimation spumAnimation;
    private PlayerStamina stamina;
    private PlayerStats playerStats;
    private InventorySlot[] equipmentSlots;
    private Transform equipmentPanel;

    public bool IsMage => PlayerClassController.IsMageSelected;
    public float EffectiveCritChance => Mathf.Clamp(critChance + Mathf.Max(0, (playerStats != null ? playerStats.strength : 1) - 1) * .01f, 0f, .35f);
    public int BasicDamage
    {
        get
        {
            var weapon = FindEquippedWeapon(IsMage ? WeaponKind.Staff : WeaponKind.Sword);
            int baseline = IsMage ? mageBaseDamage : knightBaseDamage;
            int stat = playerStats != null ? (IsMage ? playerStats.intelligence : playerStats.strength) : 1;
            return Mathf.Max(baseline, weapon != null && weapon.currentDurability > 0 ? weapon.itemData.DamageAtUpgrade(weapon.upgradeLevel) : baseline)
                + Mathf.FloorToInt(Mathf.Sqrt(Mathf.Max(0, stat - 1)));
        }
    }
    public int SpellDamage(int baseline) => Mathf.Max(1, baseline) + Mathf.FloorToInt(Mathf.Sqrt(Mathf.Max(0, (playerStats != null ? playerStats.intelligence : 1) - 1)));

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        spumAnimation = GetComponent<PlayerSPUMAnimation>();
        stamina = GetComponent<PlayerStamina>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (GameplayUiFocus.BlocksControls) { isBlocking = false; return; }
        if (playerHealth != null && playerHealth.IsDead) return;

        if (Input.GetKeyDown(KeyCode.J)) TryAttack();

        if (Input.GetKey(KeyCode.K))
        {
            if (stamina != null && stamina.currentStamina > 0f)
            {
                isBlocking = true;
                stamina.currentStamina = Mathf.Max(0f,
                    stamina.currentStamina - (stamina.staminaRegenRate + 15f) * Time.deltaTime);
            }
            else
            {
                isBlocking = false;
            }
        }
        else
        {
            isBlocking = false;
        }
    }

    public bool TryAttack()
    {
        if (!enabled || GameplayUiFocus.BlocksControls || playerHealth != null && playerHealth.IsDead) return false;
        if (Time.time < nextAttackTime) return false;
        if (attackPoint == null)
        {
            Debug.LogWarning("PlayerCombat is missing its attack point.", this);
            return false;
        }

        return IsMage ? CastMageBasicAttack() : SwingSword();
    }

    private bool SwingSword()
    {
        InventorySlot equippedWeapon = FindEquippedWeapon(WeaponKind.Sword);
        if (!RequireUsableWeapon()) return false;

        if (stamina != null && !stamina.UseStamina(knightAttackStaminaCost))
        {
            Debug.Log("Không đủ thể lực để vung kiếm.", this);
            return false;
        }

        int baseDamage = BasicDamage;
        equippedWeapon.currentDurability = Mathf.Max(0, equippedWeapon.currentDurability - 1);
        equippedWeapon.UpdateAmountUI();
        nextAttackTime = Time.time + basicAttackCooldown;
        spumAnimation?.PlayMeleeAttack();

        if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;
        comboStep = comboStep % 3 + 1;
        lastAttackTime = Time.time;

        float facingDirection = FacingDirection();
        CombatVFX.SpawnSwordSlash(attackPoint.position, facingDirection, comboStep);

        float currentRange = attackRange;
        float currentCritChance = EffectiveCritChance;
        if (comboStep == 3)
        {
            baseDamage = Mathf.CeilToInt(baseDamage * 1.25f);
            currentRange *= 1.5f;
            currentCritChance = Mathf.Min(.5f, currentCritChance + .15f);
        }

        Collider2D[] hitEnemies = enemyLayers.value == 0
            ? Physics2D.OverlapCircleAll(attackPoint.position, currentRange)
            : Physics2D.OverlapCircleAll(attackPoint.position, currentRange, enemyLayers);
        HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
        HashSet<BossAI> damagedBosses = new HashSet<BossAI>();
        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.transform.IsChildOf(transform) || enemy.transform == transform) continue;
            EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
            BossAI boss = enemy.GetComponentInParent<BossAI>();
            if (enemyHealth == null && boss == null) continue;
            Transform target = enemyHealth != null ? enemyHealth.transform : boss.transform;
            if ((target.position.x - transform.position.x) * facingDirection <= .01f) continue;
            if (enemyHealth != null && !damagedEnemies.Add(enemyHealth)) continue;
            if (enemyHealth == null && boss != null && !damagedBosses.Add(boss)) continue;

            int finalDamage = baseDamage;
            bool critical = Random.value <= currentCritChance;
            if (critical) finalDamage *= Mathf.Clamp(critMultiplier, 1, 2);

            if (critical) CameraShakeManager.instance?.ShakeHeavy();
            else CameraShakeManager.instance?.ShakeLight();
            CombatVFX.SpawnHitSpark(enemy.transform.position + Vector3.up * 0.45f, critical);

            if (enemyHealth != null)
            {
                if (Random.value <= freezeChance) enemyHealth.ApplyFreeze(2f);
                if (Random.value <= poisonChance) enemyHealth.ApplyPoison(1, 4f);
                enemyHealth.TakeDamage(finalDamage, critical);
            }
            else
            {
                boss.TakeDamage(finalDamage, critical);
            }
        }

        return true;
    }

    private bool CastMageBasicAttack()
    {
        if (!RequireUsableWeapon()) return false;
        InventorySlot equippedStaff = FindEquippedWeapon(WeaponKind.Staff);

        if (stamina != null && !stamina.UseStamina(mageAttackStaminaCost))
        {
            Debug.Log("Không đủ thể lực để bắn ma đạn.", this);
            return false;
        }

        int damage = BasicDamage;
        float direction = FacingDirection();

        equippedStaff.currentDurability = Mathf.Max(0, equippedStaff.currentDurability - 1);
        equippedStaff.UpdateAmountUI();

        GameObject projectile = new GameObject("MageBasicProjectile");
        projectile.transform.position = attackPoint.position;
        projectile.transform.localScale = Vector3.one * 0.72f;
        ElementalProjectile shot = projectile.AddComponent<ElementalProjectile>();
        shot.Initialize(ElementalProjectile.Element.Arcane, new Vector2(direction, 0f),
            mageProjectileSpeed, damage, enemyLayers);

        spumAnimation?.PlayMagicAttack();
        nextAttackTime = Time.time + basicAttackCooldown;
        return true;
    }

    public bool HasUsableStaff
    {
        get { var staff = FindEquippedWeapon(WeaponKind.Staff); return staff != null && staff.currentDurability > 0; }
    }

    public bool RequireUsableStaff()
    {
        return !IsMage || RequireUsableWeapon();
    }

    public bool RequireUsableWeapon()
    {
        var weapon = FindEquippedWeapon(IsMage ? WeaponKind.Staff : WeaponKind.Sword);
        if (weapon != null && weapon.currentDurability > 0) return true;
        GetComponent<ElementalSpellController>()?.ShowFeedback(weapon != null
            ? "Vũ khí đã hỏng — hãy sửa hoặc thay vũ khí trong sách (B)."
            : "Hãy trang bị " + (IsMage ? "gậy phép" : "kiếm") + " vào ô Vũ khí trong sách (B).");
        GameAudioManager.PlayUI(GameSound.UIError, .45f);
        return false;
    }

    public InventorySlot FindEquippedWeapon(WeaponKind requiredKind)
    {
        PlayerInventory inventory = GetComponent<PlayerInventory>();
        Transform panel = inventory != null && inventory.characterPanel != null ? inventory.characterPanel.transform : null;
        if (panel == null) return null;
        if (equipmentSlots == null || equipmentPanel != panel)
        {
            equipmentPanel = panel;
            equipmentSlots = panel.GetComponentsInChildren<InventorySlot>(true);
        }
        foreach (InventorySlot slot in equipmentSlots)
        {
            if (slot != null && slot.transform.IsChildOf(panel) && !slot.isChestSlot
                && slot.equipSlotType == 1 && slot.isFull && slot.itemAmount > 0 && slot.itemData != null
                && slot.itemData.itemType == 1
                && slot.itemData.weaponKind == requiredKind)
                return slot;
        }
        return null;
    }

    private float FacingDirection() => transform.localScale.x < 0f ? 1f : -1f;

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange * 1.5f);
    }
}
