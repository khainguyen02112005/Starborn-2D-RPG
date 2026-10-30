using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 10f; // Đạn bay bao nhanh
    public int damage = 1;    // Sát thương 1 cục
    public float lifeTime = 2f; // Sau 2 giây nếu không trúng ai thì đạn tự hủy 

    private float direction = 1;
    private bool spawnedImpact;

    void Start()
    {
        // Hẹn giờ tự hủy 
        Destroy(gameObject, lifeTime);
        if (transform.Find("FireProjectileVFX") == null)
            CombatVFX.AttachProjectileAura(transform, ElementalProjectile.Element.Fire);
    }

    // Hiệp sĩ sẽ truyền hướng mặt vào đây để đạn biết đường bay
    public void Setup(float dir)
    {
        direction = dir;
    }

    void Update()
    {
        // Bay tới trước với vận tốc bàn thờ
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    // Khi đạn va phải vật cản
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Không tự sát (Đụng trúng người chơi thì lờ đi)
        if (hitInfo.CompareTag("Player")) return;

        // Nếu đụng trúng quái vật thì trừ máu nó
        EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
        BossAI boss = hitInfo.GetComponent<BossAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        else if (boss != null)
        {
            boss.TakeDamage(damage);
        }

        // Dù đụng tường hay đụng quái thì đạn cũng nổ và biến mất
        if (enemy != null || boss != null || !hitInfo.isTrigger)
        {
            SpawnImpact(1f);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && !spawnedImpact && gameObject.scene.isLoaded) SpawnImpact(0.65f);
    }

    private void SpawnImpact(float scale)
    {
        if (spawnedImpact) return;
        spawnedImpact = true;
        CombatVFX.SpawnFireImpact(transform.position, scale);
    }
}
