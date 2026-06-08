using UnityEngine;

public sealed class DamageNumberFeedback : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.85f, 0f);
    [SerializeField, Min(0f)] private float randomHorizontalOffset = 0.12f;

    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float lifetime = 0.72f;
    [SerializeField, Min(0f)] private float riseDistance = 0.48f;
    [SerializeField, Min(0.01f)] private float startScale = 0.028f;
    [SerializeField, Min(0.01f)] private float endScale = 0.044f;

    [Header("Text")]
    [SerializeField, Min(1)] private int fontSize = 72;
    [SerializeField] private Color normalDamageColor = new Color(1f, 0.28f, 0.16f, 1f);
    [SerializeField] private Color strongDamageColor = new Color(1f, 0.74f, 0.22f, 1f);
    [SerializeField, Min(1)] private int strongDamageThreshold = 40;

    public void Show(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GameObject popup = new GameObject("DamageNumber");
        popup.transform.position = GetSpawnPosition();
        popup.transform.rotation = Quaternion.identity;

        TextMesh textMesh = popup.AddComponent<TextMesh>();
        textMesh.text = "-" + amount;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = 1f;
        textMesh.color = GetDamageColor(amount);

        FloatingDamageNumber floating = popup.AddComponent<FloatingDamageNumber>();
        floating.Initialize(
            textMesh,
            Camera.main,
            lifetime,
            riseDistance,
            startScale,
            endScale);
    }

    private Vector3 GetSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * randomHorizontalOffset;
        return transform.position + worldOffset + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    private Color GetDamageColor(int amount)
    {
        float ratio = Mathf.Clamp01(amount / (float)Mathf.Max(1, strongDamageThreshold));
        return Color.Lerp(normalDamageColor, strongDamageColor, ratio);
    }
}
