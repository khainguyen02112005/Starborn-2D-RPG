using UnityEngine;

public enum SpellCastPattern { Focused, Fan, Orbit }

public static class SpellVolley
{
    public static void Cast(Transform caster, Vector3 origin, Vector2 aim, ElementalProjectile.Element element,
        SpellCastPattern pattern, float speed, int damage, LayerMask layers, CursorBattleController cursor = null)
    {
        int count = pattern == SpellCastPattern.Fan ? 5 : pattern == SpellCastPattern.Orbit ? 8 : 1;
        int perBolt = Mathf.Max(1, Mathf.RoundToInt(damage * (count == 5 ? .65f : count == 8 ? .5f : 1f)));
        if (aim.sqrMagnitude < .01f) aim = Vector2.right;
        if (element == ElementalProjectile.Element.Lightning) CombatVFX.SpawnLightningCast(origin, .75f);
        for (int i = 0; i < count; i++)
        {
            float angle = pattern == SpellCastPattern.Orbit ? i * 360f / count : (i - (count - 1) * .5f) * 13f;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * aim.normalized;
            var bolt = new GameObject((cursor == null ? "Player" : "Corrupted") + element + "Bolt");
            bolt.transform.position = origin;
            bolt.transform.localScale = Vector3.one * .55f;
            ElementalProjectile shot = bolt.AddComponent<ElementalProjectile>();
            shot.Initialize(element, direction, speed, perBolt, layers);
            shot.SetCaster(caster, cursor);
            if (pattern == SpellCastPattern.Orbit) shot.OrbitBeforeLaunch(caster, direction, .85f, .65f);
        }
    }
}
