using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private float meleeRange = 1.2f;
    [SerializeField] private int weakDamage = 15;
    [SerializeField] private int mediumDamage = 25;
    [SerializeField] private int strongDamage = 40;
    [SerializeField] private LayerMask meleeHitMask = ~0;

    [Header("Visual")]
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private float slashDistanceFromCamera = 0.55f;
    [SerializeField] private float slashLifetime = 0.22f;
    [SerializeField] private Vector3 slashScale = Vector3.one;
    [SerializeField] private float slashAngle = -25f;
    [SerializeField] private bool logAttacks = true;

    private void OnValidate()
    {
        meleeRange = Mathf.Max(0.01f, meleeRange);
        weakDamage = Mathf.Max(0, weakDamage);
        mediumDamage = Mathf.Max(0, mediumDamage);
        strongDamage = Mathf.Max(0, strongDamage);
        slashDistanceFromCamera = Mathf.Max(0.01f, slashDistanceFromCamera);
        slashLifetime = Mathf.Max(0.01f, slashLifetime);
    }

    public bool Attack(Camera sourceCamera, Vector2 slashViewportPosition, AttackStrength strength, float multiplier)
    {
        if (sourceCamera == null)
        {
            Debug.LogWarning("[PlayerMeleeAttack] Missing camera.");
            return false;
        }

        PlaySlashVisual(sourceCamera, slashViewportPosition);

        int damage = Mathf.RoundToInt(GetDamage(strength) * multiplier);
        Transform cameraTransform = sourceCamera.transform;
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
                Debug.Log("[PlayerMeleeAttack] Melee attack missed.");
            }

            return false;
        }

        IDamageable damageable = DamageableFinder.GetInParent(hit.collider);
        if (damageable == null)
        {
            if (logAttacks)
            {
                Debug.Log($"[PlayerMeleeAttack] Melee hit {hit.collider.name}, but it has no IDamageable.");
            }

            return false;
        }

        damageable.TakeDamage(damage);

        if (logAttacks)
        {
            string targetName = damageable.DamageTransform != null
                ? damageable.DamageTransform.name
                : hit.collider.name;
            Debug.Log($"[PlayerMeleeAttack] Melee hit {targetName}. Strength={strength}, Damage={damage}");
        }

        return true;
    }

    private void PlaySlashVisual(Camera sourceCamera, Vector2 viewportPosition)
    {
        if (slashPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = sourceCamera.ViewportToWorldPoint(new Vector3(
            viewportPosition.x,
            viewportPosition.y,
            slashDistanceFromCamera));
        Quaternion spawnRotation = Quaternion.LookRotation(sourceCamera.transform.forward, sourceCamera.transform.up)
            * Quaternion.Euler(0f, 0f, slashAngle);

        GameObject slash = Instantiate(slashPrefab, spawnPosition, spawnRotation);
        slash.transform.localScale = Vector3.Scale(slash.transform.localScale, slashScale);
        Destroy(slash, slashLifetime);
    }

    private int GetDamage(AttackStrength strength)
    {
        switch (strength)
        {
            case AttackStrength.Weak:
                return weakDamage;
            case AttackStrength.Medium:
                return mediumDamage;
            case AttackStrength.Strong:
                return strongDamage;
            default:
                return 0;
        }
    }
}
