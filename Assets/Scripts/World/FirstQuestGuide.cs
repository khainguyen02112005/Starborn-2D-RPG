using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FirstQuestGuide : MonoBehaviour
{
    public NPCDialogue elder;
    public Transform player;
    private RectTransform canvasRect, marker;
    private GameObject root;
    private TextMeshProUGUI hint;
    private TextMeshProUGUI pointer;
    private Camera worldCamera;

    private void Start()
    {
        if (elder == null)
            foreach (var npc in FindObjectsByType<NPCDialogue>())
                if (npc.role == NPCDialogue.DialogueRole.VillageElder) { elder = npc; break; }
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Canvas canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null) return;
        canvasRect = canvas.transform as RectTransform;
        root = new GameObject("FirstQuestDirection", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvasRect, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var group = root.GetComponent<CanvasGroup>(); group.interactable = group.blocksRaycasts = false;
        var banner = new GameObject("ReceiveQuestHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        banner.transform.SetParent(root.transform, false);
        var bannerRect = banner.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(.32f, .89f); bannerRect.anchorMax = new Vector2(.70f, .97f);
        bannerRect.offsetMin = bannerRect.offsetMax = Vector2.zero;
        GameUITheme.Surface(banner.GetComponent<Image>(), "Dialogue"); banner.GetComponent<Image>().raycastTarget = false;
        hint = Label(banner.transform, "Hint", 20);
        hint.rectTransform.offsetMin = new Vector2(14, 3); hint.rectTransform.offsetMax = new Vector2(-14, -3);
        pointer = Label(root.transform, "ElderPointer", 31);
        pointer.text = "!\n▼"; pointer.color = GameUITheme.Gold;
        marker = pointer.rectTransform; marker.anchorMin = marker.anchorMax = new Vector2(.5f, .5f);
        marker.sizeDelta = new Vector2(64, 86); marker.offsetMin = new Vector2(-32,-43); marker.offsetMax = new Vector2(32,43);
    }
    private static TextMeshProUGUI Label(Transform parent, string name, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false); var text = go.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        text.fontSize = size; text.alignment = TextAlignmentOptions.Center; GameUITheme.Text(text);
        return text;
    }
    private void LateUpdate()
    {
        if (root == null) return;
        bool visible = elder != null && player != null && QuestManager.instance != null
            && QuestManager.instance.Stage == StoryQuestStage.NotStarted && !GameplayUiFocus.BlocksControls;
        root.SetActive(visible); if (!visible) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return;
        float distance = Vector2.Distance(player.position, elder.transform.position);
        hint.text = distance < 2.3f ? "Đã đến Trưởng làng • Nhấn E để nhận nhiệm vụ" : "Đến dấu ! gặp Trưởng làng\nW A S D: di chuyển • E: trò chuyện";
        Canvas canvas = canvasRect.GetComponent<Canvas>();
        Vector2 screen = worldCamera.WorldToScreenPoint(elder.transform.position + Vector3.up * 1.5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 point);
        Rect bounds = canvasRect.rect;
        bool offscreen = point.x < bounds.xMin + 85 || point.x > bounds.xMax - 85 || point.y < bounds.yMin + 150 || point.y > bounds.yMax - 150;
        if (offscreen)
            pointer.text = "!\n" + (Mathf.Abs(point.x) > Mathf.Abs(point.y) ? (point.x > 0 ? "→" : "←") : (point.y > 0 ? "↑" : "↓"));
        else pointer.text = "!\n▼";
        point.x = Mathf.Clamp(point.x, bounds.xMin + 85, bounds.xMax - 85);
        point.y = Mathf.Clamp(point.y, bounds.yMin + 150, bounds.yMax - 150);
        marker.anchoredPosition = point + Vector2.up * Mathf.Sin(Time.unscaledTime * 3f) * 4;
    }
    private void OnDestroy() { if (root != null) Destroy(root); }
}
