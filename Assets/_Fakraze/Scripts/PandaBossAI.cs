using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PandaBossAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Boss Settings")]
    public float maxAngularSpeed = 60f;
    public float minAngularSpeed = 30f;
    public float minAngleForSpeed = 30f;
    public float maxAngleForSpeed = 90f;
    public float faceOffsetY = 180f;

    [Header("Boss Health")]
    public int maxHealth = 100;
    public int currentHealth;
    private bool isDead = false;

    [Header("Animation")]
    public Animator animator;

    [Tooltip("你的 Animator 裡 Throw state 對應的 Trigger 名稱")]
    public string throwTriggerName = "Throw";

    [Tooltip("受傷動畫 1 的 Trigger 名稱")]
    public string hit1TriggerName = "Hit1";

    [Tooltip("受傷動畫 2 的 Trigger 名稱")]
    public string hit2TriggerName = "Hit2";

    [Tooltip("召喚/施法動畫 Trigger，可以放 Cast1, Cast2, Cast3, Cast4")]
    public string[] castTriggerNames = { "Cast1", "Cast2", "Cast3", "Cast4" };

    [Tooltip("死亡 Bool 名稱")]
    public string deadBoolName = "Dead";

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

    [Header("Meatball Meteor Attack")]
    public GameObject meatballMeteorPrefab;
    public float meteorInterval = 6.0f;
    public int meteorCount = 3;
    public float meteorSpawnDelay = 0.25f;
    public float meteorSpawnHeight = 10f;
    public float meteorRadiusAroundPlayer = 4f;
    public float meteorMinDistanceBetweenTargets = 3f;
    public int meteorTargetSearchAttempts = 30;
    public LayerMask groundLayer;

    [Header("Attack Cooldown")]
    public float clawCooldown = 2.0f;
    public float plumCooldown = 3.0f;

    [Header("Debug")]
    public bool showClawRange = true;

    private float clawTimer = 0f;
    private float plumTimer = 0f;
    private float meteorTimer = 0f;

    private bool isAttacking = false;

    private PlayerHealth playerHealth;

    private int throwTriggerHash;
    private int hit1TriggerHash;
    private int hit2TriggerHash;
    private int deadBoolHash;
    private int[] castTriggerHashes;

    private void Awake()
    {
        currentHealth = maxHealth;

        throwTriggerHash = Animator.StringToHash(throwTriggerName);
        hit1TriggerHash = Animator.StringToHash(hit1TriggerName);
        hit2TriggerHash = Animator.StringToHash(hit2TriggerName);
        deadBoolHash = Animator.StringToHash(deadBoolName);

        castTriggerHashes = new int[castTriggerNames.Length];

        for (int i = 0; i < castTriggerNames.Length; i++)
        {
            castTriggerHashes[i] = Animator.StringToHash(castTriggerNames[i]);
        }
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

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
        if (isDead) return;
        if (player == null) return;

        UpdateTimers();

        if (isAttacking) return;

        FacePlayer();

        float distance = Vector3.Distance(transform.position, player.position);

        if (!IsFacingPlayer(attackFacingAngle))
        {
            return;
        }

        if (meteorTimer <= 0f)
        {
            TryMeteorAttack();
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

        if (meteorTimer > 0f)
        {
            meteorTimer -= Time.deltaTime;
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

        PlayRandomCastAnimation();

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

        PlayThrowAnimation();

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

    private void TryMeteorAttack()
    {
        if (meteorTimer > 0f) return;

        meteorTimer = meteorInterval;

        StartCoroutine(MeteorAttackRoutine());
    }

    private IEnumerator MeteorAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss summons Meatball Meteors!");

        PlayRandomCastAnimation();

        List<Vector3> targetPositions = GetNonOverlappingMeteorTargetPositions();

        for (int i = 0; i < targetPositions.Count; i++)
        {
            SpawnMeteor(targetPositions[i]);

            yield return new WaitForSeconds(meteorSpawnDelay);
        }

        isAttacking = false;
    }

    private void SpawnMeteor(Vector3 targetPosition)
    {
        if (meatballMeteorPrefab == null)
        {
            Debug.LogWarning("PandaBossAI: Meatball Meteor Prefab is missing.");
            return;
        }

        Vector3 spawnPosition = targetPosition + Vector3.up * meteorSpawnHeight;

        GameObject meteorObj = Instantiate(
            meatballMeteorPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Meteor meteor = meteorObj.GetComponent<Meteor>();

        if (meteor != null)
        {
            meteor.Init(targetPosition);
        }
        else
        {
            Debug.LogWarning("PandaBossAI: Meteor.cs is not attached to the meatball meteor prefab.");
        }
    }

    private Vector3 GetMeteorTargetPosition()
    {
        if (player == null)
        {
            return transform.position;
        }

        Vector2 randomCircle = Random.insideUnitCircle * meteorRadiusAroundPlayer;

        Vector3 randomPosition = player.position + new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        Ray ray = new Ray(randomPosition + Vector3.up * 20f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, groundLayer))
        {
            return hit.point;
        }

        return new Vector3(randomPosition.x, player.position.y, randomPosition.z);
    }

    private List<Vector3> GetNonOverlappingMeteorTargetPositions()
    {
        List<Vector3> targets = new List<Vector3>();

        if (meteorCount <= 0)
        {
            return targets;
        }

        float minDistance = Mathf.Max(meteorMinDistanceBetweenTargets, 0f);
        int maxAttemptsPerMeteor = Mathf.Max(meteorTargetSearchAttempts, 1);

        for (int i = 0; i < meteorCount; i++)
        {
            bool foundValidPosition = false;
            Vector3 bestCandidate = player != null ? player.position : transform.position;
            float bestDistanceScore = -1f;

            for (int attempt = 0; attempt < maxAttemptsPerMeteor; attempt++)
            {
                Vector3 candidate = GetMeteorTargetPosition();
                float nearestDistance = GetNearestHorizontalDistance(candidate, targets);

                if (targets.Count == 0 || nearestDistance >= minDistance)
                {
                    targets.Add(candidate);
                    foundValidPosition = true;
                    break;
                }

                if (nearestDistance > bestDistanceScore)
                {
                    bestDistanceScore = nearestDistance;
                    bestCandidate = candidate;
                }
            }

            if (!foundValidPosition)
            {
                targets.Add(bestCandidate);

                Debug.LogWarning(
                    "PandaBossAI: Could not find a fully non-overlapping meteor target. " +
                    "Consider increasing Meteor Radius Around Player or reducing Meteor Count."
                );
            }
        }

        return targets;
    }

    private float GetNearestHorizontalDistance(Vector3 candidate, List<Vector3> existingTargets)
    {
        if (existingTargets.Count == 0)
        {
            return float.MaxValue;
        }

        float nearestDistance = float.MaxValue;

        for (int i = 0; i < existingTargets.Count; i++)
        {
            Vector2 candidateXZ = new Vector2(candidate.x, candidate.z);
            Vector2 existingXZ = new Vector2(existingTargets[i].x, existingTargets[i].z);

            float distance = Vector2.Distance(candidateXZ, existingXZ);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        return nearestDistance;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log("Panda Boss takes " + damage + " damage. Current HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayRandomHitAnimation();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;

        StopAllCoroutines();

        if (clawWarning != null)
        {
            clawWarning.Hide();
        }

        if (animator != null)
        {
            animator.SetBool(deadBoolHash, true);
        }

        Debug.Log("Panda Boss died!");
    }

    private void PlayThrowAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger(hit1TriggerHash);
        animator.ResetTrigger(hit2TriggerHash);

        animator.SetTrigger(throwTriggerHash);
    }

    private void PlayRandomHitAnimation()
    {
        if (animator == null) return;

        int randomHit = Random.Range(0, 2);

        if (randomHit == 0)
        {
            animator.SetTrigger(hit1TriggerHash);
        }
        else
        {
            animator.SetTrigger(hit2TriggerHash);
        }
    }

    private void PlayRandomCastAnimation()
    {
        if (animator == null) return;
        if (castTriggerHashes == null || castTriggerHashes.Length == 0) return;

        int randomIndex = Random.Range(0, castTriggerHashes.Length);
        animator.SetTrigger(castTriggerHashes[randomIndex]);
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