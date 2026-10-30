using TMPro;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public ItemData potionData;
    public ItemData manaPotionData;
    public int potionPrice = 10;
    public int manaPotionPrice = 12;
    public TextMeshProUGUI feedbackText;
    private int lastPurchaseFrame = -1;

    public void BuyHealthPotion() => Buy(potionData, potionPrice, "Bình máu");
    public void BuyManaPotion() => Buy(manaPotionData, manaPotionPrice, "Bình mana");

    private void Buy(ItemData item, int price, string displayName)
    {
        // Một Button từng bị gắn nhiều persistent listener trong scene. Chặn toàn bộ
        // callback trùng của cùng một cú nhấn, nên mỗi click chỉ có đúng một giao dịch.
        if (lastPurchaseFrame == Time.frameCount) return;
        lastPurchaseFrame = Time.frameCount;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerInventory inventory = playerObject != null ? playerObject.GetComponent<PlayerInventory>() : null;
        if (inventory == null) return;
        if (item != null && item.itemType == 1 && !PlayerClassController.CanUseWeapon(item))
        {
            GameAudioManager.PlayUI(GameSound.UIError, .65f);
            Show(PlayerClassController.IsMageSelected ? "Pháp sư sử dụng gậy phép, không mua kiếm." : "Hiệp sĩ sử dụng kiếm, không mua gậy phép.");
            return;
        }
        if (item == null)
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.72f);
            Show(displayName + " chưa được gán dữ liệu.");
            return;
        }
        if (inventory.goldCount < price)
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.72f);
            Show("Số vàng đó chưa đủ. Ta là thương nhân chứ không phải nhà từ thiện đâu nhé.");
            return;
        }
        if (!inventory.AddItemToBag(item))
        {
            GameAudioManager.PlayUI(GameSound.UIError, 0.72f);
            Show("Túi của ngươi đầy rồi. Hãy dọn chỗ trước khi mua thêm.");
            return;
        }

        inventory.AddGold(-price);
        inventory.RefreshCurrencyUI();
        GameAudioManager.PlayUI(GameSound.Purchase, 0.82f, 1.08f);
        Show("Giao dịch hoàn tất. Hãy sử dụng " + displayName + " đúng lúc.");
    }

    private void Show(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        Debug.Log("Thương nhân: " + message);
    }
}
