using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public sealed class CinematicTransformClip : PlayableAsset, ITimelineClipAsset
{
    public Vector3 positionDelta;
    public bool setFacing;
    public bool faceRight = true;
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CinematicTransformBehaviour> playable = ScriptPlayable<CinematicTransformBehaviour>.Create(graph);
        CinematicTransformBehaviour behaviour = playable.GetBehaviour();
        behaviour.positionDelta = positionDelta;
        behaviour.setFacing = setFacing;
        behaviour.faceRight = faceRight;
        behaviour.easing = easing;
        return playable;
    }
}

public sealed class CinematicTransformBehaviour : PlayableBehaviour
{
    public Vector3 positionDelta;
    public bool setFacing;
    public bool faceRight;
    public AnimationCurve easing;

    private Transform target;
    private Vector3 startPosition;
    private bool initialized;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (info.effectiveWeight <= 0f) return;
        Transform boundTarget = playerData as Transform;
        if (boundTarget == null) return;
        if (!initialized || target != boundTarget)
        {
            initialized = true;
            target = boundTarget;
            startPosition = target.position;
            if (setFacing)
            {
                Vector3 scale = target.localScale;
                float magnitude = Mathf.Max(0.001f, Mathf.Abs(scale.x));
                scale.x = faceRight ? -magnitude : magnitude;
                target.localScale = scale;
            }
        }

        float duration = Mathf.Max(0.0001f, (float)playable.GetDuration());
        float normalized = Mathf.Clamp01((float)playable.GetTime() / duration);
        float t = easing != null ? easing.Evaluate(normalized) : normalized;
        target.position = startPosition + positionDelta * t;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        initialized = false;
        target = null;
    }
}
