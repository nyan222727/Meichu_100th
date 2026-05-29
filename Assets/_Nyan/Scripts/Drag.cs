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
    [SerializeField, Range(0.01f, 0.5f)] private float blueRadius = 0.055f;
    [SerializeField, Range(0.01f, 0.5f)] private float greenRadius = 0.105f;
    [SerializeField, Range(0.01f, 0.5f)] private float yellowRadius = 0.155f;
    [SerializeField, Range(0.01f, 0.5f)] private float redRadius = 0.215f;

    [Header("Ranged Launch")]
    [SerializeField] private float launchDistanceFromCamera = 0.65f;
    [SerializeField] private float launchVerticalOffset = 0f;
    [SerializeField] private float weakImpulse = 3.5f;
    [SerializeField] private float mediumImpulse = 6f;
    [SerializeField] private float strongImpulse = 9f;
    [SerializeField] private int weakDamage = 10;
    [SerializeField] private int mediumDamage = 20;
    [SerializeField] private int strongDamage = 30;

    [Header("Melee")]
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private int meleeDamage = 15;
    [SerializeField] private LayerMask meleeHitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool logAttacks = true;
    [SerializeField] private bool showDebugOverlay = true;
    [SerializeField, Range(24, 192)] private int overlaySegments = 96;
    [SerializeField, Range(1f, 8f)] private float overlayLineWidth = 3f;

    private Camera mainCamera;
    private bool isDragging;
    private AttackMode currentAttackMode;
    private bool hasPointerPosition;
    private Vector2 lastPointerViewportPosition;

    private void OnValidate()
    {
        if (blueRadius > 0.5f || greenRadius > 0.5f || yellowRadius > 0.5f || redRadius > 0.5f)
        {
            blueRadius = 0.055f;
            greenRadius = 0.105f;
            yellowRadius = 0.155f;
            redRadius = 0.215f;
        }

        greenRadius = Mathf.Max(greenRadius, blueRadius);
        yellowRadius = Mathf.Max(yellowRadius, greenRadius);
        redRadius = Mathf.Max(redRadius, yellowRadius);
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        ResetInputState();
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

        if (logAttacks && !isDragging)
        {
            Debug.Log("[PlayerAttack] Drag ignored. Start in the shared blue zone.");
        }
    }

    private void ReleaseDrag(Vector2 viewportPosition)
    {
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
                FireProjectile(weakImpulse, weakDamage);
                break;
            case ChargeZone.Yellow:
                FireProjectile(mediumImpulse, mediumDamage);
                break;
            case ChargeZone.Red:
                FireProjectile(strongImpulse, strongDamage);
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

    private AttackMode GetAttackMode(Vector2 viewportPosition)
    {
        return viewportPosition.y <= attackModeSplitY ? AttackMode.Ranged : AttackMode.Melee;
    }

    private ChargeZone GetChargeZone(Vector2 viewportPosition)
    {
        float distance = Vector2.Distance(viewportPosition, chargeCenterViewport);

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
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
        {
            return;
        }

        DrawAttackModeSplitLine();
        DrawChargeRing(redRadius, new Color(1f, 0.15f, 0.05f, 0.9f));
        DrawChargeRing(yellowRadius, new Color(1f, 0.85f, 0.1f, 0.9f));
        DrawChargeRing(greenRadius, new Color(0.2f, 0.9f, 0.2f, 0.9f));
        DrawChargeRing(blueRadius, new Color(0.1f, 0.45f, 1f, 0.95f));
        DrawCrosshair();
        DrawPointerMarker();
        DrawOverlayLabels();
    }

    private void DrawAttackModeSplitLine()
    {
        float y = (1f - attackModeSplitY) * Screen.height;
        DrawLine(new Vector2(0f, y), new Vector2(Screen.width, y), new Color(0f, 0f, 0f, 0.9f), overlayLineWidth + 2f);
    }

    private void DrawChargeRing(float viewportRadius, Color color)
    {
        Vector2 center = ViewportToGuiPoint(chargeCenterViewport);
        float radiusX = viewportRadius * Screen.width;
        float radiusY = viewportRadius * Screen.height;
        DrawEllipse(center, radiusX, radiusY, color, overlayLineWidth);
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

    private void DrawOverlayLabels()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Max(16, Screen.height / 42),
            fontStyle = FontStyle.Bold
        };

        GUI.color = Color.white;
        GUI.Label(new Rect(16f, 12f, Screen.width - 32f, 34f), "Start in BLUE. Release lower for bow, upper for melee. Green/Yellow/Red sets bow power.", style);

        if (!hasPointerPosition)
        {
            return;
        }

        AttackMode mode = GetAttackMode(lastPointerViewportPosition);
        ChargeZone zone = GetChargeZone(lastPointerViewportPosition);
        GUI.Label(
            new Rect(16f, 46f, Screen.width - 32f, 34f),
            $"Pointer: {mode}, Zone: {zone}",
            style);
    }

    private static Vector2 ViewportToGuiPoint(Vector2 viewportPosition)
    {
        return new Vector2(
            viewportPosition.x * Screen.width,
            (1f - viewportPosition.y) * Screen.height);
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
