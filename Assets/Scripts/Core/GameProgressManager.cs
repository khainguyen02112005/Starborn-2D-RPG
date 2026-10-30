using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class InventoryItemSaveData
{
    public int slotIndex;
    public string itemId;
    public int amount;
    public int upgradeLevel;
    public int durability = 100;
}

[Serializable]
public class SaveSlotData
{
    public int saveVersion = 3;
    public bool exists;
    public int slotIndex;
    public string savedAt;
    public string sceneName = "Level_02";
    public string playerName = PlayerIdentity.DefaultName;
    public string playerClass = "Hiệp Sĩ";
    public float positionX;
    public float positionY;
    public float positionZ;
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int maxMana = 5;
    public int currentMana = 5;
    public int level = 1;
    public int currentExp;
    public int gold;
    public int deathCount;
    public int questStage;
    public int monstersKilled;
    public bool bossDefeated;
    public int statPoints = 5;
    public int strength = 1;
    public int agility = 1;
    public int intelligence = 1;
    public bool skill1;
    public bool skill2;
    public bool skill3;
    public bool restorePosition = true;
    public int expToNextLevel = 10;
    public int awakeningCount;
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float moveSpeed = 5f;
    public float dashSpeed = 15f;
    public float critChance = .2f;
    public int critMultiplier = 2;
    public int physicalArmor;
    public int magicResistance;
    public float facing = 1f;
    public bool level03IntroSeen;
    public bool rareWhisperShown;
    public bool knightMemory;
    public int spellPattern = 1;
    public int lightningKey = (int)KeyCode.Q;
    public int fireKey = (int)KeyCode.F;
    public int waterKey = (int)KeyCode.R;
    public SceneProgressData[] scenes = new SceneProgressData[0];
    public InventoryItemSaveData[] bagItems = new InventoryItemSaveData[0];
    public InventoryItemSaveData[] equippedItems = new InventoryItemSaveData[0];
}

public class GameProgressManager : MonoBehaviour
{
    public const int SlotCount = 5;
    public const string ProgressScene = "Tiến trình";
    public const string AchievementScene = "Thành tựu";

    public static GameProgressManager Instance { get; private set; }
    public int ActiveSlot { get; private set; } = -1;
    public int DeathCount { get; private set; }

    private SaveSlotData pendingSave;
    private SaveSlotData pendingLoad;
    private string returnScene = "Main menu";
    private bool applying;
    private readonly Dictionary<string, SceneProgressData> world = new Dictionary<string, SceneProgressData>();
    private readonly Dictionary<string, Component> sceneEnemies = new Dictionary<string, Component>();
    public bool IsRestoringState => applying || pendingLoad != null;
    public bool IsResumingSavedScene => IsRestoringState && (applying ? resumingPosition : pendingLoad.restorePosition);
    private bool resumingPosition;
    private string pendingArrivalPoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Application.runInBackground = true;
        if (Instance != null) return;
        GameObject host = new GameObject("GameProgressManager");
        host.AddComponent<GameProgressManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ActiveSlot = PlayerPrefs.GetInt("RPG.ActiveSlot", -1);
        DeathCount = PlayerPrefs.GetInt("RPG.DeathCount", 0);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void BeginNewGame()
    {
        pendingSave = null;
        pendingLoad = null;
        world.Clear();
        ActiveSlot = -1;
        DeathCount = 0;
        PlayerPrefs.SetInt("RPG.ActiveSlot", -1);
        PlayerPrefs.SetInt("RPG.DeathCount", 0);
        PlayerPrefs.SetInt("RPG.QuestStage", 0);
        PlayerPrefs.SetInt("RPG.QuestKills", 0);
        PlayerPrefs.SetInt("RPG.BossDefeated", 0);
        PlayerPrefs.DeleteKey("RPG.PendingOpeningCutscene");
        PlayerPrefs.DeleteKey("RPG.PendingResetCutscene");
        PlayerPrefs.DeleteKey("RPG.KnightMemory");
        PlayerPrefs.DeleteKey("RPG.Level03IntroSeen");
        PlayerPrefs.DeleteKey("RPG.RareRespawnWhisperShown");
        PlayerPrefs.SetString("NextScene", "Level_02");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Level_01");
    }

