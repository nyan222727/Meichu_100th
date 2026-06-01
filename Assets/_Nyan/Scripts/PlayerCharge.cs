using UnityEngine;

public sealed class PlayerCharge
{
    private bool hasExitedBlueZone;
    private bool invalidated;
    private float accumulatedDistance;
    private float multiplier = 1f;
    private Vector2 previousViewportPosition;
    private Vector2 exitDirection;

    public bool IsInvalidated => invalidated;
    public float Multiplier => multiplier;

    public void Begin(Vector2 viewportPosition, float minMultiplier)
    {
        hasExitedBlueZone = false;
        invalidated = false;
        accumulatedDistance = 0f;
        multiplier = minMultiplier;
        previousViewportPosition = viewportPosition;
        exitDirection = Vector2.zero;
    }

    public void Reset(float minMultiplier)
    {
        hasExitedBlueZone = false;
        invalidated = false;
        accumulatedDistance = 0f;
        multiplier = minMultiplier;
        previousViewportPosition = default;
        exitDirection = Vector2.zero;
    }

    public void Update(
        Vector2 viewportPosition,
        Vector2 centerViewportPosition,
        float radiusScale,
        float blueRadius,
        float chargeDistanceForMax,
        float minMultiplier,
        float maxMultiplier,
        float reverseDirectionDotThreshold,
        float reverseInvalidationGraceDistance)
    {
        float distanceFromCenter = GetViewportDistance(viewportPosition, centerViewportPosition, radiusScale);

        if (!hasExitedBlueZone)
        {
            if (distanceFromCenter <= blueRadius)
            {
                accumulatedDistance += GetViewportDistance(previousViewportPosition, viewportPosition, radiusScale);
                multiplier = GetCurrentMultiplier(chargeDistanceForMax, minMultiplier, maxMultiplier);
            }
            else
            {
                hasExitedBlueZone = true;
                exitDirection = GetDirectionFromCenter(viewportPosition, centerViewportPosition);
            }
        }
        else if (!invalidated)
        {
            if (distanceFromCenter < blueRadius - reverseInvalidationGraceDistance)
            {
                invalidated = true;
            }
            else
            {
                Vector2 currentDirection = GetDirectionFromCenter(viewportPosition, centerViewportPosition);
                if (exitDirection != Vector2.zero && Vector2.Dot(currentDirection, exitDirection) <= reverseDirectionDotThreshold)
                {
                    invalidated = true;
                }
            }

            multiplier = GetCurrentMultiplier(chargeDistanceForMax, minMultiplier, maxMultiplier);
        }

        previousViewportPosition = viewportPosition;
    }

    public float GetRatio(float minMultiplier, float maxMultiplier)
    {
        if (maxMultiplier <= minMultiplier)
        {
            return 0f;
        }

        return Mathf.InverseLerp(minMultiplier, maxMultiplier, multiplier);
    }

    private float GetCurrentMultiplier(float chargeDistanceForMax, float minMultiplier, float maxMultiplier)
    {
        if (invalidated)
        {
            return minMultiplier;
        }

        float chargeRatio = Mathf.Clamp01(accumulatedDistance / Mathf.Max(0.01f, chargeDistanceForMax));
        return Mathf.Lerp(minMultiplier, maxMultiplier, chargeRatio);
    }

    private static float GetViewportDistance(Vector2 fromViewportPosition, Vector2 toViewportPosition, float radiusScale)
    {
        Vector2 fromScreenPosition = ViewportToScreenPoint(fromViewportPosition);
        Vector2 toScreenPosition = ViewportToScreenPoint(toViewportPosition);
        return Vector2.Distance(fromScreenPosition, toScreenPosition) / Mathf.Max(1f, radiusScale);
    }

    private static Vector2 GetDirectionFromCenter(Vector2 viewportPosition, Vector2 centerViewportPosition)
    {
        Vector2 direction = ViewportToScreenPoint(viewportPosition) - ViewportToScreenPoint(centerViewportPosition);
        return direction.sqrMagnitude <= 0.0001f ? Vector2.zero : direction.normalized;
    }

    private static Vector2 ViewportToScreenPoint(Vector2 viewportPosition)
    {
        return new Vector2(
            viewportPosition.x * Screen.width,
            viewportPosition.y * Screen.height);
    }
}
