using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Level03IntroCinematic : MonoBehaviour
{
    private const string SeenKey = "RPG.Level03IntroSeen";

    [SerializeField] private float walkDistance = 1.8f;
    [SerializeField] private float walkDuration = 1.2f;
    [SerializeField] private bool playOnlyOnce = true;

    private Transform player;
    private GameObject cinematicRoot;
    private TextMeshProUGUI caption;
    private CinematicPresentationController presentation;

    private IEnumerator Start()
    {
        if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsResumingSavedScene) yield break;
        if (playOnlyOnce && PlayerPrefs.GetInt(SeenKey, 0) == 1) yield break;
        yield return null;
        while (GameProgressManager.Instance != null && GameProgressManager.Instance.IsRestoringState) yield return null;
        yield return null;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null) yield break;
        player = found.transform;
        yield return PlayIntro();
    }

    private IEnumerator PlayIntro()
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        ElementalSpellController spells = player.GetComponent<ElementalSpellController>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        PlayerSPUMAnimation animation = player.GetComponent<PlayerSPUMAnimation>();
        if (movement != null) movement.enabled = false;
        if (combat != null) combat.enabled = false;
        if (spells != null) spells.enabled = false;
        if (body != null) body.linearVelocity = Vector2.zero;

        BuildCinematicUI();
        presentation = CinematicPresentationController.GetOrCreate();
        presentation.BeginSequence(player, PlayerClassController.IsMageSelected, false);
        // Enter along the centre of the northbound stone road, not its wall edge.
        MapSceneLayout layout = FindAnyObjectByType<MapSceneLayout>();
        Vector3 start = player.position;
        if (layout != null) start = layout.ResolveSafePosition(start, layout.ArrivalPosition(null));
        Vector3 end = start + Vector3.up * Mathf.Abs(walkDistance);
        if (layout != null) end = layout.ResolveSafePosition(end, start);
        player.position = start;
        if (body != null) body.position = start;
        presentation.FocusPoint((start + end) * .5f, 5.25f);
        // SPUM's source pose faces left. Movement alone does not turn the avatar.
        Vector3 facingScale = player.localScale;
        facingScale.x = -Mathf.Max(0.001f, Mathf.Abs(facingScale.x)) * Mathf.Sign(walkDistance == 0f ? 1f : walkDistance);
        player.localScale = facingScale;
        animation?.SetMoving(true);
        float elapsed = 0f;
        while (elapsed < walkDuration)
        {
            // Keep the facing authoritative throughout the walk, including late
            // avatar/save initialisation on the first frames after scene loading.
            player.localScale = facingScale;
            if (movement != null) movement.enabled = false;
            elapsed += Time.unscaledDeltaTime;
            player.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / walkDuration));
            yield return null;
        }
        player.position = end;
        if (body != null) body.position = end;
        player.localScale = facingScale;
        animation?.SetMoving(false);
        presentation.Focus(player, 3.8f);
        yield return ShowLine("Không khí ở đây… hoàn toàn khác với ngôi làng. U ám quá.", 2.3f);
        GameAudioManager.Play(GameSound.EnemyAttack, player.position + Vector3.right * 5f, 0.32f, 0.7f);
        yield return ShowLine("Tiếng quái vật vọng lại từ rất xa… Mình không được mất cảnh giác.", 2.3f);

        if (cinematicRoot != null) Destroy(cinematicRoot);
        presentation.EndSequence();
        if (movement != null) movement.enabled = true;
        if (combat != null) combat.enabled = true;
        if (spells != null) spells.enabled = true;
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
    }

    private IEnumerator ShowLine(string line, float seconds)
    {
        if (caption != null) caption.text = "<color=#61D8FF><b>" + PlayerIdentity.Literal(PlayerIdentity.Name) + "</b></color>\n" + line;
        GameAudioManager.PlayUI(GameSound.Dialogue, 0.48f, 1f);
        yield return new WaitForSecondsRealtime(Mathf.Max(seconds, StoryReading.Duration(line)));
    }

    private void BuildCinematicUI()
    {
        Canvas canvas = CinematicPresentationController.FindGameplayCanvas();
        if (canvas == null) return;
        cinematicRoot = new GameObject("Level03IntroCinematic", typeof(RectTransform));
        cinematicRoot.transform.SetParent(canvas.transform, false);
        RectTransform root = cinematicRoot.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        CinematicUiLayer.Configure(cinematicRoot, 30000);

        GameObject bar = new GameObject("CaptionBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(root, false);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(.08f, .025f);
        barRect.anchorMax = new Vector2(.92f, .19f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        GameUITheme.Surface(bar.GetComponent<Image>(), "Dialogue");

        GameObject textObject = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(bar.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.12f);
        textRect.anchorMax = new Vector2(0.92f, 0.88f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        caption = textObject.GetComponent<TextMeshProUGUI>();
        caption.fontSize = 24f;
        GameUITheme.Text(caption);
        caption.alignment = TextAlignmentOptions.Center;
    }
}
