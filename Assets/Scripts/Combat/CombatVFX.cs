using UnityEngine;

public static class CombatVFX
{
    private const string Root = "CombatVFX/";

    public static GameObject SpawnFireImpact(Vector3 position, float scale = 1f)
        => Spawn("FireImpact", position, scale);

    public static GameObject SpawnWaterImpact(Vector3 position, float scale = 1f)
        => Spawn("WaterImpact", position, scale);

    public static GameObject SpawnLightningImpact(Vector3 position, float scale = 1f)
        => Spawn("LightningImpact", position, scale);

    public static GameObject SpawnLightningCast(Vector3 position, float scale = 1f)
        => Spawn("LightningCast", position, scale);

    public static GameObject SpawnLightningStrike(Vector3 groundPosition, float scale = 1f)
    {
        // The strike sheet's pivot is at its foot, so the bolt lands on the target.
        return Spawn("LightningStrike", groundPosition, scale);
    }

    public static GameObject SpawnHitSpark(Vector3 position, bool critical)
        => Spawn("HitSpark", position, critical ? 1.45f : 0.85f);

    public static GameObject SpawnSwordSlash(Vector3 position, float direction, int comboStep)
    {
        GameObject effect = Spawn("SwordSlash", position, comboStep == 3 ? 1.35f : 1f);
        if (effect != null)
        {
            Vector3 scale = effect.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction >= 0f ? 1f : -1f);
            effect.transform.localScale = scale;
        }
        return effect;
    }

    public static GameObject AttachProjectileAura(Transform parent, ElementalProjectile.Element element)
    {
        if (parent == null) return null;
        string name = element == ElementalProjectile.Element.Fire ? "FireProjectile" : element == ElementalProjectile.Element.Lightning ? "LightningProjectile" : "WaterProjectile";
        GameObject prefab = Resources.Load<GameObject>(Root + name);
        if (prefab == null) return null;
        GameObject effect = Object.Instantiate(prefab, parent);
        effect.name = name + "VFX";
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        MakeVisible(effect);
        if (element == ElementalProjectile.Element.Arcane)
        {
            foreach (SpriteRenderer renderer in effect.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.color = new Color(0.72f, 0.42f, 1f, renderer.color.a);
        }
        return effect;
    }

    private static GameObject Spawn(string resourceName, Vector3 position, float scale)
    {
        GameObject prefab = Resources.Load<GameObject>(Root + resourceName);
        if (prefab == null)
        {
            Debug.LogWarning("Thiếu VFX prefab Resources/" + Root + resourceName);
            return null;
        }
        GameObject effect = Object.Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale *= scale;
        MakeVisible(effect);
        return effect;
    }

    private static void MakeVisible(GameObject effect)
    {
        // Terrain/actors use depth orders down to -1000 and up to 900.
        // Emissive spell VFX must not disappear under the floor at negative Y.
        var group = effect.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (group == null) group = effect.AddComponent<UnityEngine.Rendering.SortingGroup>();
        group.sortingLayerID = 0; group.sortingOrder = 1100;
    }
}
