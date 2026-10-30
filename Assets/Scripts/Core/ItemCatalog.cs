using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "RPG/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public ItemData[] items = new ItemData[0];

    private static ItemCatalog cached;

    public static ItemCatalog Load()
    {
        if (cached == null) cached = Resources.Load<ItemCatalog>("ItemCatalog");
        return cached;
    }

    public ItemData Find(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;

        foreach (ItemData item in items)
        {
            if (item != null && item.name == itemId) return item;
        }

        foreach (ItemData item in Resources.FindObjectsOfTypeAll<ItemData>())
        {
            if (item != null && item.name == itemId) return item;
        }

        Debug.LogWarning("Không tìm thấy dữ liệu vật phẩm: " + itemId);
        return null;
    }

    public static string GetId(ItemData item)
    {
        return item != null ? item.name : string.Empty;
    }
}
