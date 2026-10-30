using System.Collections.Generic;
using UnityEngine;

// Ending 3 only. The cursor fight deliberately keeps StoryWorldDissolve.
public sealed class Ending3WorldDisintegration : MonoBehaviour
{
    public const float ErosionSeconds = 7.2f;
    public float Elapsed { get; private set; }
    public float Progress => Mathf.Clamp01(Elapsed / ErosionSeconds);
    public bool Complete => Elapsed >= ErosionSeconds + 1.2f;
    public int TotalFragments { get; private set; }
    public int SurfaceCount => originals.Count;
    private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    private readonly List<Material> materials = new List<Material>();
    private readonly Fragment[] fragments = new Fragment[256];
    private readonly List<Vector3> vertices = new List<Vector3>(1024);
    private readonly List<Vector2> uvs = new List<Vector2>(1024);
    private readonly List<Color> colors = new List<Color>(1024);
    private readonly List<int> triangles = new List<int>(1536);
    private readonly System.Random random = new System.Random(1949);
    private Texture2D nativeSurface;
    private Material debrisMaterial;
    private Mesh mesh;
    private Vector2 origin;
    private Matrix4x4 captureProjection;
    private float reach, emission;
    private int nextFragment;
    private Color cameraBackground;
    private Camera worldCamera;

    private struct Fragment
    {
        public Vector2 position, velocity, uv, uvExtent;
        public float age, lifetime, size, rotation, spin;
        public bool alive;
    }

    public static Ending3WorldDisintegration Begin(Transform hero, List<Renderer> surfaces)
    {
        var go = new GameObject("Ending3_NativeSurfaceDisintegration");
        var effect = go.AddComponent<Ending3WorldDisintegration>();
        effect.Initialize(hero, surfaces);
        return effect;
    }

    private void Initialize(Transform hero, List<Renderer> surfaces)
    {
        Material template = Resources.Load<Material>("Ending3/Disintegration");
        if (template == null) throw new System.InvalidOperationException("Missing ending 3 disintegration material");
        origin = hero != null ? (Vector2)hero.position : Vector2.zero;
        worldCamera = Camera.main;
        reach = 12f;
        if (worldCamera != null)
        {
            cameraBackground = worldCamera.backgroundColor;
            CaptureNativeSurface(hero);
            worldCamera.backgroundColor = Color.white;
            float depth = Vector3.Dot(-worldCamera.transform.position, worldCamera.transform.forward);
            reach = 0f;
            foreach (Vector2 corner in new[] { Vector2.zero, Vector2.one, Vector2.up, Vector2.right })
            {
                Vector2 point = worldCamera.ViewportToWorldPoint(new Vector3(corner.x, corner.y, depth));
                Vector2 delta = point - origin; delta.y *= 1.08f;
                reach = Mathf.Max(reach, delta.magnitude + 2f);
            }
        }
        foreach (Renderer surface in surfaces)
        {
            if (surface == null) continue;
            Material[] old = surface.sharedMaterials;
            originals.Add(surface, old);
            var replacements = new Material[old.Length];
            for (int i = 0; i < old.Length; i++)
            {
                var replacement = new Material(template);
                if (old[i] != null)
                {
                    if (old[i].HasProperty("_MainTex")) replacement.mainTexture = old[i].mainTexture;
                    if (old[i].HasProperty("_Color")) replacement.color = old[i].color;
                    if (old[i].shader.name == "Story/WorldPixelDissolve")
                    {
                        replacement.SetFloat("_Progress", old[i].GetFloat("_Progress"));
                        replacement.SetFloat("_PulseCount", old[i].GetFloat("_PulseCount"));
                        Vector4[] holes = old[i].GetVectorArray("_ErasePulses");
                        if (holes != null && holes.Length > 0) replacement.SetVectorArray("_ErasePulses", holes);
                    }
                }
                replacement.SetVector("_Origin", new Vector4(origin.x, origin.y, 0, 0));
                replacement.SetFloat("_Radius", -3.4f);
                materials.Add(replacement); replacements[i] = replacement;
            }
            surface.sharedMaterials = replacements;
        }
        if (nativeSurface != null)
        {
            debrisMaterial = new Material(template);
            debrisMaterial.SetFloat("_Debris", 1);
            debrisMaterial.mainTexture = nativeSurface;
            mesh = new Mesh { name = "Ending3_SourceColourFragments" }; mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = debrisMaterial; renderer.sortingOrder = 30000;
        }
    }

