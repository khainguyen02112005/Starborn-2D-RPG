using UnityEngine;

public sealed class ExperiencePickup : MonoBehaviour
{
    [SerializeField] private int experience = 5;
    [SerializeField] private float collectRadius = .5f;
    [SerializeField] private float magnetRadius = 5f;
    private float elapsed;
    private Transform player;
    public int Experience => experience;

    public static GameObject Spawn(Vector3 position, int amount)
    {
        if (amount <= 0 || !EnemyLoot.TryResolveDropPosition(position, out position)) return null;
        GameObject prefab = Resources.Load<GameObject>("CombatVFX/ExperiencePickup");
        if (prefab == null) { Debug.LogWarning("Thiếu prefab CombatVFX/ExperiencePickup"); return null; }
        GameObject orb = Instantiate(prefab, position, Quaternion.identity);
        orb.GetComponent<ExperiencePickup>().experience = amount;
        return orb;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || CinematicPresentationController.IsCinematicActive) return;
        elapsed += Time.deltaTime;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null || elapsed < .55f) return;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;
        Vector3 target = player.position + Vector3.up * .3f;
        float distance = Vector2.Distance(transform.position, target);
        if (distance < magnetRadius || elapsed > 8f)
            transform.position = Vector3.MoveTowards(transform.position, target, (4f + elapsed * 2f) * Time.deltaTime);
        if (distance > collectRadius) return;
        player.GetComponent<PlayerLevel>()?.AddExp(experience);
        CombatVFX.SpawnHitSpark(target, false);
        GameAudioManager.Play(GameSound.ItemPickup, target, .42f, 1.4f);
        Destroy(gameObject);
    }
}
