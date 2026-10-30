using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillTree : MonoBehaviour
{
    public GameObject skillTreePanel; // Kéo bảng SkillTreePanel vào đây
    private PlayerStats stats; // Mượn điểm từ Bảng Chỉ Số

    [Header("Trạng thái Kỹ Năng (Đã học hay chưa)")]
    public bool hasSkill1 = false;
    public bool hasSkill2 = false;
    public bool hasSkill3 = false;

    [Header("Giao diện 3 Nút Bấm")]
    public Image buttonSkill1; // Kéo Nút 1 vào đây
    public Image buttonSkill2; // Kéo Nút 2 vào đây
    public Image buttonSkill3; // Kéo Nút 3 vào đây

    [Header("Chi phí phép nguyên tố")]
    public int lightningCost = 1;
    public int fireCost = 1;
    public int waterCost = 1;

    public bool HasLightning => hasSkill1;
    public bool HasFire => hasSkill2;
    public bool HasWater => hasSkill3;

    void Awake()
    {
        if (GetComponent<ElementalSpellController>() == null)
            gameObject.AddComponent<ElementalSpellController>();
    }

    void Start()
    {
        stats = GetComponent<PlayerStats>();
        UpdateSkillUI();
    }

    // Hàm gọi khi bấm Nút Skill 1
    public void BuySkill1()
    {
        if (stats == null) return;
        if (!hasSkill1 && stats.availablePoints >= lightningCost)
        {
            stats.availablePoints -= lightningCost; 
            hasSkill1 = true;
            GameAudioManager.PlayUI(GameSound.QuestComplete, 0.82f, 1.14f);
            stats.UpdateUI(); 
            UpdateSkillUI();
            GameProgressManager.Instance?.SaveActiveSlot();
            Debug.Log("Đã học phép Sấm Sét. Dùng phím Q hoặc nút phép tương ứng.");
        }
    }

    // Hàm gọi khi bấm Nút Skill 2
    public void BuySkill2()
    {
        if (stats == null) return;
        if (!hasSkill2 && stats.availablePoints >= fireCost)
        {
            stats.availablePoints -= fireCost;
            hasSkill2 = true;
            GameAudioManager.PlayUI(GameSound.QuestComplete, 0.82f, 1.08f);
            stats.UpdateUI();
            UpdateSkillUI();
            GameProgressManager.Instance?.SaveActiveSlot();
            Debug.Log("Đã học phép Lửa. Dùng phím F hoặc nút phép tương ứng.");
        }
    }

    // Hàm gọi khi bấm Nút Skill 3
    public void BuySkill3()
    {
        if (stats == null) return;
        if (!hasSkill3 && stats.availablePoints >= waterCost)
        {
            stats.availablePoints -= waterCost;
            hasSkill3 = true;
            GameAudioManager.PlayUI(GameSound.QuestComplete, 0.82f, 1.02f);
            stats.UpdateUI();
            UpdateSkillUI();
            GameProgressManager.Instance?.SaveActiveSlot();
            Debug.Log("Đã học phép Nước. Dùng phím R hoặc nút phép tương ứng.");
        }
    }

    // Tự động nhuộm màu Vàng cho nút nào đã học, nút chưa học màu Xám
    public void UpdateSkillUI()
    {
        Color learned = Color.white;
        Color locked = new Color(.62f, .62f, .58f, 1f);
        if (buttonSkill1 != null) buttonSkill1.color = hasSkill1 ? learned : locked;
        if (buttonSkill2 != null) buttonSkill2.color = hasSkill2 ? learned : locked;
        if (buttonSkill3 != null) buttonSkill3.color = hasSkill3 ? learned : locked;
        SetLabel(buttonSkill1, "SẤM SÉT\n1 điểm • Q", hasSkill1);
        SetLabel(buttonSkill2, "HỎA CẦU\n1 điểm • F", hasSkill2);
        SetLabel(buttonSkill3, "THỦY KÍCH\n1 điểm • R", hasSkill3);
    }

    private static void SetLabel(Image buttonImage, string label, bool learned)
    {
        if (buttonImage == null) return;
        TextMeshProUGUI text = buttonImage.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = learned ? label + "\nĐÃ HỌC" : label;
            text.color = learned
                ? new Color32(255, 240, 199, 255)
                : new Color32(220, 208, 178, 255);
        }
    }
}
