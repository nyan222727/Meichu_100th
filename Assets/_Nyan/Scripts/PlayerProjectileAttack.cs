using UnityEngine;

public class PlayerProjectileAttack : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform launchPoint;
    [SerializeField] private bool useLaunchPointOverride;
    [SerializeField] private float launchDistanceFromCamera = 0.65f;
    [SerializeField] private float launchVerticalOffset;
    [SerializeField] private bool logAttacks = true;

    public bool Fire(
        Camera sourceCamera,
        Vector2 launchViewportPosition,
        float impulse,
        int damage,
        bool appliesHitStun,
        float hitStunDuration)
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
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        Rigidbody projectile = projectileObject.GetComponent<Rigidbody>();
        if (projectile == null)
        {
            Debug.LogWarning($"[PlayerProjectileAttack] Projectile prefab {projectilePrefab.name} has no Rigidbody.");
            Destroy(projectileObject);
            return false;
        }

        projectile.isKinematic = false;
        projectile.useGravity = true;
        projectile.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        projectile.linearVelocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;

        ProjectileDamage projectileDamage = projectileObject.GetComponent<ProjectileDamage>();
        if (projectileDamage == null)
        {
            projectileDamage = projectileObject.AddComponent<ProjectileDamage>();
        }

        projectileDamage.Configure(damage, appliesHitStun, hitStunDuration);
        projectile.AddForce(sourceCamera.transform.forward * impulse, ForceMode.Impulse);

        if (logAttacks)
        {
            Debug.Log(
                $"[PlayerProjectileAttack] Fired projectile. Impulse={impulse:0.00}, " +
                $"Damage={damage}, HitStun={appliesHitStun}");
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
