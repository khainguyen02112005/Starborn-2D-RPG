#pragma warning disable CS0618

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; 

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image icon;           
    public Image slotBackground; 
    public bool isFull = false;  
    public bool isChestSlot = false; 

    [Header("THÔNG TIN VẬT PHẨM (MỚI)")]
    public ItemData itemData; 
    public int itemAmount = 0; 
    public int upgradeLevel = 0; 
    public int currentDurability = 100; 

    [Header("CHỨC NĂNG CỦA Ô NÀY")]
    public int equipSlotType = 0; 

    public TextMeshProUGUI amountText; 
    public Image amountBackground;

    [Header("BOOK UI")]
    public bool useBookStyle = false;
    public Image hoverFrame;

    public static InventorySlot slotBeingDragged; 
    private static GameObject ghostIcon; 
    private int lastUseFrame = -1;
    private int lastSaleFrame = -1;

    private void InitSlot()
    {
        if (slotBackground == null) slotBackground = GetComponent<Image>(); 
        if (icon == null && transform.childCount > 0)
            icon = transform.GetChild(0).GetComponent<Image>();
        if (amountText == null)
            amountText = GetComponentInChildren<TextMeshProUGUI>(); 
    }

    public void Start()
    {
        InitSlot();
        if (isFull == false || itemData == null) ClearSlot(); 
        else UpdateAmountUI(); 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverFrame != null) hoverFrame.gameObject.SetActive(true);
        if (isFull == false || slotBeingDragged != null || itemData == null) return;
        if (ItemTooltip.instance == null) return;

        string info = "";
        string colorHex = "#FFFFFF"; 
        if (itemData.rarity == 1) colorHex = "#00FF00"; 
        if (itemData.rarity == 2) colorHex = "#FF00FF"; 
        if (itemData.rarity == 3) colorHex = "#FFD700"; 

        if (itemData.itemType == 1) 
        {
            int dmg = itemData.DamageAtUpgrade(upgradeLevel);
            info = $"<color={colorHex}><b>{itemData.DisplayName}</b></color>\n<color=#AEB9C6>{itemData.WeaponKindName} • x{itemAmount}</color>\nSát thương  <b>{dmg}</b>\nĐộ bền  {currentDurability}%   •   Cường hóa  +{upgradeLevel}\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }
        else if (itemData.itemType == 2) 
        {
            int def = itemData.DefenseAtUpgrade(upgradeLevel);
            info = $"<color={colorHex}><b>{itemData.DisplayName}</b></color>\n<color=#AEB9C6>Giáp • x{itemAmount}</color>\nPhòng thủ  <b>{def}</b>\nĐộ bền  {currentDurability}%   •   Cường hóa  +{upgradeLevel}\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }
        else if (itemData.itemType == 0) 
        {
            info = $"<b>{itemData.DisplayName}</b>\n<color=#AEB9C6>Vật phẩm dùng • x{itemAmount}</color>\nHồi phục  <color=#7DDB88>{itemData.healAmount} HP</color>\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }
        else if (itemData.itemType == 3)
        {
            info = $"<b>{itemData.DisplayName}</b>\n<color=#AEB9C6>Vật phẩm dùng • x{itemAmount}</color>\nHồi phục  <color=#78A9F5>{itemData.manaAmount} Mana</color>\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }
        else if (itemData.itemType == 4) 
        {
            info = $"<color=#E5C45F><b>{itemData.DisplayName}</b></color>\n<color=#AEB9C6>Nguyên liệu • x{itemAmount}</color>\nDùng để cường hóa trang bị\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }
        else if (itemData.itemType == 5)
        {
            info = $"<b>{itemData.DisplayName}</b>\n<color=#AEB9C6>Khoáng sản • x{itemAmount}</color>\nNguyên liệu dùng tại lò rèn\nGiá trị phân rã  <color=#E3B85B>{GetRecycleValue()} vàng</color>";
        }

        if (!isChestSlot && equipSlotType == 0)
            info += itemData.itemType == 0 || itemData.itemType == 3
                ? "\n<color=#D6BC8B>Nhấp trái: dùng • Chuột phải: bán 1 khi mở cửa hàng</color>"
                : "\n<color=#D6BC8B>Chuột phải: bán 1 khi mở cửa hàng</color>";
        ItemTooltip.instance.ShowTooltip(info);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverFrame != null) hoverFrame.gameObject.SetActive(false);
        if (ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip();
    }

    private void OnDisable()
    {
        if (hoverFrame != null) hoverFrame.gameObject.SetActive(false);
        if (slotBeingDragged == this)
        {
            if (ghostIcon != null) Destroy(ghostIcon);
            if (isFull == true && icon != null) 
            {
                icon.color = new Color(1, 1, 1, 1); 
                UpdateAmountUI(); 
            }
            slotBeingDragged = null;
        }
        if (ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip();
    }

    public void AddItem(ItemData data, int amount = 1, int upgrade = 0, int durability = 100)
    {
        InitSlot(); 
        itemData = data;
        isFull = true;
        itemAmount = amount; 
        upgradeLevel = upgrade; 
        currentDurability = durability;
        
        if (icon != null && itemData != null) 
        { 
            icon.sprite = itemData.itemIcon; 
            icon.color = new Color(1, 1, 1, 1); 
            icon.enabled = true; 
        }

        if (slotBackground != null && itemData != null)
        {
            if (useBookStyle)
            {
                if (itemData.rarity == 0) slotBackground.color = Color.white;
                else if (itemData.rarity == 1) slotBackground.color = new Color(0.78f, 1f, 0.85f, 1f);
                else if (itemData.rarity == 2) slotBackground.color = new Color(0.94f, 0.80f, 1f, 1f);
                else if (itemData.rarity == 3) slotBackground.color = new Color(1f, 0.90f, 0.58f, 1f);
            }
            else
            {
                if (itemData.rarity == 0) slotBackground.color = new Color(0.16f, 0.20f, 0.25f, 1f);
                else if (itemData.rarity == 1) slotBackground.color = new Color(0.12f, 0.42f, 0.34f, 1f);
                else if (itemData.rarity == 2) slotBackground.color = new Color(0.38f, 0.22f, 0.55f, 1f);
                else if (itemData.rarity == 3) slotBackground.color = new Color(0.68f, 0.48f, 0.13f, 1f);
            }
        }
        UpdateAmountUI(); 
    }

    public void UpdateAmountUI()
    {
        if (amountBackground != null) amountBackground.enabled = false;
        if (isFull == false || itemData == null) 
        {
            if (amountText != null) { amountText.text = ""; amountText.enabled = false; }
            return;
        }

        if (amountText != null)
        {
            if (itemData.itemType == 1 || itemData.itemType == 2) 
            {
                if (equipSlotType > 0) 
                {
                    amountText.text = currentDurability + "%";
                    amountText.color = (currentDurability <= 30) ? Color.red : Color.green; 
                    amountText.enabled = true;
                }
                else 
                {
                    if (upgradeLevel > 0)
                    {
                        amountText.text = "+" + upgradeLevel;
                        amountText.color = Color.yellow; 
                        amountText.enabled = true; 
                    }
                    else 
                    {
                        amountText.text = "";
                        amountText.enabled = false;
                    }
                }
            }
            else 
            {
                amountText.color = Color.white;
                if (itemAmount > 1) 
                {
                    amountText.text = "x" + itemAmount;
                    amountText.enabled = true; 
                }
                else 
                {
                    amountText.text = "";
                    amountText.enabled = false; 
                }
            }
        }
        if (amountBackground != null) amountBackground.enabled = amountText != null && amountText.enabled && !string.IsNullOrEmpty(amountText.text);
    }

    public void ClearSlot()
    {
        isFull = false;
        itemData = null;
        itemAmount = 0;
        upgradeLevel = 0;
        currentDurability = 100;
        if (amountBackground != null) amountBackground.enabled = false;
        
        if (icon != null) 
        { 
            icon.sprite = null; 
            icon.color = new Color(1, 1, 1, 0); 
            icon.enabled = false; 
        }
        if (slotBackground != null)
            slotBackground.color = useBookStyle ? Color.white : new Color(0.075f, 0.105f, 0.14f, 0.96f);
        
        if (amountText != null) 
        { 
            amountText.text = ""; 
            amountText.enabled = false; 
        }
    }

    public void UseItem()
    {
        if (lastUseFrame == Time.frameCount || slotBeingDragged != null) return;
        lastUseFrame = Time.frameCount;
        if (isFull == true && itemData != null && itemAmount > 0)
        {
            if (equipSlotType == 1 || equipSlotType == 2) 
            {
                return;
            }
            if (equipSlotType > 0) return; 

            StorageChest chest = FindAnyObjectByType<StorageChest>(FindObjectsInactive.Exclude);
            if (chest != null && chest.isChestOpen && chest.chestPanel != null && chest.chestPanel.activeInHierarchy)
            {
                if (isChestSlot == false) 
                {
                    if (chest.AddItemToChest(itemData, itemAmount, upgradeLevel, currentDurability)) ClearSlot();
                }
                else 
                {
                    PlayerInventory inv = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
                    if (inv != null && inv.AddItemToBag(itemData, itemAmount, upgradeLevel, currentDurability)) ClearSlot();
                }
                return; 
            }

            if (isChestSlot == false)
            {
                if (itemData.itemType == 3)
                {
                    GameObject manaPlayerObject = GameObject.FindGameObjectWithTag("Player");
                    PlayerInventory manaInventory = manaPlayerObject != null ? manaPlayerObject.GetComponent<PlayerInventory>() : null;
                    if (manaInventory != null && manaInventory.TryUseManaPotion(itemData.manaAmount)) ConsumeOne(manaInventory);
                    return;
                }
                if (itemData.itemType > 0) return;
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                PlayerInventory inv = playerObject != null ? playerObject.GetComponent<PlayerInventory>() : null;
                if (inv != null && inv.TryUsePotion(itemData.healAmount)) ConsumeOne(inv);
            }
        }
    }

    private void ConsumeOne(PlayerInventory inventory)
    {
        itemAmount--;
        if (itemAmount <= 0) ClearSlot(); else UpdateAmountUI();
        inventory.RefreshCurrencyUI();
        ItemTooltip.instance?.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || lastSaleFrame == Time.frameCount ||
            isChestSlot || equipSlotType != 0 || !isFull || itemData == null || itemAmount <= 0 || slotBeingDragged != null) return;
        NPCShop merchant = null;
        foreach (NPCShop candidate in FindObjectsByType<NPCShop>())
            if (candidate.isShopOpen && candidate.shopPanel != null && candidate.shopPanel.activeInHierarchy) { merchant = candidate; break; }
        if (merchant == null) return;
        PlayerInventory inventory = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerInventory>();
        if (inventory == null) return;
        lastSaleFrame = Time.frameCount;
        int price = Mathf.Max(0, itemData.basePrice + upgradeLevel * 15) * (merchant.isVIP ? 2 : 1);
        string soldName = itemData.DisplayName;
        inventory.AddGold(price);
        ConsumeOne(inventory);
        GameAudioManager.PlayUI(GameSound.Purchase, .65f, .95f);
        FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include)?.ShowFeedback("Đã bán 1 " + soldName + ": +" + price + " vàng");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFull == false || itemData == null) return; 
        slotBeingDragged = this; 

        if (ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip(); 

        ghostIcon = new GameObject("GhostIcon");
        ghostIcon.transform.SetParent(GetComponentInParent<Canvas>().transform); 
        ghostIcon.transform.SetAsLastSibling(); 

        Image ghostImg = ghostIcon.AddComponent<Image>();
        ghostImg.sprite = icon.sprite;
        ghostImg.raycastTarget = false; 

        RectTransform ghostRect = ghostIcon.GetComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(80, 80); 

        icon.color = new Color(1, 1, 1, 0); 
        if (amountText != null) amountText.enabled = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) ghostIcon.transform.position = Input.mousePosition; 
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null) Destroy(ghostIcon);

        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        bool droppedOnSlot = (target != null && target.GetComponentInParent<InventorySlot>() != null);

        if (droppedOnSlot == false && this.isFull == true && this.itemData != null)
        {
            if (this.equipSlotType > 0) {
                icon.color = new Color(1, 1, 1, 1);
                UpdateAmountUI();
                slotBeingDragged = null;
                return;
            }
            PlayerInventory inv = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
            
            if (inv != null)
            {
                inv.DropItemToGround(this.itemData, this.itemAmount, this.upgradeLevel, this.currentDurability);
                this.ClearSlot();
            }
            else if (icon != null)
            {
                icon.color = Color.white;
                UpdateAmountUI();
            }
        }
        else
        {
            if (isFull == true && icon != null) 
            {
                icon.color = new Color(1, 1, 1, 1); 
                UpdateAmountUI(); 
            }
        }
        slotBeingDragged = null; 
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot dragged = slotBeingDragged;

        if (dragged != null && dragged != this && dragged.itemData != null)
        {
            if (this.equipSlotType == 99)
            {
                PlayerInventory pInv = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Exclude);
                if (pInv != null)
                {
                    int scrapGold = dragged.GetRecycleValue();
                    int totalGold = scrapGold * dragged.itemAmount;
                    
                    pInv.goldCount += totalGold;
                    if (pInv.goldText != null) pInv.goldText.text = pInv.goldCount.ToString();
                    string recycledName = dragged.itemData.DisplayName;
                    int recycledAmount = dragged.itemAmount;
                    dragged.ClearSlot();
                    GameAudioManager.PlayUI(GameSound.Purchase, 0.7f, 0.88f);
                    if (ItemTooltip.instance != null)
                        ItemTooltip.instance.ShowTooltip($"<b>ĐÃ PHÂN RÃ</b>\n{recycledName}  x{recycledAmount}\nNhận <color=#E3B85B>{totalGold} vàng</color>");
                    Debug.Log($"Đã phân rã {recycledName} x{recycledAmount}, nhận {totalGold} vàng.");
                }
                return; 
            }

            if (dragged.itemData.itemType == 4 && (this.itemData != null && (this.itemData.itemType == 1 || this.itemData.itemType == 2)))
            {
                if (upgradeLevel >= ItemData.MaximumUpgrade)
                {
                    GameAudioManager.PlayUI(GameSound.UIError, .6f);
                    ItemTooltip.instance?.ShowTooltip("Trang bị đã đạt cường hóa tối đa +5. Không mất đá.");
                    return;
                }
                float successRate = (this.upgradeLevel < 3) ? 1.0f : 0.5f;
                bool upgraded = Random.value <= successRate;
                if (upgraded) this.upgradeLevel++;
                GameAudioManager.PlayUI(upgraded ? GameSound.Blacksmith : GameSound.UIError,
                    0.78f, upgraded ? 1.08f : 0.82f);
                
                this.UpdateAmountUI();
                dragged.itemAmount--;
                dragged.UpdateAmountUI();
                if (dragged.itemAmount <= 0) dragged.ClearSlot();
                return; 
            }

            if (this.equipSlotType > 0 && dragged.itemData.itemType != this.equipSlotType) return;
            if (this.equipSlotType == 1 && !PlayerClassController.CanUseWeapon(dragged.itemData))
            {
                Debug.Log(PlayerClassController.IsMageSelected
                    ? "Pháp sư chỉ có thể trang bị gậy."
                    : "Hiệp sĩ chỉ có thể trang bị kiếm.");
                return;
            }
            if (dragged.equipSlotType > 0 && this.isFull == true && this.itemData != null && this.itemData.itemType != dragged.equipSlotType) return;

            if (this.isFull && dragged.isFull && this.itemData == dragged.itemData && this.upgradeLevel == dragged.upgradeLevel && this.currentDurability == dragged.currentDurability)
            {
                int total = this.itemAmount + dragged.itemAmount;
                if (total <= 99)
                {
                    this.itemAmount = total;
                    this.UpdateAmountUI();
                    dragged.ClearSlot(); 
                }
                else
                {
                    int excess = total - 99;
                    this.itemAmount = 99;
                    this.UpdateAmountUI();
                    dragged.itemAmount = excess;
                    dragged.UpdateAmountUI();
                }
                return; 
            }

            bool myFull = this.isFull;
            ItemData myData = this.itemData;
            int myAmount = this.itemAmount;
            int myUpgrade = this.upgradeLevel; 
            int myDura = this.currentDurability; 

            this.AddItem(dragged.itemData, dragged.itemAmount, dragged.upgradeLevel, dragged.currentDurability);
            if (this.equipSlotType > 0 || dragged.equipSlotType > 0)
                GameAudioManager.PlayUI(GameSound.Equip, 0.78f, 1.04f);
            
            if (myFull == true) dragged.AddItem(myData, myAmount, myUpgrade, myDura);
            else dragged.ClearSlot(); 
        }
    }

    private int GetRecycleValue()
    {
        if (itemData == null) return 0;
        return Mathf.Max(1, itemData.basePrice / 2 + upgradeLevel * 10);
    }
}
