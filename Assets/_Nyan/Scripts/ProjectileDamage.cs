using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyOnHit = true;

    private bool appliesHitStun;
    private float hitStunDuration;

    public int Damage => damage;

    public void SetDamage(int value)
    {
        damage = Mathf.Max(0, value);
    }

    public void Configure(int value, bool shouldApplyHitStun, float stunDuration)
    {
        SetDamage(value);
        appliesHitStun = shouldApplyHitStun;
        hitStunDuration = Mathf.Max(0f, stunDuration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        IDamageable damageable = DamageableFinder.GetInParent(collision.collider);
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(damage);

        if (appliesHitStun && !damageable.IsDefeated && damageable is IHitStunnable hitStunnable)
        {
            hitStunnable.ApplyHitStun(hitStunDuration);
            HitStunStatusIndicator.ShowOn(damageable.DamageTransform, hitStunDuration);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
