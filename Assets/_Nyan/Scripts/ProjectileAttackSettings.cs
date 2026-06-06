using UnityEngine;

[CreateAssetMenu(menuName = "Meichu/Combat/Projectile Attack Settings")]
public class ProjectileAttackSettings : ScriptableObject
{
    [SerializeField] private float weakImpulse = 3.5f;
    [SerializeField] private int weakDamage = 10;
    [SerializeField] private float mediumImpulse = 6f;
    [SerializeField] private int mediumDamage = 20;
    [SerializeField] private float strongImpulse = 9f;
    [SerializeField] private int strongDamage = 30;

    public ProjectileAttackStats EvaluateStats(float displacementRatio)
    {
        float ratio = Mathf.Clamp01(displacementRatio);
        if (ratio <= 0.5f)
        {
            float lowerRatio = ratio * 2f;
            return new ProjectileAttackStats(
                Mathf.Lerp(weakImpulse, mediumImpulse, lowerRatio),
                Mathf.RoundToInt(Mathf.Lerp(weakDamage, mediumDamage, lowerRatio)));
        }

        float upperRatio = (ratio - 0.5f) * 2f;
        return new ProjectileAttackStats(
            Mathf.Lerp(mediumImpulse, strongImpulse, upperRatio),
            Mathf.RoundToInt(Mathf.Lerp(mediumDamage, strongDamage, upperRatio)));
    }

    public bool TryGetStats(AttackStrength strength, out ProjectileAttackStats stats)
    {
        switch (strength)
        {
            case AttackStrength.Weak:
                stats = new ProjectileAttackStats(weakImpulse, weakDamage);
                return true;
            case AttackStrength.Medium:
                stats = new ProjectileAttackStats(mediumImpulse, mediumDamage);
                return true;
            case AttackStrength.Strong:
                stats = new ProjectileAttackStats(strongImpulse, strongDamage);
                return true;
            default:
                stats = default;
                return false;
        }
    }

    private void OnValidate()
    {
        weakImpulse = Mathf.Max(0f, weakImpulse);
        mediumImpulse = Mathf.Max(0f, mediumImpulse);
        strongImpulse = Mathf.Max(0f, strongImpulse);
        weakDamage = Mathf.Max(0, weakDamage);
        mediumDamage = Mathf.Max(0, mediumDamage);
        strongDamage = Mathf.Max(0, strongDamage);
    }
}

public enum AttackStrength
{
    Weak,
    Medium,
    Strong
}

public readonly struct ProjectileAttackStats
{
    public ProjectileAttackStats(float impulse, int damage)
    {
        Impulse = impulse;
        Damage = damage;
    }

    public float Impulse { get; }
    public int Damage { get; }
}
