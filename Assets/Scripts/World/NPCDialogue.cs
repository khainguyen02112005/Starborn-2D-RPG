using System.Collections;
using TMPro;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    public enum DialogueRole { VillageElder, EndingElder, Generic }
    public DialogueRole role = DialogueRole.VillageElder;
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerText;
    [Header("THÔNG TIN NGƯỜI NÓI")]
    public string speakerName = "TRƯỞNG LÀNG";
    [Header("THOẠI CHUNG - CHỈ DÙNG KHI ROLE LÀ GENERIC")]
    [TextArea(2, 5)] public string[] lines;
    public float typingSpeed = 0.025f;
    public Level04Controller endingDirector;

    [Header("THOẠI TRƯỞNG LÀNG - CHƯA NHẬN NHIỆM VỤ")]
    [TextArea(2, 5)] public string[] notStartedLines =
    {
        "TRƯỞNG LÀNG|Cuối cùng thì cậu cũng trở về. Ngoài làng dạo gần đây xuất hiện ngày càng nhiều quái vật.",
        "NHÂN VẬT|Tôi vừa gặp một đám quái vật trên đường về. Có vẻ chúng đang tiến gần hơn đến làng.",
        "TRƯỞNG LÀNG|Ta lo rằng đó mới chỉ là khởi đầu. Phía sau làng còn vài con rất hung dữ, người dân đang cố ngăn chúng lại.",
        "TRƯỞNG LÀNG|Hãy ra ngoài đó và tiêu diệt 3 con quái vật. Ta sẽ trả công hậu hĩnh cho cậu.",
        "TRƯỞNG LÀNG|Khi hoàn thành, hãy quay lại gặp ta. Phần thưởng là 10 đồng vàng."
    };

    [Header("THOẠI TRƯỞNG LÀNG - ĐANG ĐÁNH QUÁI")]
    [Tooltip("Có thể dùng {killed} và {target} trong câu thoại.")]
    [TextArea(2, 5)] public string[] huntingLines =
    {
        "Ngươi đã hạ {killed}/{target} con.",
        "Hãy cẩn thận và quay lại khi con đường đã an toàn."
    };

    [Header("THOẠI TRƯỞNG LÀNG - TRẢ NHIỆM VỤ ĐẦU")]
    [TextArea(2, 5)] public string[] readyToTurnInLines =
    {
        "TRƯỞNG LÀNG|Cậu trở về rồi à. Ta có thể nhìn thấy dấu vết của trận chiến trên người cậu.",
        "NHÂN VẬT|Ba con quái vật đã bị tiêu diệt. Khu vực quanh làng tạm thời an toàn rồi.",
        "TRƯỞNG LÀNG|Làm tốt lắm. Đây là 10 đồng vàng như ta đã hứa.",
        "TRƯỞNG LÀNG|Hãy nghỉ ngơi đi. Ta có cảm giác chuyện này vẫn chưa kết thúc."
    };

    [Header("THOẠI TRƯỞNG LÀNG - NHẬN CHIẾU CHỈ")]
    [TextArea(2, 5)] public string[] royalNoticeLines =
    {
        "HỆ THỐNG|Một ngày sau…",
        "TRƯỞNG LÀNG|Cậu đến đúng lúc. Sáng nay, sứ giả của nhà vua vừa mang đến một sắc lệnh.",
        "NHÂN VẬT|Có chuyện gì xảy ra sao?",
        "TRƯỞNG LÀNG|Một con quái vật khổng lồ đã xuất hiện tại vùng đất phía bên kia khu rừng.",
        "TRƯỞNG LÀNG|Nhà vua tuyên bố rằng bất kỳ ai tiêu diệt được nó sẽ nhận được một điều ước.",
        "NHÂN VẬT|Một điều ước… bất kỳ điều gì sao?",
        "TRƯỞNG LÀNG|Đó là những gì được viết trong sắc lệnh. Nhưng rất nhiều chiến binh đã đến đó và không ai quay trở về.",
        "NHÂN VẬT|Nếu con quái vật đó có thể đe dọa ngôi làng thì tôi không thể đứng yên được.",
        "TRƯỞNG LÀNG|Ta biết cậu sẽ nói như vậy. Hãy chuẩn bị thật kỹ rồi đi theo con đường phía bắc."
    };

    [Header("THOẠI TRƯỞNG LÀNG - NHIỆM VỤ BOSS")]
    [TextArea(2, 5)] public string[] bossQuestLines =
    {
        "TRƯỞNG LÀNG|Con đường phía bắc dẫn đến vùng đất quái vật. Ở đó có cả quái thường lẫn quái tinh anh.",
        "TRƯỞNG LÀNG|Hãy nâng cấp trang bị và kỹ năng trước khi tiến vào sào huyệt của Boss."
    };

    [Header("THOẠI TRẢ BOSS - CHƯA TỪNG CHẾT")]
    [TextArea(2, 5)] public string[] bossTurnInNoDeathLines =
    {
        "TRƯỞNG LÀNG|Cậu đã trở về! Tin tức về chiến thắng của cậu đã đến hoàng cung.",
        "TRƯỞNG LÀNG|Nhà vua sẽ thực hiện điều ước như đã hứa. Hãy nói đi, cậu mong muốn điều gì?"
    };

    [Header("THOẠI TRẢ BOSS - ĐÃ TỪNG CHẾT")]
    [TextArea(2, 5)] public string[] bossTurnInAfterDeathLines =
    {
        "TRƯỞNG LÀNG|Cậu đã trở về! Tin tức về chiến thắng của cậu đã đến hoàng cung.",
        "TRƯỞNG LÀNG|Nhà vua sẽ thực hiện điều ước như đã hứa. Hãy nói đi, cậu mong muốn điều gì?"
    };

    [Header("THOẠI KHI CỐT TRUYỆN ĐÃ HOÀN THÀNH")]
    [TextArea(2, 5)] public string[] completedLines =
    {
        "Điều ước đã được trao. Câu chuyện đang bắt đầu rạn vỡ..."
    };

    [Header("THOẠI TRƯỞNG LÀNG Ở ĐOẠN KẾT")]
    [TextArea(2, 5)] public string[] endingNoDeathLines =
    {
        "TRƯỞNG LÀNG|Cậu đã tiêu diệt Chúa Tể Lôi Thú mà chưa một lần gục ngã.",
        "TRƯỞNG LÀNG|Theo sắc lệnh của nhà vua, hãy nói điều ước của cậu."
    };
    [TextArea(2, 5)] public string[] endingAfterDeathLines =
    {
        "TRƯỞNG LÀNG|Cậu đã trở về… nhưng ánh mắt ấy không còn giống trước kia.",
        "TRƯỞNG LÀNG|Theo sắc lệnh, cậu được ban một điều ước. Cậu muốn gì?"
    };

    private string[] activeLines;
    private int index;
    private bool playerNear;
    private bool typing;
    private string currentBody;

    private void Update()
    {
        if (!playerNear || dialoguePanel == null || dialogueText == null) return;
        if (!dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.E)) OpenDialogue();
        else if (dialoguePanel.activeSelf && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))) Advance();
    }

    private void OpenDialogue()
    {
        if (GameplayUiFocus.BlocksControls) return;
        if (role == DialogueRole.VillageElder && QuestManager.instance != null &&
            QuestManager.instance.Stage == StoryQuestStage.FirstQuestCompleted)
        {
            if (endingDirector == null) endingDirector = FindAnyObjectByType<Level04Controller>();
            if (endingDirector != null && endingDirector.BeginRoyalDecreeSequence(() =>
                {
                    if (QuestManager.instance != null && QuestManager.instance.Stage == StoryQuestStage.FirstQuestCompleted)
                        QuestManager.instance.AcceptRoyalQuest();
                }))
                return;
        }

        activeLines = role == DialogueRole.VillageElder ? BuildElderLines()
            : role == DialogueRole.EndingElder ? BuildEndingElderLines()
            : lines;
        if (activeLines == null || activeLines.Length == 0) activeLines = new[] { "..." };
        index = 0;
        if (speakerText != null) speakerText.text = string.IsNullOrWhiteSpace(speakerName)
            ? gameObject.name.ToUpperInvariant()
            : speakerName;
        if (!GameplayUiFocus.BeginDialogue(this)) return;
        dialoguePanel.SetActive(true);
        GameAudioManager.PlayUI(GameSound.Dialogue, 0.58f, role == DialogueRole.EndingElder ? 0.86f : 1f);
        StartCoroutine(TypeLine());
    }

    private string[] BuildElderLines()
    {
        if (QuestManager.instance == null) return FormatLines(notStartedLines);
        switch (QuestManager.instance.Stage)
        {
            case StoryQuestStage.NotStarted:
                return FormatLines(notStartedLines);
            case StoryQuestStage.HuntingMonsters:
                return FormatLines(huntingLines);
            case StoryQuestStage.ReadyToTurnIn:
                return FormatLines(readyToTurnInLines);
            case StoryQuestStage.FirstQuestCompleted:
                return FormatLines(royalNoticeLines);
            case StoryQuestStage.RoyalBossQuest:
                return FormatLines(bossQuestLines);
            case StoryQuestStage.BossReadyToTurnIn:
                int deaths = GameProgressManager.Instance != null
                    ? GameProgressManager.Instance.DeathCount
                    : PlayerPrefs.GetInt("RPG.DeathCount", 0);
                return deaths == 0
                    ? FormatLines(bossTurnInNoDeathLines)
                    : FormatLines(bossTurnInAfterDeathLines);
            default:
                return FormatLines(completedLines);
        }
    }

    private string[] BuildEndingElderLines()
    {
        int deaths = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.DeathCount
            : PlayerPrefs.GetInt("RPG.DeathCount", 0);
        if (deaths == 0)
            return FormatLines(endingNoDeathLines);
        return FormatLines(endingAfterDeathLines);
    }

    private string[] FormatLines(string[] source)
    {
        if (source == null || source.Length == 0) return new[] { "..." };
        int killed = QuestManager.instance != null ? QuestManager.instance.monstersKilled : 0;
        int deaths = GameProgressManager.Instance != null
            ? GameProgressManager.Instance.DeathCount
            : PlayerPrefs.GetInt("RPG.DeathCount", 0);
        string[] result = new string[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            string value = source[i] ?? string.Empty;
            result[i] = value
                .Replace("{killed}", killed.ToString())
                .Replace("{target}", "3")
                .Replace("{deaths}", deaths.ToString());
        }
        return result;
    }

    private void Advance()
    {
        if (typing)
        {
            StopAllCoroutines();
            dialogueText.text = currentBody;
            typing = false;
            return;
        }
        if (++index < activeLines.Length)
        {
            GameAudioManager.PlayUI(GameSound.Dialogue, 0.48f, 1.02f);
            StartCoroutine(TypeLine());
        }
        else CloseDialogue(true);
    }

    private IEnumerator TypeLine()
    {
        typing = true;
        ResolveLine(activeLines[index], out string activeSpeaker, out currentBody);
        if (speakerText != null) speakerText.text = activeSpeaker;
        dialogueText.text = string.Empty;
        foreach (char character in currentBody)
        {
            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        typing = false;
    }

    private void ResolveLine(string rawLine, out string activeSpeaker, out string body)
    {
        string fallbackSpeaker = string.IsNullOrWhiteSpace(speakerName)
            ? gameObject.name.ToUpperInvariant()
            : speakerName;
        rawLine ??= string.Empty;
        int separator = rawLine.IndexOf('|');
        if (separator > 0)
        {
            activeSpeaker = PlayerIdentity.Literal(PlayerIdentity.ResolveSpeaker(rawLine.Substring(0, separator).Trim()));
            body = rawLine.Substring(separator + 1).Trim();
            return;
        }
        activeSpeaker = PlayerIdentity.Literal(PlayerIdentity.ResolveSpeaker(fallbackSpeaker));
        body = rawLine;
    }

    private void CloseDialogue(bool applyOutcome)
    {
        GameplayUiFocus.EndDialogue(this);
        StopAllCoroutines();
        typing = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = string.Empty;
        index = 0;
        if (!applyOutcome) return;
        if (role == DialogueRole.EndingElder)
        {
            GameAudioManager.PlayUI(GameSound.QuestComplete, 0.9f, 0.92f);
            if (QuestManager.instance != null) QuestManager.instance.CompleteBossQuest();
            if (endingDirector == null) endingDirector = FindAnyObjectByType<Level04Controller>();
            endingDirector?.BeginEndingSequence();
            return;
        }
        if (role != DialogueRole.VillageElder || QuestManager.instance == null) return;

        switch (QuestManager.instance.Stage)
        {
            case StoryQuestStage.NotStarted:
                QuestManager.instance.StartQuest();
                break;
            case StoryQuestStage.ReadyToTurnIn:
                QuestManager.instance.TurnInFirstQuest();
                GameAudioManager.PlayUI(GameSound.QuestComplete, 0.92f, 1.08f);
                break;
            case StoryQuestStage.FirstQuestCompleted:
                QuestManager.instance.AcceptRoyalQuest();
                break;
            case StoryQuestStage.BossReadyToTurnIn:
                QuestManager.instance.CompleteBossQuest();
                GameAudioManager.PlayUI(GameSound.QuestComplete, 1f, 0.92f);
                if (endingDirector == null) endingDirector = FindAnyObjectByType<Level04Controller>();
                if (endingDirector != null) endingDirector.BeginEndingSequence();
                else Debug.LogError("Level 02 chưa có Ending Director để chạy đoạn kết.", this);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNear = true;
    }

    private void OnDisable() => GameplayUiFocus.EndDialogue(this);

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        if (dialoguePanel != null && dialoguePanel.activeSelf) CloseDialogue(false);
    }
}
