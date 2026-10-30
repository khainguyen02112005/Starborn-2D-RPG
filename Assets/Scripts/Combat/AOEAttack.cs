using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class AOEAttack : MonoBehaviour
{
    [Header("Cài đặt AOE")]
    public float warningDuration = 1.5f; // Thời gian nhấp nháy cảnh báo (1.5 giây)
    public int damage = 10;
    
    private SpriteRenderer sr;
    private CircleCollider2D col;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        
        if (sr != null) sr.sortingOrder = 10; // Cố tình đè lên lớp cỏ nền để dễ nhìn thấy
        
        if (sr == null || col == null)
        {
            Debug.LogError("AOEAttack requires a SpriteRenderer and CircleCollider2D.", this);
            Destroy(gameObject);
            return;
        }

        col.enabled = false; // Tắt va chạm (chưa gây sát thương lúc đang cảnh báo)
        GameAudioManager.Play(GameSound.AOEWarning, transform.position, 0.58f, 0.92f);
        StartCoroutine(TelegraphAndAttack());
    }

    IEnumerator TelegraphAndAttack()
    {
        // 1. Giai đoạn Cảnh Báo: Nhấp nháy màu đỏ đậm dần lên
        float elapsed = 0f;
        Color color = sr.color;
        
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0.2f, 0.8f, elapsed / warningDuration); // Đậm dần
            sr.color = color;
            yield return null; // Đợi khung hình tiếp theo
        }

        // 2. Giai đoạn Sát thương nổ ra
        color.a = 1f; // Chuyển sang đỏ rực
        sr.color = color;
        col.enabled = true; // Kích hoạt Collider để gây sát thương
        GameAudioManager.Play(GameSound.AOEImpact, transform.position, 0.9f, 0.88f);

        // 3. Tồn tại 0.2s rồi tự xóa
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Liên kết với máu của người chơi (PlayerHealth có sẵn của bạn)
            PlayerHealth playerHP = other.GetComponent<PlayerHealth>();
            if (playerHP != null)
            {
                playerHP.TakeDamage(damage);
                Debug.Log("💥 Kẻ địch đã ném trúng kỹ năng AOE! Mất " + damage + " máu!");
            }
        }
    }
}
