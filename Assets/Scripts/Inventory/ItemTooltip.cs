using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip instance;
    public TextMeshProUGUI tooltipText;
    public GameObject tooltipPanel;

    [Header("Bố cục gọn")]
    public Vector2 cursorOffset = new Vector2(24f, -24f);
    public float screenPadding = 14f;

    private RectTransform tooltipRect;
    private RectTransform canvasRect;
    private Canvas rootCanvas;

    private void Awake()
    {
        instance = this;
        CacheLayout();
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            FollowCursorInsideCanvas();
        }
    }

    public void ShowTooltip(string text)
    {
        if (tooltipText != null) tooltipText.text = text;
        if (tooltipPanel == null) return;

        CacheLayout();
        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling();
        FollowCursorInsideCanvas();
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void CacheLayout()
    {
        if (tooltipPanel == null) tooltipPanel = gameObject;
        if (tooltipText == null && tooltipPanel != null)
            tooltipText = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        tooltipRect = tooltipPanel != null ? tooltipPanel.GetComponent<RectTransform>() : null;
        rootCanvas = tooltipPanel != null ? tooltipPanel.GetComponentInParent<Canvas>()?.rootCanvas : null;
        canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
    }

    private void FollowCursorInsideCanvas()
    {
        if (tooltipRect == null || canvasRect == null) return;

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
            return;

        float scale = Mathf.Max(0.01f, rootCanvas.scaleFactor);
        Vector2 offset = cursorOffset / scale;
        Vector2 desired = localPoint + offset;
        Vector2 size = tooltipRect.rect.size;
        Rect bounds = canvasRect.rect;
        float padding = screenPadding / scale;

        desired.x = Mathf.Clamp(desired.x, bounds.xMin + size.x * 0.5f + padding, bounds.xMax - size.x * 0.5f - padding);
        desired.y = Mathf.Clamp(desired.y, bounds.yMin + size.y * 0.5f + padding, bounds.yMax - size.y * 0.5f - padding);
        tooltipRect.anchoredPosition = desired;
    }
}
