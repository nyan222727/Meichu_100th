using UnityEngine;

public class MeteorWarning : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float minScale = 0.85f;
    public float maxScale = 1.15f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = baseScale * scale;
    }
}
