using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small secondary caption docked directly under the lower-right spell bar.
[DisallowMultipleComponent]
public sealed class GameplayControlHints : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    RectTransform rect;
    Canvas canvas;
    Vector2 lastScreen;
    Rect lastSafeArea;
    float lastScale = -1f;
    bool lastMage;
    RectTransform patternDock;

    public static void Bind(TMP_Text text)
    {
        if (text == null || text.transform.parent == null) return;
        var helper = text.transform.parent.GetComponent<GameplayControlHints>();
        if (helper == null) helper = text.transform.parent.gameObject.AddComponent<GameplayControlHints>();
        helper.label = text;
        helper.RefreshLayout();
    }
    void OnEnable() { lastScale = -1f; }
    void Start() { RefreshLayout(); }
    void LateUpdate()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null) return;
        if (lastScreen != new Vector2(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea ||
            !Mathf.Approximately(lastScale, canvas.scaleFactor) || lastMage != PlayerClassController.IsMageSelected)
            RefreshLayout();
    }
    public void RefreshLayout()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        if (rect == null) rect = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (rect == null || canvas == null) return;

        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        float fontPixels = Mathf.Clamp(Screen.height * .015f, 7f, 13f);
        float paddingX = 6f / scale, paddingY = 3f / scale;
        if (patternDock == null)
        {
            var bar = canvas.transform.Find("ElementalSpellBar");
            if (bar != null) patternDock = bar.Find("PatternButton") as RectTransform;
        }
        float maxWidth;
        if (patternDock != null)
        {
            if (rect.parent != patternDock) rect.SetParent(patternDock, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -3f / scale);
            maxWidth = patternDock.rect.width;
        }
        else
        {
            // Matching Edit Mode preview; the spell bar itself is built at runtime.
            rect.anchorMin = new Vector2(.7253f, .1086f); rect.anchorMax = new Vector2(.9797f, .1086f);
            rect.pivot = new Vector2(.5f, 1f); rect.anchoredPosition = new Vector2(0, -3f / scale);
            maxWidth = ((RectTransform)rect.parent).rect.width * .2544f;
        }
        GameUITheme.Text(label);
        label.fontSharedMaterial = label.font.material;
        label.fontSize = fontPixels / scale;
        label.enableAutoSizing = false; label.richText = true;
        label.extraPadding = true; label.raycastTarget = false;
        label.fontStyle = FontStyles.Normal; label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = 1f / scale;
        label.text = Key("B") + " Túi đồ   ·   " + Key("V") + " Chỉ số / kỹ năng\n" +
            Key("J") + (PlayerClassController.IsMageSelected ? " Ma đạn   ·   " : " Chém   ·   ") +
            Key("K") + " Đỡ   ·   " + Key("Shift") + " Lướt";
        Vector2 preferred = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity);
        float fit = Mathf.Min(1f, Mathf.Max(1f, maxWidth - paddingX * 2f) / Mathf.Max(1f, preferred.x));
        if (fit < 1f)
        {
            label.fontSize *= fit;
            preferred = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity);
        }
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(0f, preferred.y + paddingY * 2f);
        var textRect = label.rectTransform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(paddingX, paddingY); textRect.offsetMax = new Vector2(-paddingX, -paddingY);
        textRect.localScale = Vector3.one;
        var image = GetComponent<Image>();
        if (image != null) { GameUITheme.Surface(image, "Dialogue", 1.3f); image.raycastTarget = false; }
        var outline = GetComponent<Outline>(); if (outline != null) outline.enabled = false;
        var group = GetComponent<CanvasGroup>();
        if (group != null) { group.interactable = false; group.blocksRaycasts = false; }
        lastScreen = new Vector2(Screen.width, Screen.height); lastSafeArea = Screen.safeArea;
        lastScale = scale; lastMage = PlayerClassController.IsMageSelected;
    }
    static string Key(string key) => "<color=#E5BC70>" + key + "</color>";
}
