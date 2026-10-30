using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotMenu : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Button[] slotButtons = new Button[GameProgressManager.SlotCount];
    public TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[GameProgressManager.SlotCount];
    public Button[] deleteButtons = new Button[GameProgressManager.SlotCount];
    public TextMeshProUGUI[] deleteButtonTexts = new TextMeshProUGUI[GameProgressManager.SlotCount];
    public TextMeshProUGUI hintText;

    private bool saveMode;
    private int pendingDeleteIndex = -1;

    private void Start()
    {
        saveMode = PlayerPrefs.GetString("RPG.SaveMenuMode", "load") == "save";
        if (titleText != null) titleText.text = saveMode ? "CHỌN Ô ĐỂ LƯU" : "TIẾN TRÌNH";
        if (hintText != null) hintText.text = saveMode ? "Chọn một ô để ghi tiến trình hiện tại" : "Chọn ô đã lưu để tiếp tục phiêu lưu";

        for (int index = 0; index < slotButtons.Length; index++)
        {
            int captured = index;
            if (slotButtons[index] != null)
            {
                slotButtons[index].onClick.RemoveAllListeners();
                slotButtons[index].onClick.AddListener(() => SelectSlot(captured));
            }
            if (index < deleteButtons.Length && deleteButtons[index] != null)
            {
                deleteButtons[index].onClick.RemoveAllListeners();
                deleteButtons[index].onClick.AddListener(() => RequestDeleteSlot(captured));
            }
        }
        Refresh();
    }

    public void SelectSlot(int index)
    {
        pendingDeleteIndex = -1;
        GameProgressManager.Instance?.SelectSlot(index, saveMode);
    }

    public void RequestDeleteSlot(int index)
    {
        if (GameProgressManager.Instance == null || !GameProgressManager.Instance.GetSlot(index).exists) return;

        if (pendingDeleteIndex != index)
        {
            pendingDeleteIndex = index;
            if (hintText != null) hintText.text = $"Nhấn XÁC NHẬN để xóa vĩnh viễn ô {index + 1}";
            Refresh();
            return;
        }

        GameProgressManager.Instance.DeleteSlot(index);
        pendingDeleteIndex = -1;
        UpdateHint();
        Refresh();
    }

    public void Back()
    {
        GameProgressManager.Instance?.ReturnFromSlotMenu();
    }

    private void UpdateHint()
    {
        if (hintText != null)
            hintText.text = saveMode ? "Chọn một ô để ghi tiến trình hiện tại" : "Chọn ô đã lưu để tiếp tục phiêu lưu";
    }

    private void Refresh()
    {
        for (int index = 0; index < slotTexts.Length; index++)
        {
            if (slotTexts[index] == null) continue;
            SaveSlotData data = GameProgressManager.Instance != null ? GameProgressManager.Instance.GetSlot(index) : new SaveSlotData();
            slotTexts[index].text = data.exists
                ? $"Ô {index + 1}\n{PlayerIdentity.Literal(PlayerIdentity.NormalizeName(data.playerName))}  •  Cấp {data.level}\n{data.sceneName}  •  {data.savedAt}"
                : $"Ô {index + 1}\n— Trống —\n{(saveMode ? "Lưu tại đây" : "Bắt đầu hành trình mới")}";

            if (index < deleteButtons.Length && deleteButtons[index] != null)
            {
                deleteButtons[index].interactable = data.exists;
                if (index < deleteButtonTexts.Length && deleteButtonTexts[index] != null)
                    deleteButtonTexts[index].text = !data.exists ? "—" : pendingDeleteIndex == index ? "XÁC NHẬN" : "XÓA";
            }
        }
    }
}
