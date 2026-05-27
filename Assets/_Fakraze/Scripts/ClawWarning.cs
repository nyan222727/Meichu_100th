using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ClawWarning : MonoBehaviour
{
    public int segmentCount = 40;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        meshRenderer = GetComponent<MeshRenderer>();
        Hide();
    }

    public void Show(float range, float angle)
    {
        GenerateSectorMesh(range, angle);
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        meshRenderer.enabled = false;
    }

    private void GenerateSectorMesh(float range, float angle)
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[segmentCount + 2];
        int[] triangles = new int[segmentCount * 3];

        vertices[0] = Vector3.zero;

        float startAngle = -angle * 0.5f;
        float step = angle / segmentCount;

        for (int i = 0; i <= segmentCount; i++)
        {
            float currentAngle = startAngle + step * i;
            float rad = currentAngle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * range;
            float z = Mathf.Cos(rad) * range;

            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}