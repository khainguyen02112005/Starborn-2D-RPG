using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    public Slider masterSlider;
    public Slider musicSlider;
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI musicValueText;
    public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("Audio.Master", PlayerPrefs.GetFloat("GameVolume", 1f)));
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("Audio.Music", .8f));
    private static event Action VolumesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        VolumesChanged = null;
        SceneManager.sceneLoaded -= ApplySavedAudioAfterSceneLoad;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallSceneAudioHook()
    {
        SceneManager.sceneLoaded -= ApplySavedAudioAfterSceneLoad;
        SceneManager.sceneLoaded += ApplySavedAudioAfterSceneLoad;
        AudioListener.volume = MasterVolume;
    }

    private static void ApplySavedAudioAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        RefreshAudioMix();
    }

    private void Awake()
    {
        BindSliderEvents();
    }

    private void OnEnable()
    {
        BindSliderEvents();
        VolumesChanged -= RefreshControls;
        VolumesChanged += RefreshControls;
        ApplySavedValues();
    }

    private void OnDisable()
    {
        VolumesChanged -= RefreshControls;
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMaster);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusic);
    }

    private void BindSliderEvents()
    {
        if (masterSlider != null)
        {
            masterSlider.interactable = true;
            masterSlider.onValueChanged.RemoveListener(SetMaster);
            masterSlider.onValueChanged.AddListener(SetMaster);
        }
        if (musicSlider != null)
        {
            musicSlider.interactable = true;
            musicSlider.onValueChanged.RemoveListener(SetMusic);
            musicSlider.onValueChanged.AddListener(SetMusic);
        }
    }

    public void ApplySavedValues()
    {
        RefreshControls();
        RefreshAudioMix();
    }

    private void RefreshControls()
    {
        float master = MasterVolume;
        float music = MusicVolume;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        SetPercent(masterValueText, master);
        SetPercent(musicValueText, music);
    }

    public void SetMaster(float value)
    {
        SetMasterVolume(value);
    }

    public static void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Audio.Master", value);
        PlayerPrefs.SetFloat("GameVolume", value);
        PlayerPrefs.Save();
        RefreshAudioMix();
    }

    public void SetMusic(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Audio.Music", value);
        PlayerPrefs.Save();
        RefreshAudioMix();
    }

    public static void RefreshAudioMix()
    {
        AudioListener.volume = MasterVolume;
        // Sources own their unscaled base gains. Never multiply a previously
        // scaled source again or infer music merely from AudioSource.loop.
        GameAudioManager.RefreshVolumeSettings();
        VolumesChanged?.Invoke();
    }

    private static void SetPercent(TextMeshProUGUI label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
