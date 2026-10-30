#pragma warning disable CS0618

using UnityEngine;
using TMPro;
using System.Collections.Generic; 

public class PlayerInventory : MonoBehaviour
{
    public int potionCount = 0; 
    public TextMeshProUGUI potionText; 
    public int goldCount = 0;         
    public TextMeshProUGUI goldText;  
    public GameObject inventoryPanel; 
    public InventorySlot[] slots; 
    public GameObject characterPanel; 

    [Header("LÒ PHÂN RÃ (Kéo Slot 9 vào đây)")]
    public GameObject recycleBin; 

    public class DroppedItemData
    {
        public ItemData data;
        public int upgrade;
        public int dura;
    }
    private Dictionary<int, DroppedItemData> groundItems = new Dictionary<int, DroppedItemData>();

    void Update()
    {
        if (GameplayUiFocus.HasDialogue || CinematicPresentationController.IsCinematicActive || Time.timeScale <= 0f) return;
        if (GameplayUiFocus.HasEndingChoice) return;
        if (Input.GetKeyDown(KeyCode.V))
        {
            var book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
            if (book != null) book.OpenStats();
            return;
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            BookInventoryUI book = FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include);
            if (book != null)
            {
                book.ToggleInventory();
                return;
            }

            if (inventoryPanel == null) return;
            bool isOpening = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isOpening);
            if (characterPanel != null) characterPanel.SetActive(isOpening);
            if (recycleBin != null) recycleBin.SetActive(isOpening);
            if (!isOpening && ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip();
        }
    }

    void OnTriggerEnter2D(Collider2D item)
    {
        if (item.CompareTag("Item"))
        {
            int id = item.gameObject.GetInstanceID();
            
            if (groundItems.ContainsKey(id))
            {
                DroppedItemData dropped = groundItems[id];
                if (AddItemToBag(dropped.data, 1, dropped.upgrade, dropped.dura))
                {
                    GameAudioManager.Play(GameSound.ItemPickup, transform.position, 0.8f, Random.Range(0.96f, 1.1f));
                    groundItems.Remove(id); 
                    Destroy(item.gameObject);
                }
            }
            else
            {
                MonsterLootData lootInfo = item.GetComponent<MonsterLootData>();
                if (lootInfo != null && lootInfo.itemData != null)
                {
                    if (AddItemToBag(lootInfo.itemData, 1, lootInfo.upgradeLevel, lootInfo.durability))
                    {
                        GameAudioManager.Play(GameSound.ItemPickup, transform.position, 0.8f, Random.Range(0.96f, 1.1f));
                        Destroy(item.gameObject);
                    }
                }
            }
        }
        else if (item.CompareTag("Coin"))
        {
            goldCount += 10; 
            GameAudioManager.Play(GameSound.ItemPickup, transform.position, 0.65f, 1.18f);
            if (goldText != null) goldText.text = goldCount.ToString(); 
            Destroy(item.gameObject); 
        }
    }

    public bool AddItemToBag(ItemData data, int amount = 1, int upgrade = 0, int durability = 100)
    {
        if (data == null || amount <= 0 || slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isFull == true && slots[i].itemData == data && slots[i].upgradeLevel == upgrade && slots[i].currentDurability == durability && slots[i].itemAmount < 99)
            {
                slots[i].itemAmount += amount; 
                slots[i].UpdateAmountUI();
                RefreshCurrencyUI();
                return true;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].isFull == false) 
            {
                slots[i].AddItem(data, amount, upgrade, durability); 
                RefreshCurrencyUI();
                return true;                
            }
        }
        Debug.Log("BALO ĐÃ ĐẦY ẮP!");
        return false; 
    }

    public void UsePotion(int healAmount)
    {
        TryUsePotion(healAmount);
    }

    public bool TryUsePotion(int healAmount)
    {
        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (hp == null || hp.IsDead || healAmount <= 0) return false;
        if (hp.currentHealth >= hp.maxHealth) { PotionFeedback("Máu đã đầy — không mất bình thuốc.", false); return false; }
        int previous = hp.currentHealth;
        hp.TakeDamage(-healAmount);
        GameAudioManager.PlayUI(GameSound.UIConfirm, 0.68f, 1.16f);
        PotionFeedback("Đã dùng bình máu: +" + (hp.currentHealth - previous) + " HP", true);
        return hp.currentHealth > previous;
    }

    public void DropItemToGround(ItemData data, int amount, int upgrade, int dura)
    {
        if (data == null || amount <= 0) return;
        StartCoroutine(SpawnDroppedItems(data, amount, upgrade, dura)); 
    }

    public void UseManaPotion(int manaAmount)
    {
        TryUseManaPotion(manaAmount);
    }

    public bool TryUseManaPotion(int manaAmount)
    {
        PlayerMana mana = GetComponent<PlayerMana>();
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (mana == null || (health != null && health.IsDead) || manaAmount <= 0) return false;
        if (mana.currentMana >= mana.maxMana) { PotionFeedback("Mana đã đầy — không mất bình thuốc.", false); return false; }
        int previous = mana.currentMana;
        mana.RestoreMana(manaAmount);
        GameAudioManager.PlayUI(GameSound.UIConfirm, 0.68f, 1.08f);
        PotionFeedback("Đã dùng bình mana: +" + (mana.currentMana - previous) + " MP", true);
        return mana.currentMana > previous;
    }

    private void PotionFeedback(string message, bool success)
    {
        FindAnyObjectByType<BookInventoryUI>(FindObjectsInactive.Include)?.ShowFeedback(message);
        if (!success) GameAudioManager.PlayUI(GameSound.UIError, .45f);
    }

    public void AddGold(int amount)
    {
        goldCount = Mathf.Max(0, goldCount + amount);
        RefreshCurrencyUI();
    }

    public void RefreshCurrencyUI()
    {
        potionCount = 0;
        if (slots != null)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot != null && slot.isFull && slot.itemData != null && slot.itemData.itemType == 0)
                    potionCount += Mathf.Max(0, slot.itemAmount);
            }
        }
        if (goldText != null) goldText.text = goldCount.ToString();
        if (potionText != null) potionText.text = "x" + potionCount;
    }

    private System.Collections.IEnumerator SpawnDroppedItems(ItemData data, int amount, int upgrade, int dura)
    {
        for (int i = 0; i < amount; i++)
        {
            float ranX = Random.Range(-1.5f, 1.5f);
            float ranY = Random.Range(-1.5f, 1.5f);
            if (!EnemyLoot.TryResolveDropPosition(transform.position + new Vector3(ranX, ranY, 0), out Vector3 dropPosition)) yield break;
            GameObject droppedItem = new GameObject("DroppedItem");
            droppedItem.transform.position = dropPosition;
            
            SpriteRenderer sr = droppedItem.AddComponent<SpriteRenderer>();
            sr.sprite = data.itemIcon;
            sr.sortingOrder = 5; 
            
            BoxCollider2D box = droppedItem.AddComponent<BoxCollider2D>();
            box.isTrigger = true; 
            droppedItem.tag = "Item"; 
            MonsterLootData savedLoot = droppedItem.AddComponent<MonsterLootData>();
            savedLoot.itemData = data;
            savedLoot.upgradeLevel = upgrade;
            savedLoot.durability = dura;
            
            int id = droppedItem.GetInstanceID();
            groundItems[id] = new DroppedItemData { 
                data = data, upgrade = upgrade, dura = dura 
            };
            
            box.enabled = false; 
            StartCoroutine(EnableColliderAfterDelay(box, 1.5f)); 
            
            yield return new WaitForSeconds(0.05f);
        }
    }

    private System.Collections.IEnumerator EnableColliderAfterDelay(BoxCollider2D box, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (box != null) box.enabled = true; 
    }
}
