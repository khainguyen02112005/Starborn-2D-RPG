using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    [Min(0f)] public float followSmoothing = 18f;

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 destination = target.position + offset;
        float blend = followSmoothing <= 0f
            ? 1f
            : 1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime);

        transform.position = Vector3.Lerp(transform.position, destination, blend);
    }

    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
