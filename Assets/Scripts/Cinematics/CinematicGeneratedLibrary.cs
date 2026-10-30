using UnityEngine;

[CreateAssetMenu(fileName = "CinematicGeneratedLibrary", menuName = "RPG/Cinematic Generated Library")]
public sealed class CinematicGeneratedLibrary : ScriptableObject
{
    [Header("NHÂN VẬT CUTSCENE ĐƯỢC TẠO THÊM")]
    public GameObject knightPrefab;
    public GameObject magePrefab;
    public GameObject skeletonPrefab;
    public GameObject bossPrefab;
    public GameObject messengerPrefab;

    [Header("VFX PIXEL")]
    public GameObject combatVfxPrefab;
    public GameObject cutsceneOverlayPrefab;

    [Header("ĐẠO CỤ")]
    public Sprite royalDecreeSprite;

    [Header("HIỆU ỨNG CUTSCENE MỚI")]
    public Texture2D digitalCorruptionTexture;
    public Material digitalCorruptionMaterial;
    public GameObject fireworkPrefab;
    public GameObject knightEmotionPrefab;
    public GameObject mageEmotionPrefab;
    public GameObject warningFireballPrefab;
    public GameObject warningImpactPrefab;
    [Header("LỖI PIXEL BÁM THEO NHÂN VẬT — REVISION 03")]
    public GameObject knightCorruptionPrefab;
    public GameObject mageCorruptionPrefab;
    public GameObject elderCorruptionPrefab;
    public GameObject pixelFragmentsPrefab;
}
