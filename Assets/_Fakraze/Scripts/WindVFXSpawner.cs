using UnityEngine;

public class WindVFXController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("AR Camera / Main Camera")]
    public Transform cameraTransform;

    [Tooltip("唯一保留的風特效 Particle System，會畫圈的那個")]
    public ParticleSystem swirlWindPS;

    [Header("Target Point Near Camera")]
    [Tooltip("風要吹過 Camera 前方幾公尺")]
    public float targetDistanceInFrontOfCamera = 1f;

    [Tooltip("目標點相對 Camera 的左右偏移")]
    public float targetHorizontalOffset = 0f;

    [Tooltip("目標點相對 Camera 的上下偏移")]
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

    [Header("Debug")]
    public bool drawDebugRay = true;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        PlayParticleSystem(swirlWindPS);
    }

    void LateUpdate()
    {
        if (WindManager.Instance == null || cameraTransform == null || swirlWindPS == null)
            return;

        UpdateWindVFX();
        UpdateParticlePositionByWindDirection();
        RotateParticleByWindDirection();

        if (drawDebugRay)
        {
            DebugDrawWind();
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

        SetParticleEnabled(swirlWindPS, true);
    }

    void UpdateParticlePositionByWindDirection()
    {
        Vector3 windDirection = GetWindDirection();

        if (windDirection.sqrMagnitude < 0.01f)
            return;

        float currentUpwindDistance = WindManager.Instance.isTyphoon
            ? typhoonUpwindDistance
            : normalUpwindDistance;

        Vector3 targetWorldPosition = GetTargetWorldPosition();

        // Particle 放在逆風方向，但目標點是 Camera 當前面向前方 1 公尺
        Vector3 particlePosition =
            targetWorldPosition - windDirection * currentUpwindDistance;

        swirlWindPS.transform.position = particlePosition;
    }

    Vector3 GetTargetWorldPosition()
    {
        return cameraTransform.position
            + cameraTransform.forward * targetDistanceInFrontOfCamera
            + cameraTransform.right * targetHorizontalOffset
            + cameraTransform.up * targetVerticalOffset;
    }

    void RotateParticleByWindDirection()
    {
        if (!rotateByWindDirection || swirlWindPS == null)
            return;

        Vector3 windDirection = GetWindDirection();

        if (windDirection.sqrMagnitude < 0.01f)
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
            ).normalized;
        }

        return windForce.normalized;
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

    void SetParticleEnabled(ParticleSystem ps, bool enabled)
    {
        if (ps == null)
            return;

        if (enabled)
        {
            if (!ps.isPlaying)
                ps.Play(true);
        }
        else
        {
            if (ps.isPlaying)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void PlayParticleSystem(ParticleSystem ps)
    {
        if (ps == null)
            return;

        ps.Play(true);
    }

    void DebugDrawWind()
    {
        Vector3 windDirection = GetWindDirection();

        if (windDirection.sqrMagnitude < 0.01f)
            return;

        Vector3 targetWorldPosition = GetTargetWorldPosition();

        // 綠色：WindManager 的世界風向
        Debug.DrawRay(
            targetWorldPosition,
            windDirection * 2f,
            Color.green
        );

        // 紅色：Particle System 目前面向方向
        Debug.DrawRay(
            swirlWindPS.transform.position,
            swirlWindPS.transform.forward * 2f,
            Color.red
        );

        // 黃色：Particle 位置到 Camera 前方目標點
        Debug.DrawLine(
            swirlWindPS.transform.position,
            targetWorldPosition,
            Color.yellow
        );
    }
}