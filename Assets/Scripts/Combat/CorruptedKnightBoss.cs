using System.Collections;
using UnityEngine;

public class CorruptedKnightBoss : MonoBehaviour
{
    public int baseHealth = 120;
    public int contactDamage = 12;
    public float moveSpeed = 3.8f;
    public float attackInterval = .85f;
    public float teleportCooldown = 4.5f;
    public Sprite fireSprite;
    public GameObject blockShield;
    [Header("Kết 2 — choáng khi chịu sát thương dồn")]
    [Min(1)] public int stunDamageThreshold = 30;
    [Min(.1f)] public float stunDamageWindow = 1.6f;
    [Min(.1f)] public float stunDuration = .9f;
    [Min(.1f)] public float stunRecovery = 1.4f;

    private int currentHealth;
    private int maxHealth;
    private CursorBattleController cursor;
    private float nextAttack;
    private bool blocking;
    private bool activeBattle;
    private Level04Controller director;
    private bool learnedLightning;
    private bool learnedFire;
    private bool learnedWater;
    private int spellVolleyIndex;
    private bool mage;
    private bool actionBusy;
    private float nextTeleport;
    private Rigidbody2D battleBody;
    private bool bodyWasSimulated;
    private float hitRecoveryUntil;
    private Animator battleAnimator;
    private AnimatorUpdateMode previousAnimatorMode;
    private int staggerDamage;
    private float staggerWindowRemaining;
    private float stunRemaining;
    private float stunImmunityRemaining;
    private GameObject stunVisual;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsBlocking => blocking;
    public bool IsStunned => activeBattle && stunRemaining > 0f;
    private bool Suspended => CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended;
    private float BattleDelta => Suspended ? 0f : Time.unscaledDeltaTime;

