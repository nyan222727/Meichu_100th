using System.Collections;
using UnityEngine;

public class PandaBossAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Boss Settings")]
    // Maximum angular speed (degrees/sec) when angle error is at or above `maxAngleForSpeed`.
    public float maxAngularSpeed = 60f;
    // Minimum angular speed (degrees/sec) when angle error is at or below `minAngleForSpeed`.
    public float minAngularSpeed = 30f;
    // Angle (degrees) at which angular speed equals `minAngularSpeed`.
    public float minAngleForSpeed = 30f;
    // Angle (degrees) at which angular speed equals `maxAngularSpeed`.
    public float maxAngleForSpeed = 90f;
    public float faceOffsetY = 180f;

    [Header("Attack Range")]
    public float clawRange = 2.0f;
    public float plumRange = 8.0f;

    [Header("Attack Facing Check")]
    public float attackFacingAngle = 60f;

    [Header("Claw Attack")]
    public float clawAngle = 90f;
    public int clawDamage = 15;

    [Header("Claw Warning")]
    public ClawWarning clawWarning;
    public float clawWarningTime = 0.5f;

    [Header("Plum Attack")]
    public GameObject plumProjectilePrefab;
    public Transform plumSpawnPoint;
    public int plumDamage = 10;
    public float plumProjectileSpeed = 6f;
    public float plumWindupTime = 0.3f;

    [Header("Attack Cooldown")]
    public float clawCooldown = 2.0f;
    public float plumCooldown = 3.0f;

    [Header("Debug")]
    public bool showClawRange = true;

    private float clawTimer = 0f;
    private float plumTimer = 0f;

    private bool isAttacking = false;

    private PlayerHealth playerHealth;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("PandaBossAI: Cannot find Player. Please assign player manually or set Player tag.");
            }
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                Debug.LogWarning("PandaBossAI: PlayerHealth not found on player.");
            }
        }

        if (clawWarning != null)
        {
            clawWarning.Hide();
        }
    }

    private void Update()
    {
        if (player == null) return;

        UpdateTimers();

        // 攻擊期間不轉向，也不重新選擇攻擊
        if (isAttacking) return;

        FacePlayer();

        float distance = Vector3.Distance(transform.position, player.position);

        // 如果熊貓還沒有面向玩家，就先不要攻擊
        if (!IsFacingPlayer(attackFacingAngle))
        {
            return;
        }

        if (distance <= clawRange)
        {
            TryClawAttack();
        }
        else if (distance <= plumRange)
        {
            TryPlumAttack();
        }
    }

    private void UpdateTimers()
    {
        if (clawTimer > 0f)
        {
            clawTimer -= Time.deltaTime;
        }

        if (plumTimer > 0f)
        {
            plumTimer -= Time.deltaTime;
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction) *
            Quaternion.Euler(0f, faceOffsetY, 0f);

        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);

        float maxSpeed = Mathf.Max(maxAngularSpeed, minAngularSpeed);
        float minSpeed = Mathf.Min(maxAngularSpeed, minAngularSpeed);

        float angleRange = Mathf.Max(maxAngleForSpeed - minAngleForSpeed, 0.001f);
        float t = (angleToTarget - minAngleForSpeed) / angleRange;
        t = Mathf.Clamp01(t);

        float angularSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
        float maxStep = angularSpeed * Time.deltaTime;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            maxStep
        );
    }

    private bool IsFacingPlayer(float maxAngle)
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f) return true;

        Vector3 bossForward = GetBossForward();
        float angle = Vector3.Angle(bossForward, toPlayer.normalized);

        return angle <= maxAngle;
    }

    private void TryClawAttack()
    {
        if (clawTimer > 0f) return;

        clawTimer = clawCooldown;

        StartCoroutine(ClawAttackRoutine());
    }

    private IEnumerator ClawAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss prepares Claw Attack!");

        if (clawWarning != null)
        {
            clawWarning.Show(clawRange, clawAngle);
        }

        yield return new WaitForSeconds(clawWarningTime);

        Debug.Log("Panda Boss uses Claw Attack!");

        if (IsPlayerInClawArea())
        {
            Debug.Log("Claw Attack HIT player!");

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(clawDamage);
            }
        }
        else
        {
            Debug.Log("Claw Attack missed.");
        }

        if (clawWarning != null)
        {
            clawWarning.Hide();
        }

        isAttacking = false;
    }

    private bool IsPlayerInClawArea()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > clawRange)
        {
            return false;
        }

        Vector3 bossForward = GetBossForward();
        float angle = Vector3.Angle(bossForward, toPlayer.normalized);

        return angle <= clawAngle * 0.5f && angle <= attackFacingAngle;
    }

    private Vector3 GetBossForward()
    {
        Vector3 forward = Quaternion.Euler(0f, -faceOffsetY, 0f) * transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            return transform.forward;
        }

        return forward.normalized;
    }

    private void TryPlumAttack()
    {
        if (plumTimer > 0f) return;

        plumTimer = plumCooldown;

        StartCoroutine(PlumAttackRoutine());
    }

    private IEnumerator PlumAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss prepares Plum Attack!");

        yield return new WaitForSeconds(plumWindupTime);

        Debug.Log("Panda Boss shoots Plum Blossom!");

        ShootPlumProjectile();

        isAttacking = false;
    }

    private void ShootPlumProjectile()
    {
        if (plumProjectilePrefab == null)
        {
            Debug.LogWarning("PandaBossAI: Plum Projectile Prefab is missing.");
            return;
        }

        if (plumSpawnPoint == null)
        {
            Debug.LogWarning("PandaBossAI: Plum Spawn Point is missing.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("PandaBossAI: Player is missing, cannot shoot Plum Projectile.");
            return;
        }

        Vector3 shootDirection = player.position - plumSpawnPoint.position;
        shootDirection.y = 0f;

        if (shootDirection.sqrMagnitude < 0.001f)
        {
            shootDirection = GetBossForward();
        }

        shootDirection.Normalize();

        Quaternion projectileRotation = Quaternion.LookRotation(shootDirection);

        GameObject projectileObj = Instantiate(
            plumProjectilePrefab,
            plumSpawnPoint.position,
            projectileRotation
        );

        PlumProjectile plumProjectile = projectileObj.GetComponent<PlumProjectile>();

        if (plumProjectile != null)
        {
            plumProjectile.damage = plumDamage;
            plumProjectile.speed = plumProjectileSpeed;
            plumProjectile.Init(shootDirection);
        }
        else
        {
            Debug.LogWarning("PandaBossAI: PlumProjectile.cs is not attached to the projectile prefab.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showClawRange) return;

        Vector3 origin = transform.position;
        Vector3 forward = GetBossForward();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, clawRange);

        Vector3 leftDir = Quaternion.Euler(0f, -clawAngle * 0.5f, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, clawAngle * 0.5f, 0f) * forward;

        Gizmos.DrawLine(origin, origin + leftDir * clawRange);
        Gizmos.DrawLine(origin, origin + rightDir * clawRange);

        Gizmos.color = Color.yellow;

        Vector3 facingLeftDir = Quaternion.Euler(0f, -attackFacingAngle, 0f) * forward;
        Vector3 facingRightDir = Quaternion.Euler(0f, attackFacingAngle, 0f) * forward;

        Gizmos.DrawLine(origin, origin + facingLeftDir * plumRange);
        Gizmos.DrawLine(origin, origin + facingRightDir * plumRange);
    }
}