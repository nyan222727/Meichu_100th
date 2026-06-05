using UnityEngine;

public sealed class PlayerCombatHud
{
    public void Draw(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        if (!settings.ShowDebugOverlay)
        {
            return;
        }

        DrawChargeFill(settings, state);
        DrawAttackModeSplitLine(settings, state);
        DrawChargeRing(settings, settings.RedRadius, new Color(1f, 0.15f, 0.05f, 0.9f));
        DrawChargeRing(settings, settings.YellowRadius, new Color(1f, 0.85f, 0.1f, 0.9f));
        DrawChargeRing(settings, settings.GreenRadius, new Color(0.2f, 0.9f, 0.2f, 0.9f));
        DrawChargeRing(settings, settings.BlueRadius, new Color(0.1f, 0.45f, 1f, 0.95f));
        DrawUltimateTarget(settings, state);
        DrawCrosshair(settings);
        DrawPointerMarker(settings, state);
        DrawOverlayLabels(state);
    }

    private void DrawChargeFill(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        if (state.ChargeRatio <= 0f)
        {
            return;
        }

        Vector2 center = ViewportToGuiPoint(settings.ChargeCenterViewport);
        float radius = Mathf.Lerp(settings.BlueRadius * 0.12f, settings.BlueRadius, state.ChargeRatio) * GetChargeRadiusScale();
        float alpha = Mathf.Lerp(0.04f, settings.ChargeFillMaxAlpha, state.ChargeRatio);
        Color fillColor = state.ChargeInvalidated
            ? new Color(1f, 0.1f, 0.08f, Mathf.Min(alpha, 0.1f))
            : new Color(0.1f, 0.55f, 1f, alpha);

        DrawFilledEllipse(settings, center, radius, radius, fillColor);
    }

    private void DrawAttackModeSplitLine(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        float x = settings.AttackModeSplitX * Screen.width;
        DrawLine(new Vector2(x, 0f), new Vector2(x, Screen.height), new Color(0f, 0f, 0f, 0.55f), settings.OverlayLineWidth + 1f);

        float y = (1f - settings.AimViewportPosition.y) * Screen.height;
        float centerX = settings.AimViewportPosition.x * Screen.width;
        float sideMargin = settings.HealthBarSideMargin * Screen.width;
        float centerGap = Mathf.Max(18f, settings.HealthBarCenterGap * GetChargeRadiusScale());
        float leftEdge = Mathf.Clamp(sideMargin, 0f, centerX);
        float rightEdge = Mathf.Clamp(Screen.width - sideMargin, centerX, Screen.width);
        float leftInner = Mathf.Max(leftEdge, centerX - centerGap);
        float rightInner = Mathf.Min(rightEdge, centerX + centerGap);
        float leftHealthLength = Mathf.Max(0f, leftInner - leftEdge) * state.PlayerHealthRatio;
        float rightHealthLength = Mathf.Max(0f, rightEdge - rightInner) * state.PlayerHealthRatio;

        DrawLine(new Vector2(0f, y), new Vector2(Screen.width, y), new Color(0f, 0f, 0f, 0.88f), settings.OverlayLineWidth + 2f);
        DrawLine(new Vector2(leftEdge, y), new Vector2(leftInner, y), new Color(0.12f, 0f, 0f, 0.62f), settings.HealthBarWidth + 3f);
        DrawLine(new Vector2(rightInner, y), new Vector2(rightEdge, y), new Color(0.12f, 0f, 0f, 0.62f), settings.HealthBarWidth + 3f);

        if (state.PlayerHealthRatio <= 0f)
        {
            return;
        }

        Color healthColor = GetPlayerHealthColor(settings, state.PlayerHealthRatio);
        DrawLine(new Vector2(leftInner - leftHealthLength, y), new Vector2(leftInner, y), healthColor, settings.HealthBarWidth);
        DrawLine(new Vector2(rightInner, y), new Vector2(rightInner + rightHealthLength, y), healthColor, settings.HealthBarWidth);
        DrawLine(new Vector2(leftInner - leftHealthLength, y - settings.HealthBarWidth * 0.22f), new Vector2(leftInner, y - settings.HealthBarWidth * 0.22f), new Color(1f, 0.55f, 0.45f, settings.HealthBarAlpha * 0.35f), Mathf.Max(1f, settings.HealthBarWidth * 0.18f));
        DrawLine(new Vector2(rightInner, y - settings.HealthBarWidth * 0.22f), new Vector2(rightInner + rightHealthLength, y - settings.HealthBarWidth * 0.22f), new Color(1f, 0.55f, 0.45f, settings.HealthBarAlpha * 0.35f), Mathf.Max(1f, settings.HealthBarWidth * 0.18f));
    }

    private void DrawChargeRing(PlayerCombatHudSettings settings, float viewportRadius, Color color)
    {
        Vector2 center = ViewportToGuiPoint(settings.ChargeCenterViewport);
        float radius = viewportRadius * GetChargeRadiusScale();
        DrawEllipse(settings, center, radius, radius, color, settings.OverlayLineWidth);
    }

    private void DrawCrosshair(PlayerCombatHudSettings settings)
    {
        Vector2 center = ViewportToGuiPoint(settings.AimViewportPosition);
        float size = Mathf.Max(8f, settings.BlueRadius * Mathf.Min(Screen.width, Screen.height) * 0.28f);
        DrawLine(center + Vector2.left * size, center + Vector2.right * size, Color.white, settings.OverlayLineWidth);
        DrawLine(center + Vector2.down * size, center + Vector2.up * size, Color.white, settings.OverlayLineWidth);
    }

