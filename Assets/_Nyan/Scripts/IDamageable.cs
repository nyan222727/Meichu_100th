using UnityEngine;

public interface IDamageable
{
    Transform DamageTransform { get; }
    bool IsDefeated { get; }
    void TakeDamage(int amount);
}
