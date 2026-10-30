using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("Chỗ chứa Hình ảnh Áo Giáp")]
    public SpriteRenderer bodyRenderer; // Nơi hiển thị áo giáp trên thân người
    
    [Header("Kho Đồ Trang Bị")]
    public Sprite armorSprite1; // Để kéo thả ảnh Armor_1 vào
    public Sprite armorSprite3; // Để kéo thả ảnh Armor_3 (Giáp xịn) vào
    private PlayerCombat combat;
    private PlayerClassController playerClass;
    private GameObject weaponAvatar;
    private SpriteRenderer[] weaponRenderers;
    private bool wasScripted;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        playerClass = GetComponent<PlayerClassController>();
    }

    private void LateUpdate()
    {
        if (playerClass == null) playerClass = GetComponent<PlayerClassController>();
        GameObject avatar = playerClass != null ? playerClass.ActiveAvatarObject : null;
        if (avatar == null || combat == null) return;
        if (weaponAvatar != avatar)
        {
            weaponAvatar = avatar;
            weaponRenderers = System.Array.FindAll(avatar.GetComponentsInChildren<SpriteRenderer>(true),
                r => r.name == "R_Weapon" || r.name == "L_Weapon");
        }
        // Restore story weapons once, then let the cutscene hide/dissolve its renderers freely.
        bool scripted = !combat.enabled || CinematicPresentationController.IsCinematicActive;
        if (scripted)
        {
            if (!wasScripted && System.Array.Exists(avatar.GetComponentsInChildren<SpriteRenderer>(true),
                r => r.name != "R_Weapon" && r.name != "L_Weapon" && r.enabled && r.sprite != null && r.gameObject.activeInHierarchy))
                SetWeaponVisibility(true);
            wasScripted = true;
            return;
        }
        wasScripted = false;
        SetWeaponVisibility(combat.FindEquippedWeapon(combat.IsMage ? WeaponKind.Staff : WeaponKind.Sword) != null);
    }

    private void SetWeaponVisibility(bool held)
    {
        foreach (SpriteRenderer renderer in weaponRenderers)
            if (renderer != null && renderer.sprite != null) renderer.enabled = held;
    }

    public void SetBodyRenderer(SpriteRenderer renderer)
    {
        if (renderer != null) bodyRenderer = renderer;
    }

    void Update()
    {
        // Bấm phím 1 để mặc áo giáp cùi
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipArmor1();
        
        // Bấm phím 2 để mặc áo giáp sắt xịn
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipArmor3();
    }

    // Hàm gọi khi mặc Giáp Thường (Armor 1)
        // Hàm gọi khi mặc Giáp Thường (Armor 1)
    public void EquipArmor1()
    {
        if (bodyRenderer != null && armorSprite1 != null)
        {
            bodyRenderer.sprite = armorSprite1; // Thay áo!
        }

        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.physicalArmor = 0; // 👉 SỬA SỐ 1 THÀNH SỐ 0 Ở DÒNG NÀY NÈ!
            hp.magicResistance = 0;
            Debug.Log("Đã mặc Áo Vải mỏng! (Giáp 0)");
        }
    }

    // Hàm gọi khi mặc Giáp Sắt (Armor 3)
    public void EquipArmor3()
    {
        if (bodyRenderer != null && armorSprite3 != null)
        {
            bodyRenderer.sprite = armorSprite3; // Thay áo!
        }

        PlayerHealth hp = GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.physicalArmor = 3;   // Cộng 3 Giáp vật lý
            hp.magicResistance = 2; // Cộng 2 Kháng phép
            Debug.Log("Đã mặc Giáp Hiệp Sĩ! (Giáp +3, Kháng Phép +2)");
        }
    }
}