    private void DrawPointerMarker(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        if (!state.HasPointerPosition)
        {
            return;
        }

        Vector2 point = ViewportToGuiPoint(state.LastPointerViewportPosition);
        const float size = 14f;
        DrawLine(point + new Vector2(-size, -size), point + new Vector2(size, size), Color.magenta, settings.OverlayLineWidth);
        DrawLine(point + new Vector2(-size, size), point + new Vector2(size, -size), Color.magenta, settings.OverlayLineWidth);
    }

    private void DrawUltimateTarget(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        if (!state.HasUltimateTarget)
        {
            return;
        }

        float flash = Mathf.PingPong(Time.time * 8f, 1f);
        Color color = Color.Lerp(new Color(1f, 0.45f, 0f, 0.75f), new Color(1f, 0.95f, 0f, 1f), flash);
        Vector2 center = ViewportToGuiPoint(state.UltimateTargetViewportPosition);
        float radius = state.UltimateTargetRadius * GetChargeRadiusScale();

        DrawEllipse(settings, center, radius, radius, color, settings.OverlayLineWidth + 2f);
        DrawLine(center + Vector2.left * radius, center + Vector2.right * radius, color, settings.OverlayLineWidth);
        DrawLine(center + Vector2.down * radius, center + Vector2.up * radius, color, settings.OverlayLineWidth);
    }

    private void DrawOverlayLabels(PlayerCombatHudState state)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(16, Screen.height / 42),
            fontStyle = FontStyle.Bold
        };

        GUI.color = Color.white;
        GUI.Label(new Rect(16f, 12f, Screen.width - 32f, 34f), "Start in BLUE. Release left for melee, right for bow.", style);

        if (!state.HasPointerPosition)
        {
            return;
        }

        string chargeText = state.ChargeInvalidated ? "Invalid" : $"{state.ChargeMultiplier:0.00}x";
        GUI.Label(
            new Rect(16f, 46f, Screen.width - 32f, 34f),
            $"Pointer: {state.AttackModeLabel}, Zone: {state.ChargeZoneLabel}, Charge: {chargeText}",
            style);
    }

    private static Color GetPlayerHealthColor(PlayerCombatHudSettings settings, float healthRatio)
    {
        float alpha = healthRatio <= 0.25f
            ? Mathf.Lerp(settings.HealthBarAlpha * 0.45f, settings.HealthBarAlpha, Mathf.PingPong(Time.time * 5f, 1f))
            : settings.HealthBarAlpha;

        return Color.Lerp(
            new Color(1f, 0.05f, 0.02f, alpha),
            new Color(0.95f, 0.01f, 0.01f, alpha),
            healthRatio);
    }

    private static Vector2 ViewportToGuiPoint(Vector2 viewportPosition)
    {
        return new Vector2(
            viewportPosition.x * Screen.width,
            (1f - viewportPosition.y) * Screen.height);
    }

    private static float GetChargeRadiusScale()
    {
        return Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
    }

    private static void DrawEllipse(PlayerCombatHudSettings settings, Vector2 center, float radiusX, float radiusY, Color color, float width)
    {
        int segments = Mathf.Max(12, settings.OverlaySegments);
        Vector2 previousPoint = center + new Vector2(radiusX, 0f);

        for (int index = 1; index <= segments; index++)
        {
            float angle = (index / (float)segments) * Mathf.PI * 2f;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            DrawLine(previousPoint, nextPoint, color, width);
            previousPoint = nextPoint;
        }
    }

    private static void DrawFilledEllipse(PlayerCombatHudSettings settings, Vector2 center, float radiusX, float radiusY, Color color)
    {
        int segments = Mathf.Max(12, settings.OverlaySegments);
        float rowHeight = Mathf.Max(1f, (radiusY * 2f) / segments);

        Color oldColor = GUI.color;
        GUI.color = color;

        for (int index = 0; index <= segments; index++)
        {
            float y = Mathf.Lerp(-radiusY, radiusY, index / (float)segments);
            float normalizedY = y / radiusY;
            float halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedY * normalizedY)) * radiusX;
            GUI.DrawTexture(
                new Rect(center.x - halfWidth, center.y + y - (rowHeight * 0.5f), halfWidth * 2f, rowHeight),
                Texture2D.whiteTexture);
        }

        GUI.color = oldColor;
    }

    private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        Vector2 delta = end - start;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - (width * 0.5f), delta.magnitude, width), Texture2D.whiteTexture);
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }
}

public struct PlayerCombatHudSettings
{
    public bool ShowDebugOverlay;
    public float AttackModeSplitX;
    public Vector2 ChargeCenterViewport;
    public Vector2 AimViewportPosition;
    public float BlueRadius;
    public float GreenRadius;
    public float YellowRadius;
    public float RedRadius;
    public int OverlaySegments;
    public float OverlayLineWidth;
    public float ChargeFillMaxAlpha;
    public float HealthBarCenterGap;
    public float HealthBarSideMargin;
    public float HealthBarWidth;
    public float HealthBarAlpha;
}

public struct PlayerCombatHudState
{
    public bool HasPointerPosition;
    public Vector2 LastPointerViewportPosition;
    public string AttackModeLabel;
    public string ChargeZoneLabel;
    public bool IsDragging;
    public bool ChargeInvalidated;
    public float ChargeRatio;
    public float ChargeMultiplier;
    public float PlayerHealthRatio;
    public bool HasUltimateTarget;
    public Vector2 UltimateTargetViewportPosition;
    public float UltimateTargetRadius;
}
