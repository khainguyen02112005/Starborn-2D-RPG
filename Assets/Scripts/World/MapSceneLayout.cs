using UnityEngine;
using UnityEngine.Tilemaps;

// Map metadata only: safe spawn/arrival positions when entering or loading an old save.
[DisallowMultipleComponent]
public sealed class MapSceneLayout : MonoBehaviour
{
    private const float PickupClearance = .6f;
    public int sourceLevel;
    public Rect walkableBounds;
    public Tilemap ground;
    public Transform defaultSpawn;
    public Transform[] arrivals;
    public LayerMask solidLayers = 1 << 8;

    public Vector3 ArrivalPosition(string arrivalName)
    {
        if (!string.IsNullOrEmpty(arrivalName) && arrivals != null)
            foreach (Transform point in arrivals)
                if (point != null && point.name == arrivalName)
                    return ResolveSafePosition(point.position, defaultSpawn != null ? defaultSpawn.position : point.position);
        return defaultSpawn != null ? ResolveSafePosition(defaultSpawn.position, defaultSpawn.position) : Vector3.zero;
    }

    public bool IsWalkable(Vector3 position, float radius = .28f)
    {
        if (!walkableBounds.Contains(position)) return false;
        if (ground != null && !ground.HasTile(ground.WorldToCell(position))) return false;
        foreach (Collider2D obstacle in Physics2D.OverlapCircleAll(position, radius, solidLayers))
            if (obstacle.enabled && !obstacle.isTrigger) return false;
        return true;
    }

    // Keep the whole pickup icon on real ground, clear of walls and cliff edges.
    public bool IsPickupPosition(Vector3 position)
    {
        if (!IsWalkable(position, PickupClearance)) return false;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                Vector3 edge = position + new Vector3(x, y) * PickupClearance;
                if (!walkableBounds.Contains(edge) || (ground != null && !ground.HasTile(ground.WorldToCell(edge)))) return false;
            }
        return true;
    }

    public bool TryResolvePickupPosition(Vector3 requested, out Vector3 position)
    {
        position = requested;
        if (IsPickupPosition(position)) return true;
        Vector3 nearestEdge = new Vector3(
            Mathf.Clamp(requested.x, walkableBounds.xMin + PickupClearance, walkableBounds.xMax - PickupClearance),
            Mathf.Clamp(requested.y, walkableBounds.yMin + PickupClearance, walkableBounds.yMax - PickupClearance), requested.z);
        for (float radius = 0f; radius <= 4f; radius += .25f)
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI / 16f;
                Vector3 candidate = nearestEdge + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (IsPickupPosition(candidate)) { position = candidate; return true; }
            }

        // Deleted tiles/old saves can be well outside the current footprint.
        // Find the nearest valid tile, not the player's spawn on the other side of the map.
        float bestDistance = float.PositiveInfinity;
        if (ground != null)
            foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
            {
                if (!ground.HasTile(cell)) continue;
                Vector3 candidate = ground.GetCellCenterWorld(cell);
                float distance = (candidate - requested).sqrMagnitude;
                if (distance >= bestDistance || !IsPickupPosition(candidate)) continue;
                position = candidate;
                bestDistance = distance;
            }
        return !float.IsPositiveInfinity(bestDistance);
    }

    public Vector3 ResolveSafePosition(Vector3 requested, Vector3 fallback)
    {
        if (IsWalkable(requested)) return requested;
        // A repaired prop/wall can overlap a position in an older save. Move to
        // the nearest free patch here instead of teleporting back to the spawn.
        if (walkableBounds.Contains(requested) && (ground == null || ground.HasTile(ground.WorldToCell(requested))))
            for (float radius = .35f; radius <= 3.1f; radius += .25f)
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI / 12f;
                    Vector3 nearby = requested + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (IsWalkable(nearby)) return nearby;
                }
        if (IsWalkable(fallback)) return fallback;
        for (float radius = .5f; radius <= 8f; radius += .5f)
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12f;
                Vector3 candidate = fallback + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (IsWalkable(candidate)) return candidate;
            }
        Debug.LogWarning("Không tìm thấy vị trí an toàn trong map " + sourceLevel, this);
        return fallback;
    }
}
