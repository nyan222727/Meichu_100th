using UnityEngine;

public class Drag : MonoBehaviour
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
    [SerializeField] private float launchVerticalOffset = 0f;
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
    [SerializeField] private int maxPlayerHealth = 100;
    [SerializeField] private int currentPlayerHealth = 100;
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

    private Camera mainCamera;
    private bool isDragging;
    private AttackMode currentAttackMode;
    private bool hasPointerPosition;
    private Vector2 lastPointerViewportPosition;
    private bool hasUltimateTarget;
    private float nextUltimateTargetTime;
    private float ultimateTargetExpiresAt;
    private Vector2 ultimateTargetViewportPosition;
    private bool hasExitedBlueZone;
    private bool chargeInvalidated;
    private float accumulatedChargeDistance;
    private float currentChargeMultiplier = 1f;
    private Vector2 previousDragViewportPosition;
    private Vector2 blueExitDirection;

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
        maxPlayerHealth = Mathf.Max(1, maxPlayerHealth);
        currentPlayerHealth = Mathf.Clamp(currentPlayerHealth, 0, maxPlayerHealth);
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
        ResetInputState();
        ScheduleNextUltimateTarget();
    }

    public void SetPlayerHealth(int currentHealth, int maxHealth)
    {
        maxPlayerHealth = Mathf.Max(1, maxHealth);
        currentPlayerHealth = Mathf.Clamp(currentHealth, 0, maxPlayerHealth);
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

        UpdateUltimateTarget();
        HandlePointerInput();
    }

    private void UpdateUltimateTarget()
    {
        if (hasUltimateTarget)
        {
            if (Time.time >= ultimateTargetExpiresAt)
            {
                hasUltimateTarget = false;
                ScheduleNextUltimateTarget();
            }

            return;
        }

        if (Time.time >= nextUltimateTargetTime)
        {
            SpawnUltimateTarget();
        }
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
        accumulatedChargeDistance = 0f;
        currentChargeMultiplier = minChargeMultiplier;
        chargeInvalidated = false;
        hasExitedBlueZone = false;
        blueExitDirection = Vector2.zero;
        previousDragViewportPosition = viewportPosition;

        if (logAttacks && !isDragging)
        {
            Debug.Log("[PlayerAttack] Drag ignored. Start in the shared blue zone.");
        }
    }

    private void UpdateCharge(Vector2 viewportPosition)
    {
        float distanceFromCenter = GetChargeDistance(viewportPosition);

        if (!hasExitedBlueZone)
        {
            if (distanceFromCenter <= blueRadius)
            {
                accumulatedChargeDistance += GetNormalizedScreenDistance(previousDragViewportPosition, viewportPosition);
                currentChargeMultiplier = GetCurrentChargeMultiplier();
            }
            else
            {
                hasExitedBlueZone = true;
                blueExitDirection = GetDirectionFromCenter(viewportPosition);
            }
        }
        else if (!chargeInvalidated)
        {
            if (distanceFromCenter < blueRadius - reverseInvalidationGraceDistance)
            {
                chargeInvalidated = true;
            }
            else
            {
                Vector2 currentDirection = GetDirectionFromCenter(viewportPosition);
                if (blueExitDirection != Vector2.zero && Vector2.Dot(currentDirection, blueExitDirection) <= reverseDirectionDotThreshold)
                {
                    chargeInvalidated = true;
                }
            }

            currentChargeMultiplier = GetCurrentChargeMultiplier();
        }

        previousDragViewportPosition = viewportPosition;
    }

    private void ReleaseDrag(Vector2 viewportPosition)
    {
        if (TryTriggerUltimate(viewportPosition))
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

    private bool TryTriggerUltimate(Vector2 viewportPosition)
    {
        if (!hasUltimateTarget)
        {
            return false;
        }

        if (chargeInvalidated)
        {
            return false;
        }

        if (GetUltimateDistance(viewportPosition) > ultimateTargetRadius)
        {
            return false;
        }

        SummonFox();
        hasUltimateTarget = false;
        ScheduleNextUltimateTarget();
        return true;
    }

    private void ReleaseRangedAttack(Vector2 viewportPosition)
    {
        ChargeZone zone = GetChargeZone(viewportPosition);

        switch (zone)
        {
            case ChargeZone.Green:
                FireProjectile(weakImpulse * currentChargeMultiplier, Mathf.RoundToInt(weakDamage * currentChargeMultiplier));
                break;
            case ChargeZone.Yellow:
                FireProjectile(mediumImpulse * currentChargeMultiplier, Mathf.RoundToInt(mediumDamage * currentChargeMultiplier));
                break;
            case ChargeZone.Red:
                FireProjectile(strongImpulse * currentChargeMultiplier, Mathf.RoundToInt(strongDamage * currentChargeMultiplier));
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

        PandaHealth pandaHealth = hit.collider.GetComponentInParent<PandaHealth>();
        if (pandaHealth == null)
        {
            if (logAttacks)
            {
                Debug.Log($"[PlayerAttack] Melee hit {hit.collider.name}, but it has no PandaHealth.");
            }

            return;
        }

        pandaHealth.TakeDamage(meleeDamage);

        if (logAttacks)
        {
            Debug.Log($"[PlayerAttack] Melee hit {pandaHealth.name}. Damage={meleeDamage}");
        }
    }

    private void FireProjectile(float impulse, int damage)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerAttack] Missing projectile prefab.");
            return;
        }

        Vector3 spawnPosition = GetLaunchPosition();
        Quaternion spawnRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
        Rigidbody projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);

        projectile.isKinematic = false;
        projectile.useGravity = true;
        projectile.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;

        ProjectileDamage projectileDamage = projectile.GetComponent<ProjectileDamage>();
        if (projectileDamage == null)
        {
            projectileDamage = projectile.gameObject.AddComponent<ProjectileDamage>();
        }

        projectileDamage.SetDamage(damage);
        projectile.AddForce(mainCamera.transform.forward * impulse, ForceMode.Impulse);

        if (logAttacks)
        {
            Debug.Log($"[PlayerAttack] Fired ranged projectile. Impulse={impulse}, Damage={damage}");
        }
    }

    private Vector3 GetLaunchPosition()
    {
        if (useLaunchPointOverride && launchPoint != null)
        {
            return launchPoint.position;
        }

        Transform cameraTransform = mainCamera.transform;
        Vector3 viewportPosition = new Vector3(
            chargeCenterViewport.x,
            chargeCenterViewport.y,
            launchDistanceFromCamera);

        return mainCamera.ViewportToWorldPoint(viewportPosition)
            + (cameraTransform.up * launchVerticalOffset);
    }

    private void ScheduleNextUltimateTarget()
    {
        float minDelay = Mathf.Max(0.1f, ultimateSpawnMinDelay);
        float maxDelay = Mathf.Max(minDelay, ultimateSpawnMaxDelay);
        nextUltimateTargetTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    private void SpawnUltimateTarget()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(ultimateOuterMinRadius, ultimateOuterMaxRadius) * GetChargeRadiusScale();
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 targetScreenPosition = ViewportToScreenPoint(chargeCenterViewport) + (direction * radius);

        ultimateTargetViewportPosition = ScreenToViewport(ClampUltimateTargetToScreen(targetScreenPosition));
        hasUltimateTarget = true;
        ultimateTargetExpiresAt = Time.time + Mathf.Max(0.1f, ultimateVisibleDuration);

        if (logAttacks)
        {
            Debug.Log("[PlayerAttack] Fox ultimate target appeared.");
        }
    }

    private Vector2 ClampUltimateTargetToScreen(Vector2 screenPosition)
    {
        float margin = ultimateTargetRadius * GetChargeRadiusScale();
        return new Vector2(
            Mathf.Clamp(screenPosition.x, margin, Screen.width - margin),
            Mathf.Clamp(screenPosition.y, margin, Screen.height - margin));
    }

    private float GetUltimateDistance(Vector2 viewportPosition)
    {
        Vector2 screenPosition = ViewportToScreenPoint(viewportPosition);
        Vector2 targetPosition = ViewportToScreenPoint(ultimateTargetViewportPosition);
        return Vector2.Distance(screenPosition, targetPosition) / GetChargeRadiusScale();
    }

    private void SummonFox()
    {
        GameObject fox = foxPrefab != null
            ? Instantiate(foxPrefab, GetUltimateWorldPosition(), Quaternion.identity)
            : CreatePlaceholderFox(GetUltimateWorldPosition());

        FoxBehaviour foxBehaviour = fox.GetComponent<FoxBehaviour>();
        if (foxBehaviour == null)
        {
            foxBehaviour = fox.AddComponent<FoxBehaviour>();
        }

        foxBehaviour.Initialize(FindFirstObjectByType<PandaHealth>(), ultimateFoxDamage);

        if (logAttacks)
        {
            Debug.Log("[PlayerAttack] Fox ultimate summoned.");
        }
    }

    private Vector3 GetUltimateWorldPosition()
    {
        Vector3 viewportPosition = new Vector3(
            ultimateTargetViewportPosition.x,
            ultimateTargetViewportPosition.y,
            ultimateSpawnDistanceFromCamera);

        return mainCamera.ViewportToWorldPoint(viewportPosition);
    }

    private static GameObject CreatePlaceholderFox(Vector3 spawnPosition)
    {
        GameObject fox = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fox.name = "Fox Ultimate";
        fox.transform.position = spawnPosition;
        fox.transform.localScale = new Vector3(0.25f, 0.25f, 0.45f);

        Renderer renderer = fox.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0.35f, 0.05f, 1f);
        }

        return fox;
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

    private float GetNormalizedScreenDistance(Vector2 fromViewportPosition, Vector2 toViewportPosition)
    {
        Vector2 fromScreenPosition = ViewportToScreenPoint(fromViewportPosition);
        Vector2 toScreenPosition = ViewportToScreenPoint(toViewportPosition);
        return Vector2.Distance(fromScreenPosition, toScreenPosition) / GetChargeRadiusScale();
    }

    private Vector2 GetDirectionFromCenter(Vector2 viewportPosition)
    {
        Vector2 direction = ViewportToScreenPoint(viewportPosition) - ViewportToScreenPoint(chargeCenterViewport);
        return direction.sqrMagnitude <= 0.0001f ? Vector2.zero : direction.normalized;
    }

    private float GetCurrentChargeMultiplier()
    {
        if (chargeInvalidated)
        {
            return minChargeMultiplier;
        }

        float chargeRatio = Mathf.Clamp01(accumulatedChargeDistance / chargeDistanceForMax);
        return Mathf.Lerp(minChargeMultiplier, maxChargeMultiplier, chargeRatio);
    }

    private float GetChargeRatio()
    {
        if (maxChargeMultiplier <= minChargeMultiplier)
        {
            return 0f;
        }

        return Mathf.InverseLerp(minChargeMultiplier, maxChargeMultiplier, currentChargeMultiplier);
    }

    private float GetPlayerHealthRatio()
    {
        if (maxPlayerHealth <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(currentPlayerHealth / (float)maxPlayerHealth);
    }

    private Color GetPlayerHealthColor(float healthRatio)
    {
        float alpha = healthRatio <= 0.25f
            ? Mathf.Lerp(healthBarAlpha * 0.45f, healthBarAlpha, Mathf.PingPong(Time.time * 5f, 1f))
            : healthBarAlpha;

        return Color.Lerp(
            new Color(1f, 0.05f, 0.02f, alpha),
            new Color(0.95f, 0.01f, 0.01f, alpha),
            healthRatio);
    }

    private Vector2 ScreenToViewport(Vector2 screenPosition)
    {
        return new Vector2(
            Mathf.Clamp01(screenPosition.x / Screen.width),
            Mathf.Clamp01(screenPosition.y / Screen.height));
    }

    private void ResetInputState()
    {
        isDragging = false;
        currentAttackMode = AttackMode.None;
        hasExitedBlueZone = false;
        chargeInvalidated = false;
        accumulatedChargeDistance = 0f;
        currentChargeMultiplier = minChargeMultiplier;
        previousDragViewportPosition = default;
        blueExitDirection = Vector2.zero;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
        {
            return;
        }

        DrawChargeFill();
        DrawAttackModeSplitLine();
        DrawChargeRing(redRadius, new Color(1f, 0.15f, 0.05f, 0.9f));
        DrawChargeRing(yellowRadius, new Color(1f, 0.85f, 0.1f, 0.9f));
        DrawChargeRing(greenRadius, new Color(0.2f, 0.9f, 0.2f, 0.9f));
        DrawChargeRing(blueRadius, new Color(0.1f, 0.45f, 1f, 0.95f));
        DrawUltimateTarget();
        DrawCrosshair();
        DrawPointerMarker();
        DrawOverlayLabels();
    }

    private void DrawChargeFill()
    {
        float chargeRatio = GetChargeRatio();
        if (chargeRatio <= 0f)
        {
            return;
        }

        Vector2 center = ViewportToGuiPoint(chargeCenterViewport);
        float radius = Mathf.Lerp(blueRadius * 0.12f, blueRadius, chargeRatio) * GetChargeRadiusScale();
        float alpha = Mathf.Lerp(0.04f, chargeFillMaxAlpha, chargeRatio);
        Color fillColor = chargeInvalidated
            ? new Color(1f, 0.1f, 0.08f, Mathf.Min(alpha, 0.1f))
            : new Color(0.1f, 0.55f, 1f, alpha);

        DrawFilledEllipse(center, radius, radius, fillColor);
    }

    private void DrawAttackModeSplitLine()
    {
        float y = (1f - attackModeSplitY) * Screen.height;
        float centerX = chargeCenterViewport.x * Screen.width;
        float sideMargin = healthBarSideMargin * Screen.width;
        float centerGap = Mathf.Max(blueRadius * 0.55f * GetChargeRadiusScale(), healthBarCenterGap * GetChargeRadiusScale());
        float leftEdge = Mathf.Clamp(sideMargin, 0f, centerX);
        float rightEdge = Mathf.Clamp(Screen.width - sideMargin, centerX, Screen.width);
        float leftInner = Mathf.Max(leftEdge, centerX - centerGap);
        float rightInner = Mathf.Min(rightEdge, centerX + centerGap);
        float healthRatio = GetPlayerHealthRatio();
        float leftHealthLength = Mathf.Max(0f, leftInner - leftEdge) * healthRatio;
        float rightHealthLength = Mathf.Max(0f, rightEdge - rightInner) * healthRatio;

        DrawLine(new Vector2(0f, y), new Vector2(Screen.width, y), new Color(0f, 0f, 0f, 0.88f), overlayLineWidth + 2f);
        DrawLine(new Vector2(leftEdge, y), new Vector2(leftInner, y), new Color(0.12f, 0f, 0f, 0.62f), healthBarWidth + 3f);
        DrawLine(new Vector2(rightInner, y), new Vector2(rightEdge, y), new Color(0.12f, 0f, 0f, 0.62f), healthBarWidth + 3f);

        if (healthRatio <= 0f)
        {
            return;
        }

        Color healthColor = GetPlayerHealthColor(healthRatio);
        DrawLine(new Vector2(leftInner - leftHealthLength, y), new Vector2(leftInner, y), healthColor, healthBarWidth);
        DrawLine(new Vector2(rightInner, y), new Vector2(rightInner + rightHealthLength, y), healthColor, healthBarWidth);
        DrawLine(new Vector2(leftInner - leftHealthLength, y - healthBarWidth * 0.22f), new Vector2(leftInner, y - healthBarWidth * 0.22f), new Color(1f, 0.55f, 0.45f, healthBarAlpha * 0.35f), Mathf.Max(1f, healthBarWidth * 0.18f));
        DrawLine(new Vector2(rightInner, y - healthBarWidth * 0.22f), new Vector2(rightInner + rightHealthLength, y - healthBarWidth * 0.22f), new Color(1f, 0.55f, 0.45f, healthBarAlpha * 0.35f), Mathf.Max(1f, healthBarWidth * 0.18f));
    }

    private void DrawChargeRing(float viewportRadius, Color color)
    {
        Vector2 center = ViewportToGuiPoint(chargeCenterViewport);
        float radius = viewportRadius * GetChargeRadiusScale();
        DrawEllipse(center, radius, radius, color, overlayLineWidth);
    }

    private void DrawCrosshair()
    {
        Vector2 center = ViewportToGuiPoint(chargeCenterViewport);
        float size = Mathf.Max(8f, blueRadius * Mathf.Min(Screen.width, Screen.height) * 0.45f);
        DrawLine(center + Vector2.left * size, center + Vector2.right * size, Color.white, overlayLineWidth);
        DrawLine(center + Vector2.down * size, center + Vector2.up * size, Color.white, overlayLineWidth);
    }

    private void DrawPointerMarker()
    {
        if (!hasPointerPosition)
        {
            return;
        }

        Vector2 point = ViewportToGuiPoint(lastPointerViewportPosition);
        float size = 14f;
        DrawLine(point + new Vector2(-size, -size), point + new Vector2(size, size), Color.magenta, overlayLineWidth);
        DrawLine(point + new Vector2(-size, size), point + new Vector2(size, -size), Color.magenta, overlayLineWidth);
    }

    private void DrawUltimateTarget()
    {
        if (!hasUltimateTarget)
        {
            return;
        }

        float flash = Mathf.PingPong(Time.time * 8f, 1f);
        Color color = Color.Lerp(new Color(1f, 0.45f, 0f, 0.75f), new Color(1f, 0.95f, 0f, 1f), flash);
        Vector2 center = ViewportToGuiPoint(ultimateTargetViewportPosition);
        float radius = ultimateTargetRadius * GetChargeRadiusScale();

        DrawEllipse(center, radius, radius, color, overlayLineWidth + 2f);
        DrawLine(center + Vector2.left * radius, center + Vector2.right * radius, color, overlayLineWidth);
        DrawLine(center + Vector2.down * radius, center + Vector2.up * radius, color, overlayLineWidth);
    }

    private void DrawOverlayLabels()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(16, Screen.height / 42),
            fontStyle = FontStyle.Bold
        };

        GUI.color = Color.white;
        GUI.Label(new Rect(16f, 12f, Screen.width - 32f, 34f), "Start in BLUE. Move inside BLUE to charge. Release lower for bow, upper for melee.", style);

        if (!hasPointerPosition)
        {
            return;
        }

        AttackMode mode = GetAttackMode(lastPointerViewportPosition);
        ChargeZone zone = GetChargeZone(lastPointerViewportPosition);
        string chargeText = chargeInvalidated ? "Invalid" : $"{currentChargeMultiplier:0.00}x";
        GUI.Label(
            new Rect(16f, 46f, Screen.width - 32f, 34f),
            $"Pointer: {mode}, Zone: {zone}, Charge: {chargeText}",
            style);
    }

    private static Vector2 ViewportToGuiPoint(Vector2 viewportPosition)
    {
        return new Vector2(
            viewportPosition.x * Screen.width,
            (1f - viewportPosition.y) * Screen.height);
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

    private void DrawEllipse(Vector2 center, float radiusX, float radiusY, Color color, float width)
    {
        int segments = Mathf.Max(12, overlaySegments);
        Vector2 previousPoint = center + new Vector2(radiusX, 0f);

        for (int index = 1; index <= segments; index++)
        {
            float angle = (index / (float)segments) * Mathf.PI * 2f;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            DrawLine(previousPoint, nextPoint, color, width);
            previousPoint = nextPoint;
        }
    }

    private void DrawFilledEllipse(Vector2 center, float radiusX, float radiusY, Color color)
    {
        int segments = Mathf.Max(12, overlaySegments);
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
