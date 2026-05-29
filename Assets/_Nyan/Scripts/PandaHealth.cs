using UnityEngine;

public class PandaHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

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

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"[PandaHealth] {name} took {amount} damage. HP={currentHealth}/{maxHealth}");

        if (IsDefeated)
        {
            Debug.Log($"[PandaHealth] {name} defeated.");
        }
    }
}
