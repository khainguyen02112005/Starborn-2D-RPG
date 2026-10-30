using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BookInventoryUI : MonoBehaviour
{
    private enum EditorPreviewPage
    {
        KhongCanThiep,
        AnTatCa,
        TuiDo,
        ChiSoVaKyNang,
        RuongDo,
        ThuongNhan,
        LoRen
    }

    private static readonly int OpenParameter = Animator.StringToHash("Open");

    [Header("Animation")]
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Inventory pages")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject recycleBin;

    [Header("Character pages")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject skillTreePanel;

    [Header("Context pages")]
    [SerializeField] private GameObject chestPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject craftingPanel;

    [Header("Tabs")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Image[] tabImages;
    [SerializeField] private Sprite normalTabSprite;
    [SerializeField] private Sprite selectedTabSprite;

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI bookHeading;
    [SerializeField] private TextMeshProUGUI footerHint;

    [Header("Live values")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TextMeshProUGUI goldValue;

    [Header("EDITOR PREVIEW - CHỌN TRANG ĐỂ TỰ CHỈNH UI")]
    [Tooltip("Chỉ có tác dụng khi không Play. Chọn Không Can Thiệp để tự bật/tắt GameObject bằng tay.")]
    [SerializeField] private EditorPreviewPage editorPreviewPage = EditorPreviewPage.KhongCanThiep;

    private bool isOpen;
    private GameObject activeContextPanel;
    private string footerDefault;
    private float feedbackUntil;
    private TMP_Text characterNameLabel;

    public bool IsOpen => isOpen;
    public bool IsContextOpen => activeContextPanel != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || editorPreviewPage == EditorPreviewPage.KhongCanThiep) return;
        UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
        UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
    }

    private void ApplyEditorPreview()
    {
        if (this == null || Application.isPlaying || editorPreviewPage == EditorPreviewPage.KhongCanThiep) return;

        SetAllPages(false);
        bool visible = editorPreviewPage != EditorPreviewPage.AnTatCa;
        switch (editorPreviewPage)
        {
            case EditorPreviewPage.TuiDo:
                SetActive(inventoryPanel, true);
                SetActive(characterPanel, true);
                SetActive(recycleBin, true);
                SetLabels("NHẬT KÝ HÀNH TRANG", "CHẾ ĐỘ XEM TRƯỚC TRONG EDITOR");
                break;
            case EditorPreviewPage.ChiSoVaKyNang:
                SetActive(statsPanel, true);
                SetActive(skillTreePanel, true);
                SetLabels("HỒ SƠ NHÂN VẬT", "CHỈ SỐ BÊN TRÁI  •  KỸ NĂNG BÊN PHẢI");
                break;
            case EditorPreviewPage.RuongDo:
                SetActive(chestPanel, true);
                SetActive(inventoryPanel, true);
                SetLabels("RƯƠNG ĐỒ & TÚI ĐỒ", "CHẾ ĐỘ XEM TRƯỚC TRONG EDITOR");
                break;
            case EditorPreviewPage.ThuongNhan:
                SetActive(shopPanel, true);
                SetActive(inventoryPanel, true);
                SetLabels("THƯƠNG NHÂN & TÚI ĐỒ", "CHẾ ĐỘ XEM TRƯỚC TRONG EDITOR");
                break;
            case EditorPreviewPage.LoRen:
                SetActive(craftingPanel, true);
                SetActive(inventoryPanel, true);
                SetLabels("LÒ RÈN & TÚI ĐỒ", "CHẾ ĐỘ XEM TRƯỚC TRONG EDITOR");
                break;
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = visible ? 1f : 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        if (bookAnimator != null)
        {
            bookAnimator.enabled = false;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void Awake()
    {
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            if (label.text == "NHÂN VẬT") { characterNameLabel = label; break; }
        if (inventory == null)
            inventory = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        if (overlayCanvasGroup == null) overlayCanvasGroup = GetComponent<CanvasGroup>();
        if (bookAnimator == null) bookAnimator = GetComponent<Animator>();
        if (bookAnimator != null) bookAnimator.enabled = true;

        SetAllPages(false);
        SetOpenState(false, true);
        RefreshValues();
    }

    private void LateUpdate()
    {
        if (isOpen) RefreshValues();
        if (feedbackUntil > 0f && Time.unscaledTime >= feedbackUntil)
        {
            feedbackUntil = 0f;
            if (footerHint != null) footerHint.text = footerDefault;
        }
    }

    public void ShowFeedback(string message)
    {
        if (!isOpen || footerHint == null) return;
        footerHint.text = message;
        feedbackUntil = Time.unscaledTime + 2.5f;
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseAll();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        if (GameplayUiFocus.HasDialogue || GameplayUiFocus.HasEndingChoice || CinematicPresentationController.IsCinematicActive) return;
        SetContextPages(false);
        activeContextPanel = null;
        SetActive(inventoryPanel, true);
        SetActive(characterPanel, true);
        SetActive(recycleBin, true);
        SetActive(statsPanel, false);
        SetActive(skillTreePanel, false);
        SetTabState(0);
        SetLabels("NHẬT KÝ HÀNH TRANG", "B  ĐÓNG SÁCH     •     KÉO THẢ ĐỂ SẮP XẾP     •     RÊ CHUỘT ĐỂ XEM CHI TIẾT");
        SetOpenState(true);
    }

    public void OpenStats()
    {
        if (GameplayUiFocus.HasDialogue || GameplayUiFocus.HasEndingChoice || CinematicPresentationController.IsCinematicActive) return;
        SetContextPages(false);
        activeContextPanel = null;
        SetActive(inventoryPanel, false);
        SetActive(characterPanel, false);
        SetActive(recycleBin, false);
        SetActive(statsPanel, true);
        SetActive(skillTreePanel, true);
        SetTabState(1);
        SetLabels("HỒ SƠ NHÂN VẬT", "CỘNG ĐIỂM Ở TRANG TRÁI     •     HỌC KỸ NĂNG Ở TRANG PHẢI     •     B  ĐÓNG SÁCH");
        SetOpenState(true);

        PlayerStats stats = inventory != null ? inventory.GetComponent<PlayerStats>() : null;
        if (stats != null) stats.UpdateUI();
        SkillTree skills = inventory != null ? inventory.GetComponent<SkillTree>() : null;
        if (skills != null) skills.UpdateSkillUI();
    }

    public void OpenChest(GameObject panel)
    {
        OpenContext(panel != null ? panel : chestPanel, "RƯƠNG ĐỒ & TÚI ĐỒ", "KÉO VẬT PHẨM QUA LẠI GIỮA RƯƠNG VÀ TÚI ĐỒ     •     B  ĐÓNG SÁCH");
    }

    public void OpenShop(GameObject panel)
    {
        OpenContext(panel != null ? panel : shopPanel, "THƯƠNG NHÂN & TÚI ĐỒ", "TRÁI: DÙNG THUỐC  •  PHẢI: BÁN 1 MÓN  •  MỖI NHẤP MUA 1 MÓN  •  B ĐÓNG");
    }

    public void OpenBlacksmith(GameObject panel)
    {
        OpenContext(panel != null ? panel : craftingPanel, "LÒ RÈN & TÚI ĐỒ", "RÈN VŨ KHÍ HOẶC SỬA TRANG BỊ     •     B  ĐÓNG SÁCH");
    }

    public void SelectInventoryTab()
    {
        if (!IsContextOpen) OpenInventory();
    }

    public void SelectStatsTab()
    {
        if (!IsContextOpen) OpenStats();
    }

    // Giữ tương thích với persistent listener trong những scene cũ.
    public void SelectBagTab() => SelectInventoryTab();
    public void SelectEquipmentTab() => SelectStatsTab();
    public void SelectHelpTab() => SelectStatsTab();

    public void CloseAll()
    {
        SetAllPages(false);
        activeContextPanel = null;

        foreach (NPCShop shop in FindObjectsByType<NPCShop>(FindObjectsInactive.Include))
            shop.CloseShopFromBook();
        foreach (StorageChest chest in FindObjectsByType<StorageChest>(FindObjectsInactive.Include))
            chest.CloseChestFromBook();
        foreach (NPCBlacksmith blacksmith in FindObjectsByType<NPCBlacksmith>(FindObjectsInactive.Include))
            blacksmith.CloseCraftingFromBook();

        if (ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip();
        SetOpenState(false);
    }

    private void OpenContext(GameObject panel, string title, string hint)
    {
        if (GameplayUiFocus.HasDialogue || GameplayUiFocus.HasEndingChoice || CinematicPresentationController.IsCinematicActive) return;
        SetAllPages(false);
        activeContextPanel = panel;
        SetActive(panel, true);
        SetActive(inventoryPanel, true);
        SetTabState(-1);
        SetLabels(title, hint);
        SetOpenState(true);
    }

    private void SetAllPages(bool active)
    {
        SetActive(inventoryPanel, active);
        SetActive(characterPanel, active);
        SetActive(recycleBin, active);
        SetActive(statsPanel, active);
        SetActive(skillTreePanel, active);
        SetContextPages(active);
    }

    private void SetContextPages(bool active)
    {
        SetActive(activeContextPanel, active);
        SetActive(chestPanel, active);
        SetActive(shopPanel, active);
        SetActive(craftingPanel, active);
    }

    private void SetOpenState(bool open, bool force = false)
    {
        if (!force && isOpen == open) return;
        isOpen = open;
        if (open) GameplayUiFocus.OpenBook = this;
        else if (GameplayUiFocus.OpenBook == this) GameplayUiFocus.OpenBook = null;
        if (!force)
            GameAudioManager.PlayUI(open ? GameSound.ShopOpen : GameSound.UICancel, open ? 0.48f : 0.58f,
                open ? 1.08f : 0.94f);

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.interactable = open;
            overlayCanvasGroup.blocksRaycasts = open;
        }

        if (bookAnimator != null)
        {
            bookAnimator.SetBool(OpenParameter, open);
            if (force) bookAnimator.Play(open ? "BookOpen" : "BookClosed", 0, 1f);
        }

        if (open)
        {
            transform.SetAsLastSibling();
            if (ItemTooltip.instance != null) ItemTooltip.instance.transform.SetAsLastSibling();
            RefreshValues();
        }
    }

    private void SetTabState(int activeIndex)
    {
        if (tabImages != null)
        {
            for (int i = 0; i < tabImages.Length; i++)
            {
                if (tabImages[i] == null) continue;
                tabImages[i].sprite = i == activeIndex ? selectedTabSprite : normalTabSprite;
                tabImages[i].color = Color.white;
            }
        }

        if (tabButtons != null)
        {
            bool tabsEnabled = activeIndex >= 0;
            foreach (Button button in tabButtons)
                if (button != null) button.interactable = tabsEnabled;
        }
    }

    private void SetLabels(string title, string hint)
    {
        footerDefault = hint;
        feedbackUntil = 0f;
        if (bookHeading != null) bookHeading.text = title;
        if (footerHint != null) footerHint.text = hint;
    }

    private void RefreshValues()
    {
        if (characterNameLabel != null) characterNameLabel.text = PlayerIdentity.Literal(PlayerIdentity.Name);
        if (inventory != null && goldValue != null)
            goldValue.text = inventory.goldCount.ToString();
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    private void OnDisable()
    {
        if (GameplayUiFocus.OpenBook == this) GameplayUiFocus.OpenBook = null;
    }
}
