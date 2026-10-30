using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatHudVisibility : MonoBehaviour
{
    private CanvasGroup group;
    private bool hidden;
    private float alpha;
    private bool interactable, blocksRaycasts;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }
    private void LateUpdate()
    {
        bool shouldHide = GameplayUiFocus.BlocksControls;
        if (shouldHide == hidden || group == null) return;
        if (shouldHide)
        {
            alpha = group.alpha; interactable = group.interactable; blocksRaycasts = group.blocksRaycasts;
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        }
        else Restore();
        hidden = shouldHide;
    }
    private void Restore()
    {
        if (group == null) return;
        group.alpha = alpha; group.interactable = interactable; group.blocksRaycasts = blocksRaycasts;
    }
    private void OnDisable() { if (hidden) Restore(); hidden = false; }
}
