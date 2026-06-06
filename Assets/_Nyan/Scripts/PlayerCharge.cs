using UnityEngine;

public sealed class PlayerCharge
{
    private float accumulatedTravelDistance;
    private Vector2 previousViewportPosition;

    public float AccumulatedTravelDistance => accumulatedTravelDistance;
    public float Ratio { get; private set; }
    public float Multiplier { get; private set; } = 1f;

    public void Begin(Vector2 viewportPosition, float minMultiplier)
    {
        accumulatedTravelDistance = 0f;
        previousViewportPosition = viewportPosition;
        Ratio = 0f;
        Multiplier = minMultiplier;
    }

    public void Update(
        Vector2 viewportPosition,
        float screenScale,
        float travelDistanceForMax,
        float minMultiplier,
        float maxMultiplier)
    {
        accumulatedTravelDistance += GetViewportDistance(previousViewportPosition, viewportPosition, screenScale);
        Ratio = Mathf.Clamp01(accumulatedTravelDistance / Mathf.Max(0.01f, travelDistanceForMax));
        Multiplier = Mathf.Lerp(minMultiplier, maxMultiplier, Ratio);
        previousViewportPosition = viewportPosition;
    }

    public void Reset(float minMultiplier)
    {
        accumulatedTravelDistance = 0f;
        previousViewportPosition = default;
        Ratio = 0f;
        Multiplier = minMultiplier;
    }

    private static float GetViewportDistance(Vector2 fromViewportPosition, Vector2 toViewportPosition, float screenScale)
    {
        Vector2 fromScreenPosition = new Vector2(
            fromViewportPosition.x * Screen.width,
            fromViewportPosition.y * Screen.height);
        Vector2 toScreenPosition = new Vector2(
            toViewportPosition.x * Screen.width,
            toViewportPosition.y * Screen.height);
        return Vector2.Distance(fromScreenPosition, toScreenPosition) / Mathf.Max(1f, screenScale);
    }
}
