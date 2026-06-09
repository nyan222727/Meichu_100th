using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PandaBossAI : MonoBehaviour, IDamageable, IHitStunnable
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

    public Transform DamageTransform => transform;
    public bool IsDefeated => isDead;

    [Header("Animation")]
    public Animator animator;

    [Tooltip("丟梅花動畫 Trigger 名稱")]
    public string throwTriggerName = "Throw";

    [Tooltip("爪擊動畫 Trigger 名稱")]
    public string clawTriggerName = "Claw";

    [Tooltip("受傷動畫共用 Trigger 名稱")]
    public string hitTriggerName = "Hit";

    [Tooltip("受傷動畫 Index 參數名稱")]
    public string hitIndexName = "HitIndex";

    [Tooltip("召喚/施法動畫共用 Trigger 名稱")]
    public string castTriggerName = "Cast";

    [Tooltip("召喚/施法動畫 Index 參數名稱")]
    public string castIndexName = "CastIndex";

    [Tooltip("受傷動畫數量，例如 Hit1 / Hit2 就填 2")]
    public int hitAnimationCount = 2;

    [Tooltip("召喚動畫數量，例如 Cast1 ~ Cast4 就填 4")]
    public int castAnimationCount = 4;

    [Tooltip("死亡 Bool 名稱")]
    public string deadBoolName = "Dead";

    [Header("Sound")]
    public PandaSoundController soundController;

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

    [Header("Claw Weapon")]
    [Tooltip("Claw attack 時顯示的武器物件，例如掛在手上的 OlooPivot。")]
    public GameObject clawWeaponObject;

    [Tooltip("Claw attack 開始後幾秒顯示武器。")]
    public float clawWeaponShowDelay = 0f;

    [Tooltip("Claw attack 開始後幾秒隱藏武器。")]
    public float clawWeaponHideDelay = 0.9f;

    [Tooltip("遊戲開始時是否自動隱藏 Claw Weapon。")]
    public bool hideClawWeaponOnStart = true;

    [Header("Plum Attack")]
    public GameObject plumProjectilePrefab;
    public Transform plumSpawnPoint;
    public Transform plumTargetPoint;
    public int plumDamage = 10;
    public float plumProjectileSpeed = 6f;
    public float plumWindupTime = 0.3f;

    [Header("Meatball Meteor Attack")]
    public GameObject meatballMeteorPrefab;
    public int meteorCount = 3;
    public float meteorSpawnDelay = 0.25f;
    public float meteorSpawnHeight = 10f;
    public float meteorRadiusAroundPlayer = 4f;
    public float meteorMinDistanceBetweenTargets = 3f;
    public int meteorTargetSearchAttempts = 30;
    public LayerMask groundLayer;

    [Header("Quiz Attack")]
    public bool enableQuizAttack = true;
    public QuizManager quizManager;
    public float quizRange = 8.0f;
    public float quizWindupTime = 0.35f;
    public int quizWrongAnswerDamage = 10;
    public int quizTimeoutDamage = 15;

    [Header("Individual Attack Cooldown")]
    [Tooltip("Claw 自己的冷卻時間。Claw 冷卻中時，只有 Claw 不能放。")]
    public float clawCooldown = 2.0f;

    [Tooltip("Plum 自己的冷卻時間。Plum 冷卻中時，只有 Plum 不能放。")]
    public float plumCooldown = 3.0f;

    [Tooltip("Meteor 自己的冷卻時間。Meteor 冷卻中時，只有 Meteor 不能放。")]
    public float meteorCooldown = 6.0f;

    [Tooltip("Quiz 自己的冷卻時間。Quiz 冷卻中時，只有 Quiz 不能放。")]
    public float quizCooldown = 10.0f;

    [Header("Global Attack Cooldown")]
    [Tooltip("Claw 攻擊結束後，幾秒內不能進行任何攻擊。")]
    public float globalCooldownAfterClaw = 0.5f;

    [Tooltip("Plum 攻擊結束後，幾秒內不能進行任何攻擊。")]
    public float globalCooldownAfterPlum = 0.8f;

    [Tooltip("Meteor 攻擊結束後，幾秒內不能進行任何攻擊。")]
    public float globalCooldownAfterMeteor = 1.2f;

    [Tooltip("Quiz 題目出現後，幾秒內不能進行任何攻擊。")]
    public float globalCooldownAfterQuiz = 1.0f;

    [Header("Death Sink")]
    [Tooltip("死亡時要下沉的物件。建議指定 PandaBoss 最外層物件，或一個不會被 Animator 控制的 Parent。")]
    public Transform deadSinkTarget;

    [Tooltip("死亡時，Y 會慢慢降低多少。0.4 代表往下沉 0.4。")]
    public float deadSinkDistance = 0.4f;

    [Tooltip("死亡下沉花多久完成，單位是秒。")]
    public float deadSinkDuration = 1.0f;

    [Tooltip("如果你在 Animator 視窗直接把 Dead Bool 打開，是否也要觸發下沉。")]
    public bool sinkWhenAnimatorDeadBoolIsTrue = true;

    [Header("Death Elevator")]
    public GameObject goDownElevatorObject;
    public string deathAnimationStateName = "Dead";
    public float goDownElevatorSpawnFallbackDelay = 1.2f;
    public float goDownElevatorAnimationWaitTimeout = 5f;
    public bool hideGoDownElevatorOnStart = true;

    [Header("Debug")]
    public bool showClawRange = true;

    private float clawTimer = 0f;
    private float plumTimer = 0f;
    private float meteorTimer = 0f;
    private float quizTimer = 0f;
    private float globalAttackTimer = 0f;

    private Coroutine deadSinkCoroutine;
    private Coroutine clawWeaponCoroutine;
    private bool hasStartedDeathSink = false;
    private bool hasSpawnedGoDownElevator = false;

    private bool isAttacking = false;
    private float hitStunRemaining;
    private float animatorSpeedBeforeHitStun = 1f;
    private bool warnedMissingQuizManager;

    private PlayerHealth playerHealth;

    private int throwTriggerHash;
    private int clawTriggerHash;
    private int hitTriggerHash;
    private int hitIndexHash;
    private int castTriggerHash;
    private int castIndexHash;
    private int deadBoolHash;

    private void Awake()
    {
        currentHealth = maxHealth;

        throwTriggerHash = Animator.StringToHash(throwTriggerName);
        clawTriggerHash = Animator.StringToHash(clawTriggerName);

        hitTriggerHash = Animator.StringToHash(hitTriggerName);
        hitIndexHash = Animator.StringToHash(hitIndexName);

        castTriggerHash = Animator.StringToHash(castTriggerName);
        castIndexHash = Animator.StringToHash(castIndexName);

        deadBoolHash = Animator.StringToHash(deadBoolName);
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (soundController == null)
        {
            soundController = GetComponent<PandaSoundController>();
        }

        if (deadSinkTarget == null)
        {
            deadSinkTarget = transform;
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

        if (hideClawWeaponOnStart && clawWeaponObject != null)
        {
            clawWeaponObject.SetActive(false);
        }

        ResolveGoDownElevatorObject();
        if (hideGoDownElevatorOnStart && goDownElevatorObject != null)
        {
            goDownElevatorObject.SetActive(false);
        }

        ResolveQuizManager(false);
    }
    private void Update()
    {
        CheckAnimatorDeadBoolForSink();
        UpdateHitStun();

        if (isDead) return;
        if (hitStunRemaining > 0f) return;
        if (player == null) return;

        UpdateTimers();

        if (isAttacking) return;

        FacePlayer();

        if (!IsFacingPlayer(attackFacingAngle))
        {
            return;
        }

        if (globalAttackTimer > 0f)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Meteor 是定期技能，優先檢查
        if (meteorTimer <= 0f)
        {
            TryMeteorAttack();
            return;
        }

        // 近距離優先 Claw
        if (distance <= clawRange)
        {
            TryClawAttack();
            return;
        }

        // 數學題攻擊：Boss 叫出題目 UI，答錯或超時會扣玩家血。
        if (distance <= quizRange && TryQuizAttack())
        {
            return;
        }

        // 中距離使用 Plum
        if (distance <= plumRange)
        {
            TryPlumAttack();
            return;
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

        if (quizTimer > 0f)
        {
            quizTimer -= Time.deltaTime;
        }

        if (globalAttackTimer > 0f)
        {
            globalAttackTimer -= Time.deltaTime;
        }
    }

    private void UpdateHitStun()
    {
        if (hitStunRemaining <= 0f)
        {
            return;
        }

        hitStunRemaining = Mathf.Max(0f, hitStunRemaining - Time.unscaledDeltaTime);
        if (hitStunRemaining <= 0f && animator != null)
        {
            animator.speed = animatorSpeedBeforeHitStun;
        }
    }

    private IEnumerator WaitForActiveSeconds(float duration)
    {
        float remaining = Mathf.Max(0f, duration);
        while (remaining > 0f)
        {
            if (hitStunRemaining <= 0f)
            {
                remaining -= Time.deltaTime;
            }

            yield return null;
        }
    }

    private void StartGlobalCooldown(float duration)
    {
        globalAttackTimer = Mathf.Max(duration, 0f);
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
        if (globalAttackTimer > 0f) return;

        clawTimer = clawCooldown;
        StartCoroutine(ClawAttackRoutine());
    }

    private IEnumerator ClawAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss prepares Claw Attack!");

        PlayClawAnimation();

        StartClawWeaponRoutine();

        if (soundController != null)
        {
            soundController.PlayClawSound();
        }

        if (clawWarning != null)
        {
            clawWarning.Show(clawRange, clawAngle);
        }

        yield return WaitForActiveSeconds(clawWarningTime);

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

        if (clawWeaponCoroutine == null && clawWeaponObject != null)
        {
            clawWeaponObject.SetActive(false);
        }

        isAttacking = false;
        StartGlobalCooldown(globalCooldownAfterClaw);
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

    private bool TryQuizAttack()
    {
        if (!enableQuizAttack) return false;
        if (quizTimer > 0f) return false;
        if (globalAttackTimer > 0f) return false;
        if (!ResolveQuizManager(true)) return false;
        if (quizManager.IsAnswering) return false;

        quizTimer = Mathf.Max(quizCooldown, 0f);
        StartCoroutine(QuizAttackRoutine());
        return true;
    }

    private IEnumerator QuizAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss prepares Quiz Attack!");

        PlayRandomCastAnimation();

        if (soundController != null)
        {
            soundController.PlayCastSound();
        }

        yield return WaitForActiveSeconds(quizWindupTime);

        if (ResolveQuizManager(true))
        {
            bool started = quizManager.TryTriggerNewQuiz(
                playerHealth,
                quizWrongAnswerDamage,
                quizTimeoutDamage
            );

            if (!started)
            {
                Debug.Log("Panda Boss Quiz Attack skipped because quiz is already active or UI references are missing.");
            }
        }

        isAttacking = false;
        StartGlobalCooldown(globalCooldownAfterQuiz);
    }

    private bool ResolveQuizManager(bool logIfMissing)
    {
        if (quizManager != null)
        {
            return true;
        }

        quizManager = FindFirstObjectByType<QuizManager>(FindObjectsInactive.Include);
        if (quizManager != null)
        {
            return true;
        }

        if (logIfMissing && !warnedMissingQuizManager)
        {
            warnedMissingQuizManager = true;
            Debug.LogWarning("PandaBossAI: QuizManager not found. Quiz Attack is skipped.");
        }

        return false;
    }

    private void TryPlumAttack()
    {
        if (plumTimer > 0f) return;
        if (globalAttackTimer > 0f) return;

        plumTimer = plumCooldown;
        StartCoroutine(PlumAttackRoutine());
    }

    private IEnumerator PlumAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss prepares Plum Attack!");

        PlayThrowAnimation();

        if (soundController != null)
        {
            soundController.PlayThrowSound();
        }

        yield return WaitForActiveSeconds(plumWindupTime);

        Debug.Log("Panda Boss shoots Plum Blossom!");

        ShootPlumProjectile();

        isAttacking = false;
        StartGlobalCooldown(globalCooldownAfterPlum);
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

        Vector3 targetPosition;

        if (plumTargetPoint != null)
        {
            targetPosition = plumTargetPoint.position;
        }
        else if (Camera.main != null)
        {
            targetPosition = Camera.main.transform.position;
        }
        else
        {
            targetPosition = player.position + Vector3.up * 1.0f;
        }

        Vector3 shootDirection = targetPosition - plumSpawnPoint.position;

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
            plumProjectile.Init(targetPosition);
        }
        else
        {
            Debug.LogWarning("PandaBossAI: PlumProjectile.cs is not attached to the projectile prefab.");
        }
    }

    private void TryMeteorAttack()
    {
        if (meteorTimer > 0f) return;
        if (globalAttackTimer > 0f) return;

        meteorTimer = meteorCooldown;
        StartCoroutine(MeteorAttackRoutine());
    }

    private IEnumerator MeteorAttackRoutine()
    {
        isAttacking = true;

        Debug.Log("Panda Boss summons Meatball Meteors!");

        PlayRandomCastAnimation();

        if (soundController != null)
        {
            soundController.PlayCastSound();
        }

        List<Vector3> targetPositions = GetNonOverlappingMeteorTargetPositions();

        for (int i = 0; i < targetPositions.Count; i++)
        {
            SpawnMeteor(targetPositions[i]);
            yield return WaitForActiveSeconds(meteorSpawnDelay);
        }

        isAttacking = false;
        StartGlobalCooldown(globalCooldownAfterMeteor);
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

        int previousHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        int actualDamage = previousHealth - currentHealth;

        DamageFeedbackUtility.ShowDamage(this, actualDamage);
        Debug.Log("Panda Boss takes " + actualDamage + " damage. Current HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayRandomHitAnimation();

        if (soundController != null)
        {
            soundController.PlayHitSound();
        }
    }

    public void ApplyHitStun(float duration)
    {
        if (isDead || duration <= 0f)
        {
            return;
        }

        if (hitStunRemaining <= 0f && animator != null)
        {
            animatorSpeedBeforeHitStun = animator.speed;
        }

        hitStunRemaining = Mathf.Max(hitStunRemaining, duration);
        HitStunStatusIndicator.ShowOn(transform, duration);
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    private void ClearHitStun()
    {
        hitStunRemaining = 0f;
        HitStunStatusIndicator.ClearOn(transform);
        if (animator != null)
        {
            animator.speed = animatorSpeedBeforeHitStun;
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;

        ClearHitStun();

        StopAllCoroutines();

        if (clawWarning != null)
        {
            clawWarning.Hide();
        }

        HideClawWeaponImmediately();

        if (animator != null)
        {
            animator.SetBool(deadBoolHash, true);
        }

        StartDeadSink();
        StartGoDownElevatorSpawnRoutine();

        if (soundController != null)
        {
            soundController.PlayDeathSound();
        }

        GameAudioController.PlayVictoryMusic();
        Debug.Log("Panda Boss died!");
    }

    private void CheckAnimatorDeadBoolForSink()
    {
        if (!sinkWhenAnimatorDeadBoolIsTrue) return;
        if (hasStartedDeathSink) return;
        if (animator == null) return;

        // 讓你直接在 Animator / Inspector 把 Dead Bool 打開時，也能觸發下沉。
        if (animator.GetBool(deadBoolHash))
        {
            isDead = true;
            isAttacking = false;

            StopAllCoroutines();

            if (clawWarning != null)
            {
                clawWarning.Hide();
            }

            HideClawWeaponImmediately();

            StartDeadSink();
            StartGoDownElevatorSpawnRoutine();

            if (soundController != null)
            {
                soundController.PlayDeathSound();
            }

            GameAudioController.PlayVictoryMusic();
            Debug.Log("Panda Boss dead sink triggered by Animator Dead bool.");
        }
    }

    private void StartDeadSink()
    {
        if (hasStartedDeathSink) return;

        hasStartedDeathSink = true;

        if (deadSinkTarget == null)
        {
            deadSinkTarget = transform;
        }

        if (deadSinkCoroutine != null)
        {
            StopCoroutine(deadSinkCoroutine);
        }

        deadSinkCoroutine = StartCoroutine(DeadSinkRoutine());
    }

    private IEnumerator DeadSinkRoutine()
    {
        Vector3 startPosition = deadSinkTarget.position;
        Vector3 targetPosition = startPosition + Vector3.down * deadSinkDistance;

        float duration = Mathf.Max(deadSinkDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            deadSinkTarget.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        deadSinkTarget.position = targetPosition;
        deadSinkCoroutine = null;
    }

    private void StartGoDownElevatorSpawnRoutine()
    {
        if (hasSpawnedGoDownElevator) return;
        ResolveGoDownElevatorObject();
        if (goDownElevatorObject == null) return;

        StartCoroutine(SpawnGoDownElevatorAfterDeathAnimation());
    }

    private IEnumerator SpawnGoDownElevatorAfterDeathAnimation()
    {
        yield return WaitForDeathAnimationToFinish();
        SpawnGoDownElevator();
    }

    private IEnumerator WaitForDeathAnimationToFinish()
    {
        if (animator == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, goDownElevatorSpawnFallbackDelay));
            yield break;
        }

        float remaining = Mathf.Max(0.01f, goDownElevatorAnimationWaitTimeout);
        bool enteredDeathState = false;
        int deathStateHash = Animator.StringToHash(deathAnimationStateName);

        while (remaining > 0f)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isDeathState = stateInfo.shortNameHash == deathStateHash
                || stateInfo.IsName(deathAnimationStateName);

            if (isDeathState)
            {
                enteredDeathState = true;

                if (stateInfo.normalizedTime >= 1f && !animator.IsInTransition(0))
                {
                    yield break;
                }
            }
            else if (enteredDeathState)
            {
                yield break;
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (!enteredDeathState)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, goDownElevatorSpawnFallbackDelay));
        }
    }

    private void SpawnGoDownElevator()
    {
        if (hasSpawnedGoDownElevator) return;
        ResolveGoDownElevatorObject();
        if (goDownElevatorObject == null) return;

        hasSpawnedGoDownElevator = true;

        goDownElevatorObject.SetActive(true);
    }

    private void ResolveGoDownElevatorObject()
    {
        if (goDownElevatorObject != null)
        {
            return;
        }

        Transform elevator = transform.Find("GoDownEvlelator");
        if (elevator == null)
        {
            elevator = transform.Find("GoDownElevator");
        }

        if (elevator != null)
        {
            goDownElevatorObject = elevator.gameObject;
        }
    }

    private void StartClawWeaponRoutine()
    {
        if (clawWeaponObject == null) return;

        if (clawWeaponCoroutine != null)
        {
            StopCoroutine(clawWeaponCoroutine);
        }

        clawWeaponCoroutine = StartCoroutine(ClawWeaponRoutine());
    }

    private IEnumerator ClawWeaponRoutine()
    {
        if (clawWeaponObject == null) yield break;

        float showDelay = Mathf.Max(clawWeaponShowDelay, 0f);
        float hideDelay = Mathf.Max(clawWeaponHideDelay, showDelay);

        if (showDelay > 0f)
        {
            yield return WaitForActiveSeconds(showDelay);
        }

        clawWeaponObject.SetActive(true);

        float visibleDuration = hideDelay - showDelay;

        if (visibleDuration > 0f)
        {
            yield return WaitForActiveSeconds(visibleDuration);
        }

        clawWeaponObject.SetActive(false);
        clawWeaponCoroutine = null;
    }

    private void HideClawWeaponImmediately()
    {
        if (clawWeaponCoroutine != null)
        {
            StopCoroutine(clawWeaponCoroutine);
            clawWeaponCoroutine = null;
        }

        if (clawWeaponObject != null)
        {
            clawWeaponObject.SetActive(false);
        }
    }

    private void PlayClawAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(castTriggerHash);
        animator.ResetTrigger(throwTriggerHash);

        animator.SetTrigger(clawTriggerHash);
    }

    private void PlayThrowAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger(hitTriggerHash);
        animator.ResetTrigger(castTriggerHash);
        animator.ResetTrigger(clawTriggerHash);

        animator.SetTrigger(throwTriggerHash);
    }

    private void PlayRandomHitAnimation()
    {
        if (animator == null) return;

        int count = Mathf.Max(hitAnimationCount, 1);
        int randomHitIndex = Random.Range(0, count);

        animator.ResetTrigger(clawTriggerHash);
        animator.ResetTrigger(castTriggerHash);
        animator.ResetTrigger(throwTriggerHash);

        animator.SetInteger(hitIndexHash, randomHitIndex);
        animator.SetTrigger(hitTriggerHash);

        Debug.Log("Panda Boss Hit Animation Index: " + randomHitIndex);
    }

    private void PlayRandomCastAnimation()
    {
        if (animator == null) return;

        int count = Mathf.Max(castAnimationCount, 1);
        int randomCastIndex = Random.Range(0, count);

        animator.ResetTrigger(clawTriggerHash);
        animator.ResetTrigger(throwTriggerHash);

        animator.SetInteger(castIndexHash, randomCastIndex);
        animator.SetTrigger(castTriggerHash);

        Debug.Log("Panda Boss Cast Animation Index: " + randomCastIndex);
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
