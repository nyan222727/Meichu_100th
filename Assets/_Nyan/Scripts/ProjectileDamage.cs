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
        PandaHealth pandaHealth = collision.collider.GetComponentInParent<PandaHealth>();
        if (pandaHealth == null)
        {
            return;
        }

        pandaHealth.TakeDamage(damage);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
