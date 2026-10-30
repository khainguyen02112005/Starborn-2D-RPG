using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public sealed class PlayerClassController : MonoBehaviour
{
    [Range(.8f,1.4f)] public float visualScale = 1.16f;
    private const string KnightResourcePath = "Addons/BasicPack/2_Prefab/Human/SPUM_20240911215638389";
    private const string MageResourcePath = "Addons/BasicPack/2_Prefab/Human/SPUM_20240911215639748";
    private const string KnightAvatarName = "KnightAvatar";
    private const string MageAvatarName = "MageAvatar";

    [Header("Separate class avatars")]
    [SerializeField] private GameObject knightAvatarObject;
    [SerializeField] private GameObject mageAvatarObject;

    public GameObject ActiveAvatarObject { get; private set; }

    public static bool IsMageSelected
    {
        get
        {
            string selected = PlayerPrefs.GetString("PlayerClass", "Hiệp Sĩ");
            return selected.IndexOf("Pháp", StringComparison.OrdinalIgnoreCase) >= 0
                || selected.IndexOf("Phap", StringComparison.OrdinalIgnoreCase) >= 0
                || selected.IndexOf("Mage", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public static bool CanUseWeapon(ItemData item)
    {
        if (item == null || item.itemType != 1) return false;
        return IsMageSelected
            ? item.weaponKind == WeaponKind.Staff
            : item.weaponKind == WeaponKind.Sword;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachAfterInitialSceneLoad()
    {
        AttachToScenePlayers();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToScenePlayers();
    }

    private static void AttachToScenePlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player != null && player.GetComponent<PlayerClassController>() == null)
                player.AddComponent<PlayerClassController>();
        }
    }

    private void Awake()
    {
        bool isMage = IsMageSelected;
        UpdateCharacterTitle();
        SetupSeparateAvatars(isMage);
    }

    private void Start()
    {
        // Re-apply once every component has finished Awake. This prevents both
        // class avatars from staying disabled because of scene/prefab state.
        EnsureSelectedAvatarVisible(IsMageSelected);
        UpdateCharacterTitle();
    }

    private void SetupSeparateAvatars(bool isMage)
    {
        knightAvatarObject = ResolveAvatar(knightAvatarObject, KnightAvatarName, KnightResourcePath);
        mageAvatarObject = ResolveAvatar(mageAvatarObject, MageAvatarName, MageResourcePath);

        SPUM_Prefabs rootTemplate = GetComponent<SPUM_Prefabs>();
        HideRootTemplate(rootTemplate);

        EnsureSelectedAvatarVisible(isMage);

        PlayerEquipment equipment = GetComponent<PlayerEquipment>();
        if (equipment != null && ActiveAvatarObject != null)
        {
            SpriteRenderer armorRenderer = FindRenderer(ActiveAvatarObject.transform, "BodyArmor");
            if (armorRenderer != null) equipment.SetBodyRenderer(armorRenderer);
        }
    }

    private void EnsureSelectedAvatarVisible(bool isMage)
    {
        GameObject selectedAvatar = isMage ? mageAvatarObject : knightAvatarObject;
        GameObject hiddenAvatar = isMage ? knightAvatarObject : mageAvatarObject;

        if (hiddenAvatar != null) hiddenAvatar.SetActive(false);
        if (selectedAvatar == null) return;

        ResetAvatarTransform(selectedAvatar.transform);
        selectedAvatar.SetActive(true);
        ActiveAvatarObject = selectedAvatar;

        SPUM_Prefabs activeAvatar = GetAvatarComponent(ActiveAvatarObject);
        if (activeAvatar != null)
        {
            activeAvatar.enabled = true;
            if (activeAvatar._anim != null) activeAvatar._anim.enabled = true;
        }

        // Only repair renderers when the entire selected avatar is invisible.
        // This keeps intentional per-part visibility choices made by SPUM intact.
        SpriteRenderer[] renderers = ActiveAvatarObject.GetComponentsInChildren<SpriteRenderer>(true);
        bool hasVisibleRenderer = false;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy
                && renderer.sprite != null && renderer.color.a > 0.001f)
            {
                hasVisibleRenderer = true;
                break;
            }
        }

        if (!hasVisibleRenderer)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null) continue;
                renderer.enabled = true;
                Color color = renderer.color;
                if (color.a <= 0.001f) renderer.color = new Color(color.r, color.g, color.b, 1f);
            }
        }

        GetComponent<PlayerSPUMAnimation>()?.SetAvatar(activeAvatar);
    }

    private GameObject ResolveAvatar(GameObject assigned, string objectName, string resourcePath)
    {
        if (assigned != null) return assigned;

        Transform existing = transform.Find(objectName);
        if (existing != null) return existing.gameObject;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError("Không tìm thấy SPUM avatar tại Resources/" + resourcePath, this);
            return null;
        }

        GameObject avatar = Instantiate(prefab, transform);
        avatar.name = objectName;
        ResetAvatarTransform(avatar.transform);
        return avatar;
    }

    private void ResetAvatarTransform(Transform avatarTransform)
    {
        if (avatarTransform == null) return;

        avatarTransform.localPosition = Vector3.zero;
        avatarTransform.localRotation = Quaternion.identity;
        avatarTransform.localScale = Vector3.one * visualScale;

        // SPUM roots use RectTransform. Its anchored position can keep an old
        // UI offset even when localPosition was reset, pushing the sprite far
        // outside the gameplay camera.
        if (avatarTransform is RectTransform rectTransform)
            rectTransform.anchoredPosition = Vector2.zero;
    }

    private void HideRootTemplate(SPUM_Prefabs rootTemplate)
    {
        if (rootTemplate == null) return;

        foreach (SpriteRenderer renderer in rootTemplate.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || IsPartOfAvatar(renderer.transform, knightAvatarObject)
                || IsPartOfAvatar(renderer.transform, mageAvatarObject))
                continue;
            renderer.enabled = false;
        }

        if (rootTemplate._anim != null) rootTemplate._anim.enabled = false;
        rootTemplate.enabled = false;
    }

    private static bool IsPartOfAvatar(Transform candidate, GameObject avatar)
    {
        return avatar != null && candidate.IsChildOf(avatar.transform);
    }

    private static SPUM_Prefabs GetAvatarComponent(GameObject avatarObject)
    {
        if (avatarObject == null) return null;
        SPUM_Prefabs avatar = avatarObject.GetComponent<SPUM_Prefabs>();
        return avatar != null ? avatar : avatarObject.GetComponentInChildren<SPUM_Prefabs>(true);
    }

    private static void UpdateCharacterTitle()
    {
        GameObject titleObject = GameObject.Find("KnightTitle");
        TextMeshProUGUI title = titleObject != null
            ? titleObject.GetComponent<TextMeshProUGUI>()
            : null;
        if (title != null) title.text = PlayerIdentity.Literal(PlayerIdentity.Name);
    }

    private static SpriteRenderer FindRenderer(Transform root, string objectName)
    {
        foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer != null && renderer.gameObject.name == objectName) return renderer;
        return null;
    }
}
