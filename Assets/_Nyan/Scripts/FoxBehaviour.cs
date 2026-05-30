using UnityEngine;

public class FoxBehaviour : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float hitDistance = 0.35f;
    [SerializeField] private float lifetime = 6f;

    private PandaHealth target;
    private int damage;
    private float destroyAt;

    public void Initialize(PandaHealth targetHealth, int attackDamage)
    {
        target = targetHealth;
        damage = attackDamage;
        destroyAt = Time.time + lifetime;
    }

    private void Start()
    {
        if (destroyAt <= 0f)
        {
            destroyAt = Time.time + lifetime;
        }

        if (target == null)
        {
            target = FindFirstObjectByType<PandaHealth>();
        }
    }

    private void Update()
    {
        if (Time.time >= destroyAt || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.transform.position;
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
}
