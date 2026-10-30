using UnityEngine;

public class EliteModifier : MonoBehaviour
{
    [Header("Chỉ số Tinh Anh")]
    public float eliteChance = 0.2f; // Tỉ lệ 20% hóa Tinh Anh
    public float scaleMultiplier = 1.5f; // Bự gấp rưỡi
    public Color auraColor = new Color(1f, 0.5f, 0f, 0.5f); // Màu Cam sáng

    [Header("Prefab Vòng sáng (Aura)")]
    public GameObject auraPrefab; // (Tùy chọn) Kéo 1 vòng Sprite sáng vào đây

    void Start()
    {
        // Quay xổ số xem có trúng thưởng hóa Elite không
        if (Random.value <= eliteChance) 
        {
            MakeElite();
        }
    }

    void MakeElite()
    {
        // 1. Phóng to thân hình
        transform.localScale *= scaleMultiplier;

        // 2. Sinh ra vòng sáng Aura dưới chân
        if (auraPrefab != null)
        {
            GameObject aura = Instantiate(auraPrefab, transform.position, Quaternion.identity, transform);
            aura.transform.localPosition = new Vector3(0, -0.5f, 0); // Kéo xuống dưới chân
            
            SpriteRenderer auraSr = aura.GetComponent<SpriteRenderer>();
            if (auraSr != null) auraSr.color = auraColor;
        }

        // 3. Tăng x2 Máu và Sát thương
        // (Chú ý: Code này tham chiếu tới EnemyAI.cs hoặc script Máu của bạn)
        // Ví dụ:
        // EnemyHealth hp = GetComponent<EnemyHealth>();
        // if (hp != null) {
        //     hp.maxHealth *= 2; 
        //     hp.currentHealth = hp.maxHealth;
        // }
        
        // EnemyAI ai = GetComponent<EnemyAI>();
        // if (ai != null) ai.speed *= 1.2f; // Chạy nhanh hơn 20%
        
        Debug.Log("✨ Một con quái TINH ANH vừa xuất hiện!");
    }
}