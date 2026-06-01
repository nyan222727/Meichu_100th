using UnityEngine;

public class PlayerProjectileAttack : MonoBehaviour
{
    [SerializeField] private Rigidbody projectilePrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private bool useLaunchPointOverride;
    [SerializeField] private float launchDistanceFromCamera = 0.65f;
    [SerializeField] private float launchVerticalOffset;
    [SerializeField] private bool logAttacks = true;

    public bool HasProjectilePrefab => projectilePrefab != null;

    public void Configure(
        Rigidbody prefab,
        Transform point,
        bool usePointOverride,
        float distanceFromCamera,
        float verticalOffset,
        bool shouldLogAttacks)
    {
        projectilePrefab = prefab;
        launchPoint = point;
        useLaunchPointOverride = usePointOverride;
        launchDistanceFromCamera = distanceFromCamera;
        launchVerticalOffset = verticalOffset;
        logAttacks = shouldLogAttacks;
    }

    public bool Fire(Camera sourceCamera, Vector2 launchViewportPosition, float impulse, int damage)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerProjectileAttack] Missing projectile prefab.");
            return false;
        }

        if (sourceCamera == null)
        {
            Debug.LogWarning("[PlayerProjectileAttack] Missing camera.");
            return false;
        }

        Vector3 spawnPosition = GetLaunchPosition(sourceCamera, launchViewportPosition);
        Quaternion spawnRotation = Quaternion.LookRotation(sourceCamera.transform.forward, Vector3.up);
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
        projectile.AddForce(sourceCamera.transform.forward * impulse, ForceMode.Impulse);

        if (logAttacks)
        {
            Debug.Log($"[PlayerProjectileAttack] Fired projectile. Impulse={impulse}, Damage={damage}");
        }

        return true;
    }

    private Vector3 GetLaunchPosition(Camera sourceCamera, Vector2 launchViewportPosition)
    {
        if (useLaunchPointOverride && launchPoint != null)
        {
            return launchPoint.position;
        }

        Vector3 viewportPosition = new Vector3(
            launchViewportPosition.x,
            launchViewportPosition.y,
            launchDistanceFromCamera);

        return sourceCamera.ViewportToWorldPoint(viewportPosition)
            + (sourceCamera.transform.up * launchVerticalOffset);
    }
}
