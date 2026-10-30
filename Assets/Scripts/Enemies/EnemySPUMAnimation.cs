using UnityEngine;

[DisallowMultipleComponent]
public class EnemySPUMAnimation : MonoBehaviour
{
    [Header("SPUM References")]
    [SerializeField] private SPUM_Prefabs spumPrefab;
    [SerializeField] private Animator animator;

    [Header("Animation Choices")]
    [SerializeField, Min(0)] private int idleIndex;
    [SerializeField, Min(0)] private int moveIndex;
    [SerializeField, Min(0)] private int firstPhysicalAttackIndex;
    [SerializeField, Min(0)] private int lastPhysicalAttackIndex = 1;
    [SerializeField, Min(0)] private int magicAttackIndex = 4;

    private bool initialized;
    private bool moving;
    private bool dead;
    private float nextFootstepTime;

    public bool IsDead => dead;

    private void Update()
    {
        if (animator != null && !dead) animator.SetBool("1_Move", moving);
        if (!moving || dead || Time.time < nextFootstepTime) return;
        GameAudioManager.PlayFootstep(transform.position, false, 0.48f, true);
        nextFootstepTime = Time.time + 0.4f;
    }

    private void Awake()
    {
        Initialize();
    }

    public void SetMoving(bool value)
    {
        if (dead) return;
        Initialize();
        bool changed = moving != value;
        moving = value;
        if (changed) Play(value ? PlayerState.MOVE : PlayerState.IDLE, value ? moveIndex : idleIndex);
        else if (animator != null) animator.SetBool("1_Move", value);
    }

    public void PlayPhysicalAttack()
    {
        if (dead) return;
        int index = Random.Range(firstPhysicalAttackIndex, lastPhysicalAttackIndex + 1);
        Play(PlayerState.ATTACK, index);
        GameAudioManager.Play(GameSound.EnemyAttack, transform.position, 0.75f, Random.Range(0.86f, 1.12f));
    }

    public void PlayMagicAttack()
    {
        if (dead) return;
        Play(PlayerState.ATTACK, magicAttackIndex);
        bool boss = GetComponent<BossAI>() != null;
        GameAudioManager.Play(boss ? GameSound.BossCast : GameSound.EnemyAttack,
            transform.position, boss ? 0.8f : 0.66f, boss ? 0.86f : 1.04f);
    }

    public void PlayDamaged()
    {
        if (dead) return;
        Play(PlayerState.DAMAGED, 0);
        bool boss = GetComponent<BossAI>() != null;
        GameAudioManager.Play(boss ? GameSound.BossHit : GameSound.EnemyHit,
            transform.position, boss ? 0.82f : 0.68f, Random.Range(0.88f, 1.08f));
    }

    public float PlayDeath()
    {
        if (dead) return 0f;
        dead = true;
        moving = false;
        Play(PlayerState.DEATH, 0);
        bool boss = GetComponent<BossAI>() != null;
        GameAudioManager.Play(boss ? GameSound.BossDeath : GameSound.EnemyDeath,
            transform.position, boss ? 1f : 0.86f, boss ? 0.68f : Random.Range(0.82f, 1.03f));
        return GetClipLength(PlayerState.DEATH, 0, 0.7f);
    }

    private void Initialize()
    {
        if (initialized) return;

        if (spumPrefab == null) spumPrefab = GetComponentInChildren<SPUM_Prefabs>(true);
        if (animator == null && spumPrefab != null) animator = spumPrefab._anim;
        if (animator == null) animator = GetComponentInChildren<Animator>(true);

        if (spumPrefab != null && animator != null)
        {
            if (spumPrefab._anim == null) spumPrefab._anim = animator;
            InitializeSpumController();
        }

        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        initialized = true;
    }

    private void InitializeSpumController()
    {
        // Runtime clones (the opening-cutscene monster in particular) inherit the
        // source object's AnimatorOverrideController. SPUM must reuse that controller;
        // wrapping an override inside another override is invalid and can pause Unity
        // immediately when "Error Pause" is enabled.
        if (animator.runtimeAnimatorController is AnimatorOverrideController existingOverride)
        {
            spumPrefab.OverrideController = existingOverride;
            RebuildStatePairs();
            return;
        }

        spumPrefab.OverrideControllerInit();
    }

    private void RebuildStatePairs()
    {
        if (spumPrefab.StateAnimationPairs == null)
            spumPrefab.StateAnimationPairs = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AnimationClip>>();

        spumPrefab.StateAnimationPairs[PlayerState.IDLE.ToString()] = spumPrefab.IDLE_List;
        spumPrefab.StateAnimationPairs[PlayerState.MOVE.ToString()] = spumPrefab.MOVE_List;
        spumPrefab.StateAnimationPairs[PlayerState.ATTACK.ToString()] = spumPrefab.ATTACK_List;
        spumPrefab.StateAnimationPairs[PlayerState.DAMAGED.ToString()] = spumPrefab.DAMAGED_List;
        spumPrefab.StateAnimationPairs[PlayerState.DEBUFF.ToString()] = spumPrefab.DEBUFF_List;
        spumPrefab.StateAnimationPairs[PlayerState.DEATH.ToString()] = spumPrefab.DEATH_List;
        spumPrefab.StateAnimationPairs[PlayerState.OTHER.ToString()] = spumPrefab.OTHER_List;
    }

    private void Play(PlayerState state, int requestedIndex)
    {
        Initialize();
        if (spumPrefab != null &&
            spumPrefab.StateAnimationPairs.TryGetValue(state.ToString(), out var clips) &&
            clips != null && clips.Count > 0)
        {
            int safeIndex = Mathf.Clamp(requestedIndex, 0, clips.Count - 1);
            if (clips[safeIndex] != null)
            {
                spumPrefab.PlayAnimation(state, safeIndex);
                if (animator != null && state != PlayerState.DEATH) animator.SetBool("1_Move", moving);
                return;
            }
        }

        PlayAnimatorFallback(state);
    }

    private float GetClipLength(PlayerState state, int index, float fallback)
    {
        Initialize();
        if (spumPrefab != null &&
            spumPrefab.StateAnimationPairs.TryGetValue(state.ToString(), out var clips) &&
            clips != null && clips.Count > 0)
        {
            AnimationClip clip = clips[Mathf.Clamp(index, 0, clips.Count - 1)];
            if (clip != null) return Mathf.Clamp(clip.length, 0.45f, 1.5f);
        }
        return fallback;
    }

    private void PlayAnimatorFallback(PlayerState state)
    {
        if (animator == null) return;
        animator.SetBool("1_Move", state == PlayerState.MOVE);
        animator.SetBool("5_Debuff", state == PlayerState.DEBUFF);
        animator.SetBool("isDeath", state == PlayerState.DEATH);

        string trigger = state switch
        {
            PlayerState.ATTACK => "2_Attack",
            PlayerState.DAMAGED => "3_Damaged",
            PlayerState.DEATH => "4_Death",
            PlayerState.OTHER => "6_Other",
            _ => string.Empty
        };
        if (!string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }
}
