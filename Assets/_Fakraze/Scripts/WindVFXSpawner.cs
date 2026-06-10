using UnityEngine;

public class WindVFXController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("AR Camera / Main Camera")]
    public Transform cameraTransform;

    [Tooltip("唯一保留的風特效 Particle System，會畫圈的那個")]
    public ParticleSystem swirlWindPS;

    [Header("Target Point Near Camera")]
    [Tooltip("風要吹過 Camera 前方幾公尺，只看 Camera 的 Y 軸方向")]
    public float targetDistanceInFrontOfCamera = 1f;

    [Tooltip("目標點相對 Camera 的左右偏移，只看 Camera 的 Y 軸方向")]
    public float targetHorizontalOffset = 0f;

    [Tooltip("目標點相對世界座標的上下偏移")]
    public float targetVerticalOffset = 0f;

    [Tooltip("普通風時，Particle System 放在逆風方向距離目標點多遠")]
    public float normalUpwindDistance = 1f;

    [Tooltip("颱風時，Particle System 放在逆風方向距離目標點多遠")]
    public float typhoonUpwindDistance = 3f;

    [Header("Normal Wind Settings")]
    [Tooltip("普通風粒子數量，控制 Emission Rate over Time")]
    public float normalSwirlRate = 8f;

    [Tooltip("普通風 Velocity over Lifetime 的 X 速度倍率")]
    public float normalVelocityXMultiplier = 10f;

    [Header("Typhoon Wind Settings")]
    [Tooltip("颱風粒子數量，控制 Emission Rate over Time")]
    public float typhoonSwirlRate = 20f;

    [Tooltip("颱風 Velocity over Lifetime 的 X 速度倍率")]
    public float typhoonVelocityXMultiplier = 30f;

    [Header("Wind Direction Rotation")]
    [Tooltip("讓風特效根據 WindManager.currentWindForce 旋轉，不跟 Camera 旋轉")]
    public bool rotateByWindDirection = true;

    [Tooltip("如果風特效方向不對，可以調整 Y 軸，例如 90、180、-90")]
    public float windRotationOffsetY = 90f;

    [Tooltip("是否只使用水平風向，建議開啟")]
    public bool useHorizontalWindOnly = true;

    [Header("No Wind Settings")]
    [Tooltip("風力小於這個值時，視為沒有風，Particle System 會停止")]
    public float noWindThreshold = 0.01f;

    [Header("Debug")]
    public bool drawDebugRay = true;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 一開始不要直接播放，避免無風時也顯示
        SetParticleEnabled(swirlWindPS, false, true);
    }

    void LateUpdate()
    {
        if (WindManager.Instance == null || cameraTransform == null || swirlWindPS == null)
            return;

        Vector3 windDirection = GetWindDirection();

        // 沒有風時，直接關掉 Particle System，並且不要更新位置/旋轉
        if (windDirection.sqrMagnitude < noWindThreshold * noWindThreshold)
        {
            SetParticleEnabled(swirlWindPS, false, true);
            return;
        }

        // 有風時才顯示
        SetParticleEnabled(swirlWindPS, true, false);

        UpdateWindVFX();
        UpdateParticlePositionByWindDirection(windDirection);
        RotateParticleByWindDirection(windDirection);

        if (drawDebugRay)
        {
            DebugDrawWind(windDirection);
        }
    }

    void UpdateWindVFX()
    {
        bool isTyphoon = WindManager.Instance.isTyphoon;

        if (isTyphoon)
        {
            SetParticleEmission(swirlWindPS, typhoonSwirlRate);
            SetVelocityOverLifetimeXMultiplier(swirlWindPS, typhoonVelocityXMultiplier);
        }
        else
        {
            SetParticleEmission(swirlWindPS, normalSwirlRate);
            SetVelocityOverLifetimeXMultiplier(swirlWindPS, normalVelocityXMultiplier);
        }
    }

    void UpdateParticlePositionByWindDirection(Vector3 windDirection)
    {
        float currentUpwindDistance = WindManager.Instance.isTyphoon
            ? typhoonUpwindDistance
            : normalUpwindDistance;

        Vector3 targetWorldPosition = GetTargetWorldPosition();

        // Particle 放在逆風方向
        Vector3 particlePosition =
            targetWorldPosition - windDirection * currentUpwindDistance;

        swirlWindPS.transform.position = particlePosition;
    }

    Vector3 GetTargetWorldPosition()
    {
        // 只取 Camera 的 Y 軸旋轉
        // 忽略 Camera 的 X 軸抬頭/低頭，以及 Z 軸歪斜
        float cameraYaw = cameraTransform.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);

        Vector3 yawForward = yawRotation * Vector3.forward;
        Vector3 yawRight = yawRotation * Vector3.right;

        return cameraTransform.position
            + yawForward * targetDistanceInFrontOfCamera
            + yawRight * targetHorizontalOffset
            + Vector3.up * targetVerticalOffset;
    }

    void RotateParticleByWindDirection(Vector3 windDirection)
    {
        if (!rotateByWindDirection || swirlWindPS == null)
            return;

        // 方向只看 WindManager 的世界風向，不看 Camera rotation
        Quaternion windRotation =
            Quaternion.LookRotation(windDirection, Vector3.up)
            * Quaternion.Euler(0f, windRotationOffsetY, 0f);

        swirlWindPS.transform.rotation = windRotation;
    }

    Vector3 GetWindDirection()
    {
        if (WindManager.Instance == null)
            return Vector3.zero;

        Vector3 windForce = WindManager.Instance.currentWindForce;

        if (useHorizontalWindOnly)
        {
            return new Vector3(
                windForce.x,
                0f,
                windForce.z
            );
        }

        return windForce;
    }

    void SetParticleEmission(ParticleSystem ps, float rate)
    {
        if (ps == null)
            return;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;
    }

    void SetVelocityOverLifetimeXMultiplier(ParticleSystem ps, float xMultiplier)
    {
        if (ps == null)
            return;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.xMultiplier = xMultiplier;
    }

    void SetParticleEnabled(ParticleSystem ps, bool enabled, bool clearParticles)
    {
        if (ps == null)
            return;

        if (enabled)
        {
            if (!ps.isPlaying)
            {
                ps.Play(true);
            }
        }
        else
        {
            if (ps.isPlaying)
            {
                ps.Stop(
                    true,
                    clearParticles
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting
                );
            }
        }
    }

    void DebugDrawWind(Vector3 windDirection)
    {
        if (windDirection.sqrMagnitude < noWindThreshold * noWindThreshold)
            return;

        Vector3 normalizedWindDirection = windDirection.normalized;
        Vector3 targetWorldPosition = GetTargetWorldPosition();

        Debug.DrawRay(
            targetWorldPosition,
            normalizedWindDirection * 2f,
            Color.green
        );

        Debug.DrawRay(
            swirlWindPS.transform.position,
            swirlWindPS.transform.forward * 2f,
            Color.red
        );

        Debug.DrawLine(
            swirlWindPS.transform.position,
            targetWorldPosition,
            Color.yellow
        );
    }
}