using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IPlayerDamageReceiver, IPlayerStatus
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private bool isBlocking;

    public event Action<int, int> HealthChanged;

    public Transform PlayerTransform => transform;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthRatio => maxHealth <= 0 ? 0f : Mathf.Clamp01(currentHealth / (float)maxHealth);
    public bool IsDead => currentHealth <= 0;
    public bool IsBlocking => isBlocking;

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
        int previousMaxHealth = maxHealth;
        int previousCurrentHealth = currentHealth;

        maxHealth = Mathf.Max(1, max);
        currentHealth = Mathf.Clamp(current, 0, maxHealth);

        if (previousMaxHealth != maxHealth || previousCurrentHealth != currentHealth)
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(Mathf.Max(0, currentHealth - amount));
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(Mathf.Min(maxHealth, currentHealth + amount));
    }

    public void SetBlocking(bool value)
    {
        isBlocking = value;
    }

    private void SetCurrentHealth(int value)
    {
        int clampedValue = Mathf.Clamp(value, 0, maxHealth);
        if (currentHealth == clampedValue)
        {
            return;
        }

        currentHealth = clampedValue;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
