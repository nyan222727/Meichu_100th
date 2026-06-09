using UnityEngine;
using System.Collections.Generic;

public sealed class PlayerUltimate
{
    private readonly List<float> scheduledTargetTimes = new List<float>();

    private bool hasTarget;
    private float nextTargetTime;
    private float targetExpiresAt;
    private Vector2 targetViewportPosition;
    private bool scheduleInitialized;
    private int nextScheduledTargetIndex;
    private float windowStartedAt;
    private float activeWindowDuration;
    private int activeSpawnsPerWindow;

    public bool HasTarget => hasTarget;
    public Vector2 TargetViewportPosition => targetViewportPosition;

    public void ScheduleNextTarget(PlayerUltimateConfig config)
    {
        if (config.SpawnsPerWindow <= 0)
        {
            nextTargetTime = float.PositiveInfinity;
            scheduleInitialized = false;
            return;
        }

        if (!scheduleInitialized || HasScheduleConfigChanged(config))
        {
            StartWindow(Time.time, config);
            return;
        }

        if (nextScheduledTargetIndex >= scheduledTargetTimes.Count)
        {
            StartWindow(windowStartedAt + activeWindowDuration, config);
            return;
        }

        nextTargetTime = scheduledTargetTimes[nextScheduledTargetIndex];
        nextScheduledTargetIndex++;
    }

    public void UpdateTarget(Vector2 centerViewportPosition, float radiusScale, PlayerUltimateConfig config, bool log)
    {
        if (hasTarget)
        {
            if (Time.time >= targetExpiresAt)
            {
                hasTarget = false;
                ScheduleNextTarget(config);
            }

            return;
        }

        if (Time.time >= nextTargetTime)
        {
            SpawnTarget(centerViewportPosition, radiusScale, config, log);
        }
    }

    public bool TryTrigger(Vector2 viewportPosition, bool chargeInvalidated, Camera sourceCamera, PlayerUltimateConfig config, float radiusScale, bool log)
    {
        if (!hasTarget || chargeInvalidated)
        {
            return false;
        }

        if (GetTargetDistance(viewportPosition, radiusScale) > config.TargetRadius)
        {
            return false;
        }

        SummonFox(sourceCamera, config, log);
        hasTarget = false;
        ScheduleNextTarget(config);
        return true;
    }

    public bool TryTriggerSegment(Vector2 startViewportPosition, Vector2 endViewportPosition, bool chargeInvalidated, Camera sourceCamera, PlayerUltimateConfig config, float radiusScale, bool log)
    {
        if (!hasTarget || chargeInvalidated)
        {
            return false;
        }

        if (GetTargetSegmentDistance(startViewportPosition, endViewportPosition, radiusScale) > config.TargetRadius)
        {
            return false;
        }

        SummonFox(sourceCamera, config, log);
        hasTarget = false;
        ScheduleNextTarget(config);
        return true;
    }

    private bool HasScheduleConfigChanged(PlayerUltimateConfig config)
    {
        return !Mathf.Approximately(activeWindowDuration, Mathf.Max(0.1f, config.SpawnWindowDuration))
            || activeSpawnsPerWindow != Mathf.Max(0, config.SpawnsPerWindow);
    }

    private void StartWindow(float requestedWindowStart, PlayerUltimateConfig config)
    {
        activeWindowDuration = Mathf.Max(0.1f, config.SpawnWindowDuration);
        activeSpawnsPerWindow = Mathf.Max(0, config.SpawnsPerWindow);
        windowStartedAt = requestedWindowStart;
        while (Time.time >= windowStartedAt + activeWindowDuration)
        {
            windowStartedAt += activeWindowDuration;
        }

        float scheduleStart = Mathf.Max(Time.time, windowStartedAt);
        float scheduleEnd = windowStartedAt + activeWindowDuration;
        scheduledTargetTimes.Clear();
        for (int i = 0; i < activeSpawnsPerWindow; i++)
        {
            scheduledTargetTimes.Add(Random.Range(scheduleStart, scheduleEnd));
        }

        scheduledTargetTimes.Sort();
        nextScheduledTargetIndex = 0;
        scheduleInitialized = true;
        ScheduleNextTarget(config);
    }

