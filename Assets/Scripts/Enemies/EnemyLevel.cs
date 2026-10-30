using TMPro;
using UnityEngine;

public class EnemyLevel : MonoBehaviour
{
    [Range(1, 20)] public int level = 1;
    public TextMeshPro levelLabel;

    // Apply level bonuses before EnemyHealth.Start initializes current HP.
    private void Awake()
    {
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.maxHealth += (level - 1) * 8;
            health.experienceReward = 5 + (level - 1) * 3;
        }
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.speed += (level - 1) * 0.15f;
            ai.contactDamage = 1 + Mathf.FloorToInt((level - 1) / 2f);
        }
        if (levelLabel != null) levelLabel.text = "Lv." + level;
    }
}
