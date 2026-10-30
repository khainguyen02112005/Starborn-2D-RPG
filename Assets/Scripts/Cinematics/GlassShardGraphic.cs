using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class GlassShardGraphic : MaskableGraphic
{
    [SerializeField] private Texture sourceTexture;
    [SerializeField] private Vector2[] vertices = new Vector2[3];
    [SerializeField] private Vector2[] textureCoordinates = new Vector2[3];
    [SerializeField] private Color edgeColor = new Color(0.82f, 0.94f, 1f, 0.92f);
    [SerializeField, Min(0f)] private float edgeWidth = 2.2f;

    public override Texture mainTexture => sourceTexture != null ? sourceTexture : s_WhiteTexture;

    public void Configure(Texture texture, Vector2[] localVertices, Vector2[] uvs, Color tint, Color border, float borderWidth)
    {
        sourceTexture = texture;
        vertices = localVertices;
        textureCoordinates = uvs;
        color = tint;
        edgeColor = border;
        edgeWidth = borderWidth;
        raycastTarget = false;
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (vertices == null || vertices.Length < 3) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 uv = textureCoordinates != null && i < textureCoordinates.Length ? textureCoordinates[i] : Vector2.zero;
            vh.AddVert(vertices[i], color, uv);
        }

        for (int i = 1; i < vertices.Length - 1; i++)
            vh.AddTriangle(0, i, i + 1);

        if (edgeWidth <= 0f || edgeColor.a <= 0f) return;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[(i + 1) % vertices.Length];
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * edgeWidth * 0.5f;
            int start = vh.currentVertCount;
            Color fadingEdge = edgeColor;
            fadingEdge.a *= color.a;
            // Negative U selects an untextured glint in the cinematic glass shader.
            Vector2 edgeUv = new Vector2(-1f, 0f);
            vh.AddVert(a - normal, fadingEdge, edgeUv);
            vh.AddVert(a + normal, fadingEdge, edgeUv);
            vh.AddVert(b + normal, fadingEdge, edgeUv);
            vh.AddVert(b - normal, fadingEdge, edgeUv);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
