using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AchievementMenu : MonoBehaviour
{
    public CanvasGroup[] branchGroups = new CanvasGroup[3];
    public Image[] lockIcons = new Image[3];
    public TextMeshProUGUI[] branchDescriptions = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] completionLabels = new TextMeshProUGUI[3];
    public Image[] statusBadges = new Image[3];
    public TextMeshProUGUI progressLabel;

    private readonly string[] unlockedDescriptions =
    {
        "KẾT 1 — HẠNH PHÚC?\nKhông gục ngã lần nào. Dòng chữ hạnh phúc đã méo thành Bad Ending.",
        "KẾT 2 — VÒNG LẶP KÝ ỨC\nĐánh bại hiệp sĩ hắc hóa và khởi động lại vở kịch.",
        "KẾT 3 — KHOẢNG TRẮNG\nĐi cùng hiệp sĩ, hoặc thất bại trước cơn tuyệt vọng."
    };

    private void Start() => Refresh();

    public void Refresh()
    {
        int completed = 0;
        for (int i = 0; i < 3; i++)
        {
            bool unlocked = GameProgressManager.IsEndingUnlocked(i + 1);
            if (unlocked) completed++;
            if (branchGroups[i] != null)
            {
                branchGroups[i].alpha = 1f;
                branchGroups[i].interactable = unlocked;
            }
            if (lockIcons[i] != null) lockIcons[i].gameObject.SetActive(!unlocked);
            if (branchDescriptions[i] != null)
                branchDescriptions[i].text = unlocked ? unlockedDescriptions[i] : $"NHÁNH {i + 1} — CHƯA MỞ\nHoàn thành cốt truyện để soi sáng ký ức này.";
            if (completionLabels != null && i < completionLabels.Length && completionLabels[i] != null)
                completionLabels[i].text = unlocked ? "ĐÃ HOÀN THÀNH" : "CHƯA HOÀN THÀNH";
            if (statusBadges != null && i < statusBadges.Length && statusBadges[i] != null)
                statusBadges[i].color = unlocked ? new Color(.13f,.39f,.28f,1) : new Color(.19f,.21f,.23f,1);
        }
        if (progressLabel != null) progressLabel.text = "TIẾN ĐỘ: " + completed + " / 3 KẾT ĐÃ HOÀN THÀNH";
    }

    public void BackToMenu() => SceneManager.LoadScene("Main menu");
}
