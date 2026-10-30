using UnityEngine;

// One owner for conversation input; the book and cinematics never share combat keys.
public static class GameplayUiFocus
{
    public static NPCDialogue DialogueOwner { get; private set; }
    public static BookInventoryUI OpenBook { get; set; }
    public static GameObject EndingChoicePanel { get; set; }
    public static bool HasDialogue => DialogueOwner != null && DialogueOwner.isActiveAndEnabled;
    public static bool HasBook => OpenBook != null && OpenBook.IsOpen;
    public static bool HasEndingChoice => EndingChoicePanel != null && EndingChoicePanel.activeInHierarchy;
    public static bool BlocksControls => HasDialogue || HasBook || HasEndingChoice || Time.timeScale <= 0f || CinematicPresentationController.IsCinematicActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { DialogueOwner = null; OpenBook = null; EndingChoicePanel = null; }

    public static bool BeginDialogue(NPCDialogue owner)
    {
        if (HasDialogue && DialogueOwner != owner || HasBook || HasEndingChoice || Time.timeScale <= 0f || CinematicPresentationController.IsCinematicActive) return false;
        DialogueOwner = owner;
        return true;
    }
    public static void EndDialogue(NPCDialogue owner)
    {
        if (DialogueOwner == owner) DialogueOwner = null;
    }
}
