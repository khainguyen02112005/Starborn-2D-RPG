using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Điểm Tiềm Năng")]
    public int availablePoints = 5; // Cho sẵn 5 điểm để test

    [Header("Chỉ số hiện tại")]
    public int strength = 1;     
    public int agility = 1;      
    public int intelligence = 1; 

    [Header("Giao diện UI")]
    public GameObject statsPanel; 
    public TextMeshProUGUI pointsText; 
    public TextMeshProUGUI strText;    
    public TextMeshProUGUI agiText;    
    public TextMeshProUGUI intText;    

    void Start()
    {
        UpdateUI();
    }

    public void AddStrength()
    {
        if (availablePoints > 0)
        {
            strength++;
            availablePoints--;
            UpdateUI();
            
            // --- BUFF SỨC MẠNH ---
            // Combat derives bounded damage/crit from STR, including after loading a save.
        }
    }

    public void AddAgility()
    {
        if (availablePoints > 0)
        {
            agility++;
            availablePoints--;
            UpdateUI();
            
            // --- BUFF NHANH NHẸN ---
            PlayerMovement move = GetComponent<PlayerMovement>();
            if (move != null)
            {
                move.moveSpeed += 0.5f; // Đi bộ nhanh hơn
                move.dashSpeed += 1.0f; // Lướt xa hơn
            }
        }
    }

    public void AddIntelligence()
    {
        if (availablePoints > 0)
        {
            intelligence++;
            availablePoints--;
            UpdateUI();
            
            // --- BUFF TRÍ LỰC ---
            PlayerHealth hp = GetComponent<PlayerHealth>();
            if (hp != null) hp.LevelUpBoost(2); // Tăng 2 Máu tối đa
            
            PlayerMana mana = GetComponent<PlayerMana>();
            if (mana != null) mana.LevelUpBoost(2); // Tăng 2 Mana tối đa
        }
    }

    public void UpdateUI()
    {
        if (pointsText != null) pointsText.text = "Điểm tiềm năng: " + availablePoints;
        if (strText != null) strText.text = "Sức Mạnh (STR): " + strength;
        if (agiText != null) agiText.text = "Nhanh Nhẹn (AGI): " + agility;
        if (intText != null) intText.text = "Trí Lực (INT): " + intelligence;
    }
}
