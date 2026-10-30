#pragma warning disable CS0618 // Tắt cảnh báo Obsolete

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCBlacksmith : MonoBehaviour
{
    [Header("GIAO DIỆN & VẬT PHẨM")]
    public GameObject craftingPanel; 
    public ItemData craftedSwordData; // FILE DỮ LIỆU CỦA THANH KIẾM SẼ ĐƯỢC RÈN RA
    public ItemData craftedStaffData;

    [Header("HỘI THOẠI THỢ RÈN - SỬA TRỰC TIẾP Ở ĐÂY")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerText;
    public string speakerName = "THỢ RÈN";
    [TextArea(2, 5)] public string[] dialogueLines =
    {
        "Nhìn vũ khí của ngươi kìa. Nếu cứ chiến đấu như vậy, nó sẽ sớm gãy mất.",
        "Có đủ nguyên liệu và vàng thì chuyện gì ta cũng làm được.",
        "Kiếm dành cho hiệp sĩ. Gậy phép dành cho pháp sư. Đừng cầm nhầm rồi trách ta."
    };
    [Min(0f)] public float typingSpeed = 0.025f;
    
    [Header("BẢNG GIÁ")]
    public int oreCost = 3;          
    public int repairCost = 50;      

    private bool isPlayerNear = false;
    private bool isPanelOpen = false;
    private bool isDialogueOpen;
    private bool isTyping;
    private int dialogueIndex;
    private Coroutine typingRoutine;
    private Coroutine feedbackRoutine;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (isDialogueOpen) AdvanceDialogue();
            else if (isPanelOpen) CloseCrafting();
            else if (dialoguePanel != null && dialogueText != null) OpenDialogue();
            else OpenCrafting();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNear = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNear = false;
            CloseDialogue(false);
            CloseCrafting();
        }
    }

    private void OpenDialogue()
    {
        dialogueIndex = 0;
        isDialogueOpen = true;
        if (speakerText != null) speakerText.text = speakerName;
        dialoguePanel.SetActive(true);
        GameAudioManager.PlayUI(GameSound.Dialogue, 0.58f, 0.92f);
        ShowDialogueLine();
    }

    private void AdvanceDialogue()
    {
        if (isTyping)
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            dialogueText.text = dialogueLines[dialogueIndex];
            isTyping = false;
            return;
        }
        dialogueIndex++;
        if (dialogueLines != null && dialogueIndex < dialogueLines.Length) ShowDialogueLine();
        else CloseDialogue(true);
    }

    private void ShowDialogueLine()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeDialogueLine(dialogueLines[dialogueIndex]));
    }

    private IEnumerator TypeDialogueLine(string line)
    {
        isTyping = true;
        dialogueText.text = string.Empty;
        foreach (char character in line ?? string.Empty)
        {
            dialogueText.text += character;
            if (typingSpeed > 0f) yield return new WaitForSecondsRealtime(typingSpeed);
        }
        isTyping = false;
        typingRoutine = null;
    }

    private void CloseDialogue(bool openCrafting)
    {
        if (!isDialogueOpen) return;
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTyping = false;
        isDialogueOpen = false;
        dialogueIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = string.Empty;
        if (openCrafting) OpenCrafting();
    }

    private void OpenCrafting()
    {
        isPanelOpen = true;
        if (craftingPanel != null)
            foreach (Button button in craftingPanel.GetComponentsInChildren<Button>(true))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentMethodName(i) == "CraftWeapon")
                    {
                        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (label != null) label.text = PlayerClassController.IsMageSelected ? "RÈN GẬY PHÉP" : "RÈN KIẾM";
                    }
        GameAudioManager.Play(GameSound.Blacksmith, transform.position, 0.58f, 0.86f);
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null) book.OpenBlacksmith(craftingPanel);
        else if (craftingPanel != null) craftingPanel.SetActive(true);
    }

    private void CloseCrafting()
    {
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null && book.IsOpen) book.CloseAll();
        else CloseCraftingFromBook();
    }

    public void CloseCraftingFromBook()
    {
        isPanelOpen = false;
        if (craftingPanel != null) craftingPanel.SetActive(false);
    }

    public void CraftWeapon()
    {
        PlayerInventory inv = FindAnyObjectByType<PlayerInventory>();
        if (inv == null || inv.inventoryPanel == null) return;
        InventorySlot[] bagSlots = inv.inventoryPanel.GetComponentsInChildren<InventorySlot>();
        
        int totalOre = 0;
        foreach (InventorySlot slot in bagSlots)
        {
            if (slot.isFull && slot.equipSlotType == 0 && slot.itemData != null && slot.itemData.itemType == 5) 
                totalOre += slot.itemAmount;
        }

        if (totalOre < oreCost)
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.75f);
            ShowSmithMessage("Chưa đủ nguyên liệu. Hãy tìm thêm sắt hoặc mang những món đồ cũ đến đây.");
            return;
        }

        ItemData craftedWeapon = PlayerClassController.IsMageSelected ? craftedStaffData : craftedSwordData;
        if (craftedWeapon == null || !PlayerClassController.CanUseWeapon(craftedWeapon))
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.75f);
            ShowSmithMessage("Ta chưa có bản vẽ phù hợp cho vũ khí này.");
            return;
        }

        InventorySlot emptySlot = null;
        foreach (InventorySlot slot in bagSlots)
        {
            if (!slot.isFull && slot.equipSlotType == 0)
            {
                emptySlot = slot;
                break;
            }
        }

        if (emptySlot == null)
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.75f);
            ShowSmithMessage("Túi của ngươi đầy rồi. Hãy dọn chỗ trước khi nhận vũ khí.");
            return;
        }

        int oreToDeduct = oreCost;
        foreach (InventorySlot slot in bagSlots)
        {
            if (slot.isFull && slot.equipSlotType == 0 && slot.itemData != null && slot.itemData.itemType == 5)
            {
                if (slot.itemAmount >= oreToDeduct)
                {
                    slot.itemAmount -= oreToDeduct;
                    oreToDeduct = 0;
                    if (slot.itemAmount == 0) slot.ClearSlot();
                    else slot.UpdateAmountUI();
                    break;
                }
                else
                {
                    oreToDeduct -= slot.itemAmount;
                    slot.ClearSlot();
                }
            }
        }

        emptySlot.AddItem(craftedWeapon, 1, 0, 100);
        GameAudioManager.Play(GameSound.Blacksmith, transform.position, 1f, 1.02f);
        ShowSmithMessage("Xong rồi! Vũ khí này mạnh hơn trước nhiều đấy.");
    }

    public void RepairAllItems()
    {
        PlayerInventory inv = FindAnyObjectByType<PlayerInventory>();
        if (inv == null || inv.goldCount < repairCost) 
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.75f);
            ShowSmithMessage("Ngươi chưa có đủ " + repairCost + " vàng để sửa toàn bộ trang bị.");
            return;
        }

        bool repairedAnything = false;
        
        InventorySlot[] allSlots = FindObjectsByType<InventorySlot>(FindObjectsInactive.Include);
        
        foreach(InventorySlot slot in allSlots)
        {
            if (slot.isFull && slot.equipSlotType > 0 && slot.currentDurability < 100)
            {
                slot.currentDurability = 100; 
                slot.UpdateAmountUI();
                repairedAnything = true;
            }
        }

        if (repairedAnything)
        {
            GameAudioManager.Play(GameSound.Blacksmith, transform.position, 0.92f, 1.12f);
            inv.goldCount -= repairCost; 
            if (inv.goldText != null) inv.goldText.text = inv.goldCount.ToString();
            ShowSmithMessage("Keng keng! Ta đã sửa mới toàn bộ trang bị của ngươi.");
        }
        else
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.62f, 0.92f);
            ShowSmithMessage("Trang bị của ngươi vẫn còn tốt, chưa cần sửa đâu.");
        }
    }

    private void ShowSmithMessage(string message)
    {
        Debug.Log("THỢ RÈN: " + message, this);
        if (dialoguePanel == null || dialogueText == null) return;
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(ShowSmithMessageRoutine(message));
    }

    private IEnumerator ShowSmithMessageRoutine(string message)
    {
        if (speakerText != null) speakerText.text = speakerName;
        dialogueText.text = message;
        dialoguePanel.SetActive(true);
        dialoguePanel.transform.SetAsLastSibling();
        yield return new WaitForSecondsRealtime(2.35f);
        if (!isDialogueOpen) dialoguePanel.SetActive(false);
        feedbackRoutine = null;
    }
}
