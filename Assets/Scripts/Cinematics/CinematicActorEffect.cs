using System.Collections.Generic;
using UnityEngine;

// Fragments copy the actual SPUM pieces. No redrawn NPC/hero stand-in.
public sealed class CinematicActorEffect : MonoBehaviour
{
    private sealed class Piece
    {
        public SpriteRenderer source;
        public SpriteRenderer copy;
        public bool wasEnabled;
        public Vector3 drift;
        public Color color;
    }
    private readonly List<Piece> pieces = new List<Piece>();
    private Material material;
    private float lifetime;
    private float elapsed;
    private bool replace;
    private bool reform;

    public void Initialize(Transform actor, string state, float seconds, bool replace, float size = 1f)
    {
        lifetime = Mathf.Max(.1f, seconds);
        this.replace = replace;
        reform = state == "Reform";
        Shader shader = Resources.Load<Shader>("CinematicGenerated/SPUMCorruption");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader);
        if (actor == null) return;
        foreach (SpriteRenderer source in actor.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!source.enabled || source.sprite == null || source.color.a <= .01f || source.GetComponentInParent<CinematicActorEffect>() != null) continue;
            var part = new GameObject(source.name + "_Signal", typeof(SpriteRenderer));
            part.transform.SetParent(transform, false);
            var copy = part.GetComponent<SpriteRenderer>();
            copy.sprite = source.sprite;
            copy.sharedMaterial = material;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = source.sortingOrder + 1;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;
            pieces.Add(new Piece { source = source, copy = copy, wasEnabled = source.enabled, color = source.color, drift = (Vector3)Random.insideUnitCircle * .6f + Vector3.up * .25f });
            if (replace) source.enabled = false;
        }
        UpdatePieces();
    }

    private void Update()
    {
        var presentation = CinematicPresentationController.Instance;
        if (presentation != null && presentation.IsSuspended) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= lifetime) Destroy(gameObject);
    }
    private void LateUpdate() => UpdatePieces();
    private void UpdatePieces()
    {
        float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, lifetime));
        float dissolve = replace ? (reform ? 1f - t : t) : 0f;
        if (material != null)
        {
            material.SetFloat("_Dissolve", dissolve);
            material.SetFloat("_Phase", elapsed);
            material.SetFloat("_Flicker", replace ? .1f : 1f);
        }
        foreach (Piece part in pieces)
        {
            if (part.source == null || part.copy == null) continue;
            Transform src = part.source.transform;
            part.copy.transform.SetPositionAndRotation(src.position + part.drift * dissolve, src.rotation);
            part.copy.transform.localScale = src.lossyScale;
            part.copy.sprite = part.source.sprite;
            Color color = replace ? part.color : part.source.color;
            color.a *= replace ? 1f : .1f + .2f * Mathf.Abs(Mathf.Sin(elapsed * 17f));
            part.copy.color = color;
            if (replace) part.source.enabled = false;
        }
    }
    private void OnDestroy()
    {
        foreach (Piece part in pieces) if (part.source != null && replace) part.source.enabled = part.wasEnabled;
        if (material != null) Destroy(material);
    }
}
