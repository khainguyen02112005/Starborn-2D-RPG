using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f; // Mỗi giây tự hồi 15 điểm thể lực
    
    public Image staminaBarFill; // Chỗ để kéo thả StaminaBar_Fill vào

    void Start()
    {
        currentStamina = maxStamina; // Mới vào game là đầy bình
    }

    void Update()
    {
        // Tự động hồi thể lực liên tục nếu chưa đầy bình
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            // Chặn không cho hồi vượt quá mức Max
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        // Cập nhật giao diện độ dài của thanh Xanh Lá
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = maxStamina > 0f ? Mathf.Clamp01(currentStamina / maxStamina) : 0f;
        }
    }

    // Hàm này lát nữa Kỹ năng Lướt (Dash) sẽ gọi ra để xin xài thể lực
    public bool UseStamina(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (currentStamina >= amount)
        {
            currentStamina -= amount; // Trừ thể lực
            return true;              // Báo hiệu: Đủ hơi, cho phép Lướt!
        }
        
        return false; // Báo hiệu: Đang hụt hơi, không cho lướt!
    }
}
