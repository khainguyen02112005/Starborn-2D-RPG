using TMPro;
using UnityEngine;

public enum StoryQuestStage
{
    NotStarted,
    HuntingMonsters,
    ReadyToTurnIn,
    FirstQuestCompleted,
    RoyalBossQuest,
    BossReadyToTurnIn,
    StoryCompleted
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    public TextMeshProUGUI questText;
    public int monstersToKill = 3;
    public int firstQuestReward = 10;
    public int monstersKilled;
    public bool isQuestActive;

    public StoryQuestStage Stage { get; private set; }
    public bool BossDefeated { get; private set; }

    private void Awake()
    {
        instance = this;
        Stage = (StoryQuestStage)PlayerPrefs.GetInt("RPG.QuestStage", 0);
        monstersKilled = PlayerPrefs.GetInt("RPG.QuestKills", 0);
        BossDefeated = PlayerPrefs.GetInt("RPG.BossDefeated", 0) == 1;
        if (BossDefeated && Stage < StoryQuestStage.BossReadyToTurnIn) Stage = StoryQuestStage.BossReadyToTurnIn;
        RefreshState();
    }

    public void StartQuest()
    {
        if (Stage != StoryQuestStage.NotStarted) return;
        Stage = StoryQuestStage.HuntingMonsters;
        monstersKilled = 0;
        Persist();
    }

    public void AddKill()
    {
        if (Stage != StoryQuestStage.HuntingMonsters) return;
        monstersKilled = Mathf.Min(monstersToKill, monstersKilled + 1);
        if (monstersKilled >= monstersToKill) Stage = StoryQuestStage.ReadyToTurnIn;
        Persist();
    }

    public bool TurnInFirstQuest()
    {
        if (Stage != StoryQuestStage.ReadyToTurnIn) return false;
        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>();
        if (inventory != null) inventory.AddGold(firstQuestReward);
        Stage = StoryQuestStage.FirstQuestCompleted;
        Persist();
        return true;
    }

    public void AcceptRoyalQuest()
    {
        if (Stage != StoryQuestStage.FirstQuestCompleted) return;
        Stage = StoryQuestStage.RoyalBossQuest;
        Persist();
    }

    public void NotifyBossDefeated()
    {
        BossDefeated = true;
        Stage = StoryQuestStage.BossReadyToTurnIn;
        Persist();
    }

    public void CompleteBossQuest()
    {
        if (Stage != StoryQuestStage.BossReadyToTurnIn) return;
        Stage = StoryQuestStage.StoryCompleted;
        Persist();
    }

    public void RestoreProgress(int stage, int kills, bool bossDefeated)
    {
        Stage = (StoryQuestStage)Mathf.Clamp(stage, 0, (int)StoryQuestStage.StoryCompleted);
        monstersKilled = Mathf.Clamp(kills, 0, monstersToKill);
        BossDefeated = bossDefeated;
        Persist();
    }

    private void RefreshState()
    {
        isQuestActive = Stage == StoryQuestStage.HuntingMonsters || Stage == StoryQuestStage.RoyalBossQuest;
        if (questText == null) return;
        switch (Stage)
        {
            case StoryQuestStage.NotStarted:
                questText.text = "NHIỆM VỤ\nGặp trưởng làng ở đầu làng";
                break;
            case StoryQuestStage.HuntingMonsters:
                questText.text = $"BẢO VỆ NGÔI LÀNG\nTiêu diệt quái vật  {monstersKilled}/{monstersToKill}";
                break;
            case StoryQuestStage.ReadyToTurnIn:
                questText.text = "BẢO VỆ NGÔI LÀNG\nĐã hoàn thành — về gặp trưởng làng";
                break;
            case StoryQuestStage.FirstQuestCompleted:
                questText.text = "TIN TỪ HOÀNG GIA\nHãy hỏi trưởng làng về chiếu chỉ mới";
                break;
            case StoryQuestStage.RoyalBossQuest:
                questText.text = "ĐIỀU ƯỚC CỦA NHÀ VUA\nQua rừng Level 03, đến Level 04 tiêu diệt Boss";
                break;
            case StoryQuestStage.BossReadyToTurnIn:
                questText.text = "CHIẾN THẮNG\nQuay về làng nhận điều ước";
                break;
            default:
                questText.text = "CỐT TRUYỆN\nĐiều ước đang chờ được lựa chọn";
                break;
        }
    }

    private void Persist()
    {
        PlayerPrefs.SetInt("RPG.QuestStage", (int)Stage);
        PlayerPrefs.SetInt("RPG.QuestKills", monstersKilled);
        PlayerPrefs.SetInt("RPG.BossDefeated", BossDefeated ? 1 : 0);
        PlayerPrefs.Save();
        RefreshState();
        GameProgressManager.Instance?.SaveActiveSlot();
    }
}
