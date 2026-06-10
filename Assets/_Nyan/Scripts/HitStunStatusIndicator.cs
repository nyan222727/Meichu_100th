using UnityEngine;
using UnityEngine.Rendering;

public sealed class HitStunStatusIndicator : MonoBehaviour
{
    private const int RingSegments = 28;

    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField, Min(0f)] private float cameraForwardOffset = 0.18f;
    [SerializeField, Min(0.01f)] private float radius = 0.18f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.018f;
    [SerializeField] private Color clockColor = new Color(0.2f, 0.85f, 1f, 1f);

    private Transform clockRoot;
    private LineRenderer ring;
    private LineRenderer hourHand;
    private LineRenderer minuteHand;
    private Material lineMaterial;
    private float remaining;

    public static void ShowOn(Transform target, float duration)
    {
        if (target == null || duration <= 0f)
        {
            return;
        }

        HitStunStatusIndicator indicator = target.GetComponent<HitStunStatusIndicator>();
        if (indicator == null)
        {
            indicator = target.gameObject.AddComponent<HitStunStatusIndicator>();
        }

        indicator.Show(duration);
    }

    public static void ClearOn(Transform target)
    {
        if (target == null)
        {
            return;
        }

        HitStunStatusIndicator indicator = target.GetComponent<HitStunStatusIndicator>();
        if (indicator != null)
        {
            indicator.Hide();
        }
    }

    public void Show(float duration)
    {
        remaining = Mathf.Max(remaining, duration);
        EnsureVisuals();
        clockRoot.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (remaining <= 0f)
        {
            if (clockRoot != null && clockRoot.gameObject.activeSelf)
            {
                clockRoot.gameObject.SetActive(false);
            }

            return;
        }

        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        EnsureVisuals();
        UpdateTransform();
        UpdateHands();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }

    private void EnsureVisuals()
    {
        if (clockRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("HitStunClock");
        clockRoot = rootObject.transform;
        clockRoot.SetParent(transform, false);

        lineMaterial = CreateLineMaterial();

        ring = CreateLine("Ring", RingSegments + 1);
        hourHand = CreateLine("Hour Hand", 2);
        minuteHand = CreateLine("Minute Hand", 2);
        SetRingPoints();
    }

    private void Hide()
    {
        remaining = 0f;
        if (clockRoot != null)
        {
            clockRoot.gameObject.SetActive(false);
        }
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
        material.renderQueue = (int)RenderQueue.Overlay;
        material.SetInt("_ZTest", (int)CompareFunction.Always);
        material.SetInt("_ZWrite", 0);
        return material;
    }

    private LineRenderer CreateLine(string lineName, int positionCount)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(clockRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = false;
        line.positionCount = positionCount;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = lineMaterial;
        line.startColor = clockColor;
        line.endColor = clockColor;
        line.sortingOrder = short.MaxValue;
        return line;
    }

    private void SetRingPoints()
    {
        for (int i = 0; i <= RingSegments; i++)
        {
            float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void UpdateTransform()
    {
        clockRoot.localScale = Vector3.one;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            clockRoot.position = transform.position + worldOffset;
            return;
        }

        Vector3 targetPosition = transform.position + worldOffset;
        Vector3 toCamera = targetPosition - targetCamera.transform.position;
        if (toCamera.sqrMagnitude > 0.001f)
        {
            Vector3 toCameraDirection = toCamera.normalized;
            clockRoot.position = targetPosition - toCameraDirection * cameraForwardOffset;
            clockRoot.rotation = Quaternion.LookRotation(toCameraDirection, Vector3.up);
        }
        else
        {
            clockRoot.position = targetPosition;
        }
    }

    private void UpdateHands()
    {
        float pulse = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
        float minuteAngle = Mathf.Lerp(-35f, 325f, pulse) * Mathf.Deg2Rad;
        SetHand(hourHand, 90f * Mathf.Deg2Rad, radius * 0.42f);
        SetHand(minuteHand, minuteAngle, radius * 0.72f);
    }

    private static void SetHand(LineRenderer hand, float angle, float length)
    {
        hand.SetPosition(0, Vector3.zero);
        hand.SetPosition(1, new Vector3(Mathf.Cos(angle) * length, Mathf.Sin(angle) * length, 0f));
    }
}
