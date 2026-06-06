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

    private Color powerArrowBaseColor;
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

        bool melee = state.AttackModeLabel == "Melee";
        meleeIcon.gameObject.SetActive(melee);
        rangedIcon.gameObject.SetActive(!melee);

        UpdatePowerArrow(settings, state, rootSize);
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
