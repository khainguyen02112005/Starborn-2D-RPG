using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RespawnWhisperBubble : MonoBehaviour
{
    private const string RareWhisperShownKey = "RPG.RareRespawnWhisperShown";

    [Header("BẢNG THOẠI NHỎ SAU HỒI SINH")]
    [SerializeField, Min(1f)] private float visibleSeconds = 2.65f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.15f, 0f);

    [Header("NHỮNG LẦN CHẾT ĐẦU")]
    [TextArea(1, 3)] public string[] earlyLines =
    {
        "Đau quá…",
        "Cảm giác đó… chân thật quá.",
        "Đầu mình đau quá…",
        "Sao cơ thể mình nặng thế này?"
    };

    [Header("SAU KHI ĐÃ CHẾT VÀI LẦN")]
    [TextArea(1, 3)] public string[] middleLines =
    {
        "Lại là cảm giác này…",
        "Tại sao mình vẫn nhớ cơn đau đó?",
        "Cơ thể đã lành… nhưng sao vẫn còn đau?",
        "Có cảm giác như mình vừa bị kéo trở lại."
    };

    [Header("ẨN Ý VỀ THỨ ĐANG ĐIỀU KHIỂN")]
    [TextArea(1, 3)] public string[] cursorHintLines =
    {
        "Hửm… cái gì vừa di chuyển vậy?",
        "Có gì đó ở gần đây sao?",
        "Tại sao mình lại muốn đi về hướng đó?",
        "Tay mình… vừa tự cử động sao?"
    };

    [Header("KHI ĐÃ CHẾT NHIỀU LẦN")]
    [TextArea(1, 3)] public string[] lateLines =
    {
        "Lại nữa…",
        "Mình không muốn đứng dậy nữa.",
        "Ít nhất… cho mình nằm yên một lúc được không?",
        "Có thứ gì đó không cho phép mình dừng lại."
    };

    private RectTransform bubbleRoot;
    private TextMeshProUGUI bubbleText;
    private Canvas bubbleCanvas;
    private Coroutine activeRoutine;
    private bool lookAtCursor;

    public void ShowAfterRespawn(int deathCount)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine(Mathf.Max(1, deathCount)));
    }

    private IEnumerator ShowRoutine(int deathCount)
    {
        EnsureBubble();
        if (bubbleRoot == null || bubbleText == null) yield break;

        bool showRareLine = deathCount >= 6 && PlayerPrefs.GetInt(RareWhisperShownKey, 0) == 0;
        if (showRareLine)
        {
            PlayerPrefs.SetInt(RareWhisperShownKey, 1);
            PlayerPrefs.Save();
            yield return PresentLine("…Ngươi cũng thấy ta đau, đúng không?", true, 2.7f);
            yield return PresentLine("Mình vừa… nói với ai vậy?", false, 2.25f);
            activeRoutine = null;
            yield break;
        }

        string[] pool;
        if (deathCount <= 2) pool = earlyLines;
        else if (deathCount <= 4 && Random.value < 0.55f) pool = middleLines;
        else if (deathCount >= 5 && Random.value < 0.6f) pool = lateLines;
        else pool = cursorHintLines;

        string line = pool != null && pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : "Đau quá…";
        bool cursorLine = pool == cursorHintLines;
        yield return PresentLine(line, cursorLine, visibleSeconds);
        activeRoutine = null;
    }

    private IEnumerator PresentLine(string line, bool followCursor, float duration)
    {
        bubbleText.text = line;
        bubbleText.alpha = 0f;
        Image background = bubbleRoot.GetComponent<Image>();
        if (background != null)
        {
            Color color = background.color;
            color.a = 0f;
            background.color = color;
        }
        bubbleRoot.gameObject.SetActive(true);
        lookAtCursor = followCursor;

        float fadeTime = 0.22f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyAlpha(Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, duration - fadeTime * 2f));

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyAlpha(1f - Mathf.Clamp01(elapsed / fadeTime));
            yield return null;
        }

        lookAtCursor = false;
        bubbleRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (bubbleRoot == null || !bubbleRoot.gameObject.activeSelf) return;
        bubbleRoot.position = transform.position + worldOffset;
        bubbleRoot.rotation = Quaternion.identity;

        if (!lookAtCursor) return;
        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;
        Vector3 cursorWorld = activeCamera.ScreenToWorldPoint(Input.mousePosition);
        float direction = Mathf.Sign(cursorWorld.x - transform.position.x);
        if (Mathf.Abs(direction) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction >= 0f ? -1f : 1f);
            transform.localScale = scale;
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (bubbleText != null) bubbleText.alpha = alpha;
        Image background = bubbleRoot != null ? bubbleRoot.GetComponent<Image>() : null;
        if (background == null) return;
        Color color = background.color;
        color.a = 0.84f * alpha;
        background.color = color;
    }

    private void EnsureBubble()
    {
        if (bubbleRoot != null) return;

        GameObject root = new GameObject("RespawnWhisperBubble", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(Image));
        root.transform.SetParent(null, false);
        bubbleRoot = root.GetComponent<RectTransform>();
        bubbleRoot.sizeDelta = new Vector2(310f, 82f);
        bubbleRoot.localScale = Vector3.one * 0.0085f;
        bubbleCanvas = root.GetComponent<Canvas>();
        bubbleCanvas.renderMode = RenderMode.WorldSpace;
        bubbleCanvas.sortingOrder = 600;
        GameUITheme.Surface(root.GetComponent<Image>(), "Dialogue", 2);

        GameObject textObject = new GameObject("WhisperText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);
        bubbleText = textObject.GetComponent<TextMeshProUGUI>();
        bubbleText.fontSize = 24f;
        bubbleText.fontStyle = FontStyles.Italic;
        bubbleText.alignment = TextAlignmentOptions.Center;
        GameUITheme.Text(bubbleText);
        bubbleText.textWrappingMode = TextWrappingModes.Normal;
        bubbleText.raycastTarget = false;

        bubbleRoot.position = transform.position + worldOffset;
        bubbleRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (bubbleRoot != null) Destroy(bubbleRoot.gameObject);
    }
}
