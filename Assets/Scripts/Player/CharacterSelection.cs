using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    public string selectedClass = "Hiệp Sĩ";
    public TMP_InputField nameInputField;
    public Button confirmButton;
    public TextMeshProUGUI nameValidationText;

    [Header("Selection presentation")]
    public GameObject knightGlow;
    public GameObject mageGlow;
    public CanvasGroup knightGroup;
    public CanvasGroup mageGroup;
    public SpriteRenderer[] knightRenderers;
    public SpriteRenderer[] mageRenderers;
    public TextMeshProUGUI knightName;
    public TextMeshProUGUI mageName;
    public Image knightCard;
    public Image mageCard;
    public GameObject knightSelectedBadge;
    public GameObject mageSelectedBadge;
    public Color selectedNameColor = new Color(0.35f, 0.95f, 1f, 1f);
    public Color unselectedNameColor = new Color(0.64f, 0.69f, 0.76f, 1f);

    private Color[] knightOriginalColors;
    private Color[] mageOriginalColors;

    private void Awake()
    {
        knightOriginalColors = CaptureColors(knightRenderers);
        mageOriginalColors = CaptureColors(mageRenderers);
    }

    private void Start()
    {
        SelectKnight();
        if (nameInputField != null) nameInputField.onValueChanged.AddListener(RefreshNameValidation);
        RefreshNameValidation(nameInputField != null ? nameInputField.text : "");
    }

    public static bool IsValidName(string value)
    {
        string name = (value ?? "").Replace("\u200B", "").Trim();
        return name.Length > 0 && name.Length <= 20;
    }

    private void RefreshNameValidation(string value)
    {
        bool valid = IsValidName(value);
        if (confirmButton != null) confirmButton.interactable = valid;
        if (nameValidationText != null) nameValidationText.text = valid ? "" : "Hãy nhập tên nhân vật (1–20 ký tự).";
    }

    public void SelectKnight()
    {
        selectedClass = "Hiệp Sĩ";
        ApplySelection(true);
    }

    public void SelectMage()
    {
        selectedClass = "Pháp Sư";
        ApplySelection(false);
    }

    private void ApplySelection(bool knightSelected)
    {
        if (knightGlow != null) knightGlow.SetActive(knightSelected);
        if (mageGlow != null) mageGlow.SetActive(!knightSelected);
        if (knightGroup != null) knightGroup.alpha = knightSelected ? 1f : 0.28f;
        if (mageGroup != null) mageGroup.alpha = knightSelected ? 0.28f : 1f;
        if (knightName != null) knightName.color = knightSelected ? selectedNameColor : unselectedNameColor;
        if (mageName != null) mageName.color = knightSelected ? unselectedNameColor : selectedNameColor;
        if (knightCard != null) knightCard.color = knightSelected ? Color.white : new Color(.55f, .55f, .48f, 1);
        if (mageCard != null) mageCard.color = knightSelected ? new Color(.55f, .55f, .48f, 1) : Color.white;
        if (knightSelectedBadge != null) knightSelectedBadge.SetActive(knightSelected);
        if (mageSelectedBadge != null) mageSelectedBadge.SetActive(!knightSelected);
        TintRenderers(knightRenderers, knightOriginalColors, knightSelected);
        TintRenderers(mageRenderers, mageOriginalColors, !knightSelected);
    }

    private static Color[] CaptureColors(SpriteRenderer[] renderers)
    {
        if (renderers == null) return new Color[0];
        Color[] colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        return colors;
    }

    private static void TintRenderers(SpriteRenderer[] renderers, Color[] originalColors, bool selected)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null) continue;
            Color original = originalColors != null && i < originalColors.Length ? originalColors[i] : Color.white;
            renderer.color = selected
                ? original
                : new Color(original.r * 0.42f, original.g * 0.46f, original.b * 0.56f, original.a * 0.82f);
        }
    }

    public void BackToMenu() => SceneManager.LoadScene("Main menu");

    public void SubmitCharacter()
    {
        if (nameInputField == null)
        {
            Debug.LogError("Character Selection is missing its name input field.", this);
            return;
        }

        if (!IsValidName(nameInputField.text))
        {
            RefreshNameValidation(nameInputField.text);
            nameInputField.ActivateInputField();
            GameAudioManager.PlayUI(GameSound.UIError, .6f);
            return;
        }
        PlayerIdentity.SetName(nameInputField.text.Replace("\u200B", ""));
        nameInputField.text = PlayerIdentity.Name;
        PlayerPrefs.SetString("PlayerClass", selectedClass);
        PlayerPrefs.SetString("NextScene", "Level_02");
        PlayerPrefs.SetInt("RPG.PendingOpeningCutscene", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoadingScene");
    }
}
