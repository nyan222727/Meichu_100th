using UnityEngine;

public class PandaHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public Transform DamageTransform => transform;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDefeated => currentHealth <= 0;

    private void Awake()
    {
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDefeated)
        {
            return;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        int actualDamage = previousHealth - currentHealth;
        DamageFeedbackUtility.ShowDamage(this, actualDamage);
        Debug.Log($"[PandaHealth] {name} took {actualDamage} damage. HP={currentHealth}/{maxHealth}");

        if (IsDefeated)
        {
            Debug.Log($"[PandaHealth] {name} defeated.");
        }
    }
}
