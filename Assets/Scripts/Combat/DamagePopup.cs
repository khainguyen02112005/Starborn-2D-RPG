using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float moveSpeed = .8f;
    private TextMeshPro textMesh;
    private Color textColor;
    private Material ownedMaterial;
    private float elapsed;
    private float lifetime = .95f;
    private Vector3 readableScale = Vector3.one;
    private float drift;
    private bool cinematicNumber;

    public static DamagePopup Show(Vector3 position, int amount, bool critical = false, bool playerHit = false, bool duringCinematic = false)
    {
        if (amount <= 0 || (CinematicPresentationController.IsCinematicActive && !duringCinematic)) return null;
        GameObject prefab = Resources.Load<GameObject>("CombatVFX/DamagePopup");
        if (prefab == null)
        {
            PlayerCombat combat = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerCombat>();
            if (combat != null) prefab = combat.damagePopupPrefab;
        }
        GameObject go = prefab != null ? Instantiate(prefab, position, Quaternion.identity)
            : new GameObject("DamagePopup", typeof(TextMeshPro), typeof(DamagePopup));
        go.transform.position = position;
        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup == null) popup = go.AddComponent<DamagePopup>();
        popup.Setup(amount, critical);
        popup.cinematicNumber = duringCinematic;
        if (duringCinematic) go.name = "BattleDamage_" + amount;
        if (playerHit) popup.SetColor(new Color(1f, .36f, .3f));
        return popup;
    }

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            Debug.LogError("DamagePopup requires a TextMeshPro component.", this);
            Destroy(gameObject);
            return;
        }
        textMesh.fontSize = 24f;
        textMesh.enableAutoSizing = false;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.rectTransform.sizeDelta = new Vector2(4f, 2f);
        if (textMesh.fontSharedMaterial != null)
        {
            ownedMaterial = new Material(textMesh.fontSharedMaterial);
            textMesh.fontSharedMaterial = ownedMaterial;
            textMesh.outlineColor = new Color32(18, 13, 18, 255);
            textMesh.outlineWidth = .2f;
        }
        textMesh.renderer.sortingOrder = 250;
        drift = Random.Range(-.25f, .25f);
        textColor = Color.white;
    }

    void Update()
    {
        if (textMesh == null) return;
        if (CinematicPresentationController.IsCinematicActive && !cinematicNumber) { Destroy(gameObject); return; }
        if (CinematicPresentationController.Instance != null && CinematicPresentationController.Instance.IsSuspended) return;
        float delta = cinematicNumber ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsed += delta;
        if (elapsed >= lifetime) { Destroy(gameObject); return; }
        transform.position += new Vector3(drift, moveSpeed, 0f) * delta;
        float pop = elapsed < .12f ? Mathf.Lerp(.8f, 1.12f, elapsed / .12f) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((elapsed - .12f) / .12f));
        transform.localScale = readableScale * pop;
        textColor.a = 1f - Mathf.Clamp01((elapsed - lifetime * .55f) / (lifetime * .45f));
        textMesh.color = textColor;
    }

    // Hàm gọi khi chém trúng để hiển thị số
    public void Setup(int damageAmount, bool isCrit)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) return;
        textMesh.text = damageAmount.ToString();
        textColor = isCrit ? new Color(1f, .82f, .24f) : Color.white;
        textMesh.color = textColor;
        elapsed = 0f;
        transform.localScale = Vector3.one;
        textMesh.ForceMeshUpdate();
        float worldHeight = Mathf.Max(.01f, textMesh.bounds.size.y);
        Camera camera = Camera.main;
        float desiredHeight = .3f;
        if (camera != null && camera.orthographic && Screen.height > 0)
        {
            // 17/21 pixels regardless of zoom: small combat numbers, not tiny world text.
            desiredHeight = (isCrit ? 21f : 17f) * camera.orthographicSize * 2f / Screen.height;
        }
        readableScale = Vector3.one * (desiredHeight / worldHeight);
        transform.localScale = readableScale;
    }

    private void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
    public void SetColor(Color color) { textColor = color; if (textMesh != null) textMesh.color = color; }
}
