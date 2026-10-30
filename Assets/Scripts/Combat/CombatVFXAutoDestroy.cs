using UnityEngine;

public class CombatVFXAutoDestroy : MonoBehaviour
{
    [Min(0.05f)] public float lifetime = 0.5f;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
