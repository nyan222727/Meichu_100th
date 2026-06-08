using UnityEngine;
using UnityEngine.UI;

public sealed class PowerArrowGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float length = 80f;
    [SerializeField, Min(1f)] private float thickness = 14f;
    [SerializeField, Min(1f)] private float headLength = 42f;
    [SerializeField, Range(10f, 75f)] private float headAngle = 42f;
    [SerializeField, Range(2, 16)] private int capSegments = 8;

    public void SetArrow(float newLength, float newThickness, float newHeadLength, float newHeadAngle, float alpha)
    {
        length = Mathf.Max(0f, newLength);
        thickness = Mathf.Max(1f, newThickness);
        headLength = Mathf.Max(1f, newHeadLength);
        headAngle = Mathf.Clamp(newHeadAngle, 10f, 75f);
        color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));

        float height = Mathf.Max(thickness, Mathf.Sin(headAngle * Mathf.Deg2Rad) * headLength * 2f + thickness);
        rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, length), height);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (length <= 0.01f || thickness <= 0.01f)
        {
            return;
        }

        Color32 vertexColor = color;
        float radius = thickness * 0.5f;
        float angle = headAngle * Mathf.Deg2Rad;
        float effectiveHeadLength = Mathf.Min(headLength, length * 0.72f);
        float headBackLength = Mathf.Cos(angle) * effectiveHeadLength;
        float bodyLength = Mathf.Max(0f, length - headBackLength * 0.5f);

        Vector2 tip = new Vector2(length, 0f);
        AddCapsule(vh, Vector2.zero, new Vector2(bodyLength, 0f), radius, vertexColor, capSegments);

        Vector2 upperBack = tip - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * effectiveHeadLength;
        Vector2 lowerBack = tip - new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * effectiveHeadLength;
        AddCapsule(vh, upperBack, tip, radius, vertexColor, capSegments);
        AddCapsule(vh, lowerBack, tip, radius, vertexColor, capSegments);
    }

    private static void AddCapsule(
        VertexHelper vh,
        Vector2 start,
        Vector2 end,
        float radius,
        Color32 color,
        int segments)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude <= 0.001f)
        {
            AddCircle(vh, start, radius, color, segments * 2);
            return;
        }

        Vector2 direction = delta.normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);

        AddQuad(
            vh,
            start + normal * radius,
            end + normal * radius,
            end - normal * radius,
            start - normal * radius,
            color);

        float baseAngle = Mathf.Atan2(direction.y, direction.x);
        AddArc(vh, start, baseAngle + Mathf.PI * 0.5f, baseAngle + Mathf.PI * 1.5f, radius, color, segments);
        AddArc(vh, end, baseAngle - Mathf.PI * 0.5f, baseAngle + Mathf.PI * 0.5f, radius, color, segments);
    }

    private static void AddCircle(VertexHelper vh, Vector2 center, float radius, Color32 color, int segments)
    {
        int centerIndex = AddVertex(vh, center, color);
        int previousIndex = AddVertex(vh, center + Vector2.right * radius, color);

        for (int index = 1; index <= segments; index++)
        {
            float angle = (index / (float)segments) * Mathf.PI * 2f;
            int nextIndex = AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color);
            vh.AddTriangle(centerIndex, previousIndex, nextIndex);
            previousIndex = nextIndex;
        }
    }

    private static void AddArc(
        VertexHelper vh,
        Vector2 center,
        float startAngle,
        float endAngle,
        float radius,
        Color32 color,
        int segments)
    {
        int centerIndex = AddVertex(vh, center, color);
        int previousIndex = AddVertex(vh, PointOnCircle(center, startAngle, radius), color);

        for (int index = 1; index <= segments; index++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, index / (float)segments);
            int nextIndex = AddVertex(vh, PointOnCircle(center, angle, radius), color);
            vh.AddTriangle(centerIndex, previousIndex, nextIndex);
            previousIndex = nextIndex;
        }
    }

    private static Vector2 PointOnCircle(Vector2 center, float angle, float radius)
    {
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private static void AddQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        Color32 color)
    {
        int startIndex = vh.currentVertCount;
        AddVertex(vh, a, color);
        AddVertex(vh, b, color);
        AddVertex(vh, c, color);
        AddVertex(vh, d, color);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private static int AddVertex(VertexHelper vh, Vector2 position, Color32 color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        int index = vh.currentVertCount;
        vh.AddVert(vertex);
        return index;
    }
}