    private void SpawnTarget(Vector2 centerViewportPosition, float radiusScale, PlayerUltimateConfig config, bool log)
    {
        float minAngle = Mathf.Clamp(config.TargetFanMinAngle, 0f, 180f);
        float maxAngle = Mathf.Clamp(Mathf.Max(config.TargetFanMaxAngle, minAngle), 0f, 180f);
        float angle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
        float radius = Random.Range(config.OuterMinRadius, config.OuterMaxRadius) * radiusScale;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 targetScreenPosition = ViewportToScreenPoint(centerViewportPosition) + (direction * radius);

        targetViewportPosition = ScreenToViewport(ClampTargetToScreen(targetScreenPosition, config.TargetRadius, radiusScale));
        hasTarget = true;
        targetExpiresAt = Time.time + Mathf.Max(0.1f, config.VisibleDuration);

        if (log)
        {
            Debug.Log("[PlayerUltimate] Fox ultimate target appeared.");
        }
    }

    private float GetTargetDistance(Vector2 viewportPosition, float radiusScale)
    {
        Vector2 screenPosition = ViewportToScreenPoint(viewportPosition);
        Vector2 targetPosition = ViewportToScreenPoint(targetViewportPosition);
        return Vector2.Distance(screenPosition, targetPosition) / Mathf.Max(1f, radiusScale);
    }

    private float GetTargetSegmentDistance(Vector2 startViewportPosition, Vector2 endViewportPosition, float radiusScale)
    {
        Vector2 startScreenPosition = ViewportToScreenPoint(startViewportPosition);
        Vector2 endScreenPosition = ViewportToScreenPoint(endViewportPosition);
        Vector2 targetScreenPosition = ViewportToScreenPoint(targetViewportPosition);
        Vector2 segment = endScreenPosition - startScreenPosition;

        if (segment.sqrMagnitude <= 0.01f)
        {
            return Vector2.Distance(startScreenPosition, targetScreenPosition) / Mathf.Max(1f, radiusScale);
        }

        float t = Vector2.Dot(targetScreenPosition - startScreenPosition, segment) / segment.sqrMagnitude;
        Vector2 closestPoint = startScreenPosition + (Mathf.Clamp01(t) * segment);
        return Vector2.Distance(closestPoint, targetScreenPosition) / Mathf.Max(1f, radiusScale);
    }

    private void SummonFox(Camera sourceCamera, PlayerUltimateConfig config, bool log)
    {
        if (sourceCamera == null)
        {
            Debug.LogWarning("[PlayerUltimate] Missing camera.");
            return;
        }

        if (config.FoxPrefab == null)
        {
            Debug.LogWarning("[PlayerUltimate] Missing fox prefab.");
            return;
        }

        GameObject fox = Object.Instantiate(config.FoxPrefab, GetWorldPosition(sourceCamera, config), Quaternion.identity);
        FoxBehaviour foxBehaviour = fox.GetComponent<FoxBehaviour>();
        if (foxBehaviour == null)
        {
            foxBehaviour = fox.AddComponent<FoxBehaviour>();
        }

        foxBehaviour.Initialize(DamageableFinder.FindFirst(), config.FoxDamage);

        if (log)
        {
            Debug.Log("[PlayerUltimate] Fox ultimate summoned.");
        }
    }

    private Vector3 GetWorldPosition(Camera sourceCamera, PlayerUltimateConfig config)
    {
        Vector3 viewportPosition = new Vector3(
            targetViewportPosition.x,
            targetViewportPosition.y,
            config.SpawnDistanceFromCamera);

        return sourceCamera.ViewportToWorldPoint(viewportPosition);
    }

    private static Vector2 ClampTargetToScreen(Vector2 screenPosition, float targetRadius, float radiusScale)
    {
        float margin = targetRadius * radiusScale;
        return new Vector2(
            Mathf.Clamp(screenPosition.x, margin, Screen.width - margin),
            Mathf.Clamp(screenPosition.y, margin, Screen.height - margin));
    }

    private static Vector2 ScreenToViewport(Vector2 screenPosition)
    {
        return new Vector2(
            Mathf.Clamp01(screenPosition.x / Screen.width),
            Mathf.Clamp01(screenPosition.y / Screen.height));
    }

    private static Vector2 ViewportToScreenPoint(Vector2 viewportPosition)
    {
        return new Vector2(
            viewportPosition.x * Screen.width,
            viewportPosition.y * Screen.height);
    }
}

public struct PlayerUltimateConfig
{
    public GameObject FoxPrefab;
    public float SpawnMinDelay;
    public float SpawnMaxDelay;
    public float SpawnWindowDuration;
    public int SpawnsPerWindow;
    public float VisibleDuration;
    public float TargetRadius;
    public float OuterMinRadius;
    public float OuterMaxRadius;
    public float TargetFanMinAngle;
    public float TargetFanMaxAngle;
    public float SpawnDistanceFromCamera;
    public int FoxDamage;
}
