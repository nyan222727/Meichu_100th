using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatCanvasHud : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform root;

    [Header("Replaceable Sprites")]
    [SerializeField] private Sprite meleeIconSprite;
    [SerializeField] private Sprite rangedIconSprite;
    [SerializeField] private Sprite ultimateIconSprite;
    [SerializeField] private Sprite crosshairSprite;

    [Header("Figma Layout")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(390f, 844f);
    [SerializeField] private Vector2 healthBarOffset = new Vector2(14f, -15f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(108f, 10f);
    [SerializeField] private float iconSize = 50f;
    [SerializeField] private float crosshairSize = 35f;
    [SerializeField] private float releaseFlashDuration = 0.26f;

    [Header("Colors")]
    [SerializeField] private Color healthBackgroundColor = new Color(0.86f, 0.86f, 0.86f, 0.9f);
    [SerializeField] private Color healthFillColor = new Color(0.35f, 1f, 0.35f, 0.96f);
    [SerializeField] private Color meleeZoneColor = new Color(1f, 0.95f, 0.45f, 0.45f);
    [SerializeField] private Color rangedZoneColor = new Color(1f, 0.36f, 0.38f, 0.36f);
    [SerializeField] private Color blueZoneColor = new Color(0.46f, 0.91f, 1f, 0.58f);
    [SerializeField] private Color chargeFillColor = new Color(0.1f, 0.55f, 1f, 0.22f);
    [SerializeField] private Color outerArcColor = new Color(0f, 0f, 0f, 0.82f);
    [SerializeField] private Color meleeFlashColor = new Color(1f, 0.96f, 0.3f, 1f);
    [SerializeField] private Color rangedFlashColor = new Color(1f, 0.25f, 0.35f, 1f);
    [SerializeField] private Color ultimateColor = new Color(0.7f, 0.68f, 1f, 0.95f);
    [SerializeField] private Color iconColor = new Color(0.04f, 0.04f, 0.04f, 0.92f);
    [SerializeField] private Color crosshairColor = new Color(0.78f, 0.78f, 0.78f, 0.78f);

    private Image healthBackground;
    private Image healthFill;
    private Image meleeZone;
    private Image rangedZone;
    private Image blueZone;
    private Image chargeFill;
    private Image outerArc;
    private Image meleeIcon;
    private Image rangedIcon;
    private Image ultimateTarget;
    private Image crosshair;
    private Image releaseFlash;

    private Sprite circleSprite;
    private Sprite leftHalfCircleSprite;
    private Sprite rightHalfCircleSprite;
    private Sprite ringSprite;
    private Sprite triangleSprite;
    private Sprite starSprite;
    private Sprite daggerSprite;
    private Sprite bowSprite;

    private float releaseFlashStartedAt = -999f;
    private float releaseFlashStrength;
    private bool releaseFlashIsRanged;
    private Vector2 releaseFlashViewportPosition;

    private void OnValidate()
    {
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
        healthBarSize.x = Mathf.Max(1f, healthBarSize.x);
        healthBarSize.y = Mathf.Max(1f, healthBarSize.y);
        iconSize = Mathf.Max(1f, iconSize);
        crosshairSize = Mathf.Max(1f, crosshairSize);
        releaseFlashDuration = Mathf.Max(0.01f, releaseFlashDuration);
    }

    public void SetVisible(bool visible)
    {
        EnsureBuilt();
        root.gameObject.SetActive(visible);
    }

    public void Apply(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        EnsureBuilt();

        if (!root.gameObject.activeSelf)
        {
            return;
        }

        UpdateHealth(state.PlayerHealthRatio);
        UpdateCombatArea(settings, state);
        UpdateUltimateTarget(settings, state);
        UpdateReleaseFlash(settings);
    }

    public void ShowReleaseFeedback(Vector2 viewportPosition, AttackStrength strength, bool isRanged)
    {
        releaseFlashViewportPosition = viewportPosition;
        releaseFlashStrength = GetStrengthRatio(strength);
        releaseFlashIsRanged = isRanged;
        releaseFlashStartedAt = Time.time;
    }

    private void EnsureBuilt()
    {
        EnsureSprites();
        EnsureCanvas();

        if (root == null)
        {
            GameObject rootObject = new GameObject("Combat HUD Root", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        if (healthBackground != null)
        {
            return;
        }

        meleeZone = CreateImage("Melee Zone", root, leftHalfCircleSprite, meleeZoneColor);
        rangedZone = CreateImage("Ranged Zone", root, rightHalfCircleSprite, rangedZoneColor);
        blueZone = CreateImage("Charge Start Zone", root, ringSprite, blueZoneColor);
        chargeFill = CreateImage("Charge Fill", root, circleSprite, Color.clear);
        outerArc = CreateImage("Outer Release Arc", root, ringSprite, outerArcColor);

        meleeIcon = CreateImage("Melee Icon", root, meleeIconSprite != null ? meleeIconSprite : daggerSprite, iconColor);
        rangedIcon = CreateImage("Ranged Icon", root, rangedIconSprite != null ? rangedIconSprite : bowSprite, iconColor);
        crosshair = CreateImage("Crosshair", root, crosshairSprite != null ? crosshairSprite : triangleSprite, crosshairColor);
        ultimateTarget = CreateImage("Ultimate Target", root, ultimateIconSprite != null ? ultimateIconSprite : starSprite, ultimateColor);
        releaseFlash = CreateImage("Release Flash", root, circleSprite, Color.clear);

        healthBackground = CreateImage("Player Health Background", root, circleSprite, healthBackgroundColor);
        healthFill = CreateImage("Player Health Fill", healthBackground.rectTransform, circleSprite, healthFillColor);

        chargeFill.enabled = false;
        ultimateTarget.enabled = false;
        releaseFlash.enabled = false;
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Player Combat Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 1f;
    }

    private void UpdateHealth(float healthRatio)
    {
        RectTransform backgroundRect = healthBackground.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.anchoredPosition = healthBarOffset;
        backgroundRect.sizeDelta = healthBarSize;

        healthBackground.type = Image.Type.Sliced;
        healthBackground.color = healthBackgroundColor;

        RectTransform fillRect = healthFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = new Vector2(healthBarSize.x * Mathf.Clamp01(healthRatio), 0f);
        healthFill.color = healthFillColor;
    }

    private void UpdateCombatArea(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        Vector2 rootSize = GetRootSize();
        float radiusScale = GetRadiusScale(rootSize);
        float outerSize = settings.RedRadius * radiusScale * 2f;
        float blueSize = settings.BlueRadius * radiusScale * 2f;
        float chargeRatio = Mathf.Clamp01(state.ChargeRatio);
        float chargeSize = Mathf.Lerp(blueSize * 0.32f, blueSize, chargeRatio);

        SetViewportRect(meleeZone.rectTransform, settings.ChargeCenterViewport, new Vector2(outerSize, outerSize), rootSize);
        SetViewportRect(rangedZone.rectTransform, settings.ChargeCenterViewport, new Vector2(outerSize, outerSize), rootSize);
        SetViewportRect(outerArc.rectTransform, settings.ChargeCenterViewport, new Vector2(outerSize, outerSize), rootSize);
        SetViewportRect(blueZone.rectTransform, settings.ChargeCenterViewport, new Vector2(blueSize, blueSize), rootSize);
        blueZone.enabled = true;

        chargeFill.enabled = state.IsDragging || chargeRatio > 0.01f;
        if (chargeFill.enabled)
        {
            SetViewportRect(chargeFill.rectTransform, settings.ChargeCenterViewport, new Vector2(chargeSize, chargeSize), rootSize);
            Color fillColor = state.ChargeInvalidated
                ? new Color(1f, 0.08f, 0.06f, 0.28f)
                : chargeFillColor;
            fillColor.a = Mathf.Max(fillColor.a * chargeRatio, state.IsDragging ? 0.16f : 0f);
            chargeFill.color = fillColor;
        }

        SetViewportRect(crosshair.rectTransform, settings.AimViewportPosition, Vector2.one * crosshairSize, rootSize);
        SetIconPosition(meleeIcon.rectTransform, settings, -1f, rootSize);
        SetIconPosition(rangedIcon.rectTransform, settings, 1f, rootSize);
    }

    private void UpdateUltimateTarget(PlayerCombatHudSettings settings, PlayerCombatHudState state)
    {
        ultimateTarget.enabled = state.HasUltimateTarget;
        if (!state.HasUltimateTarget)
        {
            return;
        }

        Vector2 rootSize = GetRootSize();
        float radiusScale = GetRadiusScale(rootSize);
        float size = Mathf.Max(34f, state.UltimateTargetRadius * radiusScale * 2.15f);
        float pulse = Mathf.Lerp(0.72f, 1f, Mathf.PingPong(Time.time * 8f, 1f));
        Color color = ultimateColor;
        color.a *= pulse;

        SetViewportRect(ultimateTarget.rectTransform, state.UltimateTargetViewportPosition, Vector2.one * size, rootSize);
        ultimateTarget.color = color;
    }

    private void UpdateReleaseFlash(PlayerCombatHudSettings settings)
    {
        float age = Time.time - releaseFlashStartedAt;
        if (age < 0f || age > releaseFlashDuration)
        {
            releaseFlash.enabled = false;
            return;
        }

        Vector2 rootSize = GetRootSize();
        float radiusScale = GetRadiusScale(rootSize);
        float fade = 1f - (age / releaseFlashDuration);
        float baseRadius = Mathf.Lerp(settings.GreenRadius, settings.RedRadius, releaseFlashStrength);
        float size = Mathf.Lerp(0.24f, 0.38f, releaseFlashStrength) * radiusScale * Mathf.Lerp(1.25f, 0.75f, fade);
        Color color = releaseFlashIsRanged ? rangedFlashColor : meleeFlashColor;
        color.a *= Mathf.Lerp(0.25f, 0.85f, releaseFlashStrength) * fade;

        releaseFlash.enabled = true;
        releaseFlash.color = color;
        SetViewportRect(releaseFlash.rectTransform, releaseFlashViewportPosition, Vector2.one * Mathf.Max(size, baseRadius * radiusScale * 0.65f), rootSize);
    }

    private void SetIconPosition(RectTransform iconRect, PlayerCombatHudSettings settings, float side, Vector2 rootSize)
    {
        Vector2 position = settings.ChargeCenterViewport + new Vector2(settings.RedRadius * 0.52f * side, settings.RedRadius * 0.23f);
        position.x = Mathf.Clamp01(position.x);
        position.y = Mathf.Clamp01(position.y);
        SetViewportRect(iconRect, position, Vector2.one * iconSize, rootSize);
    }

    private Vector2 GetRootSize()
    {
        Vector2 size = root.rect.size;
        if (size.x <= 1f || size.y <= 1f)
        {
            size = new Vector2(Screen.width, Screen.height);
        }

        return size;
    }

    private static float GetRadiusScale(Vector2 rootSize)
    {
        return Mathf.Max(1f, Mathf.Min(rootSize.x, rootSize.y));
    }

    private static void SetViewportRect(RectTransform rectTransform, Vector2 viewportPosition, Vector2 size, Vector2 rootSize)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(viewportPosition.x * rootSize.x, viewportPosition.y * rootSize.y);
        rectTransform.sizeDelta = size;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureSprites()
    {
        circleSprite ??= CreateCircleSprite(CircleSpriteMode.Full);
        leftHalfCircleSprite ??= CreateCircleSprite(CircleSpriteMode.LeftHalf);
        rightHalfCircleSprite ??= CreateCircleSprite(CircleSpriteMode.RightHalf);
        ringSprite ??= CreateCircleSprite(CircleSpriteMode.Ring);
        triangleSprite ??= CreateTriangleSprite();
        starSprite ??= CreateStarSprite();
        daggerSprite ??= CreateDaggerSprite();
        bowSprite ??= CreateBowSprite();
    }

    private Sprite CreateCircleSprite(CircleSpriteMode mode)
    {
        const int size = 128;
        const float center = (size - 1) * 0.5f;
        const float radius = size * 0.49f;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                bool visible = distance <= radius;

                if (mode == CircleSpriteMode.LeftHalf)
                {
                    visible &= x <= center;
                }
                else if (mode == CircleSpriteMode.RightHalf)
                {
                    visible &= x >= center;
                }
                else if (mode == CircleSpriteMode.Ring)
                {
                    visible = distance <= radius && distance >= radius * 0.94f;
                }

                byte alpha = visible ? (byte)255 : (byte)0;
                pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateTriangleSprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color32[] pixels = new Color32[size * size];
        Vector2 a = new Vector2(size * 0.5f, size * 0.78f);
        Vector2 b = new Vector2(size * 0.24f, size * 0.25f);
        Vector2 c = new Vector2(size * 0.76f, size * 0.25f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool visible = IsPointInTriangle(new Vector2(x, y), a, b, c);
                pixels[(y * size) + x] = new Color32(255, 255, 255, visible ? (byte)255 : (byte)0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateStarSprite()
    {
        const int size = 64;
        const float center = (size - 1) * 0.5f;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float angle = Mathf.Atan2(dy, dx);
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy)) / center;
                float sector = Mathf.Abs(Mathf.Repeat((angle / (Mathf.PI * 2f)) * 5f, 1f) - 0.5f) * 2f;
                float allowed = Mathf.Lerp(0.82f, 0.35f, sector);
                bool visible = distance <= allowed;
                pixels[(y * size) + x] = new Color32(255, 255, 255, visible ? (byte)255 : (byte)0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateDaggerSprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color32[] pixels = new Color32[size * size];
        DrawLine(pixels, size, new Vector2(17f, 17f), new Vector2(48f, 48f), 2.6f);
        DrawLine(pixels, size, new Vector2(18f, 12f), new Vector2(26f, 20f), 2.5f);
        DrawLine(pixels, size, new Vector2(24f, 18f), new Vector2(15f, 27f), 2.2f);
        DrawLine(pixels, size, new Vector2(43f, 43f), new Vector2(52f, 51f), 1.8f);
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateBowSprite()
    {
        const int size = 64;
        Texture2D texture = CreateTransparentTexture(size, size);
        Color32[] pixels = new Color32[size * size];
        DrawLine(pixels, size, new Vector2(12f, 31f), new Vector2(52f, 31f), 1.8f);
        DrawLine(pixels, size, new Vector2(44f, 24f), new Vector2(52f, 31f), 1.8f);
        DrawLine(pixels, size, new Vector2(44f, 38f), new Vector2(52f, 31f), 1.8f);
        DrawLine(pixels, size, new Vector2(22f, 14f), new Vector2(22f, 50f), 1.8f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Mathf.Abs(Vector2.Distance(point, new Vector2(28f, 32f)) - 21f);
                if (x >= 20 && x <= 48 && distance <= 1.8f)
                {
                    pixels[(y * size) + x] = new Color32(255, 255, 255, 255);
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private static void DrawLine(Color32[] pixels, int size, Vector2 start, Vector2 end, float width)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = DistanceToSegment(new Vector2(x, y), start, end);
                if (distance <= width)
                {
                    pixels[(y * size) + x] = new Color32(255, 255, 255, 255);
                }
            }
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLength = segment.sqrMagnitude;
        if (segmentLength <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, start);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLength);
        return Vector2.Distance(point, start + (segment * t));
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(point, a, b);
        float d2 = Sign(point, b, c);
        float d3 = Sign(point, c, a);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return ((p1.x - p3.x) * (p2.y - p3.y)) - ((p2.x - p3.x) * (p1.y - p3.y));
    }

    private static float GetStrengthRatio(AttackStrength strength)
    {
        switch (strength)
        {
            case AttackStrength.Weak:
                return 0.35f;
            case AttackStrength.Medium:
                return 0.65f;
            case AttackStrength.Strong:
                return 1f;
            default:
                return 0f;
        }
    }

    private enum CircleSpriteMode
    {
        Full,
        LeftHalf,
        RightHalf,
        Ring
    }
}
