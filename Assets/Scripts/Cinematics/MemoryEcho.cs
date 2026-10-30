using TMPro;
using UnityEngine;

public class MemoryEcho : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI messageText;

    private void Awake()
    {
        // Ký ức của vòng lặp được kể bằng cutscene tại đầu làng (Level 02).
        // Không hiển thị nó như một banner trên màn chọn nhân vật.
        if (panel != null) panel.SetActive(false);
    }
}
