#pragma warning disable CS0618

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Image healthBarFill;

    [Header("Health Text")]
    public TextMeshProUGUI hpText;

    [Header("Defense")]
    public int physicalArmor;
    public int magicResistance;

    [Header("Death")]
    [SerializeField, Min(0f)] private float gameOverDelay = 1.15f;

    public bool isBlocking;
    public bool IsDead { get; private set; }

    private float blockTimer;
    private const float ParryWindow = 0.2f;
    private PlayerSPUMAnimation spumAnimation;

    private void Awake()
    {
        spumAnimation = GetComponent<PlayerSPUMAnimation>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;
        UpdateHealthUI();
    }

    private void Update()
    {
        if (IsDead)
        {
            isBlocking = false;
            return;
        }

        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.K))
        {
            isBlocking = true;
            blockTimer += Time.deltaTime;
        }
        else
        {
            isBlocking = false;
            blockTimer = 0f;
        }
    }

    public int GetEquipmentArmor()
    {
        int armorFromGear = 0;
        var inventory = GetComponent<PlayerInventory>();
        InventorySlot[] allSlots = inventory != null && inventory.characterPanel != null
            ? inventory.characterPanel.GetComponentsInChildren<InventorySlot>(true) : new InventorySlot[0];

        foreach (InventorySlot slot in allSlots)
        {
            if (slot.isChestSlot || slot.equipSlotType != 2 || !slot.isFull || slot.itemAmount <= 0 || slot.itemData == null || slot.itemData.itemType != 2)
            {
                continue;
            }

            armorFromGear = slot.itemData.DefenseAtUpgrade(slot.upgradeLevel);
            if (slot.currentDurability > 0)
            {
                slot.currentDurability = Mathf.Max(0, slot.currentDurability - 1);
                slot.UpdateAmountUI();
            }
            else
            {
                armorFromGear = 0;
            }

            break;
        }

        return armorFromGear;
    }

    public void TakeDamage(int damage, bool isMagicDamage = false)
    {
        if (IsDead || (damage > 0 && CinematicPresentationController.IsCinematicActive))
        {
            return;
        }

        if (damage < 0)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth - damage);
            UpdateHealthUI();
            return;
        }

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null && combat.isBlocking)
        {
            Debug.Log("Blocked the attack.");
            GameAudioManager.Play(GameSound.Block, transform.position, 0.9f, Random.Range(0.96f, 1.06f));
            return;
        }

        if (isBlocking)
        {
            if (blockTimer <= ParryWindow)
            {
                GameAudioManager.Play(GameSound.Block, transform.position, 1f, 1.12f);
                return;
            }

            PlayerStamina stamina = GetComponent<PlayerStamina>();
            if (stamina != null && stamina.UseStamina(25f))
            {
                GameAudioManager.Play(GameSound.Block, transform.position, 0.82f, 0.94f);
                return;
            }
        }

        if (damage == 0) return;
        int gear = GetEquipmentArmor();
        int defense = isMagicDamage ? magicResistance + gear / 2 : physicalArmor + gear;
        int finalDamage = DamageAfterDefense(damage, defense);
        if (finalDamage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        DamagePopup.Show(transform.position + Vector3.up * 1.05f, finalDamage, false, true);
        UpdateHealthUI();

        if (CameraShakeManager.instance != null)
        {
            CameraShakeManager.instance.ShakeHeavy();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            spumAnimation?.PlayDamaged();
        }
    }

    public static int DamageAfterDefense(int damage, int defense)
    {
        if (damage <= 0) return 0;
        float reduction = Mathf.Min(.6f, Mathf.Max(0, defense) / (20f + Mathf.Max(0, defense)));
        return Mathf.Max(1, Mathf.CeilToInt(damage * (1f - reduction)));
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        isBlocking = false;
        currentHealth = 0;
        UpdateHealthUI();

        spumAnimation?.PlayDeath();
        GameProgressManager.Instance?.RecordDeath();

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        StartCoroutine(ShowGameOverAfterDeathAnimation());
    }

    private IEnumerator ShowGameOverAfterDeathAnimation()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);

        GameOverManager manager = GameOverManager.Instance;
        if (manager == null)
        {
            manager = FindFirstObjectByType<GameOverManager>(FindObjectsInactive.Include);
        }

        if (manager != null)
        {
            manager.ShowGameOver(this);
            yield break;
        }

        RespawnManager respawnManager = FindFirstObjectByType<RespawnManager>(FindObjectsInactive.Include);
        if (respawnManager != null) respawnManager.Respawn(this);
    }

    public void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        }

        if (hpText != null)
        {
            hpText.text = currentHealth + " / " + maxHealth;
        }
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void LevelUpBoost(int amount)
    {
        IncreaseMaxHealth(amount);
    }

    public void RestoreState(int savedMaxHealth, int savedCurrentHealth)
    {
        maxHealth = Mathf.Max(1, savedMaxHealth);
        currentHealth = Mathf.Clamp(savedCurrentHealth, 1, maxHealth);
        IsDead = false;
        UpdateHealthUI();
    }

    public void ReviveAt(Vector3 position)
    {
        transform.position = position;
        currentHealth = maxHealth;
        IsDead = false;
        isBlocking = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = true;
        spumAnimation?.Revive();
        UpdateHealthUI();
    }
}
