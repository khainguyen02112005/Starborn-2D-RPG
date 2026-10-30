using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Only the autonomous story hero invokes this. Ordinary combat never erases maps.
public sealed class StoryWorldDissolve : MonoBehaviour
{
    public static StoryWorldDissolve Instance { get; private set; }
    public float Progress { get; private set; }
    public int StrikeCount { get; private set; }
    private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    private readonly Vector4[] pulses = new Vector4[32];
    private readonly Vector4[] goals = new Vector4[32];
    private int pulseCount;
    private int nextPulse;
    private Material dissolve;
    private Camera worldCamera;
    private Color originalBackground;

    public static StoryWorldDissolve Begin(Transform hero)
    {
        if (Instance != null) return Instance;
        var go = new GameObject("StoryWorldPixelDestruction");
        var effect = go.AddComponent<StoryWorldDissolve>();
        effect.Configure(hero);
        return effect;
    }

    private void Configure(Transform hero)
    {
        Instance = this;
        Material template = Resources.Load<Material>("StoryWorld/WorldPixelDissolve");
        Shader shader = template != null ? template.shader : Shader.Find("Story/WorldPixelDissolve");
        if (shader == null) { Debug.LogError("Missing world pixel dissolve shader", this); return; }
        dissolve = template != null ? new Material(template) : new Material(shader);
        worldCamera = Camera.main;
        if (worldCamera != null) { originalBackground = worldCamera.backgroundColor; worldCamera.backgroundColor = Color.white; }
        foreach (Renderer renderer in FindObjectsByType<Renderer>())
        {
            if (!(renderer is SpriteRenderer) && !(renderer is TilemapRenderer)) continue;
            if (hero != null && renderer.transform.IsChildOf(hero)) continue;
            bool belongsToWorld = renderer.GetComponentInParent<MapSceneLayout>() != null
                || renderer.GetComponentInParent<NPCDialogue>() != null
                || renderer.GetComponentInParent<NPCShop>() != null
                || renderer.GetComponentInParent<NPCBlacksmith>() != null
                || renderer.GetComponentInParent<EnemyAI>() != null
                || renderer.GetComponentInParent<ScenePortal>() != null
                || renderer.GetComponentInParent<PixelSkyBackdrop>() != null;
            if (!belongsToWorld) continue;
            originals.Add(renderer, renderer.sharedMaterials);
            renderer.sharedMaterial = dissolve;
        }
    }

    public void Strike(Vector3 position, float radius = 1f, bool advance = true)
    {
        if (dissolve == null) return;
        int index;
        if (pulseCount < pulses.Length)
        {
            index = nextPulse++;
            goals[index] = new Vector4(position.x, position.y, Mathf.Max(.15f, radius), 0);
            pulses[index] = new Vector4(position.x, position.y, 0, 0);
        }
        else
        {
            index = 0;float closest = float.MaxValue;
            for (int i = 0; i < goals.Length; i++)
            {
                float distance = Vector2.Distance(position, new Vector2(goals[i].x, goals[i].y));
                if (distance < closest) { closest = distance; index = i; }
            }
            // Grow an existing hole rather than overwrite it and regrow scenery.
            goals[index].z = Mathf.Max(goals[index].z, closest + radius);
        }
        pulseCount = Mathf.Min(pulseCount + 1, pulses.Length);
        if (advance) { StrikeCount++; Progress = Mathf.Min(.72f, Progress + .008f); }
        CinematicPresentationController.Instance?.PlayPixelFragments(position, .55f, Mathf.Min(radius, .8f));
    }

    public void SetCompletion(float value) => Progress = Mathf.Clamp01(Mathf.Max(Progress, value));

    private void LateUpdate()
    {
        if (dissolve == null || (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended)) return;
        for (int i = 0; i < pulseCount; i++) pulses[i].z = Mathf.MoveTowards(pulses[i].z, goals[i].z, Time.unscaledDeltaTime * 4f);
        dissolve.SetFloat("_Progress", Progress);
        dissolve.SetFloat("_PulseCount", pulseCount);
        dissolve.SetVectorArray("_ErasePulses", pulses);
    }

    public static void RestoreWorld()
    {
        if (Instance != null) Destroy(Instance.gameObject);
    }

    private void OnDestroy()
    {
        foreach (var pair in originals) if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
        if (worldCamera != null) worldCamera.backgroundColor = originalBackground;
        if (dissolve != null) Destroy(dissolve);
        if (Instance == this) Instance = null;
    }
}
