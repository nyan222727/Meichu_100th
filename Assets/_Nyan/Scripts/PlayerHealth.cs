using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthRatio => maxHealth <= 0 ? 0f : Mathf.Clamp01(currentHealth / (float)maxHealth);

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        maxHealth = Mathf.Max(1, max);
        currentHealth = Mathf.Clamp(current, 0, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
