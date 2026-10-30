using UnityEngine;
using UnityEngine.UI;

// Marks the few UI roots which belong to a cinematic, rather than the gameplay HUD.
[DisallowMultipleComponent]
public sealed class CinematicUiLayer : MonoBehaviour
{
    [SerializeField] private bool manageSorting;
    [SerializeField] private int sortingOrder = 30000;
    private Canvas ownedCanvas;

    private void OnEnable() => ApplySorting();

    private void LateUpdate()
    {
        if (!manageSorting) return;
        if (ownedCanvas == null) ownedCanvas = GetComponent<Canvas>();
        if (ownedCanvas != null && (!ownedCanvas.overrideSorting || ownedCanvas.sortingOrder != sortingOrder)) ApplySorting();
    }

    private void ApplySorting()
    {
        if (!manageSorting) return;
        if (ownedCanvas == null) ownedCanvas = GetComponent<Canvas>();
        if (ownedCanvas == null) return;
        ownedCanvas.overrideSorting = true;
        ownedCanvas.sortingOrder = sortingOrder;
    }

    public static void Configure(GameObject root, int order, bool interactive = false)
    {
        if (root == null) return;
        if (!root.TryGetComponent<CinematicUiLayer>(out var layer)) layer = root.AddComponent<CinematicUiLayer>();
        Canvas canvas = root.GetComponent<Canvas>();
        if (canvas == null) canvas = root.AddComponent<Canvas>();
        layer.manageSorting = true;
        layer.sortingOrder = order;
        layer.ownedCanvas = canvas;
        layer.ApplySorting();
        if (interactive && root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
    }
}
