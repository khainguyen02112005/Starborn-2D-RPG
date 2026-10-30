using UnityEngine;

public enum WeaponKind
{
    None = 0,
    Sword = 1,
    Staff = 2
}

[CreateAssetMenu(fileName = "Vật Phẩm Mới", menuName = "RPG/Vật Phẩm")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    [Tooltip("0 = Bình Máu, 1 = Vũ Khí, 2 = Áo Giáp, 3 = Bình Mana, 4 = Đá Cường Hóa, 5 = Khoáng Sản")]
    public int itemType;
    [Tooltip("Chỉ dùng khi Item Type = 1: Sword cho Hiệp Sĩ, Staff cho Pháp Sư.")]
    public WeaponKind weaponKind;
    [Range(0, 3)] public int rarity;
    public int baseDamage;
    public int baseDefense;
    public int healAmount;
    public int manaAmount;
    public int basePrice;
    public const int MaximumUpgrade = 5;
    public int DamageAtUpgrade(int level) => Mathf.Max(0, baseDamage) + Mathf.Clamp(level, 0, MaximumUpgrade);
    public int DefenseAtUpgrade(int level) => Mathf.Max(0, baseDefense) + Mathf.Clamp(level, 0, MaximumUpgrade) / 2;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(itemName)) return itemName.Trim();

            switch (itemType)
            {
                case 0: return "Bình Máu";
                case 1: return "Kiếm Sắt";
                case 2: return "Giáp Sắt";
                case 3: return "Bình Mana";
                case 4: return "Đá Cường Hóa";
                case 5: return "Sắt Thô";
                default: return string.IsNullOrWhiteSpace(name) ? "Vật phẩm" : name;
            }
        }
    }

    public string WeaponKindName
    {
        get
        {
            if (weaponKind == WeaponKind.Sword) return "Kiếm • Hiệp Sĩ";
            if (weaponKind == WeaponKind.Staff) return "Gậy • Pháp Sư";
            return "Vũ khí";
        }
    }
}
