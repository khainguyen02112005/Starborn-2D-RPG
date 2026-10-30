using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int experienceReward = 5;
    private int currentHealth;
    
    // Đồ rớt ra khi chết
    public GameObject deathEffect;
    public GameObject itemDropPrefab; // Chút ra Unity bạn có thể xóa trống ô này đi vì xài hệ thống rớt đồ mới rồi
    public GameObject coinDropPrefab;

    // THÔNG SỐ ĐẨY LÙI
    private Rigidbody2D rb;
    public float knockbackForce = 7f;

    // =====================================
    // QUẢN LÝ HIỆU ỨNG TRẠNG THÁI
    private SpriteRenderer[] allRenderers; // Quản lý hình dáng để đổi màu
    private bool isPoisoned = false;
    private bool isFrozen = false;
    private bool isDead = false;
    private EnemySPUMAnimation animationDriver;
    private bool healthInitialized;
    public int CurrentHealth => healthInitialized ? currentHealth : maxHealth;
    public bool IsDead => isDead;
    public EnemyRespawnPoint RespawnPoint { get; set; }
    private float knockbackUntil;
    public bool IsMovementSuppressed => isDead || isFrozen || Time.time < knockbackUntil;
    public void RestoreHealthState(int amount) { currentHealth = Mathf.Clamp(amount, 1, maxHealth); healthInitialized = true; }
    // =====================================

    void Start()
    {
        if (!healthInitialized) currentHealth = maxHealth;
        healthInitialized = true;
        rb = GetComponent<Rigidbody2D>();
        animationDriver = GetComponent<EnemySPUMAnimation>();
        if (animationDriver == null) animationDriver = gameObject.AddComponent<EnemySPUMAnimation>();
        
        // Tìm tất cả các mảnh ghép cơ thể của quái để lát nữa nhúng vào thùng sơn
        allRenderers = GetComponentsInChildren<SpriteRenderer>(); 
    }

    public void TakeDamage(int damage, bool isCrit = false)
    {
        if (isDead) return;
        if (!healthInitialized) { currentHealth = maxHealth; healthInitialized = true; }
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));

        DamagePopup.Show(transform.position + new Vector3(Random.Range(-.25f, .25f), 1.05f, 0f), damage, isCrit);

        if (rb != null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Vector2 knockbackDirection = (transform.position - playerObject.transform.position).normalized;
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                knockbackUntil = Time.time + .2f;
                StartCoroutine(StopKnockback());
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animationDriver?.PlayDamaged();
        }
    }

    IEnumerator StopKnockback()
    {
        yield return new WaitForSeconds(0.2f); 
        // Do not toggle AI.enabled: that flag may belong to the cutscene, not this hit.
        if (rb != null && Time.time >= knockbackUntil) rb.linearVelocity = Vector2.zero;
    }

    // =====================================
    // KỸ NĂNG: TRÚNG ĐỘC (Mất máu từ từ)
    public void ApplyPoison(int tickDamage, float duration)
    {
        if (isPoisoned == false) StartCoroutine(PoisonRoutine(tickDamage, duration));
    }

    IEnumerator PoisonRoutine(int tickDamage, float duration)
    {
        isPoisoned = true;
        ChangeColor(Color.green); // Biến con quái thành màu Xanh Lá

        float timePassed = 0;
        while (timePassed < duration)
        {
            yield return new WaitForSeconds(1f); // Cứ 1 giây
            TakeDamage(tickDamage);              // Tự động rút máu 1 lần
            timePassed += 1f;
        }

        ChangeColor(Color.white); // Hết độc, tẩy màu đi
        isPoisoned = false;
    }

    // =====================================
    // KỸ NĂNG: ĐÓNG BĂNG (Bất động)
    public void ApplyFreeze(float duration)
    {
        if (isFrozen == false) StartCoroutine(FreezeRoutine(duration));
    }

    IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        ChangeColor(Color.cyan); // Biến thành màu Xanh Ngọc

        if (rb != null) rb.linearVelocity = Vector2.zero; // Phanh lại ngay lập tức

        yield return new WaitForSeconds(duration); // Đứng im chờ hết thời gian

        ChangeColor(Color.white);
        isFrozen = false;
    }

    // Hàm tiện ích: Nhuộm màu toàn bộ cơ thể
    void ChangeColor(Color newColor)
    {
        foreach (SpriteRenderer sr in allRenderers)
        {
            sr.color = newColor;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        RespawnPoint?.NotifyDeath(this);
        StopAllCoroutines();

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
        float deathDelay = animationDriver != null ? animationDriver.PlayDeath() : 0f;
        StartCoroutine(FinishDeathAfterAnimation(deathDelay));
    }

    private IEnumerator FinishDeathAfterAnimation(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // 1. Chạy hiệu ứng chết và rớt tiền như cũ
        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);
        GameObject animatedCoin = Resources.Load<GameObject>("CombatVFX/CoinPickup");
        if ((animatedCoin != null || coinDropPrefab != null) &&
            EnemyLoot.TryResolveDropPosition(transform.position + Vector3.right * .25f, out Vector3 coinPosition))
            Instantiate(animatedCoin != null ? animatedCoin : coinDropPrefab, coinPosition, Quaternion.identity);
        
        // 2. Tính nhiệm vụ & Kinh nghiệm
        if (QuestManager.instance != null) QuestManager.instance.AddKill();
        
        ExperiencePickup.Spawn(transform.position + Vector3.left * .2f, experienceReward);

        // ===============================================
        // 3. KÍCH HOẠT RỚT ĐỒ VIP (KIẾM, GIÁP, MÁU)
        // ===============================================
        EnemyLoot lootSystem = GetComponent<EnemyLoot>();
        
        if (lootSystem != null) 
        {
            // Nếu con quái có gắn script EnemyLoot -> Gọi rớt đồ, sau đó script đó sẽ tự xóa con quái
            lootSystem.DieAndDropLoot(); 
        }
        else 
        {
            // Lỡ bạn quên gắn script EnemyLoot thì quái vẫn bị xóa đi bình thường
            Destroy(gameObject);
        }
    }
}
