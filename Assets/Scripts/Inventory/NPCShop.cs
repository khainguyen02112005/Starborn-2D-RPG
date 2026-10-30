using System.Collections;
using TMPro;
using UnityEngine;

public class NPCShop : MonoBehaviour
{
    [Header("Giao diện Cửa hàng")]
    public GameObject shopPanel; 
    public bool isShopOpen = false;
    private bool isPlayerNear = false; 

    [Header("HỘI THOẠI THƯƠNG NHÂN - SỬA TRỰC TIẾP TẠI ĐÂY")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerText;
    public string speakerName = "THƯƠNG NHÂN";
    [TextArea(2, 5)]
    public string[] dialogueLines =
    {
        "Chào mừng! Ta có thuốc hồi máu và thuốc hồi mana cho chuyến đi của ngươi.",
        "Mỗi lần chọn mua, ta chỉ bán đúng một món. Nhớ kiểm tra số vàng trước khi mua."
    };
    [Min(0f)] public float typingSpeed = 0.025f;
    public bool openShopAfterDialogue = true;

    [Header("Hệ thống Độ Thân Thiết (Danh vọng)")]
    public int reputation = 0;      // Điểm thân thiết
    public int maxReputation = 100; // Đạt 100 điểm sẽ thành VIP
    public bool isVIP = false;      // Đã là khách VIP chưa?

    private int dialogueIndex;
    private bool isDialogueOpen;
    private bool isTyping;
    private Coroutine typingRoutine;

    void Update()
    {
        if (isPlayerNear && isDialogueOpen &&
            (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            AdvanceDialogue();
            return;
        }

        // 1. Nhấn E để nói chuyện trước, sau đó mới mở Shop.
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (isShopOpen) CloseShop();
            else if (CanShowDialogue()) OpenDialogue();
            else OpenShop();
        }

        if (!isPlayerNear && (isShopOpen || isDialogueOpen))
        {
            CloseDialogue(false);
            CloseShop();
        }

        // 2. Nhấn T để Bo tiền (Tốn 50 Vàng để đổi 10 Điểm thân thiết)
        if (isPlayerNear && !isDialogueOpen && Input.GetKeyDown(KeyCode.T))
        {
            if (isVIP == false) GiveGiftToNPC();
            else Debug.Log(gameObject.name + ": Chúng ta đã là anh em ruột thịt rồi, không cần khách sáo bo thêm tiền đâu!");
        }
    }

    private bool CanShowDialogue()
    {
        return dialoguePanel != null && dialogueText != null &&
            dialogueLines != null && dialogueLines.Length > 0;
    }

    private void OpenDialogue()
    {
        dialogueIndex = 0;
        isDialogueOpen = true;
        if (speakerText != null)
            speakerText.text = string.IsNullOrWhiteSpace(speakerName) ? "THƯƠNG NHÂN" : speakerName;
        dialoguePanel.SetActive(true);
        GameAudioManager.PlayUI(GameSound.Dialogue, 0.58f, 1f);
        ShowCurrentLine();
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
        if (dialogueIndex < dialogueLines.Length)
        {
            GameAudioManager.PlayUI(GameSound.Dialogue, 0.48f, 1.04f);
            ShowCurrentLine();
        }
        else CloseDialogue(openShopAfterDialogue);
    }

    private void ShowCurrentLine()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(dialogueLines[dialogueIndex]));
    }

    private IEnumerator TypeLine(string line)
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

    private void CloseDialogue(bool openShop)
    {
        if (!isDialogueOpen) return;
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        isTyping = false;
        isDialogueOpen = false;
        dialogueIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = string.Empty;
        if (openShop) OpenShop();
    }

    private void OpenShop()
    {
        isShopOpen = true;
        GameAudioManager.Play(GameSound.ShopOpen, transform.position, 0.68f, 1.02f);
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null) book.OpenShop(shopPanel);
        else if (shopPanel != null) shopPanel.SetActive(true);
    }

    private void CloseShop()
    {
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null && book.IsOpen) book.CloseAll();
        else CloseShopFromBook();
    }

    public void CloseShopFromBook()
    {
        isShopOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void GiveGiftToNPC()
    {
        PlayerInventory inv = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
        if (inv != null && inv.goldCount >= 50)
        {
            // Trừ 50 Vàng
            inv.goldCount -= 50;
            GameAudioManager.PlayUI(GameSound.Purchase, 0.72f, 1.06f);
            if (inv.goldText != null) inv.goldText.text = inv.goldCount.ToString();

            // Tăng 10 độ thân thiết
            reputation += 10;
            Debug.Log("💖 Đã Bo cho Thương nhân 50 Vàng! Độ thân thiết: " + reputation + "/" + maxReputation);

            // Kiểm tra xem đã đủ điểm lên VIP chưa
            if (reputation >= maxReputation)
            {
                isVIP = true;
                Debug.Log("👑 CHÚC MỪNG! Bạn đã trở thành BẠN THÂN VIP! Từ giờ bán rác sẽ được GẤP ĐÔI TIỀN!");
            }
        }
        else
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.72f);
            Debug.Log("❌ Nghèo quá, không đủ 50 Vàng để Bo cho Thương nhân!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Nhấn E để Giao dịch | Nhấn T để Bo 50 Vàng (+Độ thân thiết)");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}
