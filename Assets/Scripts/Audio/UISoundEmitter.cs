using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
[DisallowMultipleComponent]
public class UISoundEmitter : MonoBehaviour, IPointerUpHandler
{
    private Button button;
    private Toggle toggle;
    private Slider slider;

    private void Awake()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (button != null) button.onClick.AddListener(PlayClick);
        if (toggle != null) toggle.onValueChanged.AddListener(PlayToggle);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
        if (toggle != null) toggle.onValueChanged.RemoveListener(PlayToggle);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (slider != null && slider.IsInteractable()) GameAudioManager.PlayUI(GameSound.UIToggle, 0.5f);
    }

    private void PlayClick()
    {
        string lowerName = gameObject.name.ToLowerInvariant();
        GameSound sound = lowerName.Contains("close") || lowerName.Contains("back") || lowerName.Contains("cancel") || lowerName.Contains("thoát")
            ? GameSound.UICancel
            : lowerName.Contains("buy") || lowerName.Contains("confirm") || lowerName.Contains("start") || lowerName.Contains("save")
                ? GameSound.UIConfirm
                : GameSound.UIClick;
        GameAudioManager.PlayUI(sound, 0.75f);
    }

    private void PlayToggle(bool value)
    {
        GameAudioManager.PlayUI(GameSound.UIToggle, 0.62f, value ? 1.08f : 0.92f);
    }
}
