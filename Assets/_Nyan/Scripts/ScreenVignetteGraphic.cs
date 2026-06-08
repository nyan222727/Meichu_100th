using UnityEngine;
using UnityEngine.UI;

public sealed class ScreenVignetteGraphic : MaskableGraphic
{
    [SerializeField, Range(0.05f, 0.8f)] private float insetRatio = 0.42f;
    [SerializeField, Range(4, 32)] private int rings = 16;
    [SerializeField, Range(0.5f, 4f)] private float falloff = 2.1f;

    public void SetIntensity(Color tint, float intensity)
    {
        color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(intensity));
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (color.a <= 0.001f)
        {
            return;
        }

        Rect rect = GetPixelAdjustedRect();
        int ringCount = Mathf.Max(1, rings);
        for (int index = 0; index < ringCount; index++)
        {
            float outerT = index / (float)ringCount;
            float innerT = (index + 1) / (float)ringCount;
            Rect outer = Inset(rect, outerT * insetRatio);
            Rect inner = Inset(rect, innerT * insetRatio);

            Color ringColor = color;
            ringColor.a *= Mathf.Pow(1f - outerT, falloff);
            AddFrame(vh, outer, inner, ringColor);
        }
    }

    private static Rect Inset(Rect rect, float ratio)
    {
        float insetX = rect.width * ratio * 0.5f;
        float insetY = rect.height * ratio * 0.5f;
        return new Rect(
            rect.xMin + insetX,
            rect.yMin + insetY,
            rect.width - insetX * 2f,
            rect.height - insetY * 2f);
    }

    private static void AddFrame(VertexHelper vh, Rect outer, Rect inner, Color color)
    {
        Color32 vertexColor = color;
        AddQuad(vh, new Vector2(outer.xMin, outer.yMax), new Vector2(outer.xMax, outer.yMax), new Vector2(inner.xMax, inner.yMax), new Vector2(inner.xMin, inner.yMax), vertexColor);
        AddQuad(vh, new Vector2(inner.xMin, inner.yMin), new Vector2(inner.xMax, inner.yMin), new Vector2(outer.xMax, outer.yMin), new Vector2(outer.xMin, outer.yMin), vertexColor);
        AddQuad(vh, new Vector2(outer.xMin, outer.yMin), new Vector2(inner.xMin, inner.yMin), new Vector2(inner.xMin, inner.yMax), new Vector2(outer.xMin, outer.yMax), vertexColor);
        AddQuad(vh, new Vector2(inner.xMax, inner.yMin), new Vector2(outer.xMax, outer.yMin), new Vector2(outer.xMax, outer.yMax), new Vector2(inner.xMax, inner.yMax), vertexColor);
    }

    private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
    {
        int startIndex = vh.currentVertCount;
        AddVertex(vh, a, color);
        AddVertex(vh, b, color);
        AddVertex(vh, c, color);
        AddVertex(vh, d, color);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Color32 color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vh.AddVert(vertex);
    }
}
