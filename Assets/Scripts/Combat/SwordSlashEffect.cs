using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SwordSlashEffect : MonoBehaviour
{
    public float duration = 0.24f;
    public float radius = 0.95f;
    public float arcDegrees = 145f;
    public int points = 18;

    private LineRenderer line;
    private float elapsed;
    private readonly Color startColor = new Color(1f, 0.92f, 0.52f, 1f);
    private readonly Color endColor = new Color(0.55f, 0.9f, 1f, 0f);

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = Mathf.Max(4, points);
        line.startWidth = 0.16f;
        line.endWidth = 0.035f;
        line.numCapVertices = 2;
        line.sortingOrder = 82;
        BuildArc(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        BuildArc(t);
        float alpha = 1f - t;
        line.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
        line.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * 0.35f);
    }

    private void BuildArc(float progress)
    {
        int count = line.positionCount;
        float sweep = Mathf.Lerp(55f, arcDegrees, Mathf.SmoothStep(0f, 1f, progress));
        float start = -sweep * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float normalized = i / (float)(count - 1);
            float angle = (start + sweep * normalized) * Mathf.Deg2Rad;
            float taperedRadius = radius * Mathf.Lerp(0.82f, 1f, Mathf.Sin(normalized * Mathf.PI));
            line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.78f, 0f) * taperedRadius);
        }
    }
}
