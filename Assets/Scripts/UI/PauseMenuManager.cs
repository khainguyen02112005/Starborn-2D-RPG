using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused;
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    public Slider volumeSlider;
    public bool CinematicLock { get; set; }

    private void Start()
    {
        Time.timeScale = 1f;
        GameIsPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        float volume = PlayerPrefs.GetFloat("Audio.Master", PlayerPrefs.GetFloat("GameVolume", 1f));
        AudioSettingsManager.RefreshAudioMix();
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(volume);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || IsGameOverActive() || CinematicPresentationController.IsCinematicActive) return;
        if (GameIsPaused) Resume(); else Pause();
    }

    public void Resume()
    {
        if (CinematicLock) return;
        if (IsGameOverActive()) return;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void SaveGame()
    {
        if (CinematicLock) return;
        if (GameProgressManager.Instance != null) GameProgressManager.Instance.OpenSaveMenu();
        else SceneManager.LoadScene(GameProgressManager.ProgressScene);
    }

    public void OpenSettings()
    {
        if (CinematicLock) return;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        if (CinematicLock) return;
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
    }

    public void SetVolume(float volume)
    {
        AudioSettingsManager.SetMasterVolume(volume);
    }

    public void QuitGame()
    {
        if (CinematicLock) return;
        GameProgressManager.Instance?.SaveActiveSlot();
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene("Main menu");
    }

    private static bool IsGameOverActive()
    {
        GameOverManager manager = GameOverManager.Instance;
        if (manager == null) manager = FindAnyObjectByType<GameOverManager>(FindObjectsInactive.Include);
        return manager != null && manager.IsGameOver;
    }
}
