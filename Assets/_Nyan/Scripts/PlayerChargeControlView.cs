using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerChargeControlView : MonoBehaviour
{
    [Header("Charge Control References")]
    [SerializeField] private Image chargeArc;
    [SerializeField] private Image meleeIcon;
    [SerializeField] private Image rangedIcon;
    [SerializeField] private RectTransform powerArrowPivot;
    [SerializeField] private Image powerArrow;

    [Header("Runtime Behaviour")]
    [SerializeField] private float powerArrowSpriteForwardAngle;

    [Header("Charge Feedback")]
    [SerializeField] private Color chargingColor = new Color(0.35f, 0.84f, 0.77f, 0.92f);
    [SerializeField] private Color hitStunReadyColor = new Color(1f, 0.71f, 0.37f, 0.96f);
    [SerializeField] private Color fullChargeColor = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField, Min(0f)] private float skillReadyPulseSpeed = 6f;
    [SerializeField, Range(0f, 0.5f)] private float skillReadyBrightness = 0.28f;
    [SerializeField, Range(0f, 0.2f)] private float skillReadyScaleAmount = 0.06f;
    [SerializeField, Min(0f)] private float fullChargePulseSpeed = 5f;
    [SerializeField, Range(0f, 0.4f)] private float fullChargePulseAmount = 0.16f;

    private Color powerArrowBaseColor;
    private Vector3 chargeArcBaseScale;
    private bool initialized;
    private bool warnedMissingReferences;

    private void Awake()
    {
        Initialize();
        gameObject.SetActive(false);
    }

    public bool IsValid()
    {
        return ValidateReferences();
    }

    public void Apply(PlayerCombatHudSettings settings, PlayerCombatHudState state, Vector2 rootSize)
    {
        gameObject.SetActive(state.IsDragging);
        if (!state.IsDragging || !Initialize())
        {
            return;
        }

        SetViewportPosition(settings.ChargeCenterViewport, rootSize);
        chargeArc.fillAmount = Mathf.Clamp01(state.ChargeRatio);
        chargeArc.color = GetChargeColor(state.ChargeRatio, settings.HitStunChargeThreshold);
        UpdateSkillReadyPulse(state.ChargeRatio, settings.HitStunChargeThreshold);

        bool melee = state.AttackModeLabel == "Melee";
        meleeIcon.gameObject.SetActive(melee);
        rangedIcon.gameObject.SetActive(!melee);

        UpdatePowerArrow(settings, state, rootSize);
    }

    private void OnDisable()
    {
        if (initialized && chargeArc != null)
        {
            chargeArc.rectTransform.localScale = chargeArcBaseScale;
        }
    }

    private Color GetChargeColor(float chargeRatio, float hitStunThreshold)
    {
        float ratio = Mathf.Clamp01(chargeRatio);
        float threshold = Mathf.Clamp(hitStunThreshold, 0.01f, 0.99f);
        Color color = ratio < threshold
            ? Color.Lerp(chargingColor, hitStunReadyColor, ratio / threshold)
            : Color.Lerp(hitStunReadyColor, fullChargeColor, (ratio - threshold) / (1f - threshold));

        if (ratio >= threshold)
        {
            float readyPulse = GetPulse(skillReadyPulseSpeed);
            float brightness = Mathf.Lerp(skillReadyBrightness * 0.45f, skillReadyBrightness, readyPulse);

            if (ratio >= 0.999f)
            {
                brightness += GetPulse(fullChargePulseSpeed) * fullChargePulseAmount;
            }

            color = Color.Lerp(color, Color.white, Mathf.Clamp01(brightness));
            color.a = 1f;
        }

        return color;
    }

    private void UpdateSkillReadyPulse(float chargeRatio, float hitStunThreshold)
    {
        float threshold = Mathf.Clamp(hitStunThreshold, 0.01f, 0.99f);
        if (chargeRatio < threshold)
        {
            chargeArc.rectTransform.localScale = chargeArcBaseScale;
            return;
        }

        float pulse = GetPulse(skillReadyPulseSpeed);
        float scaleAmount = Mathf.Lerp(skillReadyScaleAmount * 0.35f, skillReadyScaleAmount, pulse);

        if (chargeRatio >= 0.999f)
        {
            scaleAmount += GetPulse(fullChargePulseSpeed) * skillReadyScaleAmount * 0.45f;
        }

        chargeArc.rectTransform.localScale = chargeArcBaseScale * (1f + scaleAmount);
    }

    private static float GetPulse(float speed)
    {
        return (Mathf.Sin(Time.unscaledTime * Mathf.Max(0f, speed)) + 1f) * 0.5f;
    }

    private bool Initialize()
    {
        if (initialized)
        {
            return true;
        }

        if (!ValidateReferences())
        {
            return false;
        }

        powerArrowBaseColor = powerArrow.color;
        chargeArcBaseScale = chargeArc.rectTransform.localScale;
        initialized = true;
        return true;
    }

    private void UpdatePowerArrow(
        PlayerCombatHudSettings settings,
        PlayerCombatHudState state,
        Vector2 rootSize)
    {
        Vector2 start = ViewportToCanvasPoint(settings.ChargeCenterViewport, rootSize);
        Vector2 end = ViewportToCanvasPoint(state.LastPointerViewportPosition, rootSize);
        Vector2 delta = end - start;
        float radiusScale = Mathf.Max(1f, Mathf.Min(rootSize.x, rootSize.y));
        float triggerDistance = settings.MinimumAttackDisplacement * radiusScale;
        float arrowLength = delta.magnitude - triggerDistance;

        powerArrow.gameObject.SetActive(
            arrowLength > 0.01f
            && delta.sqrMagnitude > 0.01f);
        if (!powerArrow.gameObject.activeSelf)
        {
            return;
        }

        Vector2 direction = delta.normalized;
        powerArrowPivot.anchoredPosition = direction * triggerDistance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        powerArrowPivot.localEulerAngles = new Vector3(0f, 0f, angle - powerArrowSpriteForwardAngle);

        RectTransform arrowRect = powerArrow.rectTransform;
        arrowRect.sizeDelta = new Vector2(arrowLength, arrowRect.sizeDelta.y);

        Color color = powerArrowBaseColor;
        color.a *= Mathf.Lerp(0.18f, 1f, Mathf.Clamp01(state.DisplacementRatio));
        powerArrow.color = color;
    }

    private void SetViewportPosition(Vector2 viewportPosition, Vector2 rootSize)
    {
        ((RectTransform)transform).anchoredPosition = ViewportToCanvasPoint(viewportPosition, rootSize);
    }

    private static Vector2 ViewportToCanvasPoint(Vector2 viewportPosition, Vector2 rootSize)
    {
        return new Vector2(
            viewportPosition.x * rootSize.x,
            viewportPosition.y * rootSize.y);
    }

    private bool ValidateReferences()
    {
        bool valid = chargeArc != null
            && meleeIcon != null
            && rangedIcon != null
            && powerArrowPivot != null
            && powerArrow != null;

        if (!valid && !warnedMissingReferences)
        {
            warnedMissingReferences = true;
            Debug.LogError("[PlayerChargeControlView] Charge control prefab references are incomplete.", this);
        }

        return valid;
    }
}
