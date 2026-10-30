using UnityEngine;
using UnityEngine.Rendering;

// Keep each multipart SPUM actor together while sorting it against map props.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SortingGroup))]
public sealed class MapDepthSort : MonoBehaviour
{
    public float feetOffset;
    private SortingGroup group;
    private void OnEnable() { group = GetComponent<SortingGroup>(); Apply(); }
    private void LateUpdate() => Apply();
    private void Apply()
    {
        if (group == null) return;
        group.sortingLayerID = 0;
        group.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt((transform.position.y + feetOffset) * 8f), -900, 900);
    }
}
