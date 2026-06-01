using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    private enum AttackMode
    {
        None,
        Ranged,
        Melee
    }

    private enum ChargeZone
    {
        None,
        Blue,
        Green,
        Yellow,
        Red,
        Outside
    }

    [Header("Projectile")]
    [SerializeField] private PlayerProjectileAttack projectileAttack;
    [SerializeField] private Rigidbody projectilePrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private bool useLaunchPointOverride;

    [Header("Screen Controls")]
    [SerializeField, Range(0.05f, 0.95f)] private float attackModeSplitY = 0.5f;
    [SerializeField] private Vector2 chargeCenterViewport = new Vector2(0.5f, 0.5f);
    [SerializeField, Range(0.01f, 0.5f)] private float blueRadius = 0.15f;
    [SerializeField, Range(0.01f, 0.5f)] private float greenRadius = 0.18f;
    [SerializeField, Range(0.01f, 0.5f)] private float yellowRadius = 0.235f;
    [SerializeField, Range(0.01f, 0.5f)] private float redRadius = 0.29f;

    [Header("Ranged Launch")]
    [SerializeField] private float launchDistanceFromCamera = 0.65f;
    [SerializeField] private float launchVerticalOffset;
    [SerializeField] private float weakImpulse = 3.5f;
    [SerializeField] private float mediumImpulse = 6f;
    [SerializeField] private float strongImpulse = 9f;
    [SerializeField] private int weakDamage = 10;
    [SerializeField] private int mediumDamage = 20;
    [SerializeField] private int strongDamage = 30;

    [Header("Blue Charge")]
    [SerializeField] private float chargeDistanceForMax = 6f;
    [SerializeField] private float minChargeMultiplier = 1f;
    [SerializeField] private float maxChargeMultiplier = 3f;
    [SerializeField, Range(-1f, 1f)] private float reverseDirectionDotThreshold = 0.4f;
    [SerializeField] private float reverseInvalidationGraceDistance = 0.015f;

    [Header("Melee")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private int meleeDamage = 15;
    [SerializeField] private LayerMask meleeHitMask = ~0;

    [Header("Fox Ultimate")]
    [SerializeField] private GameObject foxPrefab;
    [SerializeField] private float ultimateSpawnMinDelay = 2.5f;
    [SerializeField] private float ultimateSpawnMaxDelay = 5.5f;
    [SerializeField] private float ultimateVisibleDuration = 1.1f;
    [SerializeField] private float ultimateTargetRadius = 0.045f;
    [SerializeField] private float ultimateOuterMinRadius = 0.35f;
    [SerializeField] private float ultimateOuterMaxRadius = 0.43f;
    [SerializeField] private float ultimateSpawnDistanceFromCamera = 1.1f;
    [SerializeField] private int ultimateFoxDamage = 45;

    [Header("Player Health UI")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField, Range(0f, 0.2f)] private float healthBarCenterGap = 0.055f;
    [SerializeField, Range(0f, 0.2f)] private float healthBarSideMargin = 0.035f;
    [SerializeField, Range(4f, 28f)] private float healthBarWidth = 12f;
    [SerializeField, Range(0.2f, 1f)] private float healthBarAlpha = 0.72f;

    [Header("Debug")]
    [SerializeField] private bool logAttacks = true;
    [SerializeField] private bool showDebugOverlay = true;
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
        chargeDistanceForMax = Mathf.Max(0.01f, chargeDistanceForMax);
        minChargeMultiplier = Mathf.Max(0f, minChargeMultiplier);
        maxChargeMultiplier = Mathf.Max(maxChargeMultiplier, minChargeMultiplier);
        chargeFillMaxAlpha = Mathf.Clamp(chargeFillMaxAlpha, 0.02f, 0.35f);
        reverseInvalidationGraceDistance = Mathf.Max(0f, reverseInvalidationGraceDistance);
        ultimateTargetRadius = Mathf.Max(0.01f, ultimateTargetRadius);
        ultimateOuterMinRadius = Mathf.Max(ultimateOuterMinRadius, redRadius + ultimateTargetRadius);
        ultimateOuterMaxRadius = Mathf.Max(ultimateOuterMaxRadius, ultimateOuterMinRadius);
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        EnsurePlayerHealth();
        EnsureProjectileAttack();
        ResetInputState();
        ultimate.ScheduleNextTarget(GetUltimateConfig());
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

        ultimate.UpdateTarget(chargeCenterViewport, GetChargeRadiusScale(), GetUltimateConfig(), logAttacks);
        HandlePointerInput();
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
        lastPointerViewportPosition = viewportPosition;
        hasPointerPosition = true;

        if (pointerDown)
        {
            BeginDrag(viewportPosition);
        }

        if (isDragging && pointerHeld)
        {
            UpdateCharge(viewportPosition);
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
            screenPosition = touch.position;
            pointerDown = touch.phase == TouchPhase.Began;
            pointerHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            pointerUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }

        pointerDown = Input.GetMouseButtonDown(0);
        pointerHeld = Input.GetMouseButton(0);
        pointerUp = Input.GetMouseButtonUp(0);

        if (!pointerDown && !pointerHeld && !pointerUp)
        {
            return false;
        }

        screenPosition = Input.mousePosition;
        return true;
    }

    private void BeginDrag(Vector2 viewportPosition)
    {
        currentAttackMode = AttackMode.None;
        isDragging = GetChargeZone(viewportPosition) == ChargeZone.Blue;
        charge.Begin(viewportPosition, minChargeMultiplier);

        if (logAttacks && !isDragging)
        {
            Debug.Log("[PlayerAttack] Input ignored. Start in the shared blue zone.");
        }
    }

    private void UpdateCharge(Vector2 viewportPosition)
    {
        charge.Update(
            viewportPosition,
            chargeCenterViewport,
            GetChargeRadiusScale(),
            blueRadius,
            chargeDistanceForMax,
            minChargeMultiplier,
            maxChargeMultiplier,
            reverseDirectionDotThreshold,
            reverseInvalidationGraceDistance);
    }

    private void ReleaseDrag(Vector2 viewportPosition)
    {
        if (ultimate.TryTrigger(viewportPosition, charge.IsInvalidated, mainCamera, GetUltimateConfig(), GetChargeRadiusScale(), logAttacks))
        {
            ResetInputState();
            return;
        }

        currentAttackMode = GetAttackMode(viewportPosition);

        if (currentAttackMode == AttackMode.Ranged)
        {
            ReleaseRangedAttack(viewportPosition);
        }
        else if (currentAttackMode == AttackMode.Melee)
        {
            ReleaseMeleeAttack();
        }

        ResetInputState();
    }

    private void ReleaseRangedAttack(Vector2 viewportPosition)
    {
        ChargeZone zone = GetChargeZone(viewportPosition);

        switch (zone)
        {
            case ChargeZone.Green:
                FireProjectile(weakImpulse * charge.Multiplier, Mathf.RoundToInt(weakDamage * charge.Multiplier));
                break;
            case ChargeZone.Yellow:
                FireProjectile(mediumImpulse * charge.Multiplier, Mathf.RoundToInt(mediumDamage * charge.Multiplier));
                break;
            case ChargeZone.Red:
                FireProjectile(strongImpulse * charge.Multiplier, Mathf.RoundToInt(strongDamage * charge.Multiplier));
                break;
            default:
                if (logAttacks)
                {
                    Debug.Log($"[PlayerAttack] Ranged attack cancelled in {zone} zone.");
                }
                break;
        }
    }

    private void ReleaseMeleeAttack()
    {
        Transform cameraTransform = mainCamera.transform;
        bool hitSomething = Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit hit,
            meleeRange,
            meleeHitMask,
            QueryTriggerInteraction.Ignore);

        if (!hitSomething)
        {
            if (logAttacks)
            {
                Debug.Log("[PlayerAttack] Melee attack missed.");
            }

            return;
        }

        IDamageable damageable = DamageableFinder.GetInParent(hit.collider);
        if (damageable == null)
        {
            if (logAttacks)
            {
                Debug.Log($"[PlayerAttack] Melee hit {hit.collider.name}, but it has no IDamageable.");
            }

            return;
        }

        damageable.TakeDamage(meleeDamage);

        if (logAttacks)
        {
            string targetName = damageable.DamageTransform != null
                ? damageable.DamageTransform.name
                : hit.collider.name;
            Debug.Log($"[PlayerAttack] Melee hit {targetName}. Damage={meleeDamage}");
        }
    }

    private void FireProjectile(float impulse, int damage)
    {
        EnsureProjectileAttack();
        projectileAttack.Fire(mainCamera, chargeCenterViewport, impulse, damage);
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

        if (!projectileAttack.HasProjectilePrefab && projectilePrefab != null)
        {
            projectileAttack.Configure(
                projectilePrefab,
                launchPoint,
                useLaunchPointOverride,
                launchDistanceFromCamera,
                launchVerticalOffset,
                logAttacks);
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
        return viewportPosition.y <= attackModeSplitY ? AttackMode.Ranged : AttackMode.Melee;
    }

    private ChargeZone GetChargeZone(Vector2 viewportPosition)
    {
        float distance = GetChargeDistance(viewportPosition);

        if (distance <= blueRadius)
        {
            return ChargeZone.Blue;
        }

        if (distance <= greenRadius)
        {
            return ChargeZone.Green;
        }

        if (distance <= yellowRadius)
        {
            return ChargeZone.Yellow;
        }

        if (distance <= redRadius)
        {
            return ChargeZone.Red;
        }

        return ChargeZone.Outside;
    }

    private float GetChargeDistance(Vector2 viewportPosition)
    {
        Vector2 screenPosition = ViewportToScreenPoint(viewportPosition);
        Vector2 centerPosition = ViewportToScreenPoint(chargeCenterViewport);
        return Vector2.Distance(screenPosition, centerPosition) / GetChargeRadiusScale();
    }

    private float GetPlayerHealthRatio()
    {
        return playerHealth != null ? playerHealth.HealthRatio : 1f;
    }

    private PlayerUltimateConfig GetUltimateConfig()
    {
        return new PlayerUltimateConfig
        {
            FoxPrefab = foxPrefab,
            SpawnMinDelay = ultimateSpawnMinDelay,
            SpawnMaxDelay = ultimateSpawnMaxDelay,
            VisibleDuration = ultimateVisibleDuration,
            TargetRadius = ultimateTargetRadius,
            OuterMinRadius = ultimateOuterMinRadius,
            OuterMaxRadius = ultimateOuterMaxRadius,
            SpawnDistanceFromCamera = ultimateSpawnDistanceFromCamera,
            FoxDamage = ultimateFoxDamage
        };
    }

    private PlayerCombatHudSettings GetHudSettings()
    {
        return new PlayerCombatHudSettings
        {
            ShowDebugOverlay = showDebugOverlay,
            AttackModeSplitY = attackModeSplitY,
            ChargeCenterViewport = chargeCenterViewport,
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
            HealthBarAlpha = healthBarAlpha
        };
    }

    private PlayerCombatHudState GetHudState()
    {
        ChargeZone pointerZone = hasPointerPosition ? GetChargeZone(lastPointerViewportPosition) : ChargeZone.None;
        AttackMode pointerAttackMode = hasPointerPosition ? GetAttackMode(lastPointerViewportPosition) : AttackMode.None;

        return new PlayerCombatHudState
        {
            HasPointerPosition = hasPointerPosition,
            LastPointerViewportPosition = lastPointerViewportPosition,
            AttackModeLabel = pointerAttackMode.ToString(),
            ChargeZoneLabel = pointerZone.ToString(),
            ChargeInvalidated = charge.IsInvalidated,
            ChargeRatio = charge.GetRatio(minChargeMultiplier, maxChargeMultiplier),
            ChargeMultiplier = charge.Multiplier,
            PlayerHealthRatio = GetPlayerHealthRatio(),
            HasUltimateTarget = ultimate.HasTarget,
            UltimateTargetViewportPosition = ultimate.TargetViewportPosition,
            UltimateTargetRadius = ultimateTargetRadius
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

    private void ResetInputState()
    {
        isDragging = false;
        currentAttackMode = AttackMode.None;
        charge.Reset(minChargeMultiplier);
    }

    private void OnGUI()
    {
        combatHud.Draw(GetHudSettings(), GetHudState());
    }
}
