using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [Header("DANH SÁCH VẬT PHẨM RỚT RA (Kéo File ItemData vào đây)")]
    public ItemData potionData;
    public ItemData[] swordDataList;
    public ItemData[] staffDataList;
    public ItemData[] armorDataList; 

    public static bool TryResolveDropPosition(Vector3 requested, out Vector3 position)
    {
        MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
        position = requested;
        return layout == null || layout.TryResolvePickupPosition(requested, out position);
    }

    // HÀM NÀY SẼ CHẠY KHI QUÁI VẬT TẮT THỞ
    public void DieAndDropLoot()
    {
        int roll = Random.Range(1, 101); // Quay số xui rủi từ 1 đến 100
        ItemData dropData = null;

        if (roll <= 40) // 40% Tỷ lệ rớt Bình Máu
        {
            dropData = potionData;
        }
        else if (roll <= 70) // 30% Tỷ lệ rớt Kiếm
        {
            ItemData[] classWeapons = PlayerClassController.IsMageSelected ? staffDataList : swordDataList;
            if (classWeapons != null && classWeapons.Length > 0)
                dropData = classWeapons[Random.Range(0, classWeapons.Length)];
        }
        else // 30% Tỷ lệ rớt Giáp
        {
            if (armorDataList != null && armorDataList.Length > 0)
                dropData = armorDataList[Random.Range(0, armorDataList.Length)];
        }

        if (dropData == null || !TryResolveDropPosition(transform.position, out Vector3 dropPosition))
        {
            Destroy(gameObject);
            return;
        }

        // Tự động đẻ ra vật phẩm rớt dưới đất ngay chỗ quái chết
        GameObject lootObj = new GameObject("Loot_" + dropData.itemName);
        lootObj.transform.position = dropPosition;
        
        SpriteRenderer sr = lootObj.AddComponent<SpriteRenderer>();
        sr.sprite = dropData.itemIcon; // Lấy hình ảnh từ File Data
        sr.sortingOrder = 5;

        BoxCollider2D box = lootObj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        lootObj.tag = "Item"; 

        // Đóng dấu kiểm định để Balo biết đây là cái gì
        MonsterLootData data = lootObj.AddComponent<MonsterLootData>();
        data.itemData = dropData; // Truyền toàn bộ hồ sơ File Data vào

        // Xóa sổ con quái
        Destroy(gameObject);
    }
}

// Thẻ kiểm định (Code để Balo check xem lụm được cái gì)
public class MonsterLootData : MonoBehaviour
{
    public ItemData itemData;
    public int upgradeLevel;
    public int durability = 100;
}
