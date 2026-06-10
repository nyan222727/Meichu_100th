using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    private enum AttackMode
    {
        None,
        Ranged,
        Melee
    }

    [Header("Projectile")]
    [SerializeField] private PlayerProjectileAttack projectileAttack;
    [FormerlySerializedAs("rangedAttackDefinition")]
    [SerializeField] private ProjectileAttackSettings rangedAttackSettings;

    [Header("Screen Controls")]
    [FormerlySerializedAs("attackModeSplitY")]
    [SerializeField, Range(0.05f, 0.95f)] private float attackModeSplitX = 0.5f;
    [SerializeField] private bool useDynamicControlCenter = true;
    [SerializeField] private Vector2 chargeCenterViewport = new Vector2(0.5f, 0.02f);
    [SerializeField] private Vector2 aimViewportPosition = new Vector2(0.5f, 0.5f);
    [SerializeField, Range(0.01f, 0.5f)] private float blueRadius = 0.2f;
    [SerializeField, Range(0.01f, 0.5f)] private float greenRadius = 0.28f;
    [SerializeField, Range(0.01f, 0.5f)] private float yellowRadius = 0.38f;
    [SerializeField, Range(0.01f, 0.5f)] private float redRadius = 0.5f;

    [Header("Gesture Charge")]
    [Tooltip("Total finger travel, normalized by the shorter screen side. Reaching this value gives max charge.")]
    [SerializeField, Range(0.01f, 10f)] private float gestureChargeTravelForMax = 1.2f;
    [SerializeField] private float minChargeMultiplier = 1f;
    [SerializeField] private float maxChargeMultiplier = 5f;

    [Header("Charged Hit Stun")]
    [SerializeField, Range(0f, 1f)] private float hitStunChargeThreshold = 0.7f;
    [SerializeField, Min(0f)] private float hitStunDuration = 0.5f;

    [Header("Attack Displacement")]
    [InspectorName("Attack Trigger Radius")]
    [Tooltip("Normalized by the shorter screen side. The player must drag outside this visible control circle before attacks and power arrows can trigger.")]
    [FormerlySerializedAs("minimumAttackDragDistance")]
    [SerializeField, Range(0.01f, 0.5f)] private float minimumAttackDisplacement = 0.13f;
    [Tooltip("Extra distance after leaving the trigger circle. Drag at least this far for medium damage.")]
    [FormerlySerializedAs("mediumAttackDragDistance")]
    [SerializeField, Range(0f, 1f)] private float mediumAttackDisplacement = 0.16f;
    [Tooltip("Extra distance after leaving the trigger circle. Drag at least this far for strong damage.")]
    [FormerlySerializedAs("strongAttackDragDistance")]
    [SerializeField, Range(0f, 1f)] private float strongAttackDisplacement = 0.3f;
    [FormerlySerializedAs("chargeDistanceForMax")]
    [Tooltip("Extra distance after leaving the trigger circle. Used to clamp arrow brightness and displacement debug values.")]
    [SerializeField, Range(0.01f, 1f)] private float attackDisplacementForMax = 0.38f;

    [Header("Melee")]
    [SerializeField] private PlayerMeleeAttack meleeAttack;

    [Header("Fox Ultimate")]
    [FormerlySerializedAs("ultimateDefinition")]
    [SerializeField] private FoxUltimateSettings ultimateSettings;
    [SerializeField] private UltimateSkillManager ultimateSkillManager;
    [SerializeField] private ScreenFlash ultimateAvailableFlash;
    [SerializeField] private GameObject foxPrefab;
    [SerializeField] private float ultimateSpawnMinDelay = 2.5f;
    [SerializeField] private float ultimateSpawnMaxDelay = 5.5f;
    [SerializeField, Min(0.1f)] private float ultimateSpawnWindowDuration = 300f;
    [SerializeField, Min(0)] private int ultimateSpawnsPerWindow = 2;
    [SerializeField] private float ultimateVisibleDuration = 1.1f;
    [SerializeField] private float ultimateTargetRadius = 0.045f;
    [SerializeField] private float ultimateOuterMinRadius = 0.35f;
    [SerializeField] private float ultimateOuterMaxRadius = 0.43f;
    [SerializeField, Range(0f, 180f)] private float ultimateTargetFanMinAngle = 35f;
    [SerializeField, Range(0f, 180f)] private float ultimateTargetFanMaxAngle = 145f;
    [SerializeField] private float ultimateSpawnDistanceFromCamera = 1.1f;
    [SerializeField] private int ultimateFoxDamage = 150;

    [Header("Player Health UI")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField, Range(0f, 0.2f)] private float healthBarCenterGap = 0.055f;
    [SerializeField, Range(0f, 0.2f)] private float healthBarSideMargin = 0.035f;
    [SerializeField, Range(4f, 28f)] private float healthBarWidth = 12f;
    [SerializeField, Range(0.2f, 1f)] private float healthBarAlpha = 0.72f;

    [Header("Canvas HUD")]
    [SerializeField] private PlayerCombatCanvasHud canvasHud;

    [Header("Debug")]
    [SerializeField] private bool logAttacks = true;
    [SerializeField] private bool showDebugOverlay = false;
    [SerializeField, Range(24, 192)] private int overlaySegments = 96;
    [SerializeField, Range(1f, 8f)] private float overlayLineWidth = 3f;
    [SerializeField, Range(0.02f, 0.35f)] private float chargeFillMaxAlpha = 0.16f;

    private readonly PlayerCharge charge = new PlayerCharge();
    private readonly PlayerCombatHud combatHud = new PlayerCombatHud();
    private readonly PlayerUltimate ultimate = new PlayerUltimate();

    private Camera mainCamera;
    private bool isDragging;
    private AttackMode currentAttackMode;
    private bool hasPointerPosition;
    private Vector2 lastPointerViewportPosition;
    private Vector2 activeChargeCenterViewport;
    private PandaBossAI cachedBossAi;
    private PandaHealth cachedPandaHealth;

    private void OnValidate()
    {
        if (blueRadius > 0.5f || greenRadius > 0.5f || yellowRadius > 0.5f || redRadius > 0.5f)
        {
            blueRadius = 0.15f;
            greenRadius = 0.18f;
            yellowRadius = 0.235f;
            redRadius = 0.29f;
        }

        greenRadius = Mathf.Max(greenRadius, blueRadius);
        yellowRadius = Mathf.Max(yellowRadius, greenRadius);
        redRadius = Mathf.Max(redRadius, yellowRadius);
        gestureChargeTravelForMax = Mathf.Max(0.01f, gestureChargeTravelForMax);
        minimumAttackDisplacement = Mathf.Clamp(minimumAttackDisplacement, 0.01f, 0.5f);
        mediumAttackDisplacement = Mathf.Clamp01(mediumAttackDisplacement);
        strongAttackDisplacement = Mathf.Clamp01(Mathf.Max(mediumAttackDisplacement, strongAttackDisplacement));
        attackDisplacementForMax = Mathf.Clamp(Mathf.Max(strongAttackDisplacement, attackDisplacementForMax), 0.01f, 1f);
        minChargeMultiplier = Mathf.Max(0f, minChargeMultiplier);
        maxChargeMultiplier = Mathf.Max(maxChargeMultiplier, minChargeMultiplier);
        hitStunChargeThreshold = Mathf.Clamp01(hitStunChargeThreshold);
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
        chargeFillMaxAlpha = Mathf.Clamp(chargeFillMaxAlpha, 0.02f, 0.35f);
        chargeCenterViewport = ClampViewport(chargeCenterViewport);
        aimViewportPosition = ClampViewport(aimViewportPosition);
        ultimateTargetRadius = Mathf.Max(0.01f, ultimateTargetRadius);
        ultimateSpawnWindowDuration = Mathf.Max(0.1f, ultimateSpawnWindowDuration);
        ultimateSpawnsPerWindow = Mathf.Max(0, ultimateSpawnsPerWindow);
        ultimateOuterMinRadius = Mathf.Max(ultimateOuterMinRadius, redRadius + ultimateTargetRadius);
        ultimateOuterMaxRadius = Mathf.Max(ultimateOuterMaxRadius, ultimateOuterMinRadius);
        ultimateTargetFanMinAngle = Mathf.Clamp(ultimateTargetFanMinAngle, 0f, 180f);
        ultimateTargetFanMaxAngle = Mathf.Clamp(Mathf.Max(ultimateTargetFanMaxAngle, ultimateTargetFanMinAngle), 0f, 180f);
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        EnsurePlayerHealth();
        EnsureProjectileAttack();
        EnsureMeleeAttack();
        EnsureCanvasHud();
        if (canvasHud != null)
        {
            canvasHud.SetVisible(true);
        }

        GameAudioController.PlayGameMusic();
        activeChargeCenterViewport = chargeCenterViewport;
        ResetInputState();
        ultimate.ScheduleNextTarget(GetUltimateConfig());
    }

    private void OnDisable()
    {
        if (canvasHud != null)
        {
            canvasHud.SetVisible(false);
        }

        GameAudioController.StopStrengthLoop();
    }

    public void SetPlayerHealth(int currentHealth, int maxHealth)
    {
        EnsurePlayerHealth();
        playerHealth.SetHealth(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (Time.timeScale <= 0f)
        {
            if (isDragging)
            {
                ResetInputState();
            }

            UpdateCanvasHud();
            return;
        }

        ultimate.UpdateTarget(chargeCenterViewport, GetChargeRadiusScale(), GetUltimateConfig(), logAttacks);
        HandlePointerInput();
        UpdateStrengthAudio();
        UpdateCanvasHud();
    }

    private void HandlePointerInput()
    {
        if (!TryGetPointerState(out bool pointerDown, out bool pointerHeld, out bool pointerUp, out Vector2 screenPosition))
        {
            if (isDragging)
            {
                ResetInputState();
            }

            hasPointerPosition = false;
            return;
        }

        Vector2 viewportPosition = ScreenToViewport(screenPosition);
        Vector2 previousViewportPosition = lastPointerViewportPosition;
        bool hadPreviousPointerPosition = hasPointerPosition;
        lastPointerViewportPosition = viewportPosition;
        hasPointerPosition = true;

        if (pointerDown)
        {
            BeginDrag(viewportPosition);
        }

        if (isDragging && (pointerHeld || pointerUp))
        {
            charge.Update(
                viewportPosition,
                GetChargeRadiusScale(),
                gestureChargeTravelForMax,
                minChargeMultiplier,
                maxChargeMultiplier);

            Vector2 triggerStart = hadPreviousPointerPosition ? previousViewportPosition : viewportPosition;
            if (TryTriggerUltimate(triggerStart, viewportPosition))
            {
                ResetInputState();
                return;
            }
        }

        if (isDragging && pointerUp)
        {
            ReleaseDrag(viewportPosition);
        }
    }

    private bool TryGetPointerState(
        out bool pointerDown,
        out bool pointerHeld,
        out bool pointerUp,
        out Vector2 screenPosition)
    {
        pointerDown = false;
        pointerHeld = false;
        pointerUp = false;
        screenPosition = default;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (!isDragging
                && touch.phase == TouchPhase.Began
                && EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return false;
            }

            screenPosition = touch.position;
            pointerDown = touch.phase == TouchPhase.Began;
            pointerHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            pointerUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }

        pointerDown = Input.GetMouseButtonDown(0);
        pointerHeld = Input.GetMouseButton(0);
        pointerUp = Input.GetMouseButtonUp(0);

        if (!isDragging
            && pointerDown
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        if (!pointerDown && !pointerHeld && !pointerUp)
        {
            return false;
        }

        screenPosition = Input.mousePosition;
        return true;
    }

    private void BeginDrag(Vector2 viewportPosition)
    {
        activeChargeCenterViewport = useDynamicControlCenter
            ? viewportPosition
            : chargeCenterViewport;
        currentAttackMode = GetAttackMode(viewportPosition);
        isDragging = true;
        charge.Begin(viewportPosition, minChargeMultiplier);
    }

    private void ReleaseDrag(Vector2 viewportPosition)
    {
        if (TryTriggerUltimate(viewportPosition, viewportPosition))
        {
            ResetInputState();
            return;
        }

        float dragDistance = GetAttackDisplacement(viewportPosition);
        float chargeMultiplier = charge.Multiplier;
        float chargeRatio = charge.Ratio;

        if (currentAttackMode == AttackMode.Ranged)
        {
            ReleaseRangedAttack(viewportPosition, dragDistance, chargeRatio, chargeMultiplier);
        }
        else if (currentAttackMode == AttackMode.Melee)
        {
            ReleaseMeleeAttack(viewportPosition, dragDistance, chargeRatio);
        }

        ResetInputState();
    }

    private bool TryTriggerUltimate(Vector2 startViewportPosition, Vector2 endViewportPosition)
    {
        bool triggered = ultimate.TryTriggerSegment(
            startViewportPosition,
            endViewportPosition,
            false,
            mainCamera,
            GetUltimateConfig(),
            GetChargeRadiusScale(),
            logAttacks);

        if (triggered)
        {
            TriggerUltimateVisual();
        }

        return triggered;
    }

    private void TriggerUltimateVisual()
    {
        if (ultimateSkillManager == null)
        {
            ultimateSkillManager = FindFirstObjectByType<UltimateSkillManager>(FindObjectsInactive.Include);
        }

        if (ultimateSkillManager != null)
        {
            ultimateSkillManager.TriggerUltimate();
        }
    }

    private void ReleaseRangedAttack(
        Vector2 viewportPosition,
        float dragDistance,
        float chargeRatio,
        float chargeMultiplier)
    {
        if (!TryGetAttackStrength(dragDistance, out AttackStrength strength))
        {
            if (logAttacks)
            {
                Debug.Log($"[PlayerAttack] Ranged attack cancelled. Attack displacement {dragDistance:0.000} <= 0.000.");
            }

            return;
        }

        if (rangedAttackSettings == null)
        {
            if (logAttacks)
            {
                Debug.LogWarning("[PlayerAttack] Missing ranged attack settings.");
            }

            return;
        }

        float displacementRatio = Mathf.Clamp01(dragDistance / Mathf.Max(0.01f, attackDisplacementForMax));
        ProjectileAttackStats stats = rangedAttackSettings.EvaluateStats(displacementRatio);
        bool appliesHitStun = chargeRatio >= hitStunChargeThreshold;
        FireProjectile(
            stats.Impulse,
            Mathf.RoundToInt(stats.Damage * chargeMultiplier),
            appliesHitStun);
        ShowReleaseFeedback(viewportPosition, strength, true);
    }

    private void ReleaseMeleeAttack(
        Vector2 viewportPosition,
        float dragDistance,
        float chargeRatio)
    {
        if (!TryGetAttackStrength(dragDistance, out AttackStrength strength))
        {
            if (logAttacks)
            {
                Debug.Log($"[PlayerAttack] Melee attack cancelled. Attack displacement {dragDistance:0.000} <= 0.000.");
            }

            return;
        }

        EnsureMeleeAttack();
        float displacementRatio = Mathf.Clamp01(dragDistance / Mathf.Max(0.01f, attackDisplacementForMax));
        meleeAttack.Attack(
            mainCamera,
            aimViewportPosition,
            displacementRatio,
            chargeRatio,
            chargeRatio >= hitStunChargeThreshold,
            hitStunDuration);
        ShowReleaseFeedback(viewportPosition, strength, false);
    }

    private void FireProjectile(float impulse, int damage, bool appliesHitStun)
    {
        EnsureProjectileAttack();
        projectileAttack.Fire(
            mainCamera,
            aimViewportPosition,
            impulse,
            damage,
            appliesHitStun,
            hitStunDuration);
    }

    private void UpdateStrengthAudio()
    {
        if (isDragging && currentAttackMode == AttackMode.Ranged && charge.Ratio > 0.01f)
        {
            GameAudioController.StartStrengthLoop();
            return;
        }

        GameAudioController.StopStrengthLoop();
    }

    private void EnsureProjectileAttack()
    {
        if (projectileAttack == null)
        {
            projectileAttack = GetComponent<PlayerProjectileAttack>();
        }

        if (projectileAttack == null)
        {
            projectileAttack = gameObject.AddComponent<PlayerProjectileAttack>();
        }

    }

    private void EnsureCanvasHud()
    {
        if (canvasHud == null)
        {
            canvasHud = GetComponentInChildren<PlayerCombatCanvasHud>(true);
        }
    }

    private void UpdateCanvasHud()
    {
        EnsureCanvasHud();
        if (canvasHud != null)
        {
            canvasHud.Apply(GetHudSettings(), GetHudState());
        }
    }

    private void ShowReleaseFeedback(Vector2 viewportPosition, AttackStrength strength, bool isRanged)
    {
        EnsureCanvasHud();
        if (canvasHud != null)
        {
            canvasHud.ShowReleaseFeedback(viewportPosition, strength, isRanged);
        }
    }

    private void EnsureMeleeAttack()
    {
        if (meleeAttack == null)
        {
            meleeAttack = GetComponent<PlayerMeleeAttack>();
        }

        if (meleeAttack == null)
        {
            meleeAttack = gameObject.AddComponent<PlayerMeleeAttack>();
        }
    }

    private void EnsurePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = gameObject.AddComponent<PlayerHealth>();
        }
    }

    private AttackMode GetAttackMode(Vector2 viewportPosition)
    {
        return viewportPosition.x < attackModeSplitX ? AttackMode.Melee : AttackMode.Ranged;
    }

    private float GetDragDistance(Vector2 viewportPosition)
    {
        Vector2 screenPosition = ViewportToScreenPoint(viewportPosition);
        Vector2 centerPosition = ViewportToScreenPoint(activeChargeCenterViewport);
        return Vector2.Distance(screenPosition, centerPosition) / GetChargeRadiusScale();
    }

    private float GetDisplacementRatio(Vector2 viewportPosition)
    {
        return Mathf.Clamp01(GetAttackDisplacement(viewportPosition) / Mathf.Max(0.01f, attackDisplacementForMax));
    }

    private float GetAttackDisplacement(Vector2 viewportPosition)
    {
        return Mathf.Max(0f, GetDragDistance(viewportPosition) - minimumAttackDisplacement);
    }

    private Vector2 GetCurrentChargeCenterViewport()
    {
        return isDragging ? activeChargeCenterViewport : chargeCenterViewport;
    }

    private float GetPlayerHealthRatio()
    {
        return playerHealth != null ? playerHealth.HealthRatio : 1f;
    }

    private int GetPlayerCurrentHealth()
    {
        return playerHealth != null ? playerHealth.CurrentHealth : 0;
    }

    private int GetPlayerMaxHealth()
    {
        return playerHealth != null ? playerHealth.MaxHealth : 1;
    }

    private bool TryGetBossHealthRatio(out float bossHealthRatio)
    {
        if (cachedBossAi == null || cachedBossAi.IsDefeated)
        {
            cachedBossAi = FindFirstObjectByType<PandaBossAI>();
        }

        if (cachedBossAi != null)
        {
            bossHealthRatio = cachedBossAi.maxHealth <= 0
                ? 0f
                : Mathf.Clamp01(cachedBossAi.currentHealth / (float)cachedBossAi.maxHealth);
            return true;
        }

        if (cachedPandaHealth == null || cachedPandaHealth.IsDefeated)
        {
            cachedPandaHealth = FindFirstObjectByType<PandaHealth>();
        }

        if (cachedPandaHealth != null)
        {
            bossHealthRatio = cachedPandaHealth.MaxHealth <= 0
                ? 0f
                : Mathf.Clamp01(cachedPandaHealth.CurrentHealth / (float)cachedPandaHealth.MaxHealth);
            return true;
        }

        bossHealthRatio = 1f;
        return false;
    }

    private bool TryGetAttackStrength(float dragDistance, out AttackStrength strength)
    {
        if (dragDistance <= 0f)
        {
            strength = default;
            return false;
        }

        if (dragDistance >= strongAttackDisplacement)
        {
            strength = AttackStrength.Strong;
            return true;
        }

        if (dragDistance >= mediumAttackDisplacement)
        {
            strength = AttackStrength.Medium;
            return true;
        }

        strength = AttackStrength.Weak;
        return true;
    }

    private PlayerUltimateConfig GetUltimateConfig()
    {
        ResolveUltimateAvailableFlash();

        PlayerUltimateConfig config = ultimateSettings != null
            ? ultimateSettings.ToConfig()
            : new PlayerUltimateConfig
            {
                FoxPrefab = foxPrefab,
                SpawnMinDelay = ultimateSpawnMinDelay,
                SpawnMaxDelay = ultimateSpawnMaxDelay,
                SpawnWindowDuration = ultimateSpawnWindowDuration,
                SpawnsPerWindow = ultimateSpawnsPerWindow,
                VisibleDuration = ultimateVisibleDuration,
                TargetRadius = ultimateTargetRadius,
                OuterMinRadius = ultimateOuterMinRadius,
                OuterMaxRadius = ultimateOuterMaxRadius,
                TargetFanMinAngle = ultimateTargetFanMinAngle,
                TargetFanMaxAngle = ultimateTargetFanMaxAngle,
                SpawnDistanceFromCamera = ultimateSpawnDistanceFromCamera,
                FoxDamage = ultimateFoxDamage
            };

        config.TargetSpawnFlash = ultimateAvailableFlash;
        config.SpawnMinDelay = Mathf.Max(0.1f, config.SpawnMinDelay);
        config.SpawnMaxDelay = Mathf.Max(config.SpawnMinDelay, config.SpawnMaxDelay);
        config.SpawnWindowDuration = Mathf.Max(0.1f, config.SpawnWindowDuration);
        config.SpawnsPerWindow = Mathf.Max(0, config.SpawnsPerWindow);
        config.VisibleDuration = Mathf.Max(0.1f, config.VisibleDuration);
        config.TargetRadius = Mathf.Max(0.01f, config.TargetRadius);
        config.OuterMinRadius = Mathf.Max(config.OuterMinRadius, redRadius + config.TargetRadius);
        config.OuterMaxRadius = Mathf.Max(config.OuterMaxRadius, config.OuterMinRadius);
        config.TargetFanMinAngle = Mathf.Clamp(config.TargetFanMinAngle, 0f, 180f);
        config.TargetFanMaxAngle = Mathf.Clamp(Mathf.Max(config.TargetFanMaxAngle, config.TargetFanMinAngle), 0f, 180f);
        config.SpawnDistanceFromCamera = Mathf.Max(0.01f, config.SpawnDistanceFromCamera);
        config.FoxDamage = Mathf.Max(0, config.FoxDamage);
        return config;
    }

    private void ResolveUltimateAvailableFlash()
    {
        if (ultimateAvailableFlash == null)
        {
            ultimateAvailableFlash = FindFirstObjectByType<ScreenFlash>(FindObjectsInactive.Include);
        }

        if (ultimateAvailableFlash == null)
        {
            ultimateAvailableFlash = CreateRuntimeUltimateAvailableFlash();
        }

        if (ultimateAvailableFlash != null)
        {
            ultimateAvailableFlash.gameObject.SetActive(true);
        }
    }

    private ScreenFlash CreateRuntimeUltimateAvailableFlash()
    {
        Transform parent = canvasHud != null
            ? canvasHud.transform
            : FindFirstObjectByType<Canvas>(FindObjectsInactive.Include)?.transform;

        if (parent == null)
        {
            return null;
        }

        GameObject flashObject = new GameObject(
            "Ultimate Available Flash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        flashObject.transform.SetParent(parent, false);
        flashObject.transform.SetAsLastSibling();

        RectTransform rectTransform = flashObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = flashObject.GetComponent<Image>();
        image.color = new Color(1f, 0.85f, 0.08f, 0f);
        image.raycastTarget = false;

        ScreenFlash flash = flashObject.AddComponent<ScreenFlash>();
        flash.flashColor = new Color(1f, 0.85f, 0.08f, 0.38f);
        flash.fadeInTime = 0.04f;
        flash.fadeOutTime = 0.22f;
        return flash;
    }

    private PlayerCombatHudSettings GetHudSettings()
    {
        return new PlayerCombatHudSettings
        {
            ShowDebugOverlay = showDebugOverlay,
            AttackModeSplitX = attackModeSplitX,
            ChargeCenterViewport = GetCurrentChargeCenterViewport(),
            AimViewportPosition = aimViewportPosition,
            BlueRadius = blueRadius,
            GreenRadius = greenRadius,
            YellowRadius = yellowRadius,
            RedRadius = redRadius,
            OverlaySegments = overlaySegments,
            OverlayLineWidth = overlayLineWidth,
            ChargeFillMaxAlpha = chargeFillMaxAlpha,
            HealthBarCenterGap = healthBarCenterGap,
            HealthBarSideMargin = healthBarSideMargin,
            HealthBarWidth = healthBarWidth,
            HealthBarAlpha = healthBarAlpha,
            GestureChargeTravelForMax = gestureChargeTravelForMax,
            HitStunChargeThreshold = hitStunChargeThreshold,
            MinimumAttackDisplacement = minimumAttackDisplacement,
            MediumAttackDisplacement = mediumAttackDisplacement,
            StrongAttackDisplacement = strongAttackDisplacement,
            MaxAttackDisplacement = attackDisplacementForMax
        };
    }

    private PlayerCombatHudState GetHudState()
    {
        AttackMode pointerAttackMode = isDragging
            ? currentAttackMode
            : hasPointerPosition
                ? GetAttackMode(lastPointerViewportPosition)
                : AttackMode.None;
        PlayerUltimateConfig ultimateConfig = GetUltimateConfig();
        float dragDistance = isDragging && hasPointerPosition ? GetAttackDisplacement(lastPointerViewportPosition) : 0f;
        float displacementRatio = isDragging && hasPointerPosition ? GetDisplacementRatio(lastPointerViewportPosition) : 0f;
        bool hasBossHealth = TryGetBossHealthRatio(out float bossHealthRatio);

        return new PlayerCombatHudState
        {
            HasPointerPosition = hasPointerPosition,
            LastPointerViewportPosition = lastPointerViewportPosition,
            AttackModeLabel = pointerAttackMode.ToString(),
            DragDistanceLabel = isDragging ? $"{dragDistance:0.00}/{attackDisplacementForMax:0.00}" : "None",
            ChargeTravelLabel = isDragging ? $"{charge.AccumulatedTravelDistance:0.00}/{gestureChargeTravelForMax:0.00}" : "None",
            IsDragging = isDragging,
            ChargeInvalidated = false,
            ChargeRatio = charge.Ratio,
            ChargeMultiplier = charge.Multiplier,
            DisplacementRatio = displacementRatio,
            HitStunReady = charge.Ratio >= hitStunChargeThreshold,
            PlayerHealthRatio = GetPlayerHealthRatio(),
            PlayerCurrentHealth = GetPlayerCurrentHealth(),
            PlayerMaxHealth = GetPlayerMaxHealth(),
            HasBossHealth = hasBossHealth,
            BossHealthRatio = bossHealthRatio,
            HasUltimateTarget = ultimate.HasTarget,
            UltimateTargetViewportPosition = ultimate.TargetViewportPosition,
            UltimateTargetRadius = ultimateConfig.TargetRadius
        };
    }

    private Vector2 ScreenToViewport(Vector2 screenPosition)
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

    private static float GetChargeRadiusScale()
    {
        return Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
    }

    private static Vector2 ClampViewport(Vector2 viewportPosition)
    {
        return new Vector2(
            Mathf.Clamp01(viewportPosition.x),
            Mathf.Clamp01(viewportPosition.y));
    }

    private void ResetInputState()
    {
        isDragging = false;
        currentAttackMode = AttackMode.None;
        activeChargeCenterViewport = chargeCenterViewport;
        charge.Reset(minChargeMultiplier);
        GameAudioController.StopStrengthLoop();
    }

    private void OnGUI()
    {
        combatHud.Draw(GetHudSettings(), GetHudState());
    }
}
