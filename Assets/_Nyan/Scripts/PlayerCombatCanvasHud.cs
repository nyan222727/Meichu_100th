using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerCombatCanvasHud : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private Image healthBackground;
    [SerializeField] private Image healthFill;
    [SerializeField] private RectTransform crosshair;

    [Header("Charge Control")]
    [SerializeField] private PlayerChargeControlView chargeControl;

    [Header("Ultimate References")]
    [SerializeField] private RectTransform ultimateTarget;
    [SerializeField] private Image ultimateTargetBackground;
    [SerializeField] private Image ultimateTargetRing;
    [SerializeField] private Image ultimateTargetIcon;

    [Header("Feedback References")]
    [SerializeField] private Image releaseFlash;
    [SerializeField] private ScreenVignetteGraphic damageVignette;

    [Header("Health Bar")]
    [SerializeField] private Color healthyHealthColor = new Color(0.08f, 0.95f, 0.18f, 0.92f);
    [SerializeField] private Color warningHealthColor = new Color(1f, 0.78f, 0.22f, 0.94f);
    [SerializeField] private Color criticalHealthColor = new Color(1f, 0.16f, 0.12f, 0.96f);
    [SerializeField] private Color healthBackgroundColor = new Color(0f, 0f, 0f, 0.58f);

    [Header("Runtime Behaviour")]
    [SerializeField] private float releaseFlashDuration = 0.26f;
    [SerializeField] private float damageVignetteFlashDuration = 0.42f;
    [SerializeField] private Color damageVignetteColor = new Color(1f, 0.04f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float damageVignetteFlashAlpha = 0.42f;
    [SerializeField, Range(0f, 1f)] private float lowHealthVignetteAlpha = 0.24f;

    private float releaseFlashStartedAt = -999f;
    private float releaseFlashStrength;
    private Vector2 releaseFlashViewportPosition;
    private float damageVignetteStartedAt = -999f;
    private float previousHealthRatio = 1f;
    private bool hasPreviousHealthRatio;
    private bool warnedMissingReferences;
    private Color ultimateIconBaseColor;
    private Color releaseFlashBaseColor;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            return;
        }

        ultimateIconBaseColor = ultimateTargetIcon.color;
        releaseFlashBaseColor = releaseFlash.color;
    }

    private void OnValidate()
    {
        releaseFlashDuration = Mathf.Max(0.01f, releaseFlashDuration);
        damageVignetteFlashDuration = Mathf.Max(0.01f, damageVignetteFlashDuration);
    }

    public void SetVisible(bool visible)
    {
        if (!ValidateReferences())
        {
            return;
        }

        root.gameObject.SetActive(visible);
    }

    public void Apply(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        if (!ValidateReferences() || !root.gameObject.activeSelf)
        {
            return;
        }

        UpdateHealth(state.PlayerHealthRatio);
        UpdateDamageVignette(state.PlayerHealthRatio);
        UpdateCrosshair(settings.AimViewportPosition);
        chargeControl.Apply(settings, state, GetRootSize());
        UpdateUltimateTarget(state);
        UpdateReleaseFlash(settings);
    }

    public void ShowReleaseFeedback(Vector2 viewportPosition, AttackStrength strength, bool isRanged)
    {
        releaseFlashViewportPosition = viewportPosition;
        releaseFlashStrength = GetStrengthRatio(strength);
        releaseFlashStartedAt = Time.time;
    }

    private void UpdateHealth(float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);
        healthBackground.color = healthBackgroundColor;
        healthFill.color = GetHealthColor(healthRatio);

        RectTransform fillRect = healthFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(healthRatio, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void UpdateDamageVignette(float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);
        if (!hasPreviousHealthRatio)
        {
            previousHealthRatio = healthRatio;
            hasPreviousHealthRatio = true;
        }

        if (healthRatio < previousHealthRatio - 0.001f)
        {
            damageVignetteStartedAt = Time.unscaledTime;
        }

        previousHealthRatio = healthRatio;

        float flashAge = Time.unscaledTime - damageVignetteStartedAt;
        float flash = flashAge >= 0f && flashAge <= damageVignetteFlashDuration
            ? 1f - (flashAge / damageVignetteFlashDuration)
            : 0f;
        float lowHealth = Mathf.InverseLerp(0.55f, 0.16f, healthRatio);
        float intensity = Mathf.Clamp01(
            flash * damageVignetteFlashAlpha
            + lowHealth * lowHealthVignetteAlpha);

        damageVignette.gameObject.SetActive(intensity > 0.01f);
        if (intensity > 0.01f)
        {
            damageVignette.SetIntensity(damageVignetteColor, intensity);
        }
    }

    private Color GetHealthColor(float healthRatio)
    {
        Color color = healthRatio > 0.5f
            ? Color.Lerp(warningHealthColor, healthyHealthColor, (healthRatio - 0.5f) * 2f)
            : Color.Lerp(criticalHealthColor, warningHealthColor, healthRatio * 2f);

        if (healthRatio <= 0.25f)
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * 4.5f, 1f);
            color = Color.Lerp(color, Color.white, pulse * 0.18f);
        }

        return color;
    }

    private void UpdateCrosshair(Vector2 viewportPosition)
    {
        SetViewportPosition(crosshair, viewportPosition);
    }

    private void UpdateUltimateTarget(PlayerCombatHudState state)
    {
        ultimateTarget.gameObject.SetActive(state.HasUltimateTarget);
        if (!state.HasUltimateTarget)
        {
            return;
        }

        SetViewportPosition(ultimateTarget, state.UltimateTargetViewportPosition);

        float pulse = Mathf.Lerp(0.72f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
        Color iconColor = ultimateIconBaseColor;
        iconColor.a *= pulse;
        ultimateTargetIcon.color = iconColor;
    }

    private void UpdateReleaseFlash(PlayerCombatHudSettings settings)
    {
        float age = Time.time - releaseFlashStartedAt;
        if (age < 0f || age > releaseFlashDuration)
        {
            releaseFlash.gameObject.SetActive(false);
            return;
        }

        float fade = 1f - (age / releaseFlashDuration);
        Color color = releaseFlashBaseColor;
        color.a *= Mathf.Lerp(0.25f, 0.85f, releaseFlashStrength) * fade;

        releaseFlash.gameObject.SetActive(true);
        releaseFlash.color = color;
        SetViewportPosition(releaseFlash.rectTransform, releaseFlashViewportPosition);
    }

    private bool ValidateReferences()
    {
        bool valid = root != null
            && healthBackground != null
            && healthFill != null
            && crosshair != null
            && chargeControl != null
            && chargeControl.IsValid()
            && ultimateTarget != null
            && ultimateTargetBackground != null
            && ultimateTargetRing != null
            && ultimateTargetIcon != null
            && releaseFlash != null
            && damageVignette != null;

        if (!valid && !warnedMissingReferences)
        {
            warnedMissingReferences = true;
            Debug.LogError("[PlayerCombatCanvasHud] HUD prefab references are incomplete. Rebuild it from Tools/Nyan/Rebuild Player Combat HUD Prefab.", this);
        }

        return valid;
    }

    private Vector2 GetRootSize()
    {
        Vector2 size = root.rect.size;
        return size.x > 1f && size.y > 1f ? size : new Vector2(Screen.width, Screen.height);
    }

    private void SetViewportPosition(RectTransform rectTransform, Vector2 viewportPosition)
    {
        Vector2 rootSize = GetRootSize();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.anchoredPosition = ViewportToCanvasPoint(viewportPosition, rootSize);
    }

    private static Vector2 ViewportToCanvasPoint(Vector2 viewportPosition, Vector2 rootSize)
    {
        return new Vector2(viewportPosition.x * rootSize.x, viewportPosition.y * rootSize.y);
    }

    private static float GetStrengthRatio(AttackStrength strength)
    {
        return strength switch
        {
            AttackStrength.Weak => 0.35f,
            AttackStrength.Medium => 0.65f,
            AttackStrength.Strong => 1f,
            _ => 0f
        };
    }
}
