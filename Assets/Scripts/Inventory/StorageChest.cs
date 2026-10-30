using UnityEngine;

public class StorageChest : MonoBehaviour
{
    [Header("Giao diện Rương")]
    public GameObject chestPanel; 
    public InventorySlot[] chestSlots; 
    public bool isChestOpen = false;
    private bool isPlayerNear = false; 

    [Header("Vật phẩm mặc định")]
    public ItemData initialOre;
    public ItemData initialSword;
    public ItemData initialStaff;
    public bool initializeOnStart = true;
    public ItemData[] initialItems;
    public int[] initialAmounts;

    private void Start()
    {
        if (!initializeOnStart || chestSlots == null || chestSlots.Length == 0) return;
        bool alreadyFilled = false;
        foreach (InventorySlot slot in chestSlots)
        {
            if (slot != null && slot.isFull) { alreadyFilled = true; break; }
        }
        if (alreadyFilled) return;
        if (initialItems != null && initialItems.Length > 0)
        {
            for (int i = 0; i < initialItems.Length; i++) AddItemToChest(initialItems[i], initialAmounts != null && i < initialAmounts.Length ? Mathf.Max(1, initialAmounts[i]) : 1);
            return;
        }
        if (initialOre != null) AddItemToChest(initialOre, 3);
        ItemData initialWeapon = PlayerClassController.IsMageSelected ? initialStaff : initialSword;
        if (initialWeapon != null) AddItemToChest(initialWeapon, 1);
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (isChestOpen) CloseChest();
            else OpenChest();
        }

        if (!isPlayerNear && isChestOpen)
        {
            CloseChest();
        }
    }

    private void OpenChest()
    {
        isChestOpen = true;
        GameAudioManager.Play(GameSound.ChestOpen, transform.position, 0.88f, 0.9f);
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null) book.OpenChest(chestPanel);
        else if (chestPanel != null) chestPanel.SetActive(true);
    }

    private void CloseChest()
    {
        BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
        if (book != null && book.IsOpen) book.CloseAll();
        else CloseChestFromBook();
    }

    public void CloseChestFromBook()
    {
        isChestOpen = false;
        if (chestPanel != null) chestPanel.SetActive(false);
    }

    public bool AddItemToChest(ItemData data, int amount = 1, int upgrade = 0, int dura = 100)
    {
        if (data == null || amount <= 0 || chestSlots == null) return false;

        // 1: Tìm ô có đồ giống hệt để Gộp (Stacking)
        for (int i = 0; i < chestSlots.Length; i++)
        {
            if (chestSlots[i].isFull == true && chestSlots[i].itemData == data && chestSlots[i].upgradeLevel == upgrade && chestSlots[i].currentDurability == dura && chestSlots[i].itemAmount < 99)
            {
                chestSlots[i].itemAmount += amount;
                chestSlots[i].UpdateAmountUI();
                return true;
            }
        }

        // 2: Tìm ô trống mới để nhét
        for (int i = 0; i < chestSlots.Length; i++)
        {
            if (chestSlots[i].isFull == false) 
            {
                chestSlots[i].AddItem(data, amount, upgrade, dura); 
                return true;                
            }
        }
        Debug.Log("RƯƠNG ĐÃ ĐẦY ẮP!");
        GameAudioManager.PlayUI(GameSound.UIError, 0.7f);
        return false; 
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Ấn phím E để mở Rương!");
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
