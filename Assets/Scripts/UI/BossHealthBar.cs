using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar instance; // Singleton để các Boss dễ dàng gọi tới
    
    public Slider slider;
    public TextMeshProUGUI bossNameText;
    private int reportedHealth;
    private int reportedMaximum = 1;
    private bool showing;
    
    void Awake()
    {
        instance = this;
        ConfigureReadOnlyLayout();
        gameObject.SetActive(false); // Ẩn đi khi không đánh Boss
    }
    
    public void ShowBossHealth(string name, int maxHealth)
    {
        gameObject.SetActive(true);
        ConfigureReadOnlyLayout();
        if (bossNameText != null) bossNameText.text = name;
        showing = true;
        reportedMaximum = Mathf.Max(1, maxHealth);
        reportedHealth = reportedMaximum;
        
        if (slider != null)
        {
            slider.maxValue = reportedMaximum;
            slider.SetValueWithoutNotify(reportedHealth);
        }
    }
    
    public void UpdateHealth(int currentHealth)
    {
        reportedHealth = Mathf.Clamp(currentHealth, 0, reportedMaximum);
        if (slider != null) slider.SetValueWithoutNotify(reportedHealth);
        if (currentHealth <= 0)
        {
            showing = false;
            gameObject.SetActive(false); // Ẩn UI khi Boss chết
        }
    }

    private void LateUpdate()
    {
        if (showing && slider != null && !Mathf.Approximately(slider.value, reportedHealth))
            slider.SetValueWithoutNotify(reportedHealth);
    }

    public void ConfigureReadOnlyLayout()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        slider.interactable = false;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.transition = Selectable.Transition.None;
        slider.minValue = 0f;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;
        slider.handleRect = null;
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        RectTransform area = transform.Find("Fill Area") as RectTransform;
        RectTransform background = transform.Find("Background") as RectTransform;
        if (area == null && background != null) area = background.Find("Fill Area") as RectTransform;
        if (area != null && background != null)
        {
            area.SetParent(background, false);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(3f, 3f); area.offsetMax = new Vector2(-3f, -3f);
        }
        if (slider.fillRect != null)
        {
            slider.fillRect.offsetMin = slider.fillRect.offsetMax = Vector2.zero;
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null) { fill.type = Image.Type.Simple; fill.preserveAspect = false; }
        }
        if (bossNameText != null)
        {
            var rect = bossNameText.rectTransform;
            rect.anchorMin = new Vector2(0.04f, 0.6f); rect.anchorMax = new Vector2(0.96f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            bossNameText.alignment = TextAlignmentOptions.Center;
            bossNameText.textWrappingMode = TextWrappingModes.NoWrap;
            bossNameText.enableAutoSizing = true; bossNameText.fontSizeMin = 16f; bossNameText.fontSizeMax = 24f;
        }
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