    public void BeginBattle(CursorBattleController controller)
    {
        cursor = controller;
        director = FindAnyObjectByType<Level04Controller>();
        PlayerStats stats = GetComponent<PlayerStats>();
        PlayerLevel level = GetComponent<PlayerLevel>();
        SkillTree tree = GetComponent<SkillTree>();
        mage = PlayerClassController.IsMageSelected;
        spellVolleyIndex = 0;
        learnedLightning = tree != null && tree.HasLightning;
        learnedFire = tree != null && tree.HasFire;
        learnedWater = tree != null && tree.HasWater;
        maxHealth = baseHealth + (stats != null ? stats.strength * 12 + stats.intelligence * 4 : 0) + (level != null ? level.level * 10 : 0);
        currentHealth = maxHealth;
        ClearStun();
        stunImmunityRemaining = 5f;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) contactDamage = combat.BasicDamage;
        if (combat != null) combat.enabled = false;
        ElementalSpellController spells = GetComponent<ElementalSpellController>();
        if (spells != null) spells.enabled = false;
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.enabled = false;
        activeBattle = true;
        battleAnimator = GetComponent<PlayerSPUMAnimation>()?.ActiveAnimator;
        if (battleAnimator != null) { previousAnimatorMode = battleAnimator.updateMode; battleAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; }
        actionBusy = false;
        nextTeleport = Time.unscaledTime + 2.2f;
        battleBody = GetComponent<Rigidbody2D>();
        if (battleBody != null) { bodyWasSimulated = battleBody.simulated; battleBody.linearVelocity = Vector2.zero; battleBody.simulated = false; }
        nextAttack = Time.unscaledTime + 0.8f;
        cursor?.UpdateKnightHealth(currentHealth, maxHealth);
    }

    public void EndBattle()
    {
        activeBattle = false;
        blocking = false;
        actionBusy = false;
        ClearStun();
        if (battleBody != null) battleBody.simulated = bodyWasSimulated;
        if (battleAnimator != null) battleAnimator.updateMode = previousAnimatorMode;
        StopAllCoroutines();
        ElementalProjectile.ClearCaster(transform);
        if (blockShield != null) blockShield.SetActive(false);
        GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
    }

    private void Update()
    {
        if (!activeBattle || cursor == null || Suspended) return;
        TickStagger(Time.unscaledDeltaTime);
        if (IsStunned || actionBusy) return;
        Vector3 target = cursor.CursorWorldPosition;
        float distance = Vector2.Distance(transform.position, target);
        if (distance > 3.5f && Time.unscaledTime >= nextTeleport)
        {
            nextTeleport = Time.unscaledTime + teleportCooldown;
            StartCoroutine(TeleportStrike());
            return;
        }
        if (distance > 1.6f)
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.unscaledDeltaTime);
        if (Time.unscaledTime >= hitRecoveryUntil) GetComponent<PlayerSPUMAnimation>()?.SetMoving(distance > 1.6f);
        Vector3 facingScale = transform.localScale;
        facingScale.x = Mathf.Abs(facingScale.x) * (target.x > transform.position.x ? -1f : 1f);
        transform.localScale = facingScale;
        if (Time.unscaledTime < nextAttack) return;
        nextAttack = Time.unscaledTime + attackInterval;
        int patternCount = 1 + (learnedLightning ? 1 : 0) + (learnedFire ? 1 : 0) + (learnedWater ? 1 : 0);
        int choice = Random.Range(0, patternCount + 1);
        if (choice == 0 && mage) CastPatternAtCursor(ElementalProjectile.Element.Arcane, target, true);
        else if (choice == 0 && distance < 2.2f) StartCoroutine(MeleeAtCursor(target));
        else if (choice == patternCount) StartCoroutine(BlockWindow());
        else CastLearnedSpell(choice - 1, target);
    }

    public void ReceiveCursorDamage(int damage)
    {
        if (!activeBattle || Suspended) return;
        if (blocking)
        {
            CameraShakeManager.instance?.ShakeLight();
            GameAudioManager.Play(GameSound.Block, transform.position, 0.9f, 0.9f);
            return;
        }
        int applied = Mathf.Max(0, damage);
        currentHealth = Mathf.Max(0, currentHealth - applied);
        cursor?.ShowDamageNumber(transform.position, applied, new Color(1f, 0.82f, 0.2f));
        cursor?.UpdateKnightHealth(currentHealth, maxHealth);
        var animation = GetComponent<PlayerSPUMAnimation>();
        if (Time.unscaledTime >= hitRecoveryUntil)
        {
            animation?.SetMoving(false);
            animation?.PlayDamaged();
            hitRecoveryUntil = Time.unscaledTime + .35f;
        }
        cursor?.ShowHeroHitEffect(transform);
        if (currentHealth > 0)
        {
            AddStagger(applied);
            return;
        }
        activeBattle = false;
        ClearStun();
        blocking = false;
        StopAllCoroutines();
        ElementalProjectile.ClearCaster(transform);
        if (blockShield != null) blockShield.SetActive(false);
        GetComponent<PlayerSPUMAnimation>()?.PlayDeath();
        director?.OnKnightDefeated();
    }

    private void AddStagger(int damage)
    {
        if (damage <= 0 || IsStunned || stunImmunityRemaining > 0f) return;
        staggerDamage += damage;
        staggerWindowRemaining = stunDamageWindow;
        if (staggerDamage < stunDamageThreshold) return;
        staggerDamage = 0;
        stunRemaining = stunDuration;
        stunImmunityRemaining = stunDuration + stunRecovery;
        // Interrupt in-flight melee/teleport/block actions, not spells already fired.
        StopAllCoroutines();
        actionBusy = blocking = false;
        if (blockShield != null) blockShield.SetActive(false);
        GetComponent<PlayerSPUMAnimation>()?.SetMoving(false);
        var prefab = Resources.Load<GameObject>("CombatVFX/Ending2/StunStars");
        if (prefab != null)
        {
            stunVisual = Instantiate(prefab, transform);
            stunVisual.transform.localPosition = Vector3.up * 1.25f;
        }
    }

    private void TickStagger(float delta)
    {
        stunImmunityRemaining = Mathf.Max(0f, stunImmunityRemaining - delta);
        staggerWindowRemaining -= delta;
        if (staggerWindowRemaining <= 0f) staggerDamage = 0;
        if (stunRemaining <= 0f) return;
        stunRemaining = Mathf.Max(0f, stunRemaining - delta);
        if (stunRemaining > 0f) return;
        if (stunVisual != null) Destroy(stunVisual);
        nextAttack = Mathf.Max(nextAttack, Time.unscaledTime + .15f);
        nextTeleport = Mathf.Max(nextTeleport, Time.unscaledTime + .35f);
    }

    private void ClearStun()
    {
        staggerDamage = 0;
        staggerWindowRemaining = stunRemaining = stunImmunityRemaining = 0f;
        if (stunVisual != null) Destroy(stunVisual);
    }

    private void CastLearnedSpell(int index, Vector3 target)
    {
        int cursorIndex = 0;
        if (learnedLightning)
        {
            if (index == cursorIndex) { CastPatternAtCursor(ElementalProjectile.Element.Lightning, target); return; }
            cursorIndex++;
        }
        if (learnedFire)
        {
            if (index == cursorIndex) { CastPatternAtCursor(ElementalProjectile.Element.Fire, target); return; }
            cursorIndex++;
        }
        if (learnedWater)
        {
            if (index == cursorIndex) { CastPatternAtCursor(ElementalProjectile.Element.Water, target); return; }
        }
        if (mage) CastPatternAtCursor(ElementalProjectile.Element.Arcane, target, true);
        else StartCoroutine(MeleeAtCursor(target));
    }

    private void CastPatternAtCursor(ElementalProjectile.Element element, Vector3 target, bool basic = false)
    {
        PlayerSPUMAnimation animation = GetComponent<PlayerSPUMAnimation>();
        animation?.PlayMagicAttack();
        ElementalSpellController spells = GetComponent<ElementalSpellController>();
        PlayerStats stats = GetComponent<PlayerStats>();
        var combat = GetComponent<PlayerCombat>();
        int damage = basic ? combat != null ? combat.BasicDamage : 4
            : element == ElementalProjectile.Element.Fire ? spells != null ? spells.fireDamage : 10
            : element == ElementalProjectile.Element.Water ? spells != null ? spells.waterDamage : 8
            : GetComponent<LightningSkill>()?.damage ?? 12;
        if (!basic && combat != null) damage = combat.SpellDamage(damage);
        SpellCastPattern pattern = basic ? SpellCastPattern.Focused : spellVolleyIndex++ % 2 == 0 ? SpellCastPattern.Fan : SpellCastPattern.Orbit;
        Vector3 origin = transform.position + Vector3.up * .5f;
        SpellVolley.Cast(transform, origin, target - origin, element, pattern, basic ? 7f : 5.5f, damage, default, cursor);
        StoryWorldDissolve.Instance?.Strike(Vector3.Lerp(origin, target, .55f), basic ? .8f : 1.25f);
        GameAudioManager.Play(element == ElementalProjectile.Element.Fire ? GameSound.BossFire : element == ElementalProjectile.Element.Water ? GameSound.BossWater : GameSound.BossLightning, origin, .72f, .85f);
    }

    private IEnumerator BlockWindow()
    {
        blocking = true;
        GameAudioManager.Play(GameSound.Block, transform.position, 0.82f, 0.84f);
        if (blockShield != null) blockShield.SetActive(true);
        foreach (CursorProjectile projectile in FindObjectsByType<CursorProjectile>())
        {
            if (Vector2.Distance(projectile.transform.position, transform.position) <= 3f && Random.value < 0.65f)
                projectile.BreakByKnight();
        }
        yield return WaitBattleSeconds(0.72f);
        if (blockShield != null) blockShield.SetActive(false);
        blocking = false;
    }

    private IEnumerator MeleeAtCursor(Vector3 target)
    {
        actionBusy = true;
        GetComponent<PlayerSPUMAnimation>()?.PlayMeleeAttack();
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < 0.23f && activeBattle)
        {
            elapsed += BattleDelta;
            Vector3 aim = cursor != null ? cursor.CursorWorldPosition : target;
            transform.position = Vector3.Lerp(start, aim, Mathf.SmoothStep(0, .84f, elapsed / .23f));
            yield return null;
        }
        StoryWorldDissolve.Instance?.Strike(transform.position, 1.2f);
        CombatVFX.SpawnFireImpact(transform.position + Vector3.up * .35f, .45f);
        if (Vector2.Distance(transform.position, cursor.CursorWorldPosition) < 2.1f)
        {
            var combat = GetComponent<PlayerCombat>();
            int damage = combat != null ? combat.BasicDamage : contactDamage;
            if (combat != null && Random.value < combat.EffectiveCritChance) damage *= Mathf.Clamp(combat.critMultiplier, 1, 2);
            DealCursorDamage(damage, transform.position);
        }
        actionBusy = false;
    }

    private IEnumerator TeleportStrike()
    {
        actionBusy = true;
        Vector3 from = transform.position;
        CinematicPresentationController.Instance?.PlayPixelFragments(from + Vector3.up * .5f, .4f, .6f);
        GameAudioManager.Play(GameSound.Portal, from, .7f, 1.25f);
        yield return WaitBattleSeconds(.22f);
        if (!activeBattle || cursor == null) { actionBusy = false; yield break; }
        Vector3 target = cursor.CursorWorldPosition;
        Vector3 arrival = target + (Vector3)(Random.insideUnitCircle.normalized * 1.7f);
        MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
        if (layout != null) arrival = layout.ResolveSafePosition(arrival, target);
        transform.position = arrival;
        CinematicPresentationController.Instance?.PlayPixelFragments(arrival + Vector3.up * .5f, .35f, .6f);
        StoryWorldDissolve.Instance?.Strike(from, .7f, false);
        yield return WaitBattleSeconds(.16f);
        yield return MeleeAtCursor(target);
        nextAttack = Time.unscaledTime + attackInterval * .6f;
        actionBusy = false;
    }


    private void DealCursorDamage(int amount, Vector3 at)
    {
        if (!activeBattle || Suspended) return;
        cursor?.TakeDamage(amount);
        GameAudioManager.Play(GameSound.CursorHit, at, 0.9f, 0.82f);
        CameraShakeManager.instance?.ShakeHeavy();
    }

    private IEnumerator WaitBattleSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds && activeBattle) { elapsed += BattleDelta; yield return null; }
    }
}
