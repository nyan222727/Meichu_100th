using UnityEngine;

public class FoxBehaviour : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float hitDistance = 0.35f;
    [SerializeField] private float lifetime = 6f;

    private IDamageable target;
    private Transform targetTransform;
    private int damage;
    private float destroyAt;

    public void Initialize(IDamageable targetDamageable, int attackDamage)
    {
        SetTarget(targetDamageable);
        damage = attackDamage;
        destroyAt = Time.time + lifetime;
    }

    private void Start()
    {
        if (destroyAt <= 0f)
        {
            destroyAt = Time.time + lifetime;
        }

        if (targetTransform == null)
        {
            SetTarget(DamageableFinder.FindFirst());
        }
    }

    private void Update()
    {
        if (Time.time >= destroyAt || targetTransform == null || target == null || target.IsDefeated)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = targetTransform.position;
        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude <= hitDistance)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void SetTarget(IDamageable targetDamageable)
    {
        target = targetDamageable;
        targetTransform = targetDamageable?.DamageTransform;
    }
}
