using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum TimelineCinematicAction
{
    Caption,
    HideCaption,
    TelevisionGlitch,
    FireballToCursor,
    WishCelebration,
    CameraShakeLight,
    CameraShakeHeavy,
    ShowChoice,
    ShowHappyEnding,
    CorruptHappyEnding,
    ShowLoopEnding,
    ShowResetEnding,
    MaterializeReset,
    EraseWorldToWhite,
    ShowWhiteVoidEnding,
    KnightMagic,
    KnightMelee,
    KnightRevive,
    KnightMoveStart,
    KnightMoveStop,
    KnightDamaged,
    KnightDeath,
    KnightSelfDestruct,
    OpeningStrike,
    OpeningMonsterAttack,
    OpeningMonsterDeath,
    DayLaterTransition,
    MessengerRunStart,
    MessengerRunStop,
    MessengerHandoff,
    CameraWide,
    CameraHero,
    CameraElder,
    CameraBoss,
    CameraMessenger,
    CameraRestore,
    HeroEmotion,
    FreezeVillage,
    HappyVillageMontage,
    RewardBurst,
    PlaySwordVFX,
    PlayMagicVFX,
    PlayFireVFX,
    PlayLightningVFX,
    PlayWaterVFX,
    PlayCorruptionVFX,
    PlayGlassCrackVFX,
    PlayGlassShatterVFX,
    PlayPixelEraseVFX,
    PlayReconstructVFX,
    HeroTremble,
    HeroDespair,
    HeroOriginal,
    OpeningReturnToVillage,
    FadeInFromBlack
}

[Serializable]
public sealed class CinematicEventMarker : Marker, INotification, INotificationOptionProvider
{
    public TimelineCinematicAction action;
    [TextArea] public string speaker;
    [TextArea(2, 5)] public string line;
    [Tooltip("Biến thể/đối tượng thứ mấy dùng cho marker chiến đấu trong cutscene.")]
    public int variant;

    public PropertyName id => new PropertyName(action.ToString());
    public NotificationFlags flags => NotificationFlags.Retroactive;
}
