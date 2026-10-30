using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerLevel : MonoBehaviour
{
    [Header("Cày Cấp (Level)")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 10;
    
    // ===================================
    [Header("Trùng Sinh (Awakening)")]
    public int awakeningCount = 0; // Đếm số lần đã Trùng sinh
    public int levelToAwaken = 10; // Cần đạt cấp 10 để được Trùng sinh
    // ===================================

    [Header("Giao diện UI")]
    public TextMeshProUGUI levelText;
    public Image expBarFill; // Thanh cuộn kinh nghiệm

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        // Bấm phím T để kích hoạt Trùng Sinh (Chỉ thành công nếu đủ cấp)
        if (Input.GetKeyDown(KeyCode.T))
        {
            TriggerAwakening();
        }
    }

    public void AddExp(int expAmount)
    {
        currentExp += expAmount;
        
        // Vòng lặp Kiểm tra lên cấp
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel; // Trừ đi số Exp vừa tiêu hao
            level++;                      // Tăng 1 cấp
            expToNextLevel += 5;          // Lên cấp càng cao, yêu cầu Exp càng nhiều
            
            // 🎁 PHẦN THƯỞNG LÊN CẤP THƯỜNG: Tặng 1 điểm tiềm năng
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null) 
            {
                stats.availablePoints += 1;
                stats.UpdateUI();
            }

            // ===============================================
            // MỤC SỐ 5 CỦA BẠN ĐÂY: HỒI ĐẦY MÁU VÀ MANA KHI LÊN CẤP
            // ===============================================
            // 1. Tặng thêm 2 Máu Gốc và Hồi Đầy Bình Máu
            PlayerHealth hp = GetComponent<PlayerHealth>();
            if (hp != null) hp.LevelUpBoost(2); 

            // 2. Hồi Đầy Bình Mana
            PlayerMana mana = GetComponent<PlayerMana>();
            if (mana != null) mana.currentMana = mana.maxMana;
            // ===============================================

            Debug.Log("🎉 LEVEL UP! Bạn đã lên cấp " + level);
        }

        UpdateUI();
    }

    public void TriggerAwakening()
    {
        // Nếu đã cày đủ (hoặc dư) cấp 10
        if (level >= levelToAwaken)
        {
            // 1. Phế bỏ tu vi, quay về Cấp 1
            level = 1;
            currentExp = 0;
            expToNextLevel = 10; // Đưa yêu cầu Exp về lại mốc dễ ban đầu
            
            // 2. Đánh dấu 1 lần Trùng Sinh thành công
            awakeningCount++;

            // 3. THƯỞNG BUFF VĨNH VIỄN CHO CAO THỦ
            // Tặng nóng 5 điểm tiềm năng
            PlayerStats stats = GetComponent<PlayerStats>();
            if (stats != null) 
            {
                stats.availablePoints += 5;
                stats.UpdateUI();
            }

            // Small permanent crit chance bonus; awakening must not multiply
            // damage without limit on every loop. The five stat points remain.
            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null) combat.critChance = Mathf.Min(.2f, combat.critChance + .01f);

            UpdateUI();
            Debug.Log("🌟 CHÚC MỪNG! BẠN ĐÃ TRÙNG SINH THÀNH CÔNG LẦN " + awakeningCount + "!");
        }
        else
        {
            Debug.Log("Chưa đủ Cấp " + levelToAwaken + " để Trùng Sinh. Hãy cày cuốc thêm nhé tiểu đệ!");
        }
    }

    public void UpdateUI()
    {
        if (levelText != null) 
        {
            // Nếu đã trùng sinh thì gắn thêm Ngôi sao vào chữ Level
            if (awakeningCount > 0)
                levelText.text = "Level " + level + " (Trùng sinh " + awakeningCount + ")";
            else
                levelText.text = "Level " + level;
        }

        if (expBarFill != null)
        {
            expBarFill.fillAmount = expToNextLevel > 0 ? Mathf.Clamp01((float)currentExp / expToNextLevel) : 0f;
        }
    }
}
