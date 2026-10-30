using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSPUMAnimation : MonoBehaviour
{
    [Header("SPUM References")]
    [SerializeField] private SPUM_Prefabs spumPrefab;
    [SerializeField] private Animator animator;

    [Header("Knight Animation Indices")]
    [SerializeField, Min(0)] private int idleIndex;
    [SerializeField, Min(0)] private int moveIndex;
    [SerializeField, Min(0)] private int meleeAttackIndex;
    [SerializeField, Min(0)] private int magicAttackIndex = 4;

    private bool initialized;
    private bool isMoving;
    private bool isDead;
    private float nextFootstepTime;

    public bool IsDead => isDead;
    public bool IsMoving => isMoving && !isDead;
    public Animator ActiveAnimator => animator;

    private void Update()
    {
        if (animator != null && !isDead) animator.SetBool("1_Move", isMoving);
        if (!isMoving || isDead || Time.time < nextFootstepTime) return;
        GameAudioManager.PlayFootstep(transform.position);
        nextFootstepTime = Time.time + 0.31f;
    }

    private void Awake()
    {
        Initialize();
    }

    public void SetMoving(bool moving)
    {
        if (isDead) return;
        Initialize();
        bool changed = moving != isMoving;
        isMoving = moving;
        if (changed) Play(moving ? PlayerState.MOVE : PlayerState.IDLE, moving ? moveIndex : idleIndex);
        else if (animator != null) animator.SetBool("1_Move", moving);
    }

    public void PlayMeleeAttack()
    {
        if (!isDead)
        {
            Play(PlayerState.ATTACK, meleeAttackIndex);
            GameAudioManager.Play(GameSound.SwordSwing, transform.position, 0.88f, Random.Range(0.94f, 1.07f));
        }
    }

    public void PlayMagicAttack()
    {
        if (!isDead)
        {
            Play(PlayerState.ATTACK, magicAttackIndex);
            GameAudioManager.Play(GameSound.MageAttack, transform.position, 0.72f, Random.Range(0.94f, 1.08f));
        }
    }

    public void PlayDamaged()
    {
        if (!isDead)
        {
            Play(PlayerState.DAMAGED, 0);
            bool corrupted = GetComponent<CorruptedKnightBoss>() != null;
            GameAudioManager.Play(corrupted ? GameSound.BossHit : GameSound.PlayerHit,
                transform.position, 0.82f, Random.Range(0.9f, 1.04f));
        }
    }

    public void PlayDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isMoving = false;
        Play(PlayerState.DEATH, 0);
        bool corrupted = GetComponent<CorruptedKnightBoss>() != null;
        GameAudioManager.Play(corrupted ? GameSound.BossDeath : GameSound.PlayerDeath,
            transform.position, 1f, corrupted ? 0.72f : 0.9f);
    }

    public void Revive()
    {
        isDead = false;
        isMoving = false;
        GameAudioManager.Play(GameSound.Revive, transform.position, 0.9f, 1.02f);
        if (animator != null)
        {
            // Death clips leave poses on bones which are not keyed by every idle clip.
            // Reset the rig as well as the isDeath flag before returning to a story scene.
            animator.Rebind();
            animator.Update(0f);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(parameter.nameHash);
            animator.SetBool("isDeath", false);
        }
        Play(PlayerState.IDLE, idleIndex);
    }

    public void SetAvatar(SPUM_Prefabs avatar)
    {
        if (avatar == null) return;
        if (spumPrefab == avatar && initialized)
        {
            if (animator != null) animator.SetBool("1_Move", isMoving && !isDead);
            return;
        }

        spumPrefab = avatar;
        animator = avatar._anim != null ? avatar._anim : avatar.GetComponentInChildren<Animator>(true);
        initialized = false;
        Initialize();
        Play(isDead ? PlayerState.DEATH : isMoving ? PlayerState.MOVE : PlayerState.IDLE, isMoving ? moveIndex : idleIndex);
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (spumPrefab == null)
        {
            spumPrefab = GetComponent<SPUM_Prefabs>();
        }

        if (animator == null && spumPrefab != null)
        {
            animator = spumPrefab._anim;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (spumPrefab != null)
        {
            if (spumPrefab._anim == null)
            {
                spumPrefab._anim = animator;
            }

            if (animator != null && animator.runtimeAnimatorController is AnimatorOverrideController existingOverride)
            {
                spumPrefab.OverrideController = existingOverride;
                RebuildStatePairs();
            }
            else
            {
                spumPrefab.OverrideControllerInit();
            }
        }

        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        initialized = true;
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

        if (spumPrefab != null && spumPrefab.StateAnimationPairs.TryGetValue(state.ToString(), out var clips) && clips.Count > 0)
        {
            int safeIndex = Mathf.Clamp(requestedIndex, 0, clips.Count - 1);
            spumPrefab.PlayAnimation(state, safeIndex);
            if (animator != null && state != PlayerState.DEATH) animator.SetBool("1_Move", isMoving);
            return;
        }

        PlayAnimatorFallback(state);
    }

    private void PlayAnimatorFallback(PlayerState state)
    {
        if (animator == null)
        {
            return;
        }

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

        if (!string.IsNullOrEmpty(trigger))
        {
            animator.SetTrigger(trigger);
        }
    }
}
