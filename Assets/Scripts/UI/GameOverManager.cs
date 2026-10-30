using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Existing SoftTouch UI")]
    public GameObject gameOverPanel;
    public Button retryButton;
    public Button homeButton;

    public bool IsGameOver { get; private set; }
    private PlayerHealth fallenPlayer;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver()
    {
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        ShowGameOver(player);
    }

    public void ShowGameOver(PlayerHealth player)
    {
        if (IsGameOver)
        {
            return;
        }

        fallenPlayer = player;
        IsGameOver = true;
        GameAudioManager.PlayUI(GameSound.PlayerDeath, 0.82f, 0.72f);
        PauseMenuManager.GameIsPaused = true;
        if (ItemTooltip.instance != null) ItemTooltip.instance.HideTooltip();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        SetButtonLabelColor(retryButton);
        SetButtonLabelColor(homeButton);

        Time.timeScale = 0f;

        if (EventSystem.current != null && retryButton != null)
        {
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }
    }

    public void RestartGame()
    {
        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        GameAudioManager.PlayUI(GameSound.Revive, 0.82f, 1.04f);
        PlayerHealth player = fallenPlayer != null
            ? fallenPlayer
            : FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        IsGameOver = false;
        ResumeTime();

        if (player == null) return;

        RespawnManager respawn = FindAnyObjectByType<RespawnManager>(FindObjectsInactive.Include);
        if (respawn != null)
        {
            respawn.Respawn(player);
        }
        else
        {
            player.ReviveAt(player.transform.position);
            PlayerMana mana = player.GetComponent<PlayerMana>();
            if (mana != null) mana.RestoreFull();
        }

        fallenPlayer = null;
    }

    public void ReturnToMainMenu()
    {
        ResumeTime();

        if (Application.CanStreamedLevelBeLoaded("Main menu"))
        {
            SceneManager.LoadScene("Main menu");
        }
        else
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.path);
        }
    }

    private static void ResumeTime()
    {
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;
    }

    private static void SetButtonLabelColor(Button button)
    {
        if (button == null) return;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.color = new Color(0.94f, 0.93f, 0.88f, 1f);
    }
}
