using UnityEngine;

[CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "RPG/Audio Library")]
public class GameAudioLibrary : ScriptableObject
{
    [Header("Music and ambience")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    public AudioClip loadingMusic;
    public AudioClip bossMusic;
    public AudioClip ambientWind;

    [Header("Mix (0 = im lặng, 1 = tối đa)")]
    [Range(0f, 1f)] public float musicVolume = 0.07f;
    [Range(0f, 1f)] public float ambienceVolume = 0f;
    [Range(0f, 1f)] public float uiVolume = 0.48f;
    [Range(0f, 1f)] public float worldVolume = .55f;

    [Header("UI")]
    public AudioClip uiHover;
    public AudioClip uiClick;
    public AudioClip uiConfirm;
    public AudioClip uiCancel;
    public AudioClip uiError;
    public AudioClip uiToggle;
    public AudioClip dialogue;
    public AudioClip questComplete;

    [Header("Player")]
    public AudioClip[] footsteps;
    public AudioClip swordSwing;
    public AudioClip mageAttack;
    public AudioClip dash;
    public AudioClip playerHit;
    public AudioClip playerDeath;
    public AudioClip block;
    public AudioClip revive;

    [Header("Items and interactions")]
    public AudioClip itemPickup;
    public AudioClip equip;
    public AudioClip purchase;
    public AudioClip chestOpen;
    public AudioClip shopOpen;
    public AudioClip blacksmith;
    public AudioClip portal;

    [Header("Enemies")]
    public AudioClip enemyAttack;
    public AudioClip enemyHit;
    public AudioClip enemyDeath;

    [Header("Boss and magic")]
    public AudioClip bossRoar;
    public AudioClip bossCast;
    public AudioClip bossLightning;
    public AudioClip bossFire;
    public AudioClip bossWater;
    public AudioClip bossHit;
    public AudioClip bossDeath;
    public AudioClip aoeWarning;
    public AudioClip aoeImpact;

    [Header("Cursor battle")]
    public AudioClip cursorShot;
    public AudioClip cursorHit;
}
