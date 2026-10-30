using System.Collections;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Chỉ số Boss")]
    public string bossName = "Chúa Tể Hắc Ám";
    public int maxHealth = 500;
    private int currentHealth;
    private bool isPhase2 = false;

    [Header("Kỹ năng & Triệu Hồi")]
    public GameObject aoeAttackPrefab; // Kéo Prefab AOE_Warning vào đây
    public GameObject minionPrefab;    // Kéo Prefab quái nhỏ (đệ tử) vào đây
    public Transform[] spawnPoints;    // Tạo vài Game Object rỗng xung quanh Boss để làm điểm sinh đệ tử

    [Header("Trạng thái chiến đấu")]
    public SpriteRenderer spriteRenderer;
    public float attackCooldown = 4f; // Cứ 4 giây xả chiêu 1 lần
    private float nextAttackTime;

    private bool hasTriggeredBoss = false; // Đánh dấu xem người chơi đã gặp Boss chưa
    private Transform player;
    private BossEncounterController encounter;
    private ElectricBossController spellController;
    private EnemySPUMAnimation animationDriver;
    private bool dying;

    public bool IsEncounterActive => hasTriggeredBoss;
    public int CurrentHealth => currentHealth;
    public bool IsDying => dying;
    public int experienceReward = 60;

    public void RestoreEncounterState(int health, bool active)
    {
        currentHealth = Mathf.Clamp(health, 1, maxHealth);
        if (!active) return;
        GetComponent<BossEncounterController>()?.RestoreCompletedEncounter();
        ActivateFromCutscene();
        if (currentHealth <= maxHealth / 2 && !isPhase2) EnterPhase2(false);
        BossHealthBar.instance?.UpdateHealth(currentHealth);
    }

    void Start()
    {
        currentHealth = maxHealth;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
        encounter = GetComponent<BossEncounterController>();
        spellController = GetComponent<ElectricBossController>();
        animationDriver = GetComponent<EnemySPUMAnimation>();
        if (animationDriver == null) animationDriver = gameObject.AddComponent<EnemySPUMAnimation>();
        spellController?.DeactivateEncounter();
        // Bỏ lệnh hiện thanh máu ở đây để thanh máu không tự hiện từ đầu
    }

    // Hàm gọi khi Player chém trúng Boss
    public void TakeDamage(int damage, bool isCrit = false)
    {
        if (dying) return;
        if (!hasTriggeredBoss)
        {
            if (encounter != null) encounter.RequestEncounter();
            else ActivateFromCutscene();
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        DamagePopup.Show(transform.position + new Vector3(Random.Range(-.4f, .4f), 1.7f, 0f), damage, isCrit);
        Debug.Log("⚔️ Boss vừa bị chém mất " + damage + " máu! Máu còn lại: " + currentHealth + "/" + maxHealth);
        
        // Cập nhật UI
        if (BossHealthBar.instance != null)
        {
            BossHealthBar.instance.UpdateHealth(currentHealth);
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy UI Thanh Máu Boss! (BossHealthBar.instance đang NULL)");
        }

        // ĐỔI PHA KHI MÁU XUỐNG DƯỚI 50%
        if (currentHealth > 0 && currentHealth <= maxHealth / 2 && isPhase2 == false)
        {
            EnterPhase2();
        }
        
        if (currentHealth <= 0) Die();
        else animationDriver?.PlayDamaged();
    }

    void Update()
    {
        if (dying) return;
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        if (player == null) return;

        if (!hasTriggeredBoss) return;

        // ElectricBossController là bộ não phép chính. Không chạy thêm một vòng AOE song song.
        if (spellController != null && spellController.enabled) return;

        // Tự động tung chiêu vòng đỏ (AOE) dưới chân người chơi
        if (Time.time >= nextAttackTime)
        {
            CastAOEAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    public void ActivateFromCutscene()
    {
        if (hasTriggeredBoss) return;
        hasTriggeredBoss = true;
        nextAttackTime = Time.time + 1f;
        spellController?.ActivateEncounter();
        GameAudioManager.Play(GameSound.BossRoar, transform.position, 1f, 0.72f);
        
        // Hiện thanh máu Boss lên đỉnh màn hình
        if (BossHealthBar.instance != null)
        {
            BossHealthBar.instance.ShowBossHealth(bossName, maxHealth);
        }
        animationDriver?.SetMoving(false);
    }

    void EnterPhase2(bool spawnMinions = true)
    {
        isPhase2 = true;
        Debug.Log("🔥 BOSS NỔI ĐIÊN! TIẾN VÀO PHASE 2!");

        // 1. Đổi màu toàn bộ cơ thể Boss thành Đỏ rực (Hỗ trợ nhân vật SPUM)
        SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in allSprites)
        {
            sr.color = Color.red;
        }

        // 2. Kích thước to ra thêm chút nữa để thị uy
        transform.localScale *= 1.2f;

        // 3. Giảm thời gian hồi chiêu (Xả kỹ năng nhanh gấp đôi)
        attackCooldown = 2f; 
        if (spellController != null) spellController.EnterPhaseTwo();
        animationDriver?.PlayMagicAttack();
        GameAudioManager.Play(GameSound.BossRoar, transform.position, 1f, 0.62f);

        // 4. Triệu hồi đệ tử!
        if (spawnMinions) SpawnMinions();
    }

    void SpawnMinions()
    {
        // Tự động gọi ra 3 con đệ tử xung quanh Boss (Không cần tốn công tạo Spawn Points)
        for (int i = 0; i < 3; i++)
        {
            if (minionPrefab != null)
            {
                float ranX = Random.Range(-2f, 2f);
                float ranY = Random.Range(-2f, 2f);
                Vector3 spawnPos = transform.position + new Vector3(ranX, ranY, 0);
                Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    void CastAOEAttack()
    {
        if (player != null && aoeAttackPrefab != null)
        {
            animationDriver?.PlayMagicAttack();
            // Sinh vùng cảnh báo ngay dưới chân Player
            GameObject attack = Instantiate(aoeAttackPrefab, player.position, Quaternion.identity);
            attack.AddComponent<OwnedAttackLifetime>().Initialize(this, 5f);
        }
    }

    void Die()
    {
        if (dying) return;
        dying = true;
        Debug.Log("🎉 Boss đã bị tiêu diệt!");
        spellController?.DeactivateEncounter();
        OwnedAttackLifetime.Clear(this);
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
        float deathDelay = animationDriver != null ? animationDriver.PlayDeath() : 0f;
        StartCoroutine(FinishDeath(deathDelay));
    }

    private IEnumerator FinishDeath(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (encounter != null) yield return encounter.PlayDefeatSequence();
        ExperiencePickup.Spawn(transform.position, experienceReward);
        if (QuestManager.instance != null) QuestManager.instance.NotifyBossDefeated();
        else
        {
            PlayerPrefs.SetInt("RPG.BossDefeated", 1);
            PlayerPrefs.SetInt("RPG.QuestStage", (int)StoryQuestStage.BossReadyToTurnIn);
            PlayerPrefs.Save();
        }
        Destroy(gameObject);
    }
}
