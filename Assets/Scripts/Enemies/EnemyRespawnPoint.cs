using UnityEngine;

// Authored spawn point + prefab; never placed on bosses or summoned minions.
public sealed class EnemyRespawnPoint : MonoBehaviour
{
    public GameObject enemyPrefab;
    public EnemyHealth currentEnemy;
    public string actorName;
    public string saveId;
    [Min(.1f)] public float respawnDelay = 7f;
    float remaining = -1f;

    void Start()
    {
        if (currentEnemy != null) Bind(currentEnemy);
    }
    void Bind(EnemyHealth enemy)
    {
        currentEnemy = enemy;
        enemy.RespawnPoint = this;
        GameProgressManager.Instance?.RegisterRespawnedEnemy(saveId, enemy);
    }
    public void NotifyDeath(EnemyHealth enemy)
    {
        if (enemy == currentEnemy && remaining < 0f) remaining = respawnDelay;
    }
    void Update()
    {
        if (CinematicPresentationController.IsCinematicActive || Time.timeScale <= 0f ||
            (GameProgressManager.Instance != null && GameProgressManager.Instance.IsRestoringState)) return;
        if (remaining < 0f)
        {
            // A previously defeated enemy restored from a save also returns after 7s.
            if (currentEnemy == null || !currentEnemy.gameObject.activeSelf || currentEnemy.IsDead) remaining = respawnDelay;
            return;
        }
        remaining -= Time.deltaTime;
        if (remaining > 0f || enemyPrefab == null) return;
        Vector3 position = transform.position;
        var layout = FindAnyObjectByType<MapSceneLayout>();
        if (layout != null) position = layout.ResolveSafePosition(position, position);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && Vector2.Distance(position, player.transform.position) < .8f) { remaining = .25f; return; }
        if (currentEnemy != null) Destroy(currentEnemy.gameObject);
        var actor = Instantiate(enemyPrefab, position, Quaternion.identity);
        actor.name = actorName;
        Bind(actor.GetComponent<EnemyHealth>());
        remaining = -1f;
    }
}