    public void OpenContinueMenu()
    {
        pendingSave = null;
        pendingLoad = null;
        PlayerPrefs.SetString("RPG.SaveMenuMode", "load");
        PlayerPrefs.SetString("RPG.SaveMenuReturn", "Main menu");
        PlayerPrefs.Save();
        SceneManager.LoadScene(ProgressScene);
    }

    public void OpenSaveMenu()
    {
        if (IsRestoringState || GameObject.FindGameObjectWithTag("Player") == null) return;
        pendingSave = CaptureCurrentState();
        returnScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("RPG.SaveMenuMode", "save");
        PlayerPrefs.SetString("RPG.SaveMenuReturn", returnScene);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;
        SceneManager.LoadScene(ProgressScene);
    }

    public void PrepareSceneTransition(string targetScene, string arrivalPoint = null)
    {
        pendingLoad = CaptureCurrentState();
        pendingLoad.sceneName = targetScene;
        pendingLoad.restorePosition = false;
        pendingArrivalPoint = arrivalPoint;
    }

    public SaveSlotData GetSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return new SaveSlotData();
        string json = PlayerPrefs.GetString(SlotKey(slotIndex), string.Empty);
        if (string.IsNullOrEmpty(json)) return new SaveSlotData { slotIndex = slotIndex };
        try
        {
            bool legacySave = !json.Contains("\"saveVersion\"");
            SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);
            if (data != null && legacySave)
            {
                data.saveVersion = 1;
                data.restorePosition = true;
            }
            // Old autosaves wrote StoryCompleted before an ending could finish.
            // They have no cinematic playhead: resume at the elder's reward instead.
            if (data != null && data.saveVersion < 3 && data.bossDefeated && data.questStage == (int)StoryQuestStage.StoryCompleted)
                data.questStage = (int)StoryQuestStage.BossReadyToTurnIn;
            return data ?? new SaveSlotData { slotIndex = slotIndex };
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không đọc được ô lưu " + (slotIndex + 1) + ": " + exception.Message);
            return new SaveSlotData { slotIndex = slotIndex };
        }
    }

    public void SelectSlot(int slotIndex, bool saveMode)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;

        if (saveMode)
        {
            // There is no player in the slot-selection scene. Never save its defaults.
            if (pendingSave == null) return;
            SaveSlotData data = pendingSave;
            data.restorePosition = true;
            data.exists = true;
            data.slotIndex = slotIndex;
            data.savedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            WriteSlot(data);
            ActiveSlot = slotIndex;
            PlayerPrefs.SetInt("RPG.ActiveSlot", slotIndex);
            PlayerPrefs.Save();
            data.restorePosition = true;
            pendingLoad = data;
            pendingSave = null;
            SceneManager.LoadScene(PlayerPrefs.GetString("RPG.SaveMenuReturn", "Level_02"));
            return;
        }

        SaveSlotData selected = GetSlot(slotIndex);
        if (!selected.exists)
        {
            ActiveSlot = slotIndex;
            PlayerPrefs.SetInt("RPG.ActiveSlot", slotIndex);
            PlayerPrefs.Save();
            BeginNewGame();
            ActiveSlot = slotIndex;
            PlayerPrefs.SetInt("RPG.ActiveSlot", slotIndex);
            return;
        }

        ActiveSlot = slotIndex;
        DeathCount = selected.deathCount;
        pendingLoad = selected;
        world.Clear();
        if (selected.scenes != null)
            foreach (SceneProgressData progress in selected.scenes)
                if (progress != null && !string.IsNullOrEmpty(progress.sceneName)) world[progress.sceneName] = progress;
        PlayerPrefs.SetInt("RPG.ActiveSlot", slotIndex);
        PlayerPrefs.SetInt("RPG.DeathCount", DeathCount);
        PlayerIdentity.SetName(selected.playerName);
        PlayerPrefs.SetString("PlayerClass", string.IsNullOrWhiteSpace(selected.playerClass) ? "Hiệp Sĩ" : selected.playerClass);
        ApplyStoryPreferences(selected);
        PlayerPrefs.DeleteKey("RPG.PendingOpeningCutscene");
        PlayerPrefs.DeleteKey("RPG.PendingResetCutscene");
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;
        SceneManager.LoadScene(string.IsNullOrEmpty(selected.sceneName) ? "Level_02" : selected.sceneName);
    }

    public void ReturnFromSlotMenu()
    {
        if (pendingSave != null)
        {
            pendingSave.restorePosition = true;
            pendingLoad = pendingSave;
        }
        pendingSave = null;
        SceneManager.LoadScene(PlayerPrefs.GetString("RPG.SaveMenuReturn", "Main menu"));
    }

    public void RecordDeath()
    {
        DeathCount++;
        PlayerPrefs.SetInt("RPG.DeathCount", DeathCount);
        PlayerPrefs.Save();
    }

    public void PrepareStoryLoopReset()
    {
        // Không cho dữ liệu vị trí/quest đang chờ từ ô lưu cũ ghi đè lần reset mới.
        pendingSave = null;
        pendingLoad = null;
        world.Clear();
        DeathCount = 0;
        PlayerPrefs.SetInt("RPG.DeathCount", 0);
        PlayerPrefs.SetInt("RPG.QuestStage", 0);
        PlayerPrefs.SetInt("RPG.QuestKills", 0);
        PlayerPrefs.SetInt("RPG.BossDefeated", 0);
        PlayerPrefs.DeleteKey("RPG.Level03IntroSeen");
        PlayerPrefs.SetString("NextScene", "Level_02");
        PlayerPrefs.Save();
    }

    public static void UnlockEnding(int ending)
    {
        PlayerPrefs.SetInt("RPG.Ending." + ending, 1);
        PlayerPrefs.Save();
    }

    public static bool IsEndingUnlocked(int ending)
    {
        return PlayerPrefs.GetInt("RPG.Ending." + ending, 0) == 1;
    }

    public static void ClearEnding(int ending)
    {
        PlayerPrefs.DeleteKey("RPG.Ending." + ending);
        PlayerPrefs.Save();
    }

    public void DeleteSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        PlayerPrefs.DeleteKey(SlotKey(slotIndex));
        if (ActiveSlot == slotIndex)
        {
            ActiveSlot = -1;
            PlayerPrefs.SetInt("RPG.ActiveSlot", -1);
        }
        PlayerPrefs.Save();
    }

    public void SaveActiveSlot()
    {
        // Quest/skill/portal callbacks update the session, not the user's manual save.
        // Only selecting a slot in the Save menu is allowed to overwrite that slot.
        if (IsRestoringState || pendingSave != null) return;
        CaptureSceneProgress();
    }

    private SaveSlotData CaptureCurrentState()
    {
        SaveSlotData data = new SaveSlotData
        {
            saveVersion = 3,
            exists = true,
            sceneName = SceneManager.GetActiveScene().name,
            playerName = PlayerIdentity.Name,
            playerClass = PlayerPrefs.GetString("PlayerClass", "Hiệp Sĩ"),
            deathCount = DeathCount
        };

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return data;

        Vector3 position = player.transform.position;
        data.positionX = position.x;
        data.positionY = position.y;
        data.positionZ = position.z;
        data.facing = Mathf.Sign(player.transform.localScale.x);
        data.level03IntroSeen = PlayerPrefs.GetInt("RPG.Level03IntroSeen", 0) == 1;
        data.rareWhisperShown = PlayerPrefs.GetInt("RPG.RareRespawnWhisperShown", 0) == 1;
        data.knightMemory = PlayerPrefs.GetInt("RPG.KnightMemory", 0) == 1;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) { data.maxHealth = health.maxHealth; data.currentHealth = health.currentHealth; data.physicalArmor = health.physicalArmor; data.magicResistance = health.magicResistance; }
        PlayerMana mana = player.GetComponent<PlayerMana>();
        if (mana != null) { data.maxMana = mana.maxMana; data.currentMana = mana.currentMana; }
        PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
        if (playerLevel != null) { data.level = playerLevel.level; data.currentExp = playerLevel.currentExp; data.expToNextLevel = playerLevel.expToNextLevel; data.awakeningCount = playerLevel.awakeningCount; }
        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null) { data.maxStamina = stamina.maxStamina; data.currentStamina = stamina.currentStamina; }
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            data.moveSpeed = movement.moveSpeed;
            data.dashSpeed = movement.dashSpeed;
            foreach (ElectricBossController boss in FindObjectsByType<ElectricBossController>())
                if (boss.TryGetUnslowedSpeed(movement, out float normalSpeed)) data.moveSpeed = normalSpeed;
        }
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null) { data.critChance = combat.critChance; data.critMultiplier = combat.critMultiplier; }
        ElementalSpellController spells = player.GetComponent<ElementalSpellController>();
        if (spells != null) { data.spellPattern = (int)spells.castPattern; data.lightningKey = (int)spells.lightningKey; data.fireKey = (int)spells.fireKey; data.waterKey = (int)spells.waterKey; }
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            data.gold = inventory.goldCount;
            data.bagItems = CaptureSlots(inventory.slots);
            InventorySlot[] equipmentSlots = inventory.characterPanel != null
                ? Array.FindAll(inventory.characterPanel.GetComponentsInChildren<InventorySlot>(true), slot => slot != null && (slot.equipSlotType == 1 || slot.equipSlotType == 2))
                : new InventorySlot[0];
            data.equippedItems = CaptureSlots(equipmentSlots);
        }
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            data.statPoints = stats.availablePoints;
            data.strength = stats.strength;
            data.agility = stats.agility;
            data.intelligence = stats.intelligence;
        }
        SkillTree skills = player.GetComponent<SkillTree>();
        if (skills != null) { data.skill1 = skills.hasSkill1; data.skill2 = skills.hasSkill2; data.skill3 = skills.hasSkill3; }
        QuestManager quest = QuestManager.instance;
        if (quest != null)
        {
            data.questStage = (int)quest.Stage;
            data.monstersKilled = quest.monstersKilled;
            data.bossDefeated = quest.BossDefeated;
        }

        CaptureSceneProgress();
        data.scenes = new List<SceneProgressData>(world.Values).ToArray();
        return data;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneEnemies.Clear();
        if (scene.name != "Level_02" && scene.name != "Level_03" && scene.name != "Level_04") return;
        MigrateMovedBossState();
        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include))
            sceneEnemies[WorldId(enemy.transform)] = enemy;
        foreach (BossAI boss in FindObjectsByType<BossAI>(FindObjectsInactive.Include))
            sceneEnemies[WorldId(boss.transform)] = boss;
        if (pendingLoad == null) return;
        if (pendingLoad.sceneName != scene.name) return;
        applying = true;
        resumingPosition = pendingLoad.restorePosition;
        ApplyStoryPreferences(pendingLoad);
        StartCoroutine(ApplyAfterSceneStart(pendingLoad, pendingArrivalPoint));
        pendingArrivalPoint = null;
        pendingLoad = null;
    }

    private System.Collections.IEnumerator ApplyAfterSceneStart(SaveSlotData data, string arrivalPoint = null)
    {
        yield return null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { applying = false; yield break; }

        MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
        if (data.saveVersion < 2 || data.restorePosition)
        {
            Vector3 savedPosition = new Vector3(data.positionX, data.positionY, data.positionZ);
            player.transform.position = layout != null ? layout.ResolveSafePosition(savedPosition, player.transform.position) : savedPosition;
        }
        else if (layout != null) player.transform.position = layout.ArrivalPosition(arrivalPoint);
        if (resumingPosition && data.saveVersion >= 3)
        {
            Vector3 scale = player.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (data.facing < 0f ? -1f : 1f);
            player.transform.localScale = scale;
        }
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) { health.RestoreState(data.maxHealth, data.currentHealth); if (data.saveVersion >= 3) { health.physicalArmor = data.physicalArmor; health.magicResistance = data.magicResistance; } }
        PlayerMana mana = player.GetComponent<PlayerMana>();
        if (mana != null) mana.RestoreState(data.maxMana, data.currentMana);
        PlayerLevel playerLevel = player.GetComponent<PlayerLevel>();
        if (playerLevel != null) { playerLevel.level = data.level; playerLevel.currentExp = data.currentExp; playerLevel.expToNextLevel = data.saveVersion >= 3 ? Mathf.Max(1, data.expToNextLevel) : 10 + Mathf.Max(0, data.level - 1) * 5; playerLevel.awakeningCount = data.awakeningCount; playerLevel.UpdateUI(); }
        PlayerStamina stamina = player.GetComponent<PlayerStamina>();
        if (stamina != null) { stamina.maxStamina = data.saveVersion >= 3 ? Mathf.Max(1f, data.maxStamina) : 100f; stamina.currentStamina = data.saveVersion >= 3 ? Mathf.Clamp(data.currentStamina, 0f, stamina.maxStamina) : stamina.maxStamina; }
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null) { movement.moveSpeed = data.saveVersion >= 3 ? data.moveSpeed : movement.moveSpeed + Mathf.Max(0, data.agility - 1) * .5f; movement.dashSpeed = data.saveVersion >= 3 ? data.dashSpeed : movement.dashSpeed + Mathf.Max(0, data.agility - 1); }
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null) { combat.critChance = data.saveVersion >= 3 ? data.critChance : combat.critChance + Mathf.Max(0, data.strength - 1) * .05f; combat.critMultiplier = data.saveVersion >= 3 ? data.critMultiplier : combat.critMultiplier + Mathf.Max(0, data.strength - 1); }
        ElementalSpellController spells = player.GetComponent<ElementalSpellController>();
        if (spells != null && data.saveVersion >= 3) { spells.castPattern = (SpellCastPattern)Mathf.Clamp(data.spellPattern, 0, 2); spells.lightningKey = (KeyCode)data.lightningKey; spells.fireKey = (KeyCode)data.fireKey; spells.waterKey = (KeyCode)data.waterKey; }
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.goldCount = data.gold;
            if (data.saveVersion >= 2)
            {
                RestoreSlots(inventory.slots, data.bagItems);
                InventorySlot[] equipmentSlots = inventory.characterPanel != null
                    ? Array.FindAll(inventory.characterPanel.GetComponentsInChildren<InventorySlot>(true), slot => slot != null && (slot.equipSlotType == 1 || slot.equipSlotType == 2))
                    : new InventorySlot[0];
                RestoreSlots(equipmentSlots, data.equippedItems);
            }
            inventory.RefreshCurrencyUI();
        }
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.availablePoints = data.statPoints;
            stats.strength = data.strength;
            stats.agility = data.agility;
            stats.intelligence = data.intelligence;
            stats.UpdateUI();
        }
        SkillTree skills = player.GetComponent<SkillTree>();
        if (skills != null)
        {
            skills.hasSkill1 = data.skill1;
            skills.hasSkill2 = data.skill2;
            skills.hasSkill3 = data.skill3;
            skills.UpdateSkillUI();
        }
        if (QuestManager.instance != null) QuestManager.instance.RestoreProgress(data.questStage, data.monstersKilled, data.bossDefeated);
        if (world.TryGetValue(SceneManager.GetActiveScene().name, out SceneProgressData progress)) RestoreSceneProgress(progress);
        else if (data.bossDefeated)
            foreach (BossAI defeatedBoss in FindObjectsByType<BossAI>()) defeatedBoss.gameObject.SetActive(false);
        FindAnyObjectByType<RespawnManager>()?.SetCheckpoint(player.transform.position);
        Physics2D.SyncTransforms();
        applying = false;
    }

    private static void ApplyStoryPreferences(SaveSlotData data)
    {
        PlayerPrefs.SetInt("RPG.QuestStage", data.questStage);
        PlayerPrefs.SetInt("RPG.QuestKills", data.monstersKilled);
        PlayerPrefs.SetInt("RPG.BossDefeated", data.bossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("RPG.DeathCount", data.deathCount);
        PlayerPrefs.SetInt("RPG.Level03IntroSeen", data.level03IntroSeen || (data.saveVersion < 3 && data.sceneName == "Level_03") ? 1 : 0);
        PlayerPrefs.SetInt("RPG.RareRespawnWhisperShown", data.rareWhisperShown ? 1 : 0);
        PlayerPrefs.SetInt("RPG.KnightMemory", data.knightMemory ? 1 : 0);
    }

    private void CaptureSceneProgress()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "Level_02" && scene != "Level_03" && scene != "Level_04") return;
        var enemies = new List<WorldEnemySaveData>();
        foreach (var pair in sceneEnemies)
        {
            Component actor = pair.Value;
            var saved = new WorldEnemySaveData { id = pair.Key, defeated = actor == null || !actor.gameObject.activeSelf };
            if (actor != null)
            {
                saved.position = actor.transform.position;
                if (actor is EnemyHealth enemy) { saved.health = enemy.CurrentHealth; saved.defeated |= enemy.IsDead; }
                if (actor is BossAI boss) { saved.health = boss.CurrentHealth; saved.defeated |= boss.IsDying; saved.encounterActive = boss.IsEncounterActive; }
            }
            enemies.Add(saved);
        }
        foreach (EnemyHealth summoned in FindObjectsByType<EnemyHealth>())
            if (!summoned.IsDead && !sceneEnemies.ContainsValue(summoned))
                enemies.Add(new WorldEnemySaveData { id = "Summoned", spawnedMinion = true, health = summoned.CurrentHealth, position = summoned.transform.position });
        var chests = new List<ChestSaveData>();
        foreach (StorageChest chest in FindObjectsByType<StorageChest>(FindObjectsInactive.Include))
            chests.Add(new ChestSaveData { id = WorldId(chest.transform), items = CaptureSlots(chest.chestSlots) });
        world[scene] = new SceneProgressData { sceneName = scene, enemies = enemies.ToArray(), chests = chests.ToArray(), pickups = WorldPickupSaveData.CaptureAll() };
    }

    private void RestoreSceneProgress(SceneProgressData progress)
    {
        if (progress.enemies != null)
        foreach (WorldEnemySaveData saved in progress.enemies)
        {
            if (saved.spawnedMinion)
            {
                BossAI owner = FindAnyObjectByType<BossAI>(FindObjectsInactive.Include);
                if (owner != null && owner.minionPrefab != null && !saved.defeated)
                {
                    MapSceneLayout minionLayout = FindAnyObjectByType<MapSceneLayout>();
                    Vector3 spawn = minionLayout != null ? minionLayout.ResolveSafePosition(saved.position, owner.transform.position + Vector3.right * 2f) : saved.position;
                    GameObject minion = Instantiate(owner.minionPrefab, spawn, Quaternion.identity);
                    minion.GetComponent<EnemyHealth>()?.RestoreHealthState(saved.health);
                }
                continue;
            }
            if (!sceneEnemies.TryGetValue(saved.id, out Component actor))
            {
                // Map replacement changes anchors, not the identity of existing actors.
                string path = WorldPath(saved.id);
                foreach (var pair in sceneEnemies)
                    if (WorldPath(pair.Key) == path) { actor = pair.Value; break; }
            }
            if (actor == null) continue;
            if (saved.defeated) { actor.gameObject.SetActive(false); continue; }
            MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
            actor.transform.position = layout != null ? layout.ResolveSafePosition(saved.position, actor.transform.position) : saved.position;
            if (actor is EnemyHealth enemy) enemy.RestoreHealthState(saved.health);
            if (actor is BossAI boss) boss.RestoreEncounterState(saved.health, saved.encounterActive);
        }
        if (progress.chests != null)
        foreach (StorageChest chest in FindObjectsByType<StorageChest>(FindObjectsInactive.Include))
            foreach (ChestSaveData saved in progress.chests)
                if (saved.id == WorldId(chest.transform) || WorldPath(saved.id) == WorldPath(WorldId(chest.transform))) { RestoreSlots(chest.chestSlots, saved.items); break; }
        WorldPickupSaveData.RestoreAll(progress.pickups);
    }

    private static string WorldPath(string id) { if (string.IsNullOrEmpty(id)) return string.Empty; int at = id.LastIndexOf('@'); return at < 0 ? id : id.Substring(0, at); }

    private void MigrateMovedBossState()
    {
        // Preserve the boss's HP/defeat state from saves made before its arena
        // moved out of Level 03. Never replace a newer Level 04 snapshot.
        if (world.ContainsKey("Level_04") || !world.TryGetValue("Level_03", out SceneProgressData oldForest) || oldForest.enemies == null) return;
        var moved = Array.FindAll(oldForest.enemies, e => e != null && (WorldPath(e.id) == "Boss" || e.spawnedMinion));
        if (!Array.Exists(moved, e => WorldPath(e.id) == "Boss")) return;
        world["Level_04"] = new SceneProgressData { sceneName = "Level_04", enemies = moved, chests = new ChestSaveData[0], pickups = new WorldPickupSaveData[0] };
    }

    public void RegisterRespawnedEnemy(string id, EnemyHealth enemy)
    {
        if (!string.IsNullOrEmpty(id) && enemy != null) sceneEnemies[id] = enemy;
    }

    public static string WorldId(Transform actor)
    {
        Vector3 anchor = actor.position;
        string path = actor.name;
        while (actor.parent != null) { actor = actor.parent; path = actor.name + "/" + path; }
        return path + "@" + anchor.x.ToString("F3", CultureInfo.InvariantCulture) + "," + anchor.y.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static InventoryItemSaveData[] CaptureSlots(InventorySlot[] slots)
    {
        if (slots == null || slots.Length == 0) return new InventoryItemSaveData[0];

        List<InventoryItemSaveData> savedItems = new List<InventoryItemSaveData>();
        for (int index = 0; index < slots.Length; index++)
        {
            InventorySlot slot = slots[index];
            if (slot == null || !slot.isFull || slot.itemData == null || slot.itemAmount <= 0) continue;
            savedItems.Add(new InventoryItemSaveData
            {
                slotIndex = index,
                itemId = ItemCatalog.GetId(slot.itemData),
                amount = slot.itemAmount,
                upgradeLevel = slot.upgradeLevel,
                durability = slot.currentDurability
            });
        }
        return savedItems.ToArray();
    }

    private static void RestoreSlots(InventorySlot[] slots, InventoryItemSaveData[] savedItems)
    {
        if (slots == null) return;
        foreach (InventorySlot slot in slots)
        {
            if (slot != null) slot.ClearSlot();
        }

        if (savedItems == null || savedItems.Length == 0) return;
        ItemCatalog catalog = ItemCatalog.Load();
        if (catalog == null)
        {
            Debug.LogError("Thiếu Resources/ItemCatalog nên không thể khôi phục túi đồ.");
            return;
        }

        foreach (InventoryItemSaveData savedItem in savedItems)
        {
            if (savedItem == null || savedItem.slotIndex < 0 || savedItem.slotIndex >= slots.Length) continue;
            InventorySlot slot = slots[savedItem.slotIndex];
            ItemData item = catalog.Find(savedItem.itemId);
            if (slot == null || item == null) continue;
            slot.AddItem(item, Mathf.Max(1, savedItem.amount), Mathf.Max(0, savedItem.upgradeLevel), Mathf.Clamp(savedItem.durability, 0, 100));
        }
    }

    private static string SlotKey(int slotIndex) => "RPG.SaveSlot." + slotIndex;

    private static void WriteSlot(SaveSlotData data)
    {
        PlayerPrefs.SetString(SlotKey(data.slotIndex), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
