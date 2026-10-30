using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StarBornTitleAnimation : MonoBehaviour
{
    public float Elapsed { get; private set; }
    private Image image;
    private Material original, owned;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        image = GetComponent<Image>();
        if (image == null || image.material == null) return;
        original = image.material;
        owned = new Material(original) { name = "StarBorn_Title_Runtime" };
        image.material = owned;
        Elapsed = 0;
        owned.SetFloat("_Reveal", 0);
    }

    private void Update()
    {
        if (owned == null) return;
        Elapsed += Time.unscaledDeltaTime;
        owned.SetFloat("_Phase", Elapsed);
        owned.SetFloat("_Reveal", Mathf.SmoothStep(0, 1, Mathf.Clamp01(Elapsed / .65f)));
    }

    private void OnDisable()
    {
        if (image != null && owned != null && image.material == owned) image.material = original;
        if (owned != null) Destroy(owned);
        owned = null;
    }
}
