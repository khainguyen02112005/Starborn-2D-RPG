using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    public string targetScene = "Level_03";
    public bool requireRoyalQuest;
    [Tooltip("Tên điểm đến trong MapSceneLayout của scene đích.")]
    public string arrivalPointName;
    [Tooltip("Khóa cổng rời đấu trường cho đến khi boss bị tiêu diệt.")]
    public bool requireBossDefeatedToLeave;
    public TextMeshProUGUI promptText;
    private bool playerNear;
    private bool travelling;
    private bool wasExitLocked;

    private void Update()
    {
        if (playerNear && requireBossDefeatedToLeave && wasExitLocked != IsExitLocked)
        {
            wasExitLocked = IsExitLocked;
            if (promptText != null) promptText.text = wasExitLocked ? "Cổng bị phong ấn — hãy đánh bại Boss." : "Phong ấn đã tan. Nhấn E để rời đấu trường.";
        }
        if (!playerNear || !Input.GetKeyDown(KeyCode.E)) return;
        TryTravel();
    }

    public bool TryTravel()
    {
        if (!Application.isPlaying || travelling || !playerNear || Time.timeScale <= 0f || CinematicPresentationController.IsCinematicActive) return false;
        if (IsExitLocked)
        {
            if (promptText != null) promptText.text = "Cổng đã bị phong ấn. Hãy đánh bại Boss để rời khỏi đây.";
            GameAudioManager.PlayUI(GameSound.UIError, .65f);
            return false;
        }
        if (requireRoyalQuest && QuestManager.instance != null && QuestManager.instance.Stage < StoryQuestStage.RoyalBossQuest)
        {
            if (promptText != null) promptText.text = "Hãy nhận chiếu chỉ từ trưởng làng trước.";
            GameAudioManager.PlayUI(GameSound.UIError, 0.72f);
            return false;
        }
        GameAudioManager.Play(GameSound.Portal, transform.position, 0.92f, 1.04f);
        GameProgressManager.Instance?.SaveActiveSlot();
        GameProgressManager.Instance?.PrepareSceneTransition(targetScene, arrivalPointName);
        PlayerPrefs.SetString("NextScene", targetScene);
        PlayerPrefs.Save();
        travelling = true;
        SceneManager.LoadSceneAsync("LoadingScene");
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = true;
        if (promptText != null) promptText.text = IsExitLocked ? "Cổng bị phong ấn — hãy đánh bại Boss." : "Nhấn E để đến " + targetScene;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        if (promptText != null) promptText.text = string.Empty;
    }

    public bool IsExitLocked => requireBossDefeatedToLeave
        && !(QuestManager.instance != null ? QuestManager.instance.BossDefeated : PlayerPrefs.GetInt("RPG.BossDefeated", 0) == 1);
}
