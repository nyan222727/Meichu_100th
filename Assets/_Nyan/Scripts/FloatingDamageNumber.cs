using UnityEngine;

public sealed class FloatingDamageNumber : MonoBehaviour
{
    private TextMesh textMesh;
    private Camera targetCamera;
    private Vector3 startPosition;
    private Color startColor;
    private float lifetime;
    private float riseDistance;
    private float startScale;
    private float endScale;
    private float elapsed;

    public void Initialize(
        TextMesh targetTextMesh,
        Camera cameraToFace,
        float visibleLifetime,
        float verticalRiseDistance,
        float initialScale,
        float finalScale)
    {
        textMesh = targetTextMesh;
        targetCamera = cameraToFace;
        startPosition = transform.position;
        startColor = textMesh != null ? textMesh.color : Color.white;
        lifetime = Mathf.Max(0.1f, visibleLifetime);
        riseDistance = Mathf.Max(0f, verticalRiseDistance);
        startScale = Mathf.Max(0.01f, initialScale);
        endScale = Mathf.Max(0.01f, finalScale);
        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        if (textMesh == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        float eased = 1f - Mathf.Pow(1f - t, 2f);

        transform.position = startPosition + Vector3.up * (riseDistance * eased);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);
        FaceCamera();

        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, t);
        textMesh.color = color;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void FaceCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 toCamera = transform.position - targetCamera.transform.position;
        if (toCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
