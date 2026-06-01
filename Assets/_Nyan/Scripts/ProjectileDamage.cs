using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnHit = true;

    public int Damage => damage;

    public void SetDamage(int value)
    {
        damage = Mathf.Max(0, value);
    }

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = DamageableFinder.GetInParent(collision.collider);
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
