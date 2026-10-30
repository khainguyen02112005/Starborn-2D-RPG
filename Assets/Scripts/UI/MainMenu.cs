using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject menuContent;

    private void Start()
    {
        Time.timeScale = 1f;
        PauseMenuManager.GameIsPaused = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuContent != null) menuContent.SetActive(true);
    }

    public void StartNewGame()
    {
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.BeginNewGame();
        else SceneManager.LoadScene("Level_01");
    }

    public void ContinueGame()
    {
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.OpenContinueMenu();
        else SceneManager.LoadScene(GameProgressManager.ProgressScene);
    }

    public void OpenAchievements() => SceneManager.LoadScene(GameProgressManager.AchievementScene);

    public void OpenSettings()
    {
        if (menuContent != null) menuContent.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuContent != null) menuContent.SetActive(true);
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