    private void CaptureNativeSurface(Transform hero)
    {
        // Sample the real map once, without the hero or overlay UI. Fragments
        // therefore contain the wood/stone/grass pixels they break away from.
        Renderer[] heroPieces = hero != null ? hero.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        var enabled = new bool[heroPieces.Length];
        RenderTexture previousTarget = worldCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(768, 432, 24, RenderTextureFormat.ARGB32);
        try
        {
            for (int i = 0; i < heroPieces.Length; i++) { enabled[i] = heroPieces[i].enabled; heroPieces[i].enabled = false; }
            captureProjection = worldCamera.projectionMatrix * worldCamera.worldToCameraMatrix;
            worldCamera.targetTexture = target; worldCamera.Render(); RenderTexture.active = target;
            nativeSurface = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false) { name = "Ending3_NativeWorldSnapshot", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            nativeSurface.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); nativeSurface.Apply(false, false);
        }
        finally
        {
            for (int i = 0; i < heroPieces.Length; i++) if (heroPieces[i] != null) heroPieces[i].enabled = enabled[i];
            worldCamera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    public void Step(float delta)
    {
        delta = Mathf.Clamp(delta, 0f, .1f);
        Elapsed += delta;
        float p = Progress;
        // A restrained onset, then a continuous outward collapse with a short tail.
        float radius = Mathf.Lerp(-3.4f, reach + 1.5f, p * (.8f + .2f * p));
        foreach (Material material in materials)
        {
            material.SetFloat("_Radius", radius);
            material.SetFloat("_Completion", p);
        }
        if (nativeSurface == null) return;
        if (Elapsed < ErosionSeconds && radius > .8f)
        {
            emission += delta * 105f;
            while (emission >= 1f) { emission--; Emit(radius); }
        }
        UpdateFragments(delta);
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    private static float Hash(Vector2 p) => Mathf.Repeat(Mathf.Sin(Vector2.Dot(p, new Vector2(127.1f, 311.7f))) * 43758.5453f, 1f);
    private static float Grain(Vector2 p)
    {
        Vector2 cell = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
        Vector2 f = p - cell; f = new Vector2(f.x * f.x * (3 - 2 * f.x), f.y * f.y * (3 - 2 * f.y));
        return Mathf.Lerp(Mathf.Lerp(Hash(cell), Hash(cell + Vector2.right), f.x), Mathf.Lerp(Hash(cell + Vector2.up), Hash(cell + Vector2.one), f.x), f.y);
    }
    private static float Roughness(Vector2 p) => (Grain(p * .27f) - .5f) * 5.2f + (Grain(p * 1.6f) - .5f) * .8f;

    private void Emit(float radius)
    {
        float angle = Range(0, Mathf.PI * 2);
        Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 direction = new Vector2(outward.x, outward.y / 1.08f);
        float edge = radius;
        // Follow the same irregular surface front, not an unrelated particle ring.
        for (int i = 0; i < 4; i++) edge = Mathf.Lerp(edge, Mathf.Max(.8f, radius - Roughness(origin + direction * edge)), .5f);
        Vector2 point = origin + direction * (edge + Range(-.2f, .06f));
        Vector4 projected = captureProjection * new Vector4(point.x, point.y, 0, 1);
        Vector2 uv = new Vector2(projected.x / projected.w, projected.y / projected.w) * .5f + Vector2.one * .5f;
        if (uv.x < .025f || uv.x > .975f || uv.y < .06f || uv.y > .94f) return;
        Color source = nativeSurface.GetPixelBilinear(uv.x, uv.y);
        if (source.a < .05f || (source.r > .93f && source.g > .93f && source.b > .93f)) return;
        float size = Range(.055f, .13f);
        fragments[nextFragment] = new Fragment
        {
            alive = true, position = point, velocity = outward * Range(.14f, .48f) + Vector2.up * Range(.15f, .42f),
            uv = uv, uvExtent = new Vector2(size * Mathf.Abs(captureProjection.m00), size * Mathf.Abs(captureProjection.m11)) * .25f,
            lifetime = Range(.65f, 1.15f), size = size, rotation = Range(-.15f, .15f), spin = Range(-.85f, .85f)
        };
        nextFragment = (nextFragment + 1) % fragments.Length; TotalFragments++;
    }

    private void UpdateFragments(float delta)
    {
        vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear();
        for (int i = 0; i < fragments.Length; i++)
        {
            Fragment f = fragments[i]; if (!f.alive) continue;
            f.age += delta;
            if (f.age >= f.lifetime) { f.alive = false; fragments[i] = f; continue; }
            f.position += f.velocity * delta; f.velocity += new Vector2(.06f, -.62f) * delta; f.rotation += f.spin * delta;
            float life = f.age / f.lifetime;
            float half = f.size * .5f * (1f - .35f * life);
            float sin = Mathf.Sin(f.rotation), cos = Mathf.Cos(f.rotation);
            Color color = new Color(1, 1, 1, .96f * (1 - Mathf.SmoothStep(.32f, 1f, life)));
            int first = vertices.Count;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner == 0 || corner == 3) ? -1 : 1;
                float y = corner < 2 ? -1 : 1;
                vertices.Add(new Vector3(f.position.x + (x * cos - y * sin) * half, f.position.y + (x * sin + y * cos) * half, -.1f));
                uvs.Add(f.uv + Vector2.Scale(new Vector2(x, y), f.uvExtent)); colors.Add(color);
            }
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
            triangles.Add(first); triangles.Add(first + 2); triangles.Add(first + 3);
            fragments[i] = f;
        }
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        foreach (var pair in originals) if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        foreach (Material material in materials) if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
        if (debrisMaterial != null) Destroy(debrisMaterial);
        if (nativeSurface != null) Destroy(nativeSurface);
        // Keep white after successful erasure; a cancelled/debug sequence restores it.
        if (!Complete && worldCamera != null) worldCamera.backgroundColor = cameraBackground;
    }
}
