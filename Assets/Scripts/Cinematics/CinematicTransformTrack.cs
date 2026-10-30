using UnityEngine;
using UnityEngine.Timeline;

[TrackColor(0.18f, 0.75f, 0.9f)]
[TrackClipType(typeof(CinematicTransformClip))]
[TrackBindingType(typeof(Transform))]
public sealed class CinematicTransformTrack : TrackAsset
{
}
