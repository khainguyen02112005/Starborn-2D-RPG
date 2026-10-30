using UnityEngine;
using UnityEngine.Rendering;

// A short, independently-owned visual: stopping a caster coroutine cannot leave
// a bolt hanging in the map. Shared unlit material also works in unlit map areas.
public sealed class LightningArcVFX : MonoBehaviour
{
    const float Lifetime = .55f;
    Vector3 start, end;
    LineRenderer rim, core;
    readonly LineRenderer[] branches = new LineRenderer[3];
    readonly Vector3[] points = new Vector3[13];
    float elapsed, nextFlicker;
    Material fallbackMaterial;

    public static GameObject Spawn(Vector3 from, Vector3 to)
    {
        var go = new GameObject("LightningSkillEffect");
        var arc = go.AddComponent<LightningArcVFX>(); arc.Initialize(from, to);
        return go;
    }
    void Initialize(Vector3 from, Vector3 to)
    {
        start = from; end = to;
        Material material = Resources.Load<Material>("CombatVFX/StormArcUnlit");
        if (material == null) { fallbackMaterial = new Material(Shader.Find("Sprites/Default")); material = fallbackMaterial; }
        var group = gameObject.AddComponent<SortingGroup>(); group.sortingLayerID = 0; group.sortingOrder = 1110;
        rim = CreateLine(gameObject, material, .15f, new Color(.08f, .58f, 1f), 0);
        core = CreateLine(new GameObject("White electric core"), material, .048f, new Color(.86f, 1f, 1f), 1);
        for (int i = 0; i < branches.Length; i++)
            branches[i] = CreateLine(new GameObject("Electric fork " + i), material, .028f, new Color(.3f, .85f, 1f), 0);
        Rebuild();
    }
    LineRenderer CreateLine(GameObject go, Material material, float width, Color color, int order)
    {
        if (go != gameObject) go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material; line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ; line.textureMode = LineTextureMode.Stretch;
        line.startWidth = width; line.endWidth = width * .5f;
        line.startColor = color; line.endColor = color;
        line.sortingLayerID = 0; line.sortingOrder = go == gameObject ? 1110 : order;
        line.positionCount = go == gameObject || go.name == "White electric core" ? points.Length : 4;
        return line;
    }
    void Update()
    {
        if (rim == null) return;
        if (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended) return;
        elapsed += Time.deltaTime;
        if (elapsed >= Lifetime) { Destroy(gameObject); return; }
        if (elapsed >= nextFlicker) { nextFlicker = elapsed + .045f; Rebuild(); }
        float alpha = Mathf.Clamp01((Lifetime - elapsed) / .16f);
        SetAlpha(rim, alpha * .85f); SetAlpha(core, alpha);
        foreach (var line in branches) SetAlpha(line, alpha * .8f);
    }
    static void SetAlpha(LineRenderer line, float alpha)
    {
        Color c = line.startColor; c.a = alpha; line.startColor = line.endColor = c;
    }
    void Rebuild()
    {
        Vector3 direction = end - start;
        Vector3 normal = new Vector3(-direction.y, direction.x, 0).normalized;
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            points[i] = Vector3.Lerp(start, end, t);
            if (i > 0 && i < points.Length - 1) points[i] += normal * Random.Range(-.23f, .23f);
            points[i].z = 0;
        }
        rim.SetPositions(points); core.SetPositions(points);
        for (int b = 0; b < branches.Length; b++)
        {
            Vector3 origin = points[3 + b * 3];
            Vector3 target = origin + normal * (b % 2 == 0 ? .5f : -.5f) + direction.normalized * .35f;
            for (int i = 0; i < 4; i++)
                branches[b].SetPosition(i, Vector3.Lerp(origin, target, i / 3f) + (i == 1 || i == 2 ? normal * Random.Range(-.12f, .12f) : Vector3.zero));
        }
    }
    void OnDestroy() { if (fallbackMaterial != null) Destroy(fallbackMaterial); }
}
